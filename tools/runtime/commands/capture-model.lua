-- lab setup: stands an item's own actor or a copy of its mesh in the character preview to show it face-on
-- Renders an item's model alone with the colours of the character sheet's preview, the game's own picture of
-- equipment. The subject depends on what the game draws for the item:
-- - Armor is equipped through its inventory slot, and the preview character wears it. The render shows the body
--   parts that draw the meshes of its ArmorInfo and its own actor where it hangs at a socket, such as a helmet, in
--   the preview's pose, seen three quarters from the front. A female character shows the female meshes. Armor the
--   preview does not draw, such as a belt with its own meshes, shows as an object.
-- - What a hand holds is its own actor, turned by its largest mesh and seen face-on with its thinnest side toward
--   the camera. A weapon stands with the end farther from the hand up, and a bow with its back to the left. A
--   firearm lies on its side and a crossbow on its back, both with the muzzle to the left. A shield shows its
--   front. The item's actor is built in the frame of the socket that holds it, so the socket tells which way the
--   item points and which side is up.
-- - A weapon part draws the mesh, materials and scale its row in WeaponComponentTable names, given as args.part,
--   and stands as it sits in a weapon that stands with its tip up.
-- - Any other item is its own actor, as it lies in the world, seen three quarters from above.
-- The capture takes the preview's light and its scene colour without tonemapping, which the sheet shows as it is.
-- Two captures go to args.directory: <args.file>-color.png and <args.file>-mask.png, the depth that marks the
-- subject's pixels. lab.mjs combines them into a transparent picture. Armor must be in the inventory of a character
-- that wears nothing, and the character sheet must be open.
local world = UEHelpers.GetWorld()
local rendering = StaticFindObject("/Script/Engine.Default__KismetRenderingLibrary")
local math3 = StaticFindObject("/Script/Engine.Default__KismetMathLibrary")
local preview = live("3DMeshCapture_BP_C")
if not preview then error("The level has no character preview, 3DMeshCapture_BP") end
local itemClass = LoadAsset(args.item)
if not itemClass or not itemClass:IsValid() then error("Class " .. args.item .. " did not load") end
local defaults = itemClass:GetCDO()
local armor = defaults:IsA(LoadAsset("/Game/BloodlinesAlpha/Interactables/Armor/BP_BaseArmor.BP_BaseArmor_C"))
-- E_SlotType 6 is Primary, 7 Secondary and 8 Shield, the slots of what a hand holds. E_AttackType 2 is Bow,
-- 8 Crossbow, 9 Gun and 10 Pistol. WeaponTypes 20 is Lute.
local slot = itemInfo(defaults.ItemInfo, "ItemSlot")
local weaponType = itemInfo(defaults.ItemInfo, "WeaponType")
-- args.subject = "object" shows armor as its own actor, for armor that the preview wears without drawing it.
local subject = args.part and "part" or args.subject == "object" and "object" or armor and "worn"
  or (slot == 6 or slot == 7 or slot == 8) and "carried" or "object"
-- A capture taken again in a refined frame finds the armor already equipped, as the first capture left it.
if subject == "worn" and not args.again then commands["equip"]({ item = args.item }) end
local posed = subject == "worn"

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
-- The other actor a component hangs on, or nil when it hangs on nothing beyond its own actor. An item can hang a
-- mesh on a character instead of on itself, as pauldrons hang one piece on each shoulder.
local function hungOn(actor, component)
  local own = actor:GetFullName()
  local at = component
  for _ = 1, 16 do
    at = at:GetAttachParent()
    if not (at and at:IsValid()) then return nil end
    local owner = at:GetOwner()
    if owner:GetFullName() ~= own then return owner end
  end
end
-- The meshes an actor draws at its own place or on the preview character. A mesh it hangs on another character is
-- no part of the picture: Assassin's Belt, lying in the world, hangs one of its daggers on the player's body.
local function visibleMeshes(actor)
  local visible = {}
  for _, component in ipairs(components(actor, "/Script/Engine.MeshComponent")) do
    if component:IsVisible() and not component.bHiddenInGame and meshOf(component) then
      local other = hungOn(actor, component)
      if not other or other:GetFullName() == preview:GetFullName() then visible[#visible + 1] = component end
    end
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

    -- The third of a mesh's axes from the two that have their directions. Local X, Y and Z keep their handedness
    -- in the turned copy: X = Y x Z, Y = Z x X, Z = X x Y.
    local function complete(images)
      if not images.X then images.X = math3:Cross_VectorVector(images.Y, images.Z)
      elseif not images.Y then images.Y = math3:Cross_VectorVector(images.Z, images.X)
      else images.Z = math3:Cross_VectorVector(images.X, images.Y) end
      return images
    end
    -- The axes of a box from its longest to its thinnest.
    local function ranked(low, high)
      local names = { "X", "Y", "Z" }
      table.sort(names, function(a, b) return math.abs(high[a] - low[a]) > math.abs(high[b] - low[b]) end)
      return names[1], names[2], names[3]
    end

    local shown, meshes, actors = {}, {}, {}
    -- Every capture is lit by the preview's own light alone. An item's lights would light other captures too:
    -- Altarlight Pauldrons' candles stay on the preview character until the next piece of armor is captured, and a
    -- sweep found every render after them nearly white. So the lights of whatever the character wears and of every
    -- actor spawned for a capture are switched off.
    local function unlit(actor)
      for _, light in ipairs(components(actor, "/Script/Engine.LightComponent")) do light:SetVisibility(false, true) end
    end
    local wearing = {}
    preview:GetAttachedActors(wearing, true)
    for _, entry in ipairs(wearing) do unlit(entry.get and entry:get() or entry) end
    -- A spawned item actor goes away after the capture with every actor attached to it.
    local function own(actor)
      local list = { actor }
      local attached = {}
      actor:GetAttachedActors(attached, true)
      for _, entry in ipairs(attached) do list[#list + 1] = entry.get and entry:get() or entry end
      for _, each in ipairs(list) do
        actors[#actors + 1] = each
        unlit(each)
      end
    end
    local function asObject()
      -- The item's own actor, as it lies in the world, facing the way the character faces.
      local actor = spawn(itemClass, bodyAt)
      own(actor)
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
      -- A piece that colours its body parts leaves the skin colours it has no use for at the material's default, a
      -- bright pink, where the body hides them, as inside the collar of Askillian Soldier Cuirass. Shown alone, a
      -- piece would show that pink, so every skin colour it left at the default takes the character's own.
      local own = requirePlayer()["Dynamic Material"]
      local dynamic = StaticFindObject("/Script/Engine.MaterialInstanceDynamic")
      local materials = StaticFindObject("/Script/Engine.Default__KismetMaterialLibrary")
      local function ownSkin(part)
        for index = 0, part:GetNumMaterials() - 1 do
          local material = part:GetMaterial(index)
          if material and material:IsValid() and material:IsA(dynamic) and material:GetFullName() ~= own:GetFullName() then
            local unset = materials:CreateDynamicMaterialInstance(world, material.Parent, FName("None"), 0)
            for _, name in ipairs({ "Color_Skin", "Color_Stubble", "Color_Scar" }) do
              local colour, default = material:K2_GetVectorParameterValue(FName(name)), unset:K2_GetVectorParameterValue(FName(name))
              if math.abs(colour.R - default.R) + math.abs(colour.G - default.G) + math.abs(colour.B - default.B) < 0.01 then
                material:SetVectorParameterValue(FName(name), own:K2_GetVectorParameterValue(FName(name)))
              end
            end
          end
        end
      end
      for _, part in ipairs(components(preview, "/Script/Engine.SkeletalMeshComponent")) do
        local mesh = meshOf(part)
        if mesh and wanted[mesh:GetFName():ToString()] and part:IsVisible() then
          ownSkin(part)
          shown[#shown + 1] = part
        end
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
      own(actor)
      local visible = visibleMeshes(actor)
      if #visible == 0 then error("The item's own actor draws no mesh: " .. args.item) end
      -- The game attaches the actor to a socket of the hand without turning it, so the unturned actor shows each
      -- mesh's axes in the socket's frame, and the actor's own place is the hand's.
      local hand = actor:K2_GetActorLocation()
      local function axesOf(component)
        return { X = component:GetForwardVector(), Y = component:GetRightVector(), Z = component:GetUpVector() }
      end
      -- Every corner of every mesh's box, from the hand.
      local corners = {}
      for _, component in ipairs(visible) do
        local a, b = localBounds(meshOf(component))
        local transform = component:K2_GetComponentToWorld()
        for _, x in ipairs({ a.X, b.X }) do
          for _, y in ipairs({ a.Y, b.Y }) do
            for _, z in ipairs({ a.Z, b.Z }) do
              corners[#corners + 1] = add(math3:TransformLocation(transform, { X = x, Y = y, Z = z }), scale(hand, -1))
            end
          end
        end
      end
      -- The box around all of the item's meshes along a set of axes, measured from the hand.
      local function boxAlong(axes)
        local low, high = {}, {}
        for _, corner in ipairs(corners) do
          for name, axis in pairs(axes) do
            local along = math3:Dot_VectorVector(corner, axis)
            low[name] = math.min(low[name] or along, along)
            high[name] = math.max(high[name] or along, along)
          end
        end
        return low, high
      end
      -- The largest mesh lends the item its axes, and the actor turns so those axes take their places.
      local source, largest = nil, -1
      for _, component in ipairs(visible) do
        local low, high = localBounds(meshOf(component), component:K2_GetComponentScale())
        local size = math.abs(high.X - low.X) + math.abs(high.Y - low.Y) + math.abs(high.Z - low.Z)
        if size > largest then source, largest = component, size end
      end
      local inSocket = axesOf(source)
      local kind = slot == 8 and "shield" or weaponType == 8 and "crossbow"
        or (weaponType == 9 or weaponType == 10) and "firearm" or weaponType == 2 and "bow" or "weapon"
      -- A firearm, a crossbow, a shield and a bow are measured by their largest mesh alone, because what is fitted
      -- to it, such as The Embersnap's pipe or a nocked arrow, reaches sideways. A standing weapon is measured as a
      -- whole from the hand, so a blade on a separate hilt counts.
      local low, high = localBounds(meshOf(source), source:K2_GetComponentScale())
      if kind == "weapon" then low, high = boxAlong(inSocket) end
      local function toward(name, socketAxis) return inSocket[name][socketAxis] < 0 and -1 or 1 end
      local long, mid, thin = ranked(low, high)
      -- tools/runtime/checks/hand-frame.lua measures where the player looks and what is up in each socket's frame.
      local images
      if kind == "shield" and math.abs(inSocket[thin].Z) > math.abs(inSocket[thin].Y) then
        -- The shield sockets' Z axis points up. An item whose thinnest side faces up lies flat in the hand, as an
        -- open spellbook does, and shows as it lies.
        subject = "object"
        actor:K2_SetActorRotation(math3:MakeRotFromXZ(front, up), false)
      elseif kind == "shield" then
        -- The shield sockets' Y axis points away from the arm, so that side is the front.
        local top = math.abs(inSocket[long].Z) >= math.abs(inSocket[mid].Z) and long or mid
        images = { [thin] = scale(front, toward(thin, "Y")), [top] = scale(up, toward(top, "Z")) }
      elseif kind == "crossbow" then
        -- The Crossbow socket's Z axis points where the player aims and its Y axis up. A crossbow's thinnest side
        -- is its height, so it shows from above, and its limbs can span more than its length.
        local barrel = math.abs(inSocket[long].Z) >= math.abs(inSocket[mid].Z) and long or mid
        images = { [barrel] = scale(right, -toward(barrel, "Z")), [thin] = scale(front, toward(thin, "Y")) }
      elseif kind == "firearm" then
        -- The Gun socket's Y axis points where the player aims and its Z axis up. A firearm's longest side is its
        -- barrel and its thinnest its width, so it shows from the side.
        images = { [long] = scale(right, -toward(long, "Y")), [mid] = scale(up, toward(mid, "Z")) }
      elseif kind == "bow" then
        -- The Bow socket holds a bow along its Z axis, with its X axis pointing back at the archer. A bow stands
        -- along that Z axis instead of its largest mesh's, because a bow can be two limbs that each lean, as
        -- Warpwood Bolter's are. The mesh's thinnest side, turned level, still tells the bow's sides apart. The
        -- bow's back faces left, where its arrow flies.
        local across = math3:Normal({ X = inSocket[thin].X, Y = inSocket[thin].Y, Z = 0 }, 0.0001)
        inSocket = { X = math3:Cross_VectorVector(across, up), Y = across, Z = up }
        images = { Z = up, X = scale(right, toward("X", "X")) }
      else
        -- The end farther from the hand is up. A hand at the middle, within a tenth of the length, has no farther
        -- end, and a lute hangs from the hand by its neck. For both, the end toward the socket's Z axis is up,
        -- along which the sockets of these weapons hold them.
        local far = high[long] >= -low[long] and 1 or -1
        local centred = math.abs(high[long] + low[long]) < 0.1 * (high[long] - low[long])
        if centred or defaults["Weapon Class"] == 20 then far = toward(long, "Z") end
        images = { [long] = scale(up, far), [thin] = front }
      end
      if images then
        complete(images)
        local function image(axis)
          local v = { X = 0, Y = 0, Z = 0 }
          for _, name in ipairs({ "X", "Y", "Z" }) do v = add(v, scale(images[name], inSocket[name][axis])) end
          return v
        end
        actor:K2_SetActorRotation(math3:MakeRotationFromAxes(image("X"), image("Y"), image("Z")), false)
      end
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
      -- A part's mesh is modelled as it sits in a weapon that points up its Z axis: on every part mesh the socket
      -- where a blade or a head joins the handle lies at its lower end, a pommel's at its upper end, and a grip's
      -- socket for the pommel below the one for the guard. So Z is up and the thinner of the two level sides faces
      -- the camera. A part that is thinnest along Z, such as a crossguard or a plate the blade passes through,
      -- shows that face from the blade's side instead, with its longer side level, as a guard lies across a weapon.
      local low, high = localBounds(mesh, args.part.scale)
      local long, _, thin = ranked(low, high)
      local images
      if thin == "Z" then
        images = { [long] = right, Z = front }
      else
        images = { Z = up, [thin] = front }
      end
      complete(images)
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
    -- Published renders are at most 640 pixels, and lab.mjs combines four samples into each output pixel.
    local size = args.size or 1280
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
  if posed then preview.SkeletalMesh:SetAnimationMode(0) end
  if ok then finish(result) else finish(nil, result) end
end
-- Only equipped armor needs the frames the preview takes to put it on.
ExecuteInGameThreadAfterFrames(posed and 20 or 2, function()
  if not posed then return capture() end
  preview.SkeletalMesh:SetAnimationMode(1)
  ExecuteInGameThreadAfterFrames(5, capture)
end)
