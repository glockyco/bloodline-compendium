-- lab setup: reads how the first-person body holds what is equipped, without changing anything
-- Lists each item a hand of the player's first-person body holds with its socket and, in the item's own frame,
-- the direction the player looks and the direction that is up, each also as the nearest axis, and the place of
-- the player's eye. The game attaches an item to its socket without turning it, so these are directions in the
-- socket's frame, by which capture-model.lua turns firearms, crossbows, bows and shields. A shield's front is the
-- side away from the eye: an eye at a negative Y sees the shield's back, so its front faces +Y.
-- Equip the items first, in a game launched in render mode, because a body the game does not draw keeps its rest
-- pose. A shield's frame is read with a one-handed weapon or a spear in the other hand, because a shield alone
-- hangs at the side.
local pawn = requirePlayer()
local math3 = StaticFindObject("/Script/Engine.Default__KismetMathLibrary")
local camera = UEHelpers.GetPlayerController().PlayerCameraManager
local rotation = camera:GetCameraRotation()
local function rounded(v)
  return { X = math.floor(v.X * 100 + 0.5) / 100, Y = math.floor(v.Y * 100 + 0.5) / 100, Z = math.floor(v.Z * 100 + 0.5) / 100 }
end
local function nearest(v)
  local name = "X"
  for _, axis in ipairs({ "Y", "Z" }) do if math.abs(v[axis]) > math.abs(v[name]) then name = axis end end
  return (v[name] < 0 and "-" or "+") .. name
end
local held = {}
local attached = {}
pawn:GetAttachedActors(attached, true)
for _, entry in ipairs(attached) do
  local actor = entry.get and entry:get() or entry
  local root = actor:K2_GetRootComponent()
  local parent = root:GetAttachParent()
  local slot
  pcall(function() slot = itemInfo(actor.ItemInfo, "ItemSlot") end)
  -- CharacterMesh0 is the first-person body. The third-person body holds a copy of each item at its own sockets.
  -- E_SlotType 6 is Primary, 7 Secondary and 8 Shield. Worn armor hangs on the same body and is left out.
  if parent and parent:IsValid() and parent:GetFName():ToString() == "CharacterMesh0" and (slot == 6 or slot == 7 or slot == 8) then
    local transform = actor:GetTransform()
    local looks = math3:InverseTransformDirection(transform, math3:GetForwardVector(rotation))
    local up = math3:InverseTransformDirection(transform, math3:GetUpVector(rotation))
    local eye = math3:InverseTransformLocation(transform, camera:GetCameraLocation())
    held[#held + 1] = {
      item = classPath(actor:GetClass()),
      socket = root:GetAttachSocketName():ToString(),
      turned = root.RelativeRotation.Pitch ~= 0 or root.RelativeRotation.Yaw ~= 0 or root.RelativeRotation.Roll ~= 0,
      looks = rounded(looks),
      looksAlong = nearest(looks),
      up = rounded(up),
      upAlong = nearest(up),
      eye = rounded(eye),
    }
  end
end
return { held = held }
