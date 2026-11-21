-- Finishes character creation with the default appearance, as pressing Create Character does. With
-- args.body = "female" it presses the Female button first, as a player choosing the female body does.
local creation = live("CharacterCreation1_C")
if not creation then error("Character creation is not open") end
if args.body == "female" then press(creation, "FemaleButton") end
return press(creation, "CreateCharacter")
