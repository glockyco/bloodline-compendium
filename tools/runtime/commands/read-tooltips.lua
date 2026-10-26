-- Reads each inventory item's tooltip as the game builds it on hover: calls the slot's own mouse enter and leave
-- handlers and reads the shown text of every text block in the tooltip. The character sheet must be open.
local rows = {}
for _, slot in ipairs(inventorySlots()) do
  local class = itemInfo(slot.ItemInfo, "ItemClass")
  if class and class:IsValid() then
    slot:OnMouseEnter({}, {})
    local tooltip = slot.ToolTipWidget
    local row = { item = classPath(class) }
    if tooltip and tooltip:IsValid() then
      row.widget = tooltip:GetClass():GetFName():ToString()
      row.text, row.hidden = texts(tooltip)
    end
    slot:OnMouseLeave({})
    rows[#rows + 1] = row
  end
end
return rows
