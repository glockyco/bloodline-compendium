-- lab setup: with args.open, creates the trading window with the live merchant whose name contains args.keeper as
-- its Owner, as the merchant's shop event does after its Delay, which needs a world the dialogue object lacks here.
-- Without it, lists what the open window's player side offers to sell: each slot's item class in the player's
-- grid, so a run can see which kinds of item a player can sell.
local pawn = requirePlayer()
if args.open then
  local keeper
  for _, candidate in ipairs(liveAll(args.class or "ShopKeeper_Base_C")) do
    if candidate:GetFullName():find(args.keeper or "", 1, true) then keeper = candidate end
  end
  if not keeper then error("no merchant " .. (args.keeper or "")) end
  LoadAsset("/Game/fantasy_gui_4/widgets/sample_windows/Window_Trading_Shop")
  local windowClass = StaticFindObject("/Game/fantasy_gui_4/widgets/sample_windows/Window_Trading_Shop.Window_Trading_Shop_C")
  local library = StaticFindObject("/Script/UMG.Default__WidgetBlueprintLibrary")
  local window = library:Create(pawn, windowClass, nil)
  window.Owner = keeper
  window:AddToViewport(0)
  return { keeper = keeper:GetFullName(), money = pawn.Money }
end
local window = live("Window_Trading_Shop_C")
if not window then error("no trading window") end
local slots = {}
local grid = window.PlayerInventoryGrid
for index = 0, grid:GetChildrenCount() - 1 do
  local child = grid:GetChildAt(index)
  local entry = { widget = child:GetClass():GetFName():ToString() }
  pcall(function() entry.name = itemInfo(child.Item, "ItemName"):ToString() end)
  pcall(function() entry.value = itemInfo(child.Item, "ItemValue") end)
  slots[#slots + 1] = entry
end
return { slots = slots, inventory = items(pawn.Inventory) }
