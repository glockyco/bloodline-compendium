-- Saves the game through the game instance's own Save Game, as the pause menu's save does, so a check can load a
-- level again and see what the save keeps.
local instance = live('BP_PersistantGameInstance_C')
instance['Save Game'](instance)
return { saved = true }
