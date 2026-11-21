-- lab setup: builds item actors far below the player to read their meshes, as an item lying in the world draws them
-- Lists the visible mesh components of each item class in args.items: the mesh, its class and its bounds, so a
-- render pipeline can tell which items draw one mesh, several, or none.
local pawn = requirePlayer()
local world = UEHelpers.GetWorld()
local below = pawn:K2_GetActorLocation()
below.Z = below.Z - 100000.0
local transform = { Rotation = { X = 0, Y = 0, Z = 0, W = 1 }, Translation = below, Scale3D = { X = 1, Y = 1, Z = 1 } }
local meshClass = StaticFindObject("/Script/Engine.MeshComponent")
local results = {}
for _, path in ipairs(args.items) do
  local entry = { item = path, meshes = {} }
  local ok, problem = pcall(function()
    local class = LoadAsset(path)
    if not class or not class:IsValid() then error("class did not load") end
    local actor = statics:BeginDeferredActorSpawnFromClass(world, class, transform, 1, nil)
    statics:FinishSpawningActor(actor, transform)
    for _, found in ipairs(actor:K2_GetComponentsByClass(meshClass)) do
      local component = found.get and found:get() or found
      local mesh
      pcall(function() mesh = component.StaticMesh end)
      if not (mesh and mesh:IsValid()) then pcall(function() mesh = component.SkeletalMesh end) end
      if mesh and mesh:IsValid() then
        local origin, extent, radius = {}, {}, {}
        system:GetComponentBounds(component, origin, extent, radius)
        entry.meshes[#entry.meshes + 1] = {
          mesh = mesh:GetFName():ToString(),
          kind = component:GetClass():GetFName():ToString(),
          visible = component:IsVisible(),
          hidden = component.bHiddenInGame,
          extent = { extent.X, extent.Y, extent.Z },
        }
      end
    end
    actor:K2_DestroyActor()
  end)
  if not ok then entry.error = tostring(problem) end
  results[#results + 1] = entry
end
return { results = results }
