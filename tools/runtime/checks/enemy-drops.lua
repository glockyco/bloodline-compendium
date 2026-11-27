-- lab setup: places enemies and kills them through their own Die, which a killing blow reaches too.
-- args.spawn places args.count enemies of that class in front of the player, with movement and AI off. args.kill
-- then kills them, and args.read lists the loot that lies in the level, so a run can tell whether a class that only
-- inherits a loot table drops from it. Each step is its own call, so the game runs the frames between them.
local p = requirePlayer()
local state = _G.EnemyDropsCheck or { enemies = {} }
_G.EnemyDropsCheck = state
if args.read then
    local dropped = {}
    for _, loot in ipairs(liveAll('DroppedLoot_Common_C')) do
        local ok, item = pcall(function() return loot.Item:GetFName():ToString() end)
        dropped[#dropped + 1] = ok and item or 'unreadable'
    end
    return { dropped = dropped }
end
if args.kill then
    local died = 0
    for _, enemy in ipairs(state.enemies) do
        if enemy:IsValid() then
            enemy['Die'](enemy)
            died = died + 1
        end
    end
    state.enemies = {}
    return { died = died }
end
local world = UEHelpers.GetWorld()
commands['close-tutorials']({})
statics:SetGamePaused(world, false)
local class = LoadAsset(args.spawn)
assert(class and class:IsValid(), 'Cannot load ' .. args.spawn)
local at = p:K2_GetActorLocation()
for index = 1, args.count or 4 do
    local transform = {
        Rotation = { X = 0, Y = 0, Z = 0, W = 1 },
        Translation = { X = at.X + 400, Y = at.Y + (index - 2) * 200, Z = at.Z },
        Scale3D = { X = 1, Y = 1, Z = 1 },
    }
    local enemy = statics:BeginDeferredActorSpawnFromClass(world, class, transform, 1, nil)
    assert(enemy and enemy:IsValid(), 'Cannot spawn enemy')
    statics:FinishSpawningActor(enemy, transform)
    enemy.CharacterMovement:DisableMovement()
    if enemy.BehaviorComponent and enemy.BehaviorComponent:IsValid() then
        enemy.BehaviorComponent:SetComponentTickEnabled(false)
    end
    state.enemies[#state.enemies + 1] = enemy
end
return { spawned = #state.enemies }
