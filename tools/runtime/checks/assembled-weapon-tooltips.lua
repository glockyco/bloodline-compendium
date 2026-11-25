-- lab setup: opens the hot-swap widget directly rather than pressing a hotbar assignment key.
-- The trade window must already be open for the trade read.
local pawn = requirePlayer()
local function describe(slot, info, tooltip)
  local text, hidden = texts(tooltip)
  return { item = itemInfo(info, 'ItemName'):ToString(), storedDamage = itemInfo(info, 'Damage'),
    slot = slot:GetFullName(), text = text, hidden = hidden }
end
local function place(tooltip)
  if args.capture then
    if not tooltip:IsInViewport() then tooltip:AddToViewport(100) end
    local position = args.action == 'trade' and { X = 780, Y = 230 } or { X = 450, Y = 20 }
    tooltip:SetPositionInViewport(position, true)
    _G.IssueTooltip = tooltip
  end
end
if args.action == 'clear' then
  if _G.IssueTooltip and _G.IssueTooltip:IsValid() then _G.IssueTooltip:RemoveFromParent() end
  _G.IssueTooltip = nil
  return {}
elseif args.action == 'inventory' then
  for _, slot in ipairs(inventorySlots()) do
    if itemInfo(slot.ItemInfo, 'ItemName'):ToString() == args.name then
      slot:OnMouseEnter({}, {})
      local tooltip = slot.ToolTipWidget
      assert(tooltip:IsValid(), 'hover did not create an inventory tooltip')
      place(tooltip)
      return describe(slot, slot.ItemInfo, tooltip)
    end
  end
  error('no inventory slot named ' .. args.name)
end
if args.action == 'trade' then
  local grid = assert(live('Window_Trading_Shop_C')).PlayerInventoryGrid
  local slots = {}
  for i = 0, grid:GetChildrenCount() - 1 do
    local slot = grid:GetChildAt(i)
    local name = itemInfo(slot.Item, 'ItemName'):ToString()
    slots[#slots + 1] = name
    if name == args.name then
      slot['BndEvt__Trader_Inventory_Item_ItemButton_K2Node_ComponentBoundEvent_0_OnButtonHoverEvent__DelegateSignature'](slot)
      local tooltip = slot['Tooltip Ref']
      assert(tooltip:IsValid(), 'hover did not create a trade tooltip')
      -- Trading's tooltip holder does not draw the programmatically hovered widget in off-screen mode.
      -- Move the same widget to the viewport for capture without changing its values or visibility.
      if args.capture then tooltip:RemoveFromParent() end
      place(tooltip)
      return describe(slot, slot.Item, tooltip)
    end
  end
  return { slots = slots }
end
local class = LoadAsset('/Game/Widgets/HotSwapItems.HotSwapItems_C')
local library = StaticFindObject('/Script/UMG.Default__WidgetBlueprintLibrary')
local window = library:Create(pawn, class, nil)
window:AddToViewport(0)
press(window, 'Item1Button')
local grid = window.UniformGridPanel_3
local slots = {}
for i = 0, grid:GetChildrenCount() - 1 do
  local slot = grid:GetChildAt(i)
  local name = itemInfo(slot.ItemInfo, 'ItemName'):ToString()
  slots[#slots + 1] = name
  if name == args.name then
    slot:OnMouseEnter({}, {})
    local tooltip = slot['Slot Tooltip']
    assert(tooltip:IsValid(), 'hover did not create a hot-swap tooltip')
    return describe(slot, slot.ItemInfo, tooltip)
  end
end
return { slots = slots }
