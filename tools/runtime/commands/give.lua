-- lab setup: adds items to the inventory without finding, buying or crafting them
-- Builds each item as an actor far below the player, so its construction script fills its item info as for an item
-- lying in the world (95 class defaults lack their own item class), passes that item info to the player's own
-- AddItemsToInventory as a pickup does, and removes the actor again.
local pawn = requirePlayer()
local world = UEHelpers.GetWorld()
local below = pawn:K2_GetActorLocation()
below.Z = below.Z - 100000.0
local transform = { Rotation = { X = 0, Y = 0, Z = 0, W = 1 }, Translation = below, Scale3D = { X = 1, Y = 1, Z = 1 } }
local results = {}
for _, path in ipairs(args.items) do
  local class = LoadAsset(path)
  if class and class:IsValid() then
    local ok, problem = pcall(function()
      local actor = statics:BeginDeferredActorSpawnFromClass(world, class, transform, 1, nil)
      statics:FinishSpawningActor(actor, transform)
      pawn:AddItemsToInventory(actor.ItemInfo, args.count or 1)
      actor:K2_DestroyActor()
    end)
    results[#results + 1] = { item = path, added = ok, error = ok and nil or tostring(problem) }
  else
    results[#results + 1] = { item = path, added = false, error = "class did not load" }
  end
end
return { results = results, inventory = #pawn.Inventory }
