-- lab setup: rewrites one loaded harvestable's preset and calls its pick directly, which a player cannot do.
-- Measures what a harvest gives beyond its loot table, through the harvestable's own functions.
-- Without arguments, gives one loaded harvestable each preset name, as harvest-presets.lua does, and lists the
-- experience its Get ... EXP function returns and the bonus chance Calculate Bonus Chance returns with the checked
-- profession's level on the game instance set to the required level and to 5, 10, 25, 50 and 100 levels above it. The
-- profession's level is restored afterwards. args.curve = N instead lists, for every Woodcutting and Mining preset, the
-- bonus chance at each level from 1 to N.
-- args.pick = "<preset>" instead calls Spawn Harvest FX args.times times on a loaded harvestable of that preset and
-- lists, after each call, its Stage, whether it is still valid, and the player's inventory count of each loot item.
-- The player's harvest animation calls Spawn Harvest FX at each swing, and it chooses the loot, rolls the bonus,
-- advances the stage and destroys the harvestable. args.skip passes over that many harvestables of the preset, so a
-- repeat measures a fresh one, and args.name picks the harvestable by its actor name, which the result lists, so calls
-- a few seconds apart can swing at one harvestable as a player does. A harvestable that no longer exists returns gone.
local harvestables = liveAll("HarvestableInteractionObject_C")
if #harvestables == 0 then error("No harvestable is loaded") end

local function className(object)
  return (object:GetFullName():gsub("^%S+ ", ""))
end

local expFunction = {
  ["Woodcutting Items"] = { "Get Woodcutting EXP to Add", "EXP To Add", "Woodcutting Level" },
  ["Mining Items"] = { "Get Mining EXP to add", "Exp", "Mining Level" },
  ["Foraging EXP Items"] = { "Get Foraging EXP to Add", "EXP To Add", "Foraging Level" },
}

if args.pick then
  local actor
  local skip = args.skip or 0
  for _, candidate in ipairs(harvestables) do
    if args.name and candidate:GetFName():ToString() == args.name then actor = candidate break end
    if not args.name and candidate["Custom Preset Name"]:ToString() == args.pick then
      if skip == 0 then actor = candidate break end
      skip = skip - 1
    end
  end
  if not actor then return { preset = args.pick, name = args.name, gone = true, items = nil } end
  local loot = {}
  actor["Selected Loot Table"]:ForEach(function(key) loot[#loot + 1] = key:get() end)
  local player = requirePlayer()
  local wanted = {}
  for _, class in ipairs(loot) do wanted[classPath(class)] = true end
  local function counts()
    local out = {}
    for _, entry in ipairs(items(player.Inventory)) do
      if wanted[entry.item] then out[entry.item] = (out[entry.item] or 0) + entry.quantity end
    end
    return out
  end
  local steps = { { call = 0, stage = actor.Stage, valid = actor:IsValid(), items = counts() } }
  for call = 1, args.times or 6 do
    if not actor:IsValid() then break end
    actor["Spawn Harvest FX"](actor)
    steps[#steps + 1] = { call = call, stage = actor:IsValid() and actor.Stage or nil, valid = actor:IsValid(), items = counts() }
  end
  return { preset = args.pick, name = actor:IsValid() and actor:GetFName():ToString() or args.name, steps = steps }
end

local actor = harvestables[1]
local gi = live("BP_PersistantGameInstance_C")
local out = {}
if args.curve then
  for list, call in pairs(expFunction) do
    if list ~= "Foraging EXP Items" then
      local names = {}
      actor[list]:ForEach(function(_, element) names[#names + 1] = element:get():ToString() end)
      for _, name in ipairs(names) do
        actor["Custom Preset"] = true
        actor["Table Based Preset?"] = false
        actor["Custom Preset Name"] = name
        actor["NewFunction_0"](actor)
        actor["Required level"] = 1
        actor["Check Player Level"](actor)
        local before = gi[call[3]]
        local chances = {}
        for level = 1, args.curve do
          gi[call[3]] = level
          local chance = {}
          actor["Calculate Bonus Chance"](actor, chance)
          chances[level] = math.floor(chance["Bonus Chance"] * 10000 + 0.5) / 10000
        end
        gi[call[3]] = before
        out[#out + 1] = { preset = name, required = actor["Required level"], chances = chances }
      end
    end
  end
  return out
end
for list, call in pairs(expFunction) do
  local names = {}
  actor[list]:ForEach(function(_, element) names[#names + 1] = element:get():ToString() end)
  for _, name in ipairs(names) do
    actor["Custom Preset"] = true
    actor["Table Based Preset?"] = false
    actor["Custom Preset Name"] = name
    actor["NewFunction_0"](actor)
    -- Check Player Level sets the preset's required level, as harvest-presets.lua records it.
    actor["Required level"] = 1
    actor["Check Player Level"](actor)
    local result = {}
    actor[call[1]](actor, result)
    local required = actor["Required level"]
    local field = call[3]
    local before = gi[field]
    local bonus = {}
    for _, above in ipairs({ 0, 5, 10, 25, 50, 100 }) do
      gi[field] = math.max(required, 1) + above
      local chance = {}
      actor["Calculate Bonus Chance"](actor, chance)
      bonus[#bonus + 1] = { level = gi[field], chance = chance["Bonus Chance"] }
    end
    gi[field] = before
    out[#out + 1] = {
      list = list,
      preset = name,
      required = required,
      checks = field,
      exp = result[call[2]],
      harnessBoost = result["Harness Boost"],
      bonus = bonus,
    }
  end
end
return out
