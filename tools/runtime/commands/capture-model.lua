-- lab setup: stands a copy of a weapon in the character preview to show it face-on
-- Renders an item's model alone with the colours of the character sheet's preview, the game's own picture of
-- equipment. The item is equipped through its inventory slot, and the preview, 3DMeshCapture_BP, then wears it.
-- Armor shows what the preview character wears for it, in the preview's pose, seen three quarters from the front:
-- the body parts that draw the meshes of its ArmorInfo, and its own actor where it hangs at a socket, such as a
-- helmet. A weapon's actor is copied in place, upright with its grip down, and seen face-on. The capture takes the
-- preview's light and its scene colour without tonemapping, which the sheet shows as it is.
-- Two captures go to args.directory: <args.file>-color.png and <args.file>-mask.png, the depth that marks the
-- subject's pixels. lab.mjs combines them into a transparent picture. The inventory must hold the item and the
-- character sheet must be open.
local world = UEHelpers.GetWorld()
local rendering = StaticFindObject("/Script/Engine.Default__KismetRenderingLibrary")
local math3 = StaticFindObject("/Script/Engine.Default__KismetMathLibrary")
local preview = live("3DMeshCapture_BP_C")
if not preview then error("The level has no character preview, 3DMeshCapture_BP") end
local itemClass = LoadAsset(args.item)
if not itemClass or not itemClass:IsValid() then error("Class " .. args.item .. " did not load") end
local armor = itemClass:GetCDO():IsA(LoadAsset("/Game/BloodlinesAlpha/Interactables/Armor/BP_BaseArmor.BP_BaseArmor_C"))
commands["equip"]({ item = args.item })

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

-- The preview character takes on what the player equips a few frames later, so the rest waits.
local finish = defer()
ExecuteInGameThreadAfterFrames(20, function()
  local ok, result = pcall(function()
    -- The item's own actor where the preview character carries it. Every equip attaches one more, so one of
    -- them stands for all.
    local carried
    local list = {}
    preview:GetAttachedActors(list, true)
    for _, entry in ipairs(list) do
      local actor = entry.get and entry:get() or entry
      if actor:GetClass():GetFullName() == itemClass:GetFullName() then carried = actor end
    end
    local visible = {}
    if carried then
      for _, component in ipairs(components(carried, "/Script/Engine.MeshComponent")) do
        if component:IsVisible() and meshOf(component) then visible[#visible + 1] = component end
      end
    end

    -- Every view starts from the way the preview character faces. Modular character meshes face their local +Y.
    -- The preview shows the character turned 27 degrees from its camera, so the camera's direction is no front.
    local body = preview.SkeletalMesh
    local up = { X = 0, Y = 0, Z = 1 }
    local bodyAt = body:K2_GetComponentLocation()
    local facing = body:GetRightVector()
    local front = math3:Normal({ X = facing.X, Y = facing.Y, Z = 0 }, 0.0001)

    local shown, meshes, actors = {}, {}, {}
    if armor then
      -- The meshes the game puts on the body come from the item's ArmorInfo: one mesh, or a left and a right
      -- one with their upper parts. A female body draws the female counterpart of each.
      local info = itemClass:GetCDO().ArmorInfo
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
        if mesh and wanted[mesh:GetFName():ToString()] then shown[#shown + 1] = part end
      end
      for _, component in ipairs(visible) do shown[#shown + 1] = component end
    else
      -- A weapon is copied, so it can stand upright facing the camera whatever socket holds it.
      -- The bounds of a mesh in its own space. A skeletal mesh, such as a crossbow with a moving string, gives
      -- the bounds of its reference pose.
      local function localBounds(component)
        local mesh = meshOf(component)
        if mesh:IsA(StaticFindObject("/Script/Engine.StaticMesh")) then
          local box = mesh:GetBoundingBox()
          return box.Min, box.Max
        end
        local bounds = mesh:GetBounds()
        return add(bounds.Origin, scale(bounds.BoxExtent, -1)), add(bounds.Origin, bounds.BoxExtent)
      end
      local source, size = nil, -1
      for _, component in ipairs(visible) do
        local low, high = localBounds(component)
        local extent = (high.X - low.X) + (high.Y - low.Y) + (high.Z - low.Z)
        if extent > size then source, size = component, extent end
      end
      if not source then error("The preview character carries no visible mesh of " .. args.item) end
      -- E_AttackType 8 is Crossbow, which rifles share, and 10 is Pistol. Firearms and crossbows lie on their
      -- side with the muzzle to the right. Every other weapon stands upright with the end farther from the mesh's
      -- origin, where the hand holds it, at the top. Either way the thinnest axis faces the camera, so the broad
      -- face or the side shows.
      local weaponType = itemInfo(itemClass:GetCDO().ItemInfo, "WeaponType")
      local lying = weaponType == 8 or weaponType == 10
      local low, high = localBounds(source)
      local names = { "X", "Y", "Z" }
      table.sort(names, function(a, b) return (high[a] - low[a]) > (high[b] - low[b]) end)
      local long, thin = names[1], names[3]
      local right = math3:Cross_VectorVector(up, scale(front, -1))
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
      local copyActor, copy
      if meshOf(source):IsA(StaticFindObject("/Script/Engine.StaticMesh")) then
        copyActor = spawn(StaticFindObject("/Script/Engine.StaticMeshActor"), bodyAt)
        copy = copyActor.StaticMeshComponent
        -- EComponentMobility 2 is Movable. A spawned static mesh actor is Static and refuses a new mesh.
        copy:SetMobility(2)
        copy:SetStaticMesh(source.StaticMesh)
      else
        copyActor = spawn(StaticFindObject("/Script/Engine.SkeletalMeshActor"), bodyAt)
        copy = copyActor.SkeletalMeshComponent
        copy:SetSkeletalMesh(source.SkeletalMesh, true)
      end
      actors[#actors + 1] = copyActor
      for index = 0, source:GetNumMaterials() - 1 do copy:SetMaterial(index, source:GetMaterial(index)) end
      copyActor:K2_SetActorRotation(math3:MakeRotationFromAxes(images.X, images.Y, images.Z), false)
      -- The copy's middle goes where the character's chest is, inside the preview's light.
      local origin, extent, radius = {}, {}, {}
      system:GetComponentBounds(copy, origin, extent, radius)
      local chest = add(bodyAt, { X = 0, Y = 0, Z = 120 })
      copyActor:K2_SetActorLocation(add(copyActor:K2_GetActorLocation(), add(chest, scale(origin, -1))), false, {}, false)
      shown[1] = copy
    end
    if #shown == 0 then error("The preview character shows nothing of " .. args.item) end
    for _, component in ipairs(shown) do meshes[#meshes + 1] = meshOf(component):GetFullName() end

    local low, high
    for _, component in ipairs(shown) do
      local origin, extent, radius = {}, {}, {}
      system:GetComponentBounds(component, origin, extent, radius)
      local a, b = add(origin, scale(extent, -1)), add(origin, extent)
      low = low and { X = math.min(low.X, a.X), Y = math.min(low.Y, a.Y), Z = math.min(low.Z, a.Z) } or a
      high = high and { X = math.max(high.X, b.X), Y = math.max(high.Y, b.Y), Z = math.max(high.Z, b.Z) } or b
    end
    local center = scale(add(low, high), 0.5)
    local radius = 0.5 * math.sqrt((high.X - low.X) ^ 2 + (high.Y - low.Y) ^ 2 + (high.Z - low.Z) ^ 2)

    local view
    if armor then
      -- Three quarters: the camera stands 25 degrees to the subject's left and 12 degrees above it, so the subject
      -- turns toward the viewer's right and its left side shows.
      local flat = rotate(front, up, -25)
      view = rotate(flat, math3:Cross_VectorVector(up, flat), -12)
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
    for _, actor in ipairs(actors) do actor:K2_DestroyActor() end
    local sky = live("Ultra_Dynamic_Sky_C")
    return { item = args.item, kind = armor and "worn" or "weapon", meshes = meshes, timeOfDay = sky and sky["Time of Day"] or nil }
  end)
  if ok then finish(result) else finish(nil, result) end
end)
