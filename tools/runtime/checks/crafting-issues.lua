-- lab setup: opens the anvil directly. The assembly check sets Weaponsmithing to 5 and
-- calls two successful hammer inputs instead of timing them. Both paths use the live crafting buttons.
local pawn = requirePlayer()
if args.action == 'open' then
  local anvil = assert(live('Anvil_Workbench_C'), 'no anvil in this level')
  anvil['Open Weaponcrafting Menu'](anvil)
  return { anvil = anvil:GetFullName() }
end
if args.action == 'inventory' then
  local rows = {}
  pawn.Inventory:ForEach(function(_, entry)
    local info = entry:get()
    local item = classPath(itemInfo(info, 'ItemClass'))
    if item then
      rows[#rows + 1] = {
        item = item, name = itemInfo(info, 'ItemName'):ToString(),
        description = itemInfo(info, 'Description'):ToString(), quantity = itemInfo(info, 'Quantity'),
        damage = itemInfo(info, 'Damage'), value = itemInfo(info, 'ItemValue'),
      }
    end
  end)
  return { inventory = rows }
end
local window
for _, candidate in ipairs(liveAll('Crafting_C')) do
  if candidate:IsInViewport() then window = candidate end
end
assert(window, 'no crafting window in viewport')
if args.action == 'select' then
  window['GoToWeaponCrafting'](window)
  local claymore = LoadAsset('/Game/BloodlinesAlpha/Interactables/Weapons/Copper1HWeapons/CopperClaymore.CopperClaymore_C')
  window['Update Selected Weapon'](window, claymore)
  return { row = window['Data Table Row NameT']:ToString(), enabled = window.AutoCraftButton:GetIsEnabled() }
elseif args.action == 'auto' then
  return press(window, 'AutoCraftButton')
elseif args.action == 'close' then
  return press(window, 'CloseButton')
elseif args.action == 'assemble' then
  local instance = assert(live('BP_PersistantGameInstance_C'))
  instance['Weaponsmithing Level'] = 5
  window['GoToWeaponAssembly'](window)
  window.WeaponAssemblyWeaponClass_Dropdown:SetSelectedIndex(3)
  window.WeaponPartDropdown1:SetSelectedIndex(0)
  window.WeaponPartDropdown4:SetSelectedIndex(0)
  assert(window.WeaponPartDropdown1.RowName:ToString() == 'PinePolearmShaft')
  assert(window.WeaponPartDropdown4.RowName:ToString() == 'SmallCopperSpearhead')
  window.WeaponName_Input:SetText(FText(args.name))
  local result = press(window, 'CraftButtonWeaponAssembly')
  commands['close-tutorials']()
  return result
elseif args.action == 'hit' then
  window['Successful Hit'](window)
  return { bar = window['Blacksmithing Minigame Ref'].QualityBar.Percent }
elseif args.action == 'finish' then
  local grade = window['Blacksmithing Minigame Ref'].Quality:ToString()
  window['End Crafting'](window)
  return { grade = grade }
end
error('unknown action: ' .. tostring(args.action))
