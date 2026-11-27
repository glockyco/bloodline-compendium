-- Lists every live text block whose text contains one of args.names, with the widget that holds it and whether that
-- widget is on screen, so a run can tell which name the game shows for a person and where: over their head, in the
-- interaction prompt or in the dialogue window. Stand beside the person first, as design-facts.lua's "near" does.
local found = {}
for _, block in ipairs(FindAllOf('TextBlock') or {}) do
    if block:IsValid() then
        local ok, shown = pcall(function() return block.Text:ToString() end)
        if ok and shown then
            for _, name in ipairs(args.names) do
                if shown:find(name, 1, true) then
                    local outer = block:GetOuter()
                    local holder = outer:IsValid() and outer:GetOuter() or nil
                    found[#found + 1] = {
                        text = shown,
                        block = block:GetFName():ToString(),
                        widget = holder and holder:IsValid() and holder:GetFullName() or nil,
                        visible = block:IsVisible(),
                    }
                end
            end
        end
    end
end
return { found = found }
