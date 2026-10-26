// The lab runtime runs a modded copy of the game off screen, drives it through the game's own functions and
// records every run under the build's artifacts. .agent/skills/runtime-checks/SKILL.md gives the procedure.
import { execFileSync, spawn } from "node:child_process";
import { createRequire } from "node:module";
import { createHash } from "node:crypto";
import {
  appendFile,
  copyFile,
  mkdir,
  readFile,
  readdir,
  rename,
  rm,
  stat,
  writeFile,
} from "node:fs/promises";
import { basename, dirname, join, relative, resolve, sep } from "node:path";

const root = resolve(import.meta.dirname, "../..");
const commandDirectory = join(import.meta.dirname, "commands");
const appId = 1159290;
const executableName = "TheBloodline-Win64-Shipping.exe";
const cxstart =
  "/Applications/CrossOver.app/Contents/SharedSupport/CrossOver/bin/cxstart";

// UE4SS is pinned by the SHA-256 of its release archive and of the loader it installs. The experimental-latest
// release replaces its files, so prepare keeps the archive in the artifacts cache once downloaded.
const ue4ss = {
  file: "UE4SS_v3.0.1-1183-gcdbc0b66.zip",
  url: "https://github.com/UE4SS-RE/RE-UE4SS/releases/download/experimental-latest/UE4SS_v3.0.1-1183-gcdbc0b66.zip",
  sha256: "e4a47e23e2eeb8379c4fc388d1b1686f01c6c5930d84a2ee4a6a9574b274e681",
  loaderSha256:
    "c273e7228687b647226fd63d7ca65888d313ece6be18996ec1320d00a5efe1f8",
};

// Every launch keeps saves and logs inside the lab copy and stays silent and free of dialogs.
const commonArguments = [
  "-NotInstalled",
  "-nosound",
  "-NOSPLASH",
  "-unattended",
];
const modes = {
  // No renderer and no window: Blueprints and widgets run, which suffices for checks that read values.
  headless: ["-nullrhi"],
  // Draws into an off-screen buffer without showing a window, for screenshots and model captures.
  render: ["-RenderOffScreen", "-windowed", "-ResX=1600", "-ResY=900"],
};

// --- Configuration -----------------------------------------------------------------------------------------

function readJson(text) {
  return JSON.parse(text.replace(/^\s*\/\/.*$/gm, ""));
}

function inside(path, parent) {
  return path === parent || path.startsWith(parent + sep);
}

async function configuration() {
  const file = join(root, "bloodline.local.json");
  const text = await readFile(file, "utf8").catch(() => null);
  if (text === null)
    throw new Error(
      "Missing bloodline.local.json. Copy bloodline.example.json and set steamLibrary.",
    );
  const local = readJson(text);
  const steamLibrary = resolve(root, local.steamLibrary ?? "");
  const manifestFile = join(
    steamLibrary,
    "steamapps",
    `appmanifest_${appId}.acf`,
  );
  const manifest = await readFile(manifestFile, "utf8").catch(() => {
    throw new Error(
      `Setting steamLibrary names ${steamLibrary}, which has no steamapps/appmanifest_${appId}.acf.`,
    );
  });
  const value = (key) =>
    new RegExp(`"${key}"\\s+"([^"]*)"`).exec(manifest)?.[1];
  const parts = steamLibrary.split(sep);
  const drive = parts.lastIndexOf("drive_c");
  if (drive < 1)
    throw new Error(
      "Setting steamLibrary must lie on the C: drive of a CrossOver bottle, because the lab runs the game through CrossOver.",
    );
  const driveC = parts.slice(0, drive + 1).join(sep);
  const labDirectory = resolve(
    root,
    local.labDirectory ?? join(driveC, "BloodlineLab"),
  );
  // Runs never touch the Steam installation or a player's saves, so lab copies live apart from both.
  if (inside(labDirectory, steamLibrary) || inside(steamLibrary, labDirectory))
    throw new Error(
      `Setting labDirectory (${labDirectory}) overlaps the Steam library ${steamLibrary}. Point it at a separate folder on the bottle's C: drive.`,
    );
  if (!inside(labDirectory, driveC) || labDirectory === driveC)
    throw new Error(
      `Setting labDirectory (${labDirectory}) must be a folder on ${driveC}, the C: drive of the bottle that holds the Steam library.`,
    );
  const buildId = value("buildid");
  const output = resolve(root, local.outputDirectory ?? "artifacts");
  return {
    steamLibrary,
    manifestFile,
    buildId,
    installDirectory: value("installdir"),
    fullyInstalled: value("StateFlags") === "4",
    bottle: basename(dirname(driveC)),
    labDirectory,
    copy: join(labDirectory, buildId),
    runtime: join(output, buildId, "runtime"),
    cache: join(output, "cache"),
    windowsPath: (path) =>
      "C:\\" + relative(driveC, path).split(sep).join("\\"),
  };
}

async function exists(path) {
  return (await stat(path).catch(() => null)) !== null;
}

async function sha256(path) {
  return createHash("sha256")
    .update(await readFile(path))
    .digest("hex");
}

/** The prepared lab copy for the installed Steam build, refusing one whose loader differs from the pin. */
async function labCopy(config) {
  const marker = await readFile(join(config.copy, "lab.json"), "utf8").catch(
    () => null,
  );
  if (!marker)
    throw new Error(
      `No lab copy for build ${config.buildId} in ${config.labDirectory}. Run: bun tools/runtime/lab.mjs prepare`,
    );
  const lab = JSON.parse(marker);
  const executable = join(config.copy, lab.executable);
  if (!inside(executable, config.copy))
    throw new Error(
      `${join(config.copy, "lab.json")} names an executable outside the lab copy.`,
    );
  const binaries = dirname(executable);
  const loader = await sha256(join(binaries, "ue4ss/UE4SS.dll"));
  if (loader !== ue4ss.loaderSha256)
    throw new Error(
      `The lab copy's UE4SS.dll has SHA-256 ${loader}, not the pinned ${ue4ss.loaderSha256}. Run prepare again.`,
    );
  return {
    ...lab,
    executable,
    binaries,
    project: resolve(binaries, "../.."),
    bridge: join(config.copy, "bridge"),
    captures: join(config.copy, "captures"),
  };
}

// --- Preparing a lab copy ------------------------------------------------------------------------------------

async function findExecutable(directory, depth = 0) {
  const found = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory() && depth < 6)
      found.push(...(await findExecutable(path, depth + 1)));
    else if (
      entry.name === executableName &&
      path.endsWith(join("Binaries", "Win64", executableName))
    )
      found.push(path);
  }
  return found;
}

async function ue4ssArchive(config) {
  const archive = join(config.cache, ue4ss.file);
  if (!(await exists(archive))) {
    const response = await fetch(ue4ss.url);
    if (!response.ok)
      throw new Error(
        `Downloading ${ue4ss.url} failed with status ${response.status}. Place ${ue4ss.file} in ${config.cache} or pin a newer release.`,
      );
    await mkdir(config.cache, { recursive: true });
    await writeFile(
      archive + ".partial",
      Buffer.from(await response.arrayBuffer()),
    );
    await rename(archive + ".partial", archive);
  }
  const actual = await sha256(archive);
  if (actual !== ue4ss.sha256)
    throw new Error(
      `${archive} has SHA-256 ${actual}, not the pinned ${ue4ss.sha256}.`,
    );
  return archive;
}

async function prepare(config) {
  if (!config.fullyInstalled)
    throw new Error(
      `Steam reports build ${config.buildId} as not fully installed. Let Steam finish the update first.`,
    );
  const game = join(config.copy, "game");
  if (!(await exists(game))) {
    await mkdir(config.copy, { recursive: true });
    // APFS clones share the installation's blocks, so a lab copy takes no space until the game writes to it.
    execFileSync("/bin/cp", [
      "-cR",
      join(config.steamLibrary, "steamapps/common", config.installDirectory),
      game,
    ]);
    await copyFile(
      config.manifestFile,
      join(config.copy, "steam-manifest.acf"),
    );
  }
  const executables = await findExecutable(game);
  if (executables.length !== 1)
    throw new Error(
      `Expected one ${executableName} in ${game}, found ${executables.length}.`,
    );
  const binaries = dirname(executables[0]);
  // The game restarts itself through Steam unless it finds its app ID beside it.
  await writeFile(join(binaries, "steam_appid.txt"), String(appId));
  execFileSync("unzip", [
    "-o",
    "-q",
    await ue4ssArchive(config),
    "-d",
    binaries,
  ]);
  if ((await sha256(join(binaries, "ue4ss/UE4SS.dll"))) !== ue4ss.loaderSha256)
    throw new Error(`${ue4ss.file} installed an unexpected UE4SS.dll.`);
  // UE4SS needs the engine version set for UE 4.27 games.
  const settingsFile = join(binaries, "ue4ss/UE4SS-settings.ini");
  const settings = (await readFile(settingsFile, "utf8"))
    .replace(/^MajorVersion =.*$/m, "MajorVersion = 4")
    .replace(/^MinorVersion =.*$/m, "MinorVersion = 27");
  await writeFile(settingsFile, settings);
  // Only the bridge runs, so the lab game differs from the shipped game as little as possible.
  const mods = join(binaries, "ue4ss/Mods");
  const list = (await readFile(join(mods, "mods.txt"), "utf8"))
    .replace(/^(\w+)[ \t]*:[ \t]*\d[ \t]*$/gm, "$1 : 0")
    .replace(
      /^; Built-in keybinds/m,
      "BloodlineBridge : 1\n\n; Built-in keybinds",
    );
  await writeFile(join(mods, "mods.txt"), list);
  const enabled = JSON.parse(await readFile(join(mods, "mods.json"), "utf8"))
    .filter((mod) => mod.mod_name !== "BloodlineBridge")
    .map((mod) => ({ ...mod, mod_enabled: false }));
  enabled.push({ mod_name: "BloodlineBridge", mod_enabled: true });
  await writeFile(
    join(mods, "mods.json"),
    JSON.stringify(enabled, null, 4) + "\n",
  );
  const bridge = join(config.copy, "bridge");
  for (const directory of [
    join(bridge, "inbox"),
    join(bridge, "outbox"),
    join(config.copy, "captures"),
  ])
    await mkdir(directory, { recursive: true });
  const script = join(mods, "BloodlineBridge/Scripts/main.lua");
  await mkdir(dirname(script), { recursive: true });
  const source = await readFile(
    join(import.meta.dirname, "bridge/main.lua"),
    "utf8",
  );
  if (!source.includes("@BRIDGE@"))
    throw new Error("bridge/main.lua lacks its @BRIDGE@ marker.");
  const bridgePath =
    config.windowsPath(bridge).replaceAll("\\", "\\\\") + "\\\\";
  await writeFile(
    script,
    source.replace("@BRIDGE@", () => bridgePath),
  );
  const lab = {
    buildId: config.buildId,
    executable: relative(config.copy, executables[0]),
    prepared: new Date().toISOString(),
    ue4ss: ue4ss.file,
    bridgeSha256: await sha256(script),
  };
  await writeFile(
    join(config.copy, "lab.json"),
    JSON.stringify(lab, null, 2) + "\n",
  );
  return { copy: config.copy, ...lab };
}

// --- Sessions and runs -----------------------------------------------------------------------------------------

const sessionFile = (config) => join(config.runtime, "session.json");

async function readSession(config) {
  return JSON.parse(
    await readFile(sessionFile(config), "utf8").catch(() => "null"),
  );
}

async function writeSession(config, session) {
  await mkdir(config.runtime, { recursive: true });
  await writeFile(sessionFile(config), JSON.stringify(session, null, 2) + "\n");
}

async function requireSession(config) {
  const session = await readSession(config);
  if (!session || !gameProcesses(config).includes(session.pid))
    throw new Error(
      "The lab game is not running. Start it with: bun tools/runtime/lab.mjs launch",
    );
  return session;
}

/** A hash over the bridge and every command, so a run records which script versions produced it. */
async function scriptsVersion() {
  const hash = createHash("sha256");
  hash.update(await readFile(join(import.meta.dirname, "bridge/main.lua")));
  for (const file of (await readdir(commandDirectory)).sort())
    hash.update(file).update(await readFile(join(commandDirectory, file)));
  return hash.digest("hex");
}

async function openRun(config, label, details) {
  const id =
    new Date().toISOString().replace(/[:.]/g, "-") +
    "-" +
    label.replace(/[^\w-]+/g, "-");
  const run = join(config.runtime, id);
  // A run directory is never reused, so repeated measurements stay side by side.
  await mkdir(config.runtime, { recursive: true });
  await mkdir(run);
  const git = (...args) =>
    execFileSync("git", args, { cwd: root, encoding: "utf8" }).trim();
  await writeFile(
    join(run, "run.json"),
    JSON.stringify(
      {
        label,
        started: new Date().toISOString(),
        buildId: config.buildId,
        commit: git("rev-parse", "HEAD"),
        uncommittedRuntimeChanges: git("status", "--porcelain", "tools/runtime")
          .split("\n")
          .filter(Boolean),
        scripts: await scriptsVersion(),
        ue4ss: ue4ss.file,
        ...details,
      },
      null,
      2,
    ) + "\n",
  );
  return id;
}

async function record(config, session, entry) {
  await appendFile(
    join(config.runtime, session.run, "run.jsonl"),
    JSON.stringify({ at: new Date().toISOString(), ...entry }) + "\n",
  );
}

// --- The game process ------------------------------------------------------------------------------------

/**
 * Process IDs of games started from this build's lab copy. Wine shows the game's Windows command line, which
 * starts with the lab copy's path, so the launcher, other games and the Steam installation never match.
 */
function gameProcesses(config) {
  const prefix = config.windowsPath(config.copy).toLowerCase() + "\\";
  return execFileSync("ps", ["-axo", "pid=,command="], { encoding: "utf8" })
    .split("\n")
    .map((line) => /^\s*(\d+)\s+(.*)$/.exec(line))
    .filter(
      (match) =>
        match &&
        match[2].toLowerCase().startsWith(prefix) &&
        match[2].split(" -", 1)[0].endsWith(executableName),
    )
    .map((match) => Number(match[1]));
}

async function waitFor(check, seconds, what) {
  const deadline = Date.now() + seconds * 1000;
  while (Date.now() < deadline) {
    const value = await check();
    if (value) return value;
    await new Promise((done) => setTimeout(done, 250));
  }
  throw new Error(`Timed out after ${seconds} s waiting for ${what}.`);
}

async function launch(config, lab, mode, label) {
  if (!modes[mode])
    throw new Error(
      `Unknown mode ${mode}. Use ${Object.keys(modes).join(" or ")}.`,
    );
  const running = gameProcesses(config);
  if (running.length)
    throw new Error(
      `The lab game already runs as process ${running.join(", ")}. Quit it first.`,
    );
  const ready = join(lab.bridge, "outbox/ready.json");
  for (const directory of ["inbox", "outbox"])
    for (const stale of await readdir(join(lab.bridge, directory)))
      await rm(join(lab.bridge, directory, stale));
  const gameArguments = [...commonArguments, ...modes[mode]];
  spawn(
    cxstart,
    [
      "--bottle",
      config.bottle,
      "--dll",
      "dwmapi=n,b",
      "--no-wait",
      "--",
      config.windowsPath(lab.executable),
      ...gameArguments,
    ],
    { stdio: "ignore", detached: true },
  ).unref();
  const pid = await waitFor(
    () => gameProcesses(config)[0],
    120,
    "the game process",
  );
  // A lower priority keeps the machine responsive while the lab game runs.
  execFileSync("renice", ["-n", "10", "-p", String(pid)], { stdio: "ignore" });
  const run = await openRun(config, label ?? mode, {
    mode,
    arguments: gameArguments,
    pid,
  });
  const session = {
    pid,
    mode,
    started: new Date().toISOString(),
    run,
    next: null,
    bridgeSession: null,
  };
  await writeSession(config, session);
  await waitFor(() => exists(ready), 180, "the bridge to load");
  await record(config, session, {
    command: "launch",
    mode,
    arguments: gameArguments,
    pid,
  });
  // The lab game needs no more frames than a check samples, and fewer leave the machine to its user. The game
  // resets the limit on every level load, so the command keeps holding it.
  await game(config, lab, "limit-frame-rate", { fps: 60 });
  return { pid, mode, run: relative(root, join(config.runtime, run)) };
}

async function quit(config, lab) {
  const session = await readSession(config);
  if (!session) return { stopped: null };
  if (gameProcesses(config).includes(session.pid)) {
    await game(config, lab, "quit-game", {}).catch(() => null);
    const exited = await waitFor(
      () => !gameProcesses(config).includes(session.pid),
      30,
      "the game to quit",
    ).catch(() => false);
    // Only the process this tool started is ever stopped.
    if (!exited) process.kill(session.pid);
    await record(config, session, { command: "quit", forced: !exited });
  }
  await rm(sessionFile(config), { force: true });
  return {
    stopped: session.pid,
    run: relative(root, join(config.runtime, session.run)),
  };
}

// --- Requests ------------------------------------------------------------------------------------------------

function lua(value) {
  if (value === null || value === undefined) return "nil";
  if (typeof value === "string") return JSON.stringify(value);
  if (typeof value === "number" || typeof value === "boolean")
    return String(value);
  if (Array.isArray(value)) return "{" + value.map(lua).join(", ") + "}";
  return (
    "{" +
    Object.entries(value)
      .map(([key, entry]) => `[${JSON.stringify(key)}] = ${lua(entry)}`)
      .join(", ") +
    "}"
  );
}

async function commandFiles() {
  return (await readdir(commandDirectory))
    .filter((file) => file.endsWith(".lua") && !file.startsWith("_"))
    .sort();
}

/** The prelude, every command as a function in `commands`, then the body that runs this request. */
async function bundle(args, body) {
  let source = `local args = ${lua(args)}\n${await readFile(join(commandDirectory, "_prelude.lua"), "utf8")}\nlocal commands = {}\n`;
  for (const file of await commandFiles())
    source += `commands[${JSON.stringify(file.slice(0, -4))}] = function(args)\n${await readFile(join(commandDirectory, file), "utf8")}\nend\n`;
  return source + body;
}

async function send(config, lab, code, seconds) {
  const session = await requireSession(config);
  const ready = JSON.parse(
    await readFile(join(lab.bridge, "outbox/ready.json"), "utf8"),
  );
  if (session.bridgeSession !== ready.session)
    Object.assign(session, { bridgeSession: ready.session, next: ready.next });
  const id = session.next;
  session.next = id + 1;
  await writeSession(config, session);
  const partial = join(lab.bridge, "inbox", `${id}.lua.partial`);
  await writeFile(partial, code);
  await rename(partial, join(lab.bridge, "inbox", `${id}.lua`));
  const reply = join(lab.bridge, "outbox", `${id}.json`);
  return waitFor(
    async () => {
      const text = await readFile(reply, "utf8").catch(() => null);
      if (!text) return null;
      try {
        const data = JSON.parse(text);
        return data.session === ready.session ? data : null;
      } catch {
        return null;
      }
    },
    seconds,
    `reply ${id} from the game`,
  );
}

/** The `-- lab setup:` line of a command, naming what it does that a player could not. */
async function labSetup(name) {
  const source = await readFile(
    join(commandDirectory, `${name}.lua`),
    "utf8",
  ).catch(() => "");
  return /^-- lab setup: (.+)$/m.exec(source)?.[1];
}

async function game(config, lab, name, args, code) {
  if (name !== "eval" && !(await commandFiles()).includes(`${name}.lua`))
    throw new Error(
      `Unknown command ${name}. Run lab.mjs without arguments for the list.`,
    );
  const session = await requireSession(config);
  const body = code ?? `return commands[${JSON.stringify(name)}](args)\n`;
  const started = performance.now();
  const reply = await send(
    config,
    lab,
    await bundle(args, body),
    60 + (args.seconds ?? 0),
  );
  await record(config, session, {
    command: name,
    args,
    ...(code ? { code, labSetup: "runs code that the run records" } : {}),
    ...((await labSetup(name)) ? { labSetup: await labSetup(name) } : {}),
    scripts: await scriptsVersion(),
    ok: reply.ok,
    result: reply.result,
    out: reply.out,
    seconds: Math.round(performance.now() - started) / 1000,
  });
  if (!reply.ok) throw new Error(`${name} failed in the game: ${reply.result}`);
  return reply.result;
}

// --- Composite commands ------------------------------------------------------------------------------------

async function waitForGame(config, lab, check, seconds, what) {
  return waitFor(
    async () => {
      const state = await game(config, lab, "probe", {
        classes: check.classes ?? [],
      });
      return check.until(state) ? state : null;
    },
    seconds,
    what,
  );
}

async function newSave(config, lab, name) {
  if (!name) throw new Error("Name the save: lab.mjs new-save <name>");
  await waitForGame(
    config,
    lab,
    { classes: ["WBP_NewGame_C"], until: (s) => s.present.WBP_NewGame_C },
    120,
    "the main menu",
  );
  await game(config, lab, "start-new-game", { name });
  await waitForGame(
    config,
    lab,
    {
      classes: ["CharacterCreation1_C"],
      until: (s) => s.present.CharacterCreation1_C,
    },
    180,
    "character creation",
  );
  await game(config, lab, "create-character", {});
  return waitForGame(
    config,
    lab,
    {
      classes: ["CharacterCreation1_C"],
      until: (s) =>
        !s.present.CharacterCreation1_C && s.pawn === "FirstPersonCharacter2_C",
    },
    180,
    "the first level",
  );
}

/**
 * Moves a file the game wrote into the current run and records it. A transform rewrites the file on the way,
 * and the game's original is removed afterwards.
 */
async function keep(config, session, file, name, transform) {
  const target = join(
    config.runtime,
    session.run,
    name + file.slice(file.lastIndexOf(".")),
  );
  if (await exists(target))
    throw new Error(
      `${relative(root, target)} exists. Captures in a run are never overwritten.`,
    );
  if (transform) {
    await transform(file, target);
    await rm(file);
  } else await rename(file, target);
  await record(config, session, {
    command: "keep",
    file: relative(root, target),
    sha256: await sha256(target),
  });
  return relative(root, target);
}

async function newFile(directory, before, extension, what) {
  const file = await waitFor(
    async () =>
      (await listFiles(directory)).find(
        (path) => !before.has(path) && path.endsWith(extension),
      ),
    30,
    what,
  );
  // The engine writes the file at the end of a frame, so wait until its size settles.
  let size = -1;
  await waitFor(
    async () => {
      const current = (await stat(file)).size;
      const settled = current > 0 && current === size;
      size = current;
      return settled;
    },
    30,
    `${what} to finish`,
  );
  return file;
}

async function listFiles(directory) {
  const files = [];
  for (const entry of await readdir(directory, { withFileTypes: true }).catch(
    () => [],
  )) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) files.push(...(await listFiles(path)));
    else files.push(path);
  }
  return files;
}

/**
 * Off screen, the engine writes images with alpha 0 wherever the scene is, while the colours are right, so kept
 * screenshots and renders drop the alpha channel.
 */
function opaque(from, to) {
  const sharp = createRequire(join(root, "site/package.json"))("sharp");
  return sharp(from).removeAlpha().png().toFile(to);
}

async function captureScreen(config, lab, name, ui) {
  const session = await requireSession(config);
  if (session.mode !== "render")
    throw new Error(
      "Screenshots need a game launched with: lab.mjs launch render",
    );
  const directory = join(lab.project, "Saved/Screenshots");
  const before = new Set(await listFiles(directory));
  await game(config, lab, "capture-screen", { ui });
  const file = await newFile(directory, before, ".png", "the screenshot");
  return { file: await keep(config, session, file, name, opaque) };
}

/**
 * Combines a model capture's colour pass and depth pass into a transparent picture. Both passes are rendered at
 * twice the output size. Each output pixel takes the share of its four samples that the depth marks as the
 * subject as its opacity, and the mean colour of those samples, so edges are smooth without background colour
 * bleeding in. The picture is cropped to the subject with a margin of 5% of its longer side on every side.
 */
async function composeModel(colorFile, maskFile, target) {
  const sharp = createRequire(join(root, "site/package.json"))("sharp");
  const color = await sharp(colorFile)
    .removeAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const mask = await sharp(maskFile)
    .removeAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const { width, height } = color.info;
  const out = { width: width / 2, height: height / 2 };
  const pixels = Buffer.alloc(out.width * out.height * 4);
  let left = out.width,
    top = out.height,
    right = -1,
    bottom = -1;
  for (let y = 0; y < out.height; y++)
    for (let x = 0; x < out.width; x++) {
      let covered = 0;
      const sum = [0, 0, 0];
      for (let dy = 0; dy < 2; dy++)
        for (let dx = 0; dx < 2; dx++) {
          const i = ((2 * y + dy) * width + 2 * x + dx) * 3;
          if (mask.data[i] + mask.data[i + 1] + mask.data[i + 2] === 0)
            continue;
          covered++;
          for (let c = 0; c < 3; c++) sum[c] += color.data[i + c];
        }
      if (!covered) continue;
      const o = (y * out.width + x) * 4;
      for (let c = 0; c < 3; c++) pixels[o + c] = Math.round(sum[c] / covered);
      pixels[o + 3] = Math.round((255 * covered) / 4);
      ((left = Math.min(left, x)),
        (top = Math.min(top, y)),
        (right = Math.max(right, x)),
        (bottom = Math.max(bottom, y)));
    }
  if (right < 0) throw new Error("The model capture shows no subject.");
  const margin = Math.round(
    0.05 * Math.max(right - left + 1, bottom - top + 1),
  );
  const crop = {
    left: Math.max(0, left - margin),
    top: Math.max(0, top - margin),
    width:
      Math.min(out.width - 1, right + margin) - Math.max(0, left - margin) + 1,
    height:
      Math.min(out.height - 1, bottom + margin) - Math.max(0, top - margin) + 1,
  };
  // A subject that touches the edge of the frame may be cut off, so the render is refused.
  if (
    left === 0 ||
    top === 0 ||
    right === out.width - 1 ||
    bottom === out.height - 1
  )
    throw new Error(
      "The subject touches the edge of the model capture and may be cut off.",
    );
  await sharp(pixels, {
    raw: { width: out.width, height: out.height, channels: 4 },
  })
    .extract(crop)
    .png()
    .toFile(target);
}

async function captureModel(config, lab, args) {
  const session = await requireSession(config);
  if (session.mode !== "render")
    throw new Error(
      "Model captures need a game launched with: lab.mjs launch render",
    );
  if (!args.item || !args.name)
    throw new Error(
      'Name the item and the capture: lab.mjs capture-model {"item": "...", "name": "..."}',
    );
  // Every render shares the light of a clear noon, so colours compare across items.
  await game(config, lab, "set-time", { hours: 12 });
  await game(config, lab, "set-weather", { type: 0 });
  await game(config, lab, "give", { items: [args.item] });
  await game(config, lab, "open-sheet", {});
  const file = `capture-${Date.now()}`;
  const result = await game(config, lab, "capture-model", {
    item: args.item,
    directory: config.windowsPath(lab.captures),
    file,
  });
  const passes = [
    join(lab.captures, `${file}-color.png`),
    join(lab.captures, `${file}-mask.png`),
  ];
  await waitFor(
    async () => (await Promise.all(passes.map(exists))).every(Boolean),
    30,
    "the model capture's files",
  );
  const target = join(config.runtime, session.run, `${args.name}.png`);
  if (await exists(target))
    throw new Error(
      `${relative(root, target)} exists. Captures in a run are never overwritten.`,
    );
  await composeModel(passes[0], passes[1], target);
  for (const pass of passes) await rm(pass);
  await record(config, session, {
    command: "keep",
    file: relative(root, target),
    sha256: await sha256(target),
  });
  return { ...result, file: relative(root, target) };
}

// --- Entry -----------------------------------------------------------------------------------------------------

const usage = `Usage: bun tools/runtime/lab.mjs <command> [arguments]

Lab copy and game:
  prepare                         make the lab copy for the installed Steam build
  launch [headless|render] [label] start the lab game off screen and open a run
  begin <label>                   open a new run in the running session
  quit                            quit the lab game and close its session
  status                          show the lab copy, session and run

In the game (arguments are one JSON object):
  new-save <name>                 start a new game and create a character
  capture-screen <name> [noui]    keep an engine screenshot in the run (render mode)
  capture-model <json>            keep a model render in the run (render mode)
  eval <file>                     run a Lua file with the command helpers
  ${"<command> [json]"}                run tools/runtime/commands/<command>.lua`;

async function main([command, ...rest]) {
  const config = await configuration();
  switch (command) {
    case "prepare":
      return prepare(config);
    case "status": {
      const session = await readSession(config);
      return {
        build: config.buildId,
        copy: config.copy,
        prepared: await exists(join(config.copy, "lab.json")),
        session,
        running: session ? gameProcesses(config).includes(session.pid) : false,
      };
    }
    case undefined:
    case "help":
      console.log(
        usage +
          "\n\nCommands: " +
          (await commandFiles()).map((file) => file.slice(0, -4)).join(", "),
      );
      return null;
  }
  const lab = await labCopy(config);
  switch (command) {
    case "launch": {
      const mode = modes[rest[0]] ? rest.shift() : "headless";
      return launch(config, lab, mode, rest[0]);
    }
    case "quit":
      return quit(config, lab);
    case "begin": {
      const session = await requireSession(config);
      if (!rest[0]) throw new Error("Name the run: lab.mjs begin <label>");
      session.run = await openRun(config, rest[0], {
        mode: session.mode,
        pid: session.pid,
      });
      await writeSession(config, session);
      return { run: relative(root, join(config.runtime, session.run)) };
    }
    case "new-save":
      return newSave(config, lab, rest[0]);
    case "capture-screen":
      return captureScreen(
        config,
        lab,
        rest[0] ?? "screen",
        rest[1] !== "noui",
      );
    case "capture-model":
      return captureModel(config, lab, JSON.parse(rest[0] ?? "{}"));
    case "eval": {
      if (!rest[0]) throw new Error("Name the Lua file: lab.mjs eval <file>");
      return game(
        config,
        lab,
        "eval",
        {},
        await readFile(resolve(rest[0]), "utf8"),
      );
    }
    default:
      return game(config, lab, command, rest[0] ? JSON.parse(rest[0]) : {});
  }
}

try {
  const result = await main(process.argv.slice(2));
  if (result !== null) console.log(JSON.stringify(result, null, 2));
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
