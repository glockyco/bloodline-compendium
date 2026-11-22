-- lab setup: raises character and discipline experience through the game's own tracking functions
-- Observes character progression as the skill planner relies on it. args.action chooses what:
-- - "state": every number the game instance, the player and the passive stat component hold.
-- - "levels": adds args.exp character experience args.times times through the player's own Add EXP, and lists what
--   each step changed.
-- - "discipline": calls the passive stat component's args.function, or "<args.discipline> Level Tracking", with
--   args.exp experience args.times times, the amount multiplied by args.grow after each call, and lists what each
--   call changed.
-- - "passive": presses the passive button args.widget of the open tree args.tree args.times times, and lists the
--   button's cost, rank and state and every number that changed after each press.
-- - "skill": presses the skill slot args.widget of the open tree args.tree twice, and lists what each press changed.
-- - "attributes": opens the level up window through the passive stat component's own Level Up and presses each
--   button in args.buttons once, listing what each press changed.
-- - "hotbar": the slots of the player's skill bar.
-- - "open": opens the skill window and the tree of args.button in the menu of args.category, as a player does.
-- Every change is listed as the field's owner and name with its value before and after, so a reader needs no list of
-- field names. Waits between steps let the game's own delays run.
local pawn = requirePlayer()
local gi = live("BP_PersistantGameInstance_C")
local component = pawn.PlayerPassiveLevelStatComponent
local owners = { instance = gi, player = pawn, passives = component }

local numeric = { IntProperty = true, FloatProperty = true, ByteProperty = true, BoolProperty = true }
local function snapshot()
  local values = {}
  for label, object in pairs(owners) do
    local class = object:GetClass()
    while class and class:IsValid() and not class:GetFullName():find("/Script/", 1, true) do
      class:ForEachProperty(function(property)
        if numeric[property:GetClass():GetFName():ToString()] then
          local name = property:GetFName():ToString()
          local ok, value = pcall(function() return object[name] end)
          if ok and type(value) ~= "userdata" then values[label .. "." .. name] = value end
        end
      end)
      class = class:GetSuperStruct()
    end
  end
  return values
end
local function changes(before, after)
  local list = {}
  for name, value in pairs(after) do
    if before[name] ~= value then list[name] = { before[name], value } end
  end
  return list
end
local function inTree(class, widget)
  for _, found in ipairs(liveAll(class)) do
    local path = found:GetFullName()
    if path:find("." .. args.tree .. "_C_", 1, true) and path:sub(-#widget - 1) == "." .. widget and found:IsVisible() then
      return found
    end
  end
  error("No visible " .. class .. " " .. widget .. " in an open " .. args.tree .. ". Open the tree first.")
end
local function clickButton(widget)
  local names = {}
  widget:GetClass().DynamicBindingObjects:ForEach(function(_, element)
    local binding = element:get()
    if binding:IsValid() and binding.ComponentDelegateBindings then
      binding.ComponentDelegateBindings:ForEach(function(_, entry)
        local value = entry:get()
        if value.DelegatePropertyName:ToString():find("Click", 1, true) then names[#names + 1] = value.ComponentPropertyName:ToString() end
      end)
    end
  end)
  return names[1] or error("No click binding on " .. widget:GetFullName())
end

local finish = defer()
local steps = {}
local times = args.times or 1
local function step(index, act, describe)
  if index > times then return finish({ action = args.action, steps = steps }) end
  local before = snapshot()
  local ok, problem = pcall(act, index)
  ExecuteInGameThreadAfterFrames(args.frames or 20, function()
    local row = { changed = changes(before, snapshot()) }
    if not ok then row.error = tostring(problem) end
    if describe then
      local described, value = pcall(describe)
      row.widget = described and value or tostring(value)
    end
    steps[#steps + 1] = row
    step(index + 1, act, describe)
  end)
end

if args.action == "state" then
  return snapshot()
elseif args.action == "levels" then
  step(1, function() pawn["Add EXP"](pawn, args.exp) end)
elseif args.action == "discipline" then
  local name = args["function"] or (args.discipline .. " Level Tracking")
  -- args.grow multiplies the experience after every call, so one run crosses low levels one at a time and still
  -- reaches high ones.
  local amount = args.exp
  step(1, function()
    component[name](component, amount)
    amount = amount * (args.grow or 1)
  end)
elseif args.action == "passive" then
  local button = inTree("PassiveSkillButton_C", args.widget)
  local click = clickButton(button)
  local function describe()
    return { cost = button["Point Cost"], unlocked = button.Unlocked, available = button.Available, locked = button.Locked, multi = button.MultiLevel, max = button["Max MultiLevel"] }
  end
  steps[1] = { widget = describe() }
  step(1, function() press(button, click) end, describe)
elseif args.action == "skill" then
  local slot = inTree("SkillSlot_C", args.widget)
  local click = clickButton(slot)
  times = 2
  step(1, function() press(slot, click) end, function() return { unlocked = slot.Unlocked, available = slot["Is Available"], known = #pawn.UnlockedSkills } end)
elseif args.action == "attributes" then
  component["Level Up"](component)
  ExecuteInGameThreadAfterFrames(30, function()
    local popup = live("BaseStatIncreasePopup_C")
    if not popup then return finish(nil, "The level up window did not open") end
    times = #args.buttons
    step(1, function(index) press(popup, args.buttons[index]) end)
  end)
elseif args.action == "hotbar" then
  local rows = {}
  for _, bar in ipairs(liveAll("W_SkillBar_C")) do
    local row = { bar = bar:GetFullName():match("[^.]+$"), visible = bar:IsVisible(), columns = bar.ColumnCount, rows = bar.RowCount, offhand = bar["Offhand Bar?"], slots = 0 }
    for _, slot in ipairs(liveAll("UserWidget")) do
      local path = slot:GetFullName()
      if path:find(row.bar .. ".WidgetTree.", 1, true) and slot:GetClass():GetFName():ToString():lower():find("slot", 1, true) then
        row.slots = row.slots + 1
      end
    end
    rows[#rows + 1] = row
  end
  finish(rows)
elseif args.action == "open" then
  -- Opens the skill window, presses the category button args.category and then the discipline button args.button,
  -- as a player opens a tree.
  if not pawn["Skill Window Open?"] then pawn["Open/close Skill Window"](pawn) end
  ExecuteInGameThreadAfterFrames(10, function()
    local ok, problem = pcall(function()
      press(live("SkillTree_CategorySelection1_C") or error("No category selection"), args.category)
    end)
    if not ok then return finish(nil, problem) end
    ExecuteInGameThreadAfterFrames(10, function()
      local menu
      for _, widget in ipairs(liveAll("UserWidget")) do
        if widget:GetClass():GetFName():ToString():find("Subcategor", 1, true) and widget:IsVisible() then menu = widget end
      end
      if not menu then return finish(nil, "No discipline menu is shown") end
      local pressed, failure = pcall(press, menu, args.button)
      if not pressed then return finish(nil, failure) end
      ExecuteInGameThreadAfterFrames(10, function() finish({ opened = args.button }) end)
    end)
  end)
else
  error("Unknown action " .. tostring(args.action))
end
