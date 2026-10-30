-- lab setup: shows the tooltips of all inventory slots at once, which a player sees one at a time
-- Builds each shown slot's tooltip as hovering does, through the slot's own mouse enter handler, and places the
-- tooltips in the viewport four to a row, so one screenshot shows several as the game draws them. A run in a game
-- window showed the same tooltips as hovering with the real cursor. The character sheet must be open. Returns the
-- items in placement order, left to right and top to bottom.
local placed = {}
for index, slot in ipairs(inventorySlots()) do
  slot:OnMouseEnter({}, {})
  local tooltip = slot.ToolTipWidget
  if tooltip and tooltip:IsValid() then
    local column = #placed % 4
    local row = math.floor(#placed / 4)
    tooltip:AddToViewport(100)
    tooltip:SetPositionInViewport({ X = 20 + column * 395, Y = 20 + row * 440 }, true)
    placed[#placed + 1] = classPath(itemInfo(slot.ItemInfo, "ItemClass"))
  end
end
return { placed = placed }
