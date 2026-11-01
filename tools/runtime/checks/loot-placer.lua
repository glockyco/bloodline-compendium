-- lab setup: spawns the dungeon loot placer in front of the player with a preset loot table, as a dungeon places
-- it, and reports what it spawned, so a capture shows what a player finds at such a spot.
-- args.preset names a PresetLootTables_DT row, such as SupplyBox. args.distance is in centimetres.
local p = requirePlayer()
local world = UEHelpers.GetWorld()
local controller = UEHelpers.GetPlayerController()
local rotation = controller:GetControlRotation()
controller:SetControlRotation({ Pitch = -20, Yaw = rotation.Yaw, Roll = 0 })
local forward, location = p:GetActorForwardVector(), p:K2_GetActorLocation()
local distance = args.distance or 300
local transform = {
    Rotation = { X = 0, Y = 0, Z = 0, W = 1 },
    Translation = { X = location.X + forward.X * distance, Y = location.Y + forward.Y * distance, Z = location.Z - 80 },
    Scale3D = { X = 1, Y = 1, Z = 1 },
}
local class = LoadAsset('/Game/Overworld/Villages/VillageBuildings/GenericDungeon_StructuresAndEvents/Lootables/Dungeon_Generic_Lootable_Item_Placer.Dungeon_Generic_Lootable_Item_Placer_C')
local placer = statics:BeginDeferredActorSpawnFromClass(world, class, transform, 1, nil)
placer['Preset Table Name'] = FName(args.preset)
placer['Use Preset Table?'] = true
statics:FinishSpawningActor(placer, transform)
local finish = defer()
local started = statics:GetRealTimeSeconds(world)
local handle
handle = LoopInGameThreadAfterFrames(1, function()
    if statics:GetRealTimeSeconds(world) - started < (args.wait or 2) then return end
    CancelDelayedAction(handle)
    local ok, result = pcall(function()
        local spawned = {}
        placer['Spawned Loot']:ForEach(function(_, element)
            local actor = element:get()
            if actor and actor:IsValid() then spawned[#spawned + 1] = actor:GetClass():GetFullName() end
        end)
        local components = {}
        for _, name in ipairs({ 'Box', 'DefaultSceneRoot' }) do
            local ok2, component = pcall(function() return placer[name] end)
            if ok2 and component and component:IsValid() then components[#components + 1] = component:GetClass():GetFName():ToString() end
        end
        return { placer = placer:GetFullName(), spawned = spawned, components = components }
    end)
    if ok then finish(result) else finish(nil, result) end
end)
