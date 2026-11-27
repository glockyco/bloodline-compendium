-- Lists the name each live name widget shows over a person's head, by calling the widget's own text binding, so a run
-- can compare it with the name the dialogue window gives the same person. args.classes overrides the name widget
-- classes. The widgets exist for the people of the loaded level.
local found = {}
for _, class in ipairs(args.classes or { 'VitalNPCName_C', 'ShopkeeperName_C', 'CivilianName_C', 'TownCrierNameWidget_C' }) do
    for _, widget in ipairs(liveAll(class)) do
        local ok, shown = pcall(function() return widget['Get_Name_Text_1'](widget):ToString() end)
        found[#found + 1] = { class = class, widget = widget:GetFullName(), name = ok and shown or nil }
    end
end
return { found = found }
