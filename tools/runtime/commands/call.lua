-- lab setup: sets the inputs it is given directly and restores them afterwards
-- Calls a game function for each case of inputs and reads its outputs, so a rule can be checked against the
-- game's own result. args.class names a live object, or args.default names a class default object by path for
-- functions that only compute. A case's keys listed in args.parameters are passed to the function in that order,
-- and its other keys are set on the object first. With args.out the function also gets a table for its output
-- parameters, and an output named in that table is read from it.
local target = args.default and StaticFindObject(args.default) or live(args.class)
if not target or not target:IsValid() then error("No object " .. (args.default or args.class)) end
local parameters = args.parameters or {}
local passed = {}
for _, name in ipairs(parameters) do passed[name] = true end
local originals = {}
for _, case in ipairs(args.cases) do
  for path in pairs(case) do
    if not passed[path] and originals[path] == nil then originals[path] = getPath(target, path) end
  end
end
local function invoke(case)
  local arguments = {}
  for index, name in ipairs(parameters) do arguments[index] = case[name] end
  local out = {}
  if args.out then arguments[#parameters + 1] = out end
  target[args["function"]](target, table.unpack(arguments, 1, #parameters + (args.out and 1 or 0)))
  return out
end
local rows = {}
for _, case in ipairs(args.cases) do
  for path, value in pairs(case) do
    if not passed[path] then setPath(target, path, value) end
  end
  local out = invoke(case)
  local outputs = {}
  for _, path in ipairs(args.outputs) do
    if out[path] ~= nil then outputs[path] = out[path] else outputs[path] = getPath(target, path) end
  end
  rows[#rows + 1] = { inputs = case, outputs = outputs }
end
for path, value in pairs(originals) do setPath(target, path, value) end
if #parameters == 0 then target[args["function"]](target) end
return { target = target:GetFullName(), ["function"] = args["function"], restored = originals, rows = rows }
