-- BloodlineBridge: the lab runtime's request channel. `lab.mjs prepare` installs it into a lab copy and writes
-- that copy's bridge directory in place of the marker below.
-- The host writes inbox\<n>.lua (atomic rename), this mod runs it on the game thread and writes outbox\<n>.json.
-- A file channel needs no sockets under Wine, and the request shape follows HotRepl so a HotRepl host can
-- replace it without changing callers.
local BRIDGE = "@BRIDGE@"
local INBOX = BRIDGE .. "inbox\\"
local OUTBOX = BRIDGE .. "outbox\\"
local UEHelpers = require("UEHelpers")

local ESCAPES = { ['"'] = '\\"', ['\\'] = '\\\\', ['\b'] = '\\b', ['\f'] = '\\f', ['\n'] = '\\n', ['\r'] = '\\r', ['\t'] = '\\t' }

local function json_escape(s)
  return (s:gsub('[%c"\\]', function(c) return ESCAPES[c] or string.format("\\u%04x", c:byte()) end))
end

local function describe(v, depth, seen)
  depth = depth or 0
  local t = type(v)
  if t == "nil" or t == "boolean" or t == "number" or t == "string" then return v end
  if t == "table" then
    if depth > 6 then return "<depth limit>" end
    seen = seen or {}
    if seen[v] then return "<cycle>" end
    seen[v] = true
    local out = {}
    for k, val in pairs(v) do
      local key = (type(k) == "number" or type(k) == "string") and k or tostring(k)
      out[key] = describe(val, depth + 1, seen)
    end
    return out
  end
  if t == "userdata" then
    local ok, name = pcall(function() return v:GetFullName() end)
    if ok and type(name) == "string" then return name end
    ok, name = pcall(function() return v:ToString() end)
    if ok and type(name) == "string" then return name end
  end
  return tostring(v)
end

local function encode(v)
  local t = type(v)
  if t == "nil" then return "null" end
  if t == "boolean" then return v and "true" or "false" end
  if t == "number" then
    if v ~= v or v == math.huge or v == -math.huge then return '"' .. tostring(v) .. '"' end
    return tostring(v)
  end
  if t == "string" then return '"' .. json_escape(v) .. '"' end
  if t == "table" then
    local n = #v
    local is_array = n > 0
    for k in pairs(v) do
      if type(k) ~= "number" or k < 1 or k > n or k % 1 ~= 0 then is_array = false break end
    end
    local parts = {}
    if is_array then
      for i = 1, n do parts[#parts + 1] = encode(v[i]) end
      return "[" .. table.concat(parts, ",") .. "]"
    end
    for k, val in pairs(v) do parts[#parts + 1] = '"' .. json_escape(tostring(k)) .. '":' .. encode(val) end
    return "{" .. table.concat(parts, ",") .. "}"
  end
  return '"' .. json_escape(tostring(v)) .. '"'
end

local function write_file(path, content)
  local tmp = path .. ".tmp"
  local f = assert(io.open(tmp, "wb"))
  f:write(content)
  f:close()
  os.remove(path)
  assert(os.rename(tmp, path))
end

local function read_file(path)
  local f = io.open(path, "rb")
  if not f then return nil end
  local s = f:read("a")
  f:close()
  return s
end

math.randomseed(os.time())
local session = tostring(os.time()) .. "-" .. tostring(math.random(1, 1000000000))
local next_id = 1

local function run(id, code)
  local buf = {}
  local deferred = false
  local function respond(ok, result)
    local payload = { id = id, session = session, ok = ok, result = describe(result), out = buf }
    local enc_ok, text = pcall(encode, payload)
    if not enc_ok then
      text = encode({ id = id, session = session, ok = false, result = "encode failed: " .. tostring(text), out = buf })
    end
    write_file(OUTBOX .. tostring(id) .. ".json", text)
  end
  local env = setmetatable({
    out = function(...)
      local parts = {}
      for i = 1, select("#", ...) do parts[#parts + 1] = tostring((select(i, ...))) end
      buf[#buf + 1] = table.concat(parts, "\t")
    end,
    -- Keeps the request open after the code returns, for work that spans frames. The returned function
    -- finishes the request: finish(result) on success, finish(nil, problem) on failure.
    defer = function()
      deferred = true
      local finished = false
      return function(result, problem)
        if finished then return end
        finished = true
        if problem ~= nil then respond(false, problem) else respond(true, result) end
      end
    end,
    UEHelpers = UEHelpers,
    describe = describe,
    encode = encode,
    write_file = write_file,
  }, { __index = _G })
  local fn, err = load(code, "=bridge#" .. id, "t", env)
  local ok, result
  if not fn then ok, result = false, err else ok, result = xpcall(fn, debug.traceback) end
  if ok and deferred then return end
  respond(ok, result)
end

write_file(OUTBOX .. "ready.json", encode({ session = session, next = next_id }))

LoopAsync(100, function()
  local path = INBOX .. tostring(next_id) .. ".lua"
  local code = read_file(path)
  if code then
    os.remove(path)
    local id = next_id
    next_id = next_id + 1
    ExecuteInGameThread(function() run(id, code) end)
  end
  return false
end)

print("[BloodlineBridge] ready, session " .. session .. "\n")
