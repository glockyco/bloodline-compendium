-- lab setup: opens the skill window and every discipline tree through their bound buttons, without changing anything
-- Reads the tooltip the game builds for every skill slot of every tree the skill window opens: the slot's own
-- HandleTooltip fills its Skill Tooltip widget, and every text block of that widget is listed in widget order, keyed
-- by the slot's skill class. The catalog compares these texts with each skill's facts.
-- Run it with a character in the world: lab.mjs eval tools/runtime/checks/skill-tooltips.lua
local pawn = requirePlayer()
if not pawn["Skill Window Open?"] then pawn["Open/close Skill Window"](pawn) end

local function names()
  local seen = {}
  for _, widget in ipairs(liveAll("UserWidget")) do seen[widget:GetFullName()] = true end
  return seen
end
local function created(before)
  local found = {}
  for _, widget in ipairs(liveAll("UserWidget")) do
    if not before[widget:GetFullName()] then found[#found + 1] = widget end
  end
  return found
end
local function buttons(widget)
  local list = {}
  widget:GetClass().DynamicBindingObjects:ForEach(function(_, element)
    local binding = element:get()
    if binding:IsValid() and binding.ComponentDelegateBindings then
      binding.ComponentDelegateBindings:ForEach(function(_, entry)
        local value = entry:get()
        if value.DelegatePropertyName:ToString():find("Click", 1, true) then
          list[#list + 1] = value.ComponentPropertyName:ToString()
        end
      end)
    end
  end)
  table.sort(list)
  return list
end
-- The texts of a widget's text blocks, in the order of their full names.
local function texts(widget)
  local prefix = widget:GetFullName():match("^%S+%s+(.+)$") .. "."
  local list = {}
  for _, block in ipairs(liveAll("TextBlock")) do
    local path = block:GetFullName():match("^%S+%s+(.+)$")
    if path:sub(1, #prefix) == prefix then
      list[#list + 1] = { widget = path:sub(#prefix + 1), text = block:GetText():ToString(), visible = block:IsVisible() }
    end
  end
  table.sort(list, function(a, b) return a.widget < b.widget end)
  return list
end

local skills = {}
local failures = {}
local selection = live("SkillTree_CategorySelection1_C") or error("The skill window shows no category selection")
for _, category in ipairs(buttons(selection)) do
  local before = names()
  local menu
  if pcall(press, selection, category) then
    for _, widget in ipairs(created(before)) do
      if widget:GetClass():GetFName():ToString():find("Subcategor", 1, true) then menu = widget end
    end
  end
  for _, button in ipairs(menu and buttons(menu) or {}) do
    local opened = names()
    if pcall(press, menu, button) then
      for _, tree in ipairs(created(opened)) do
        local prefix = tree:GetFullName():match("^%S+%s+(.+)$") .. "."
        for _, slot in ipairs(liveAll("SkillSlot_C")) do
          local path = slot:GetFullName():match("^%S+%s+(.+)$")
          if path:sub(1, #prefix) == prefix and slot.Skill and slot.Skill:IsValid() then
            local skill = classPath(slot.Skill)
            local ok, problem = pcall(function() slot:HandleTooltip(true) end)
            local tooltip = slot["Skill Tooltip"]
            if ok and tooltip and tooltip:IsValid() then
              skills[skill] = texts(tooltip)
            else
              failures[skill] = tostring(problem)
            end
          end
        end
      end
    end
  end
end
return { check = "skill-tooltips", skills = skills, failures = failures }
