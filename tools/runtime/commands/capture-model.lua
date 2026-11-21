-- lab setup: stands an item's own actor or a copy of its mesh in the character preview to show it face-on
-- Renders an item's model alone with the colours of the character sheet's preview, the game's own picture of
-- equipment. The subject depends on what the game draws for the item:
-- - Armor is equipped through its inventory slot, and the preview character wears it. The render shows the body
--   parts that draw the meshes of its ArmorInfo and its own actor where it hangs at a socket, such as a helmet, in
--   the preview's pose, seen three quarters from the front. A female character shows the female meshes. Armor the
--   preview does not draw, such as a belt with its own meshes, shows as an object.
-- - A weapon is its own actor, as the hand carries it, standing upright with its grip down and seen face-on.
--   Firearms and crossbows lie on their side with the muzzle to the right.
-- - A weapon part draws the mesh, materials and scale its row in WeaponComponentTable names, given as args.part,
--   and stands like a weapon.
-- - Any other item is its own actor, as it lies in the world, seen three quarters from above.
-- The capture takes the preview's light and its scene colour without tonemapping, which the sheet shows as it is.
-- Two captures go to args.directory: <args.file>-color.png and <args.file>-mask.png, the depth that marks the
-- subject's pixels. lab.mjs combines them into a transparent picture. Armor must be in the inventory, and the
-- character sheet must be open.
local world = UEHelpers.GetWorld()
local rendering = StaticFindObject("/Script/Engine.Default__KismetRenderingLibrary")
local math3 = StaticFindObject("/Script/Engine.Default__KismetMathLibrary")
local preview = live("3DMeshCapture_BP_C")
if not preview then error("The level has no character preview, 3DMeshCapture_BP") end
local itemClass = LoadAsset(args.item)
if not itemClass or not itemClass:IsValid() then error("Class " .. args.item .. " did not load") end
local defaults = itemClass:GetCDO()
local armor = defaults:IsA(LoadAsset("/Game/BloodlinesAlpha/Interactables/Armor/BP_BaseArmor.BP_BaseArmor_C"))
-- E_AttackType 0 is None: an item without a weapon type is no weapon.
local weaponType = itemInfo(defaults.ItemInfo, "WeaponType")
-- args.subject = "object" shows armor as its own actor, for armor that the preview wears without drawing it.
local subject = args.part and "part" or args.subject == "object" and "object" or armor and "worn"
  or (weaponType and weaponType ~= 0) and "carried" or "object"
if subject == "worn" then commands["equip"]({ item = args.item }) end

local function scale(v, s) return { X = v.X * s, Y = v.Y * s, Z = v.Z * s } end
local function add(a, b) return { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z } end
local function rotate(v, axis, degrees) return math3:RotateAngleAxis(v, degrees, axis) end
local function spawn(class, location)
  local transform = { Rotation = { X = 0, Y = 0, Z = 0, W = 1 }, Translation = location, Scale3D = { X = 1, Y = 1, Z = 1 } }
  local actor = statics:BeginDeferredActorSpawnFromClass(world, class, transform, 1, nil)
  statics:FinishSpawningActor(actor, transform)
  return actor
end
local function components(actor, class)
  local found = {}
  for _, entry in ipairs(actor:K2_GetComponentsByClass(StaticFindObject(class))) do
    found[#found + 1] = entry.get and entry:get() or entry
  end
  return found
end
local function meshOf(component)
  local mesh
  pcall(function() mesh = component.StaticMesh end)
  if not (mesh and mesh:IsValid()) then pcall(function() mesh = component.SkeletalMesh end) end
  return mesh and mesh:IsValid() and mesh or nil
end
local function visibleMeshes(actor)
  local visible = {}
  for _, component in ipairs(components(actor, "/Script/Engine.MeshComponent")) do
    if component:IsVisible() and not component.bHiddenInGame and meshOf(component) then visible[#visible + 1] = component end
  end
  return visible
end
-- The bounds of a mesh in its own space, scaled. A skeletal mesh, such as a crossbow with a moving string, gives the
-- bounds of its reference pose.
local function localBounds(mesh, size)
  local low, high
  if mesh:IsA(StaticFindObject("/Script/Engine.StaticMesh")) then
    local box = mesh:GetBoundingBox()
    low, high = box.Min, box.Max
  else
    local bounds = mesh:GetBounds()
    low, high = add(bounds.Origin, scale(bounds.BoxExtent, -1)), add(bounds.Origin, bounds.BoxExtent)
  end
  size = size or { X = 1, Y = 1, Z = 1 }
  return { X = low.X * size.X, Y = low.Y * size.Y, Z = low.Z * size.Z }, { X = high.X * size.X, Y = high.Y * size.Y, Z = high.Z * size.Z }
end
-- The world box around a list of components, from the engine's bounds. A skeletal part's bounds can reach far
-- beyond what it draws, as a pauldron's do, so lab.mjs refines the frame from the first capture where needed.
local function boundsOf(list)
  local low, high
  for _, component in ipairs(list) do
    local origin, extent, radius = {}, {}, {}
    system:GetComponentBounds(component, origin, extent, radius)
    local a, b = add(origin, scale(extent, -1)), add(origin, extent)
    low = low and { X = math.min(low.X, a.X), Y = math.min(low.Y, a.Y), Z = math.min(low.Z, a.Z) } or a
    high = high and { X = math.max(high.X, b.X), Y = math.max(high.Y, b.Y), Z = math.max(high.Z, b.Z) } or b
  end
  return low, high
end

-- The preview character takes on what the player equips a few frames later, so the rest waits. Worn armor is then
-- shown in the body's rest pose, as the meshes were made, because the preview's idle animation bends the arms and
-- hunches the torso: EAnimationMode 1, a single animation node without an animation, holds the rest pose, and the
-- worn parts follow the body's pose. The preview's own animation returns afterwards.
local finish = defer()
local function capture()
  local ok, result = pcall(function()
    -- Every view starts from the way the preview character faces. Modular character meshes face their local +Y.
    -- The preview shows the character turned 27 degrees from its camera, so the camera's direction is no front.
    local body = preview.SkeletalMesh
    local up = { X = 0, Y = 0, Z = 1 }
    local bodyAt = body:K2_GetComponentLocation()
    local facing = body:GetRightVector()
    local front = math3:Normal({ X = facing.X, Y = facing.Y, Z = 0 }, 0.0001)
    local right = math3:Cross_VectorVector(up, scale(front, -1))
    -- Subjects that are not worn stand where the character's chest is, inside the preview's light.
    local chest = add(bodyAt, { X = 0, Y = 0, Z = 120 })
    local function centre(actor, list)
      local low, high = boundsOf(list)
      actor:K2_SetActorLocation(add(actor:K2_GetActorLocation(), add(chest, scale(add(low, high), -0.5))), false, {}, false)
    end

    -- The world directions that a mesh's local X, Y and Z should take so it stands upright with the end farther
    -- from its origin, where the hand holds it, at the top, or lies on its side with that end to the right. Its
    -- thinnest axis faces the camera, so the broad face of a blade and the curve of a bow show.
    local function standing(mesh, size, lying)
      local low, high = localBounds(mesh, size)
      local names = { "X", "Y", "Z" }
      table.sort(names, function(a, b) return (high[a] - low[a]) > (high[b] - low[b]) end)
      local long, thin = names[1], names[3]
      local along = lying and right or up
      local function axes(face)
        local images = { [long] = high[long] >= -low[long] and along or scale(along, -1), [thin] = face }
        -- Local X, Y and Z keep their handedness in the turned copy: X = Y x Z, Y = Z x X, Z = X x Y.
        if not images.X then images.X = math3:Cross_VectorVector(images.Y, images.Z)
        elseif not images.Y then images.Y = math3:Cross_VectorVector(images.Z, images.X)
        else images.Z = math3:Cross_VectorVector(images.X, images.Y) end
        return images
      end
      local images = axes(front)
      -- A lying weapon keeps the mesh's own top up, so it shows the other side when that would turn it over.
      if lying and long ~= "Z" and thin ~= "Z" and math3:Dot_VectorVector(images.Z, up) < 0 then images = axes(scale(front, -1)) end
      return images
    end

    local shown, meshes, actors = {}, {}, {}
    local function asObject()
      -- The item's own actor, as it lies in the world, facing the way the character faces.
      local actor = spawn(itemClass, bodyAt)
      actors[#actors + 1] = actor
      actor:K2_SetActorRotation(math3:MakeRotFromXZ(front, up), false)
      local visible = visibleMeshes(actor)
      if #visible == 0 then error("The item's own actor draws no mesh: " .. args.item) end
      centre(actor, visible)
      for _, component in ipairs(visible) do shown[#shown + 1] = component end
    end

    if subject == "worn" then
      -- The meshes the game puts on the body come from the item's ArmorInfo: one mesh, or a left and a right
      -- one with their upper parts. A female body draws the female counterpart of each.
      local info = defaults.ArmorInfo
      local wanted = {}
      for _, name in ipairs({ "SkeletalMesh", "SkeletalMeshL", "SkeletalMeshLU", "SkeletalMeshR", "SkeletalMeshRU" }) do
        local mesh = field(info, "/Game/BloodlinesAlpha/Interactables/Armor/F_ArmorInfo.F_ArmorInfo", name)
        if mesh and mesh:IsValid() then
          local meshName = mesh:GetFName():ToString()
          wanted[meshName] = true
          wanted[(meshName:gsub("_Male_", "_Female_"))] = true
        end
      end
      for _, part in ipairs(components(preview, "/Script/Engine.SkeletalMeshComponent")) do
        local mesh = meshOf(part)
        if mesh and wanted[mesh:GetFName():ToString()] and part:IsVisible() then shown[#shown + 1] = part end
      end
      -- The item's own actor where the preview character carries it at a socket, such as a helmet or pauldrons.
      -- Every equip attaches one more, and a copy attached last may still be scaled down to almost nothing, so the
      -- largest copy stands for all.
      local list = {}
      preview:GetAttachedActors(list, true)
      local carried, largest = nil, -1
      for _, entry in ipairs(list) do
        local actor = entry.get and entry:get() or entry
        if actor:GetClass():GetFullName() == itemClass:GetFullName() then
          local size = math.abs(actor:GetActorScale3D().X)
          for _, component in ipairs(visibleMeshes(actor)) do size = math.max(size, math.abs(component:K2_GetComponentScale().X)) end
          if size > largest then carried, largest = actor, size end
        end
      end
      for _, component in ipairs(carried and visibleMeshes(carried) or {}) do shown[#shown + 1] = component end
      if #shown == 0 then
        subject = "object"
        asObject()
      end
    elseif subject == "carried" then
      local actor = spawn(itemClass, bodyAt)
      actors[#actors + 1] = actor
      local visible = visibleMeshes(actor)
      if #visible == 0 then error("The item's own actor draws no mesh: " .. args.item) end
      -- The largest mesh decides how the weapon stands, and the actor turns so that mesh's axes take their places.
      local source, extentSum = nil, -1
      for _, component in ipairs(visible) do
        local low, high = localBounds(meshOf(component))
        local sum = (high.X - low.X) + (high.Y - low.Y) + (high.Z - low.Z)
        if sum > extentSum then source, extentSum = component, sum end
      end
      -- E_AttackType 8 is Crossbow, which rifles share, 9 is Gun and 10 is Pistol.
      local images = standing(meshOf(source), nil, weaponType == 8 or weaponType == 9 or weaponType == 10)
      -- The actor stands unturned, so the mesh's axes in world space are its axes within the actor.
      local inActor = { X = source:GetForwardVector(), Y = source:GetRightVector(), Z = source:GetUpVector() }
      local function image(axis)
        local v = { X = 0, Y = 0, Z = 0 }
        for _, name in ipairs({ "X", "Y", "Z" }) do v = add(v, scale(images[name], inActor[name][axis])) end
        return v
      end
      actor:K2_SetActorRotation(math3:MakeRotationFromAxes(image("X"), image("Y"), image("Z")), false)
      centre(actor, visible)
      for _, component in ipairs(visible) do shown[#shown + 1] = component end
    elseif subject == "part" then
      local mesh = LoadAsset(args.part.mesh)
      if not mesh or not mesh:IsValid() then error("Mesh " .. args.part.mesh .. " did not load") end
      local copyActor = spawn(StaticFindObject("/Script/Engine.StaticMeshActor"), bodyAt)
      actors[#actors + 1] = copyActor
      local copy = copyActor.StaticMeshComponent
      -- EComponentMobility 2 is Movable. A spawned static mesh actor is Static and refuses a new mesh.
      copy:SetMobility(2)
      copy:SetStaticMesh(mesh)
      -- LoadAsset returns more than the object, so the call keeps only its first value.
      for index, path in ipairs(args.part.materials or {}) do copy:SetMaterial(index - 1, (LoadAsset(path))) end
      if args.part.scale then copy:SetWorldScale3D(args.part.scale) end
      local images = standing(mesh, args.part.scale, false)
      copyActor:K2_SetActorRotation(math3:MakeRotationFromAxes(images.X, images.Y, images.Z), false)
      centre(copyActor, { copy })
      shown[1] = copy
    else
      asObject()
    end
    if #shown == 0 then error("The preview character shows nothing of " .. args.item) end
    for _, component in ipairs(shown) do meshes[#meshes + 1] = meshOf(component):GetFullName() end

    -- The frame is the subject's bounds, or the centre and radius that lab.mjs measured on an earlier capture.
    local center, radius
    if args.frame then
      center, radius = args.frame.center, args.frame.radius
    else
      local low, high = boundsOf(shown)
      center = scale(add(low, high), 0.5)
      radius = 0.5 * math.sqrt((high.X - low.X) ^ 2 + (high.Y - low.Y) ^ 2 + (high.Z - low.Z) ^ 2)
    end

    local view
    if subject == "worn" then
      -- Three quarters: the camera stands 25 degrees to the subject's left and 12 degrees above it, so the subject
      -- turns toward the viewer's right and its left side shows. A piece worn on the back, a Support item at a
      -- spine socket such as wings or a pack, is seen from behind, as the player sees it on others.
      local attach = field(defaults.ArmorInfo, "/Game/BloodlinesAlpha/Interactables/Armor/F_ArmorInfo.F_ArmorInfo", "AttachPoint")
      local onBack = itemInfo(defaults.ItemInfo, "ItemSlot") == 9 and attach and attach:ToString():find("^spine") ~= nil
      local flat = rotate(onBack and scale(front, -1) or front, up, -25)
      view = rotate(flat, math3:Cross_VectorVector(up, flat), -12)
    elseif subject == "object" then
      -- An object lying in the world is seen from 25 degrees to its left and 35 degrees above, so its top shows.
      local flat = rotate(front, up, -25)
      view = rotate(flat, math3:Cross_VectorVector(up, flat), -35)
    else
      -- Face-on, turned 15 degrees so the blade's thickness shows.
      view = rotate(front, up, 15)
    end
    -- A narrow perspective keeps proportions close to a flat view while the depth still reads.
    local fov = 25
    local capture = spawn(StaticFindObject("/Script/Engine.SceneCapture2D"), add(center, scale(view, radius / math.sin(math.rad(fov / 2)) * 1.05)))
    actors[#actors + 1] = capture
    capture:K2_SetActorRotation(math3:MakeRotFromXZ(scale(view, -1), up), false)
    local component = capture.CaptureComponent2D
    component.ProjectionType = 0
    component.FOVAngle = fov
    -- ESceneCapturePrimitiveRenderMode 2 renders only the listed components, without the body and the backdrop.
    component.PrimitiveRenderMode = 2
    for _, shownComponent in ipairs(shown) do component:ShowOnlyComponent(shownComponent) end
    component.bCaptureEveryFrame = false
    local size = args.size or 2048
    -- ESceneCaptureSource 0 is the scene colour that the preview captures, 5 the device depth, which is 0 only
    -- where nothing was drawn. ETextureRenderTargetFormat 3, RGBA8 in sRGB, stores the scene colour as the sheet
    -- shows it.
    for _, pass in ipairs({ { "color", 0 }, { "mask", 5 } }) do
      local target = rendering:CreateRenderTarget2D(world, size, size, 3, { R = 0, G = 0, B = 0, A = 0 }, false)
      component.TextureTarget = target
      component.CaptureSource = pass[2]
      component:CaptureScene()
      rendering:ExportRenderTarget(world, target, args.directory, args.file .. "-" .. pass[1] .. ".png")
    end
    -- The frame, so lab.mjs can turn the subject's pixels in the capture back into a centre and a radius.
    local frame = {
      center = center,
      radius = radius,
      halfWidth = radius / math.sin(math.rad(fov / 2)) * 1.05 * math.tan(math.rad(fov / 2)),
      right = capture:GetActorRightVector(),
      up = capture:GetActorUpVector(),
    }
    for _, actor in ipairs(actors) do actor:K2_DestroyActor() end
    local sky = live("Ultra_Dynamic_Sky_C")
    -- The preview character's own body mesh tells which body it shows.
    local bodyMesh = meshOf(body)
    local female = bodyMesh and bodyMesh:GetFName():ToString():find("Female", 1, true) ~= nil
    return {
      item = args.item,
      kind = subject,
      body = female and "female" or "male",
      meshes = meshes,
      timeOfDay = sky and sky["Time of Day"] or nil,
      frame = frame,
    }
  end)
  if subject == "worn" then preview.SkeletalMesh:SetAnimationMode(0) end
  if ok then finish(result) else finish(nil, result) end
end
ExecuteInGameThreadAfterFrames(20, function()
  if subject ~= "worn" then return capture() end
  preview.SkeletalMesh:SetAnimationMode(1)
  ExecuteInGameThreadAfterFrames(5, capture)
end)
