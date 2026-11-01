-- lab setup: dispatches the equipped weapon's Enemy Hit Event to fresh Goblin Brutes to isolate its chance.
-- The target's health, resistances and debuff values are not edited. One successful effect is sampled to expiry.
local p = requirePlayer()
local world = UEHelpers.GetWorld()
local weapon = args.offhand and p.HeavyItem or p['Equipped Weapon']
if not weapon or not weapon:IsValid() then error('Equip a weapon first') end
local finish = defer()
local started = statics:GetTimeSeconds(world)
local trials, successes, phaseAt = {}, {}, started
local enemy, before, previous, sampleStart
local phase = 'spawn'
local function spawn()
    local location = p:K2_GetActorLocation()
    local transform = { Rotation = { X = 0, Y = 0, Z = 0, W = 1 },
        Translation = { X = location.X + 500, Y = location.Y, Z = location.Z },
        Scale3D = { X = 1, Y = 1, Z = 1 } }
    local actor = statics:BeginDeferredActorSpawnFromClass(world,
        LoadAsset('/Game/AIBehaviorToolkit/Blueprints/GoblinHeavy2Handed.GoblinHeavy2Handed_C'), transform, 1, nil)
    statics:FinishSpawningActor(actor, transform)
    actor.CharacterMovement:DisableMovement()
    actor.BehaviorComponent:SetComponentTickEnabled(false)
    actor:SetActorTickEnabled(false)
    return actor
end
local rows = {}
local handle
handle = LoopInGameThreadAfterFrames(1, function()
    local ok, problem = pcall(function()
        local now = statics:GetTimeSeconds(world)
        if phase == 'spawn' then
            enemy = spawn()
            phase, phaseAt = 'settle', now
        elseif phase == 'settle' and now - phaseAt > 0.2 then
            before = enemy.Health
            weapon['Enemy Hit Event'](weapon, enemy)
            phase, phaseAt = 'read', now
        elseif phase == 'read' and now - phaseAt > 0.15 then
            local proc = enemy.Health < before
            trials[#trials + 1] = { before = before, after = enemy.Health, applied = proc }
            if proc then successes[#successes + 1] = #trials end
            if proc and not sampleStart then
                sampleStart, previous = now, enemy.Health
                rows[#rows + 1] = { t = now - phaseAt, health = enemy.Health, taken = before - enemy.Health }
                phase = 'sample'
            else
                enemy:K2_DestroyActor()
                phase = 'spawn'
            end
        elseif phase == 'sample' then
            if enemy.Health ~= previous then
                rows[#rows + 1] = { t = now - phaseAt, health = enemy.Health, taken = previous - enemy.Health }
                previous = enemy.Health
            end
            if now - sampleStart > (args.duration or 11) then
                enemy:K2_DestroyActor()
                phase = 'spawn'
            end
        end
        if #trials >= (args.trials or 100) and phase == 'spawn' then
            CancelDelayedAction(handle)
            finish({ trials = trials, successes = successes, count = #trials, procs = #successes, ticks = rows })
        end
    end)
    if not ok then CancelDelayedAction(handle); finish(nil, problem) end
end)
