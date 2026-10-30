-- Helpers shared by every command. lab.mjs sends them with each request, followed by every command as a
-- function in `commands`, so one command can run another.

local statics = UEHelpers.GetGameplayStatics()
local system = StaticFindObject("/Script/Engine.Default__KismetSystemLibrary")

--- Whether an object is an instance the game uses. Class defaults and the templates a class holds, such as a
--- widget class's own widget tree, are not: a template's outer chain reaches a class or a class default.
local function isInstance(object)
  local RF_ClassDefaultObject = 0x10
  if not object:IsValid() or object:HasAnyFlags(RF_ClassDefaultObject) then return false end
  local outer = object:GetOuter()
  while outer and outer:IsValid() do
    if outer:IsAnyClass() or outer:HasAnyFlags(RF_ClassDefaultObject) then return false end
    outer = outer:GetOuter()
  end
  return true
end

--- The first instance of a class the game uses. A needle narrows by full name.
local function live(class, needle)
  for _, object in ipairs(FindAllOf(class) or {}) do
    if isInstance(object) and (not needle or object:GetFullName():find(needle, 1, true)) then return object end
  end
  return nil
end

--- Every instance of a class the game uses.
local function liveAll(class)
  local found = {}
  for _, object in ipairs(FindAllOf(class) or {}) do
    if isInstance(object) then found[#found + 1] = object end
  end
  return found
end

local function player()
  local controller = UEHelpers.GetPlayerController()
  if not controller:IsValid() or not controller.Pawn:IsValid() then return nil end
  return controller.Pawn
end

local function requirePlayer()
  return player() or error("No player in the world. Start a game with: lab new-save")
end

--- Reads a dotted property path such as "Player Ref.Level".
local function getPath(object, path)
  local value = object
  for part in path:gmatch("[^.]+") do value = value[part] end
  return value
end

local function setPath(object, path, newValue)
  local parent, last = object, nil
  for part in path:gmatch("[^.]+") do
    if last then parent = parent[last] end
    last = part
  end
  parent[last] = newValue
end

--- The function a widget binds to a component's click and the click delegate's name, read from the widget
--- class's own bindings.
local function clickBinding(widget, component)
  local found, delegate
  widget:GetClass().DynamicBindingObjects:ForEach(function(_, element)
    local binding = element:get()
    if binding:IsValid() and binding.ComponentDelegateBindings then
      binding.ComponentDelegateBindings:ForEach(function(_, entry)
        local value = entry:get()
        if value.ComponentPropertyName:ToString() == component
          and value.DelegatePropertyName:ToString():find("Click", 1, true) then
          found, delegate = value.FunctionNameToBind:ToString(), value.DelegatePropertyName:ToString()
        end
      end)
    end
  end)
  return found, delegate
end

--- Presses a widget's button by calling the function the widget class binds to the button's click. A CommonUI
--- button's click passes the button itself, a plain button's click passes nothing.
local function press(widget, component)
  local name, delegate = clickBinding(widget, component)
  if not name then error(widget:GetClass():GetFName():ToString() .. " binds no click function to " .. component) end
  if delegate == "OnButtonBaseClicked" then widget[name](widget, widget[component]) else widget[name](widget) end
  return { widget = widget:GetFullName(), ["function"] = name }
end

local function text(widget)
  if not widget or not widget:IsValid() then return nil end
  return widget:GetText():ToString()
end

local function console(command)
  system:ExecuteConsoleCommand(UEHelpers.GetWorld(), command, UEHelpers.GetPlayerController())
end

--- The class path of a class, such as /Game/Path/Item.Item_C.
local function classPath(class)
  if not class or not class:IsValid() then return nil end
  return class:GetFullName():match("^%S+%s+(.+)$")
end

local fieldNames = {}

--- Reads a field of a user-defined struct by its plain name. Shipping builds keep such fields as
--- Name_<n>_<GUID>, so the names are looked up once per struct.
local function field(value, structPath, name)
  local names = fieldNames[structPath]
  if not names then
    names = {}
    StaticFindObject(structPath):ForEachProperty(function(property)
      local full = property:GetFName():ToString()
      names[full:match("^(.-)_%d+_%x+$") or full] = full
    end)
    fieldNames[structPath] = names
  end
  return value[names[name] or error(structPath .. " has no field " .. name)]
end

local function itemInfo(info, name)
  return field(info, "/Game/BloodlinesAlpha/Interactables/F_ItemInfoo.F_ItemInfoo", name)
end

--- The items of an inventory or equipment array as class paths and quantities.
local function items(array)
  local list = {}
  array:ForEach(function(_, element)
    local info = element:get()
    local class = itemInfo(info, "ItemClass")
    if class and class:IsValid() then
      list[#list + 1] = { item = classPath(class), quantity = itemInfo(info, "Quantity") }
    end
  end)
  return list
end

--- The inventory slot widgets the open character sheet shows. The sheet rebuilds its grid when the inventory
--- changes and leaves the replaced slots alive without a parent, and pressing such a slot does nothing.
local function inventorySlots()
  local slots = {}
  for _, slot in ipairs(liveAll("UI_InventoryItem_2_C")) do
    local parent = slot:GetParent()
    if parent and parent:IsValid() and slot:IsVisible() then slots[#slots + 1] = slot end
  end
  return slots
end

--- The inventory slot widget that shows an item class. The character sheet must be open.
local function inventorySlot(path)
  for _, slot in ipairs(inventorySlots()) do
    if classPath(itemInfo(slot.ItemInfo, "ItemClass")) == path then return slot end
  end
  error("No inventory slot shows " .. path .. ". Give the item and open the sheet first.")
end

--- Whether a widget is off screen: it or a panel that holds it is collapsed or hidden, the ESlateVisibility values
--- 1 and 2. A tooltip keeps its unused lines filled in and collapses the box around them.
local function offScreen(widget)
  while widget and widget:IsValid() do
    local visibility = widget:GetVisibility()
    if visibility == 1 or visibility == 2 then return true end
    widget = widget:GetParent()
  end
  return false
end

--- The text of every text block a widget class declares that the widget shows, by variable name, and the names of
--- the ones it keeps off screen.
local function texts(widget)
  local shown, hidden = {}, {}
  widget:GetClass():ForEachProperty(function(property)
    if property:GetClass():GetFName():ToString() ~= "ObjectProperty" then return end
    local name = property:GetFName():ToString()
    local value = widget[name]
    if value and value:IsValid() and value:GetClass():GetFName():ToString():find("TextBlock", 1, true) then
      if offScreen(value) then hidden[#hidden + 1] = name else shown[name] = text(value) end
    end
  end)
  return shown, hidden
end
