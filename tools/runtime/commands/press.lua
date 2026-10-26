-- Presses a widget's button by calling the function its class binds to the button's click.
local widget = live(args.widget, args.needle)
if not widget then error("No live " .. args.widget) end
return press(widget, args.button)
