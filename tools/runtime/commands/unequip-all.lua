-- Takes off everything the character wears and holds as a player does, by clicking each equipment slot of the
-- character sheet that holds an item, and fails when something stays on. The items return to the inventory. The
-- character sheet must be open.
local pawn = requirePlayer()
local sheet = pawn.CharacterSheetRef
if not (sheet and sheet:IsValid()) then error("The player has no character sheet. Open it first.") end
local slotClass = StaticFindObject("/Game/BloodlinesAlpha/UI/CharacterSheet/UI_EquipmentSlot.UI_EquipmentSlot_C")
local taken = {}
-- The sheet keeps each equipment slot in a variable of its own. Sheets that were replaced stay alive with slots
-- that show what was worn before, so only the player's own sheet is clicked.
sheet:GetClass():ForEachProperty(function(property)
  if property:GetClass():GetFName():ToString() ~= "ObjectProperty" then return end
  local slot = sheet[property:GetFName():ToString()]
  if not (slot and slot:IsValid() and slot:IsA(slotClass)) then return end
  local class = itemInfo(slot.ItemInfo, "ItemClass")
  if class and class:IsValid() then
    taken[#taken + 1] = classPath(class)
    press(slot, "EquipmentItemButton")
  end
end)
local left = items(pawn.Equipment)
if #left > 0 then
  local names = {}
  for _, entry in ipairs(left) do names[#names + 1] = entry.item end
  error("Still equipped after clicking every equipment slot: " .. table.concat(names, ", "))
end
return { taken = taken }
