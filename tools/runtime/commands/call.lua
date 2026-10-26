-- lab setup: sets the inputs it is given directly and restores them afterwards
-- Calls a game function for each case of inputs and reads its outputs, so a rule can be checked against the
-- game's own result.
local target = live(args.class)
if not target then error("No live " .. args.class) end
local originals = {}
for _, case in ipairs(args.cases) do
  for path in pairs(case) do
    if originals[path] == nil then originals[path] = getPath(target, path) end
  end
end
local rows = {}
for _, case in ipairs(args.cases) do
  for path, value in pairs(case) do setPath(target, path, value) end
  target[args["function"]](target)
  local outputs = {}
  for _, path in ipairs(args.outputs) do outputs[path] = getPath(target, path) end
  rows[#rows + 1] = { inputs = case, outputs = outputs }
end
for path, value in pairs(originals) do setPath(target, path, value) end
target[args["function"]](target)
return { target = target:GetFullName(), ["function"] = args["function"], restored = originals, rows = rows }
