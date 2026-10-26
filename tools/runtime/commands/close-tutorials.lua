-- Closes every tutorial popup on screen with its Close button, as a player does. Tutorials pause the game.
local closed = {}
for _, popup in ipairs(liveAll("Tutorial_Popup_C")) do
  if popup:IsInViewport() then closed[#closed + 1] = press(popup, "Close_BTN") end
end
return { closed = closed, paused = statics:IsGamePaused(UEHelpers.GetWorld()) }
