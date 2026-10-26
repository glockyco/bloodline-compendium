-- Reads the shown text of every text block that a live widget declares, such as the character sheet's armor and
-- mobility lines. args.class names the widget class, args.needle narrows by full name.
local widget = live(args.class, args.needle)
if not widget then error("No live " .. args.class) end
local shown, hidden = texts(widget)
return { widget = widget:GetFullName(), text = shown, hidden = hidden }
