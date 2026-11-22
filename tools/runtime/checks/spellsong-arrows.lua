-- lab setup: adds a spellsong note sheet widget to the screen and changes nothing in the game's state
-- Lists the arrows the game draws for a spellsong's tune: a Spellsong_Notes widget gets the tune args.tune, such as
-- "1,2,4,3", and builds its own arrows on construction, and each arrow is listed in order with its Direction and icon
-- texture.
-- Run it with a character in the world: lab.mjs eval tools/runtime/checks/spellsong-arrows.lua '{"tune":"1,2,3,4"}'
local pawn = requirePlayer()
local library = StaticFindObject("/Script/UMG.Default__WidgetBlueprintLibrary")
local class = LoadAsset("/Game/BloodlinesAlpha/Interactables/Weapons/Lutes/Spellsong_Notes.Spellsong_Notes_C")
local notes = library:Create(pawn, class, UEHelpers.GetPlayerController())
notes.Spellsong = FString(args.tune)
notes:AddToViewport(60)
local finish = defer()
ExecuteInGameThreadAfterFrames(10, function()
  local arrows = {}
  local box = notes.HorizontalBox_1
  for index = 0, box:GetChildrenCount() - 1 do
    local arrow = box:GetChildAt(index)
    local brush = arrow.ArrowImage and arrow.ArrowImage.Brush
    local texture = brush and brush.ResourceObject
    arrows[#arrows + 1] = {
      direction = arrow.Direction,
      icon = texture and texture:IsValid() and texture:GetFName():ToString() or nil,
    }
  end
  notes:RemoveFromParent()
  finish({ check = "spellsong-arrows", tune = args.tune, arrows = arrows })
end)
