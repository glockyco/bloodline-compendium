-- lab setup: opens the skill window and every discipline tree through their bound buttons, without changing anything
-- Lists every discipline the skill window reaches: for each category button of the window's category selection and
-- each discipline button of its menu, the tree widget the button builds, and in that tree every skill slot and
-- passive button with its widget name and the values the game set on it. Every int, float, bool, name, string, text
-- and class field the slot or button class declares is listed, so a catalog producer can be checked against what the
-- game builds. A tree that a button builds again is listed once.
-- Run it with a character in the world: lab.mjs eval tools/runtime/checks/skill-trees.lua
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
-- The buttons a widget class binds a click to, read from its own bindings.
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
local simple = {
  IntProperty = true, FloatProperty = true, BoolProperty = true, NameProperty = true, StrProperty = true,
  TextProperty = true, ClassProperty = true, ByteProperty = true, EnumProperty = true,
}
-- The values of the fields that a widget's own class declares, from its class up to the first native class.
local function fields(widget)
  local row = {}
  local class = widget:GetClass()
  while class and class:IsValid() and not class:GetFullName():find("/Script/", 1, true) do
    class:ForEachProperty(function(property)
      local name = property:GetFName():ToString()
      local kind = property:GetClass():GetFName():ToString()
      if simple[kind] and row[name] == nil then
        local ok, value = pcall(function() return widget[name] end)
        if ok and value ~= nil then
          if kind == "TextProperty" or kind == "StrProperty" or kind == "NameProperty" then
            value = value:ToString()
          elseif kind == "ClassProperty" then
            value = value:IsValid() and classPath(value) or nil
          end
          row[name] = value
        end
      end
    end)
    class = class:GetSuperStruct()
  end
  return row
end
local function children(tree, class)
  local list = {}
  local prefix = tree:GetFullName():match("^%S+%s+(.+)$") .. "."
  for _, widget in ipairs(liveAll(class)) do
    local path = widget:GetFullName():match("^%S+%s+(.+)$")
    if path:sub(1, #prefix) == prefix then
      local row = fields(widget)
      row.widget = path:sub(#prefix + 1)
      row.visible = widget:IsVisible()
      list[#list + 1] = row
    end
  end
  table.sort(list, function(a, b) return a.widget < b.widget end)
  return list
end

local selection = live("SkillTree_CategorySelection1_C") or error("The skill window shows no category selection")
local trees = {}
local menus = {}
for _, category in ipairs(buttons(selection)) do
  local before = names()
  local ok = pcall(press, selection, category)
  local menu
  for _, widget in ipairs(ok and created(before) or {}) do
    if widget:GetClass():GetFName():ToString():find("Subcategor", 1, true) then menu = widget end
  end
  if menu then
    local entry = { category = category, menu = menu:GetClass():GetFName():ToString(), disciplines = {} }
    menus[#menus + 1] = entry
    for _, button in ipairs(buttons(menu)) do
      local opened = names()
      local pressed, problem = pcall(press, menu, button)
      local built = {}
      for _, widget in ipairs(pressed and created(opened) or {}) do
        local class = widget:GetClass():GetFName():ToString()
        if class:lower():find("tree", 1, true) and not class:find("Button", 1, true) then built[#built + 1] = widget end
      end
      entry.disciplines[button] = pressed and {} or ("error: " .. tostring(problem):sub(1, 120))
      for _, tree in ipairs(built) do
        local class = classPath(tree:GetClass())
        table.insert(entry.disciplines[button], class)
        if not trees[class] then
          trees[class] = {
            slots = children(tree, "SkillSlot_C"),
            passives = children(tree, "PassiveSkillButton_C"),
            spellsongs = children(tree, "Spellsong_SkilTree_Button_C"),
          }
        end
      end
    end
  end
end
return { menus = menus, trees = trees }
