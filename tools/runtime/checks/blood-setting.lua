-- lab setup: opens the video settings window, picks a Blood Levels option and grants Blood Magic experience
-- Opens the game's video settings window, picks the Blood Levels option at args.index (0 No blood to 4 BLOODBATH)
-- through the box's own selection, which runs the window's handler, then grants Blood Magic exactly the experience
-- of one level through the game's tracking. Lists the options, the Blood Level the handler stored, and Blood Magic's
-- level and points before and after.
-- Run it with a character in the world: lab.mjs eval tools/runtime/checks/blood-setting.lua '{"index":0}'
local pawn = requirePlayer()
local gi = live("BP_PersistantGameInstance_C")
local component = pawn.PlayerPassiveLevelStatComponent
local library = StaticFindObject("/Script/UMG.Default__WidgetBlueprintLibrary")
local class = LoadAsset("/Game/fantasy_gui_4/widgets/templates/popup/SettingsWindow1.SettingsWindow1_C")
local window = library:Create(pawn, class, UEHelpers.GetPlayerController())
window:AddToViewport(50)
local function state()
  return { blood = gi["Blood Level"], level = gi["Blood Magic Level"], points = gi["Bloodmagic Points"] }
end
local finish = defer()
ExecuteInGameThreadAfterFrames(20, function()
  local box = window.Blood
  local options = {}
  for i = 0, box:GetOptionCount() - 1 do options[#options + 1] = box:GetOptionAtIndex(i):ToString() end
  local before = state()
  box:SetSelectedIndex(args.index)
  ExecuteInGameThreadAfterFrames(10, function()
    local stored = gi["Blood Level"]
    window:RemoveFromParent()
    component["Blood Magic Level Tracking"](component, component["Required Blood Magic EXP"] + 1)
    ExecuteInGameThreadAfterFrames(10, function()
      finish({
        options = options,
        selected = box:GetSelectedOption():ToString(),
        stored = stored,
        before = before,
        after = state(),
      })
    end)
  end)
end)
