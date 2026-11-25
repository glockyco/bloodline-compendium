-- lab setup: eval with {"talk":"<actor class>","name":"<optional actor name>","handler":"<optional function>"}
-- opens the live NPC's dialogue without walking over. {"gold":500} grants gold by the reward function.
-- {"class":"<actor class>"} lists instances. {"reply":"<button text>"} presses a visible dialogue button.
-- With no arguments, lists the dialogue widget's shown text and reply buttons. {"unequip":"<item class path>"}
-- clicks one equipped item by its class, leaving the other equipment in place.
-- {"near":"<actor class>","name":"<optional actor name>","offset":{"X":0,"Y":0,"Z":150}} moves
-- the player beside that actor and turns the camera toward it, to skip the journey.
-- {"recipe":"<item name>"} invokes a recipe's bound row click. {"mounts":true} reads the Mounts list.
-- {"input":"IA_Interact","phase":1} invokes the live Enhanced Input handlers for that action and phase.
-- {"offer":"<item name>"} selects one item on the player side of an open Trade window. Empty text
-- selects a nameless item. {"mountState":true} reads the spawned mount and mounted flag.
-- {"tradeHover":"<name>"} invokes a Trade slot's hover and reads its tooltip.
-- {"status":true} reads the attached ongoing status widgets and their remaining times.
if args.status then
  local effects = {}
  for _, widget in ipairs(liveAll('OngoingStatusEffects_C')) do
    if widget:GetParent():IsValid() and not offScreen(widget) then
      effects[#effects + 1] = { name = widget['Status Effect Name']:ToString(),
        remaining = widget['Current Time'], text = texts(widget) }
    end
  end
  return { effects = effects }
end

if args.tradeHover then
  local window = assert(live('Window_Trading_Shop_C'), 'No live Trade window')
  local grid = window.PlayerInventoryGrid
  for index = 0, grid:GetChildrenCount() - 1 do
    local slot = grid:GetChildAt(index)
    if itemInfo(slot.Item, 'ItemName'):ToString() == args.tradeHover then
      slot['BndEvt__Trader_Inventory_Item_ItemButton_K2Node_ComponentBoundEvent_0_OnButtonHoverEvent__DelegateSignature'](slot)
      local tooltip = slot['Tooltip Ref']
      assert(tooltip:IsValid(), 'Trader hover did not create a tooltip')
      return { shown = texts(tooltip), visibility = tooltip:GetVisibility() }
    end
  end
  error('No item of that name in Trade')
end

if args.mountState then
  local pawn = requirePlayer()
  local mount = pawn['Spawned Mount']
  return { actor = mount:IsValid() and mount:GetFullName() or nil, mounted = pawn.Mounted,
    inventory = items(pawn.Inventory) }
end

if args.offer then
  local window = assert(live('Window_Trading_Shop_C'), 'No live Trade window')
  local grid = window.PlayerInventoryGrid
  for index = 0, grid:GetChildrenCount() - 1 do
    local slot = grid:GetChildAt(index)
    if itemInfo(slot.Item, 'ItemName'):ToString() == args.offer then
      slot['BndEvt__Trader_Inventory_Item_ItemButton_K2Node_ComponentBoundEvent_3_OnButtonPressedEvent__DelegateSignature'](slot)
      slot['BndEvt__Trader_Inventory_Item_ItemButton_K2Node_ComponentBoundEvent_2_OnButtonReleasedEvent__DelegateSignature'](slot)
      return { slot = slot:GetFullName(), gold = requirePlayer().Money }
    end
  end
  error('No item of that name in Trade')
end

if args.input then
  local phase = args.phase or 1
  local calls = {}
  for _, target in ipairs({ UEHelpers.GetPlayerController(), requirePlayer() }) do
    target:GetClass().DynamicBindingObjects:ForEach(function(_, element)
      local binding = element:get()
      if binding:GetClass():GetFName():ToString() ~= 'EnhancedInputActionDelegateBinding' then return end
      binding.InputActionDelegateBindings:ForEach(function(_, entry)
        local action = entry:get()
        if action.InputAction:IsValid() and action.InputAction:GetFName():ToString() == args.input
          and action.TriggerEvent == phase then
          local name = action.FunctionNameToBind:ToString()
          target[name](target, { Value = { X = phase < 3 and 1 or 0, Y = 0, Z = 0 }, ValueType = 0 }, 0, 0)
          calls[#calls + 1] = name
        end
      end)
    end)
  end
  return { calls = calls }
end

if args.recipe then
  local window
  for _, candidate in ipairs(liveAll('Crafting_C')) do
    if candidate:IsInViewport() then window = candidate end
  end
  assert(window, 'Anvil window is not open')
  local prefix = window:GetFName():ToString() .. '.'
  for _, class in ipairs({ 'UI_InventoryItem_TextOnly_C', 'UI_InventoryItem_4_C' }) do
    for _, slot in ipairs(liveAll(class)) do
      if slot:GetFullName():find(prefix, 1, true) and slot:GetParent():IsValid() and not offScreen(slot)
        and itemInfo(slot.ItemInfo, 'ItemName'):ToString() == args.recipe then
        return press(slot, 'ItemButton')
      end
    end
  end
  error('No matching anvil recipe: ' .. args.recipe)
end

if args.mounts then
  local window
  for _, candidate in ipairs(liveAll('MountSelection_C')) do
    if candidate:IsInViewport() then window = candidate end
  end
  assert(window, 'Mounts is not open')
  local list = window.Mount_ScrollBox
  local mounts = {}
  for index = 0, list:GetChildrenCount() - 1 do
    local slot = list:GetChildAt(index)
    local shown = texts(slot)
    mounts[#mounts + 1] = { slot = slot:GetFullName(), text = shown }
  end
  return { mounts = mounts, text = texts(window) }
end

if args.near then
  local actor = assert(live(args.near, args.name), 'No live actor: ' .. args.near)
  local pawn = requirePlayer()
  local goal = actor:K2_GetActorLocation()
  local offset = args.offset or { X = 0, Y = 0, Z = 150 }
  local position = { X = goal.X + offset.X, Y = goal.Y + offset.Y, Z = goal.Z + offset.Z }
  pawn:K2_SetActorLocation(position, false, {}, true)
  if offset.X ~= 0 or offset.Y ~= 0 then
    local mathLibrary = StaticFindObject('/Script/Engine.Default__KismetMathLibrary')
    UEHelpers.GetPlayerController():SetControlRotation(mathLibrary:FindLookAtRotation(position, goal))
  end
  return { actor = actor:GetFullName(), position = position }
end

if args.gold then
  local pawn = requirePlayer()
  pawn['Recieve Gold'](pawn, args.gold)
  return { gold = pawn.Money }
end

if args.unequip then
  local sheet = requirePlayer().CharacterSheetRef
  local slotClass = StaticFindObject('/Game/BloodlinesAlpha/UI/CharacterSheet/UI_EquipmentSlot.UI_EquipmentSlot_C')
  local target
  sheet:GetClass():ForEachProperty(function(property)
    if property:GetClass():GetFName():ToString() ~= 'ObjectProperty' then return end
    local slot = sheet[property:GetFName():ToString()]
    if slot and slot:IsValid() and slot:IsA(slotClass)
      and classPath(itemInfo(slot.ItemInfo, 'ItemClass')) == args.unequip then target = slot end
  end)
  return press(assert(target, 'Equipped item not found: ' .. args.unequip), 'EquipmentItemButton')
end

if args.talk then
  local npc = assert(live(args.talk, args.name), 'No live NPC: ' .. args.talk)
  local handler = args.handler or 'Force Talk'
  npc[handler](npc)
  return { actor = npc:GetFullName(), handler = handler }
end

if args.class then
  local actors = {}
  for _, actor in ipairs(liveAll(args.class)) do
    local entry = { actor = actor:GetFName():ToString(), class = actor:GetClass():GetFName():ToString() }
    pcall(function() entry.name = actor['Full Name']:ToString() end)
    local dialogue = actor.Dialogue
    if dialogue and dialogue:IsValid() then entry.dialogue = dialogue:GetFullName() end
    actors[#actors + 1] = entry
  end
  return { actors = actors }
end

local dialogue
for _, candidate in ipairs(liveAll('MyDialogueWidget_C')) do
  local parent = candidate:GetParent()
  if candidate:IsInViewport() or (parent:IsValid() and not offScreen(parent)) then dialogue = candidate end
end
dialogue = dialogue or assert(live('MyDialogueWidget_C'), 'No live dialogue')
if args.reply == 'Continue' then
  dialogue:SimulateNextClicked()
  return { continued = true }
end
if args.reply == 'Close' then
  dialogue['Close Dialogue'](dialogue)
  return { closed = true }
end
local replies = {}
for _, reply in ipairs(liveAll('MyReplyWidget_C')) do
  if reply:GetFullName():find(dialogue:GetFullName():match('([^%.]+)$') .. '.', 1, true)
    and reply:GetParent():IsValid() and not offScreen(reply) then
    local shown = texts(reply)
    replies[#replies + 1] = { index = reply.Index, text = shown }
    if args.reply then
      for _, line in pairs(shown) do
        if line:find(args.reply, 1, true) then
          reply:SimulateClick()
          return { reply = line, index = reply.Index }
        end
      end
    end
  end
end
if args.reply then error('No bound reply found: ' .. args.reply) end
table.sort(replies, function(a, b) return a.index < b.index end)
return { widget = dialogue:GetFullName(), text = texts(dialogue), replies = replies }
