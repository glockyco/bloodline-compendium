-- Finishes character creation with the default appearance, as pressing Create Character does.
local creation = live("CharacterCreation1_C")
if not creation then error("Character creation is not open") end
return press(creation, "CreateCharacter")
