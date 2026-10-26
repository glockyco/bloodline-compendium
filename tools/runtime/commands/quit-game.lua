-- Quits through the engine's QuitGame, as the menu's Quit does.
system:QuitGame(UEHelpers.GetWorld(), UEHelpers.GetPlayerController(), 0, false)
return { quitting = true }
