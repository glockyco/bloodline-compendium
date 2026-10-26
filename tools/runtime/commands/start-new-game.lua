-- Starts a new game from the main menu, as typing a save name and pressing Start does.
local dialog = live("WBP_NewGame_C", "WidgetTree.NewGame")
if not dialog then error("The main menu's new game dialog is not loaded") end
dialog.InputText:SetText(FText(args.name))
return press(dialog, "Start_BTN")
