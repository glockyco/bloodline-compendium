-- lab setup: supplies a skill with AddNewSkill, sets requested saved passive ranks, restores player mana,
-- and places a fresh enemy with movement and AI disabled. The check does not set Health, armor or Damage.
-- Run in a loaded lab save. "skill" is the full class path (including .Class_C) and "school" optionally reads
-- the live elemental damage and mana helpers. "ranks" sets named saved ranks (0 removes a rank).
-- "slot" selects the controller's offhand hotbar slot (default 1). The check binds the skill there.
-- "action": "inspect" supplies the skill and reads ranks without casting.
-- "distance" (default 300), "pitch" (default -8), "height" and "lateral" place and aim at the target.
-- "enemy" picks its class, "stationaryTick" leaves its actor tick on, and "seconds" sets the sample duration.
-- "thirdPerson" keeps the current camera instead of switching to first person.
-- "recastAfter" presses the same slot a second time after that many seconds, for skills with a follow-up cast.
-- "actorOverlap" explicitly dispatches the spawned actor class's actor-overlap event at the target, once.
-- This isolates an impact branch. It does not reproduce a normal collision.
local p = requirePlayer()
local world = UEHelpers.GetWorld()
local controller = UEHelpers.GetPlayerController()
local state = _G.SkillDamageCheck or {}
_G.SkillDamageCheck = state
local function valid(object) return object and object:IsValid() end
local function number(object, key)
    local value = object[key]
    if type(value) == 'number' then return value end
end
local function coords(vector) return { x = vector.X, y = vector.Y, z = vector.Z } end

local skillPath = assert(args.skill, 'Give the skill class path in skill')
local skillClass = assert(skillPath:match('%.([^%.]+)$'), 'Include .Class_C in skill')
local skill = live(skillClass)
if not skill then
    local class = LoadAsset(skillPath)
    assert(valid(class), 'Cannot load ' .. skillPath)
    p:AddNewSkill(class, FName('BLADES'))
    skill = assert(live(skillClass), 'AddNewSkill did not create ' .. skillClass)
end

local function setRanks(ranks)
    local array = p['Persistant Ref']['Completed Achievements']
    local retained = {}
    for i = 1, #array do
        local achievement = array[i]:ToString()
        local key = achievement:match('^(.-) %d+$')
        if not key or ranks[key] == nil then retained[#retained + 1] = achievement end
    end
    for key, rank in pairs(ranks) do
        if rank > 0 then retained[#retained + 1] = key .. ' ' .. rank end
    end
    array:Empty()
    for i, achievement in ipairs(retained) do array[i] = FName(achievement) end
end
if args.ranks then setRanks(args.ranks) end

local function bonus(suffix)
    local component = p.PlayerPassiveLevelStatComponent
    local result = {}
    local fn = 'Get ' .. args.school .. ' ' .. suffix
    component[fn](component, result)
    return result['Bonus Percent']
end
local inputs = { skill = skill:GetFullName(), ranks = args.ranks }
if args.school then
    inputs.damageBonus = bonus('Damage Bonuses')
    inputs.manaReduction = bonus('Mana Cost Reduction')
end
if args.action == 'inspect' then return inputs end

local slotIndex = args.slot or 1
local slot
for _, candidate in ipairs(liveAll('W_SkillBarSlot_C')) do
    if candidate['Skill Slot Int'] == slotIndex and candidate['Offhand Spell?'] then
        slot = candidate
        break
    end
end
assert(slot, 'No offhand hotbar slot ' .. slotIndex)
slot['Bind Skill'](slot, skill)
inputs.slot = slotIndex
if not args.thirdPerson and not p['First Person?'] then p['Set camera to First Person'](p) end
commands['close-tutorials']({})
statics:SetGamePaused(world, false)
if valid(state.enemy) then state.enemy:K2_DestroyActor() end
local rotation = controller:GetControlRotation()
controller:SetControlRotation({ Pitch = args.pitch or -8, Yaw = rotation.Yaw, Roll = 0 })
local angle = math.rad(rotation.Yaw)
local x, y = math.cos(angle), math.sin(angle)
local location, distance, lateral = p:K2_GetActorLocation(), args.distance or 300, args.lateral or 0
local transform = {
    Rotation = { X = 0, Y = 0, Z = 0, W = 1 },
    Translation = { X = location.X + x * distance - y * lateral, Y = location.Y + y * distance + x * lateral,
        Z = location.Z + (args.height or 0) },
    Scale3D = { X = 1, Y = 1, Z = 1 },
}
local enemyClass = LoadAsset(args.enemy or '/Game/AIBehaviorToolkit/Blueprints/GoblinHeavy2Handed.GoblinHeavy2Handed_C')
assert(valid(enemyClass), 'Cannot load target class')
local enemy = statics:BeginDeferredActorSpawnFromClass(world, enemyClass, transform, 1, nil)
assert(valid(enemy), 'Cannot spawn target')
statics:FinishSpawningActor(enemy, transform)
enemy.CharacterMovement:DisableMovement()
if valid(enemy.BehaviorComponent) then enemy.BehaviorComponent:SetComponentTickEnabled(false) end
enemy:SetActorTickEnabled(args.stationaryTick == true)
state.enemy = enemy
p['Current Mana'] = p['Maximum Mana']

if not state.hooks then
    local function observe(phase, object)
        if not state.capture or not valid(object) or not isInstance(object) then return end
        local capture = state.capture
        local id = object:GetAddress()
        local spawn = capture.byAddress[id]
        if not spawn then
            spawn = { actor = object:GetFullName(), class = classPath(object:GetClass()), samples = {} }
            capture.byAddress[id] = spawn
            capture.spawns[#capture.spawns + 1] = spawn
            capture.objects[#capture.objects + 1] = { object = object, spawn = spawn }
        end
        local damage = number(object, 'Damage')
        local explosionDamage = number(object, 'Explosion Damage')
        local samples = spawn.samples
        local previous = samples[#samples]
        if phase ~= 'frame' or not previous or previous.damage ~= damage or previous.explosionDamage ~= explosionDamage then
            samples[#samples + 1] = { phase = phase, t = capture.time(), damage = damage,
                explosionDamage = explosionDamage, location = phase ~= 'created' and coords(object:K2_GetActorLocation()) or nil }
        end
    end
    NotifyOnNewObject('/Script/Engine.Actor', function(actor) observe('created', actor) end)
    local pre, post = RegisterHook('/Script/Engine.GameplayStatics:FinishSpawningActor',
        function(_, actor) observe('beforeFinish', actor:get()) end,
        function(_, actor) observe('afterFinish', actor:get()) end)
    RegisterHook('/Game/AIBehaviorToolkit/Blueprints/Goblin_Base.Goblin_Base_C:Recieve Damage',
        function(context, damage, _, element)
            local capture = state.capture
            local target = context:get()
            if not capture or target:GetAddress() ~= capture.targetAddress then return end
            capture.receives[#capture.receives + 1] = {
                t = capture.time(), damage = damage:get(), element = element:get(), health = target.Health,
            }
        end)
    state.observe = observe
    state.hooks = { pre, post }
end

local function snapshot()
    return { health = valid(enemy) and enemy.Health or nil, mana = p['Current Mana'],
        stamina = p.Stamina, cooldown = number(skill, 'CurrentCooldownTime') }
end
local finish = defer()
local elapsed, rows, hits, mana, deduction = 0, {}, {}, {}, 0
local started = statics:GetTimeSeconds(world)
local target = { actor = enemy:GetFullName(), armor = number(enemy, 'Armor'), location = coords(enemy:K2_GetActorLocation()),
    caster = coords(location), distance = distance, lateral = lateral, pitch = args.pitch or -8 }
local capture = { spawns = {}, objects = {}, byAddress = {}, receives = {}, targetAddress = enemy:GetAddress(),
    time = function() return statics:GetTimeSeconds(world) - started end }
local initial, previous, castAt, recastAt, dispatched
local handle
local function stopSampling()
    CancelDelayedAction(handle)
    state.capture = nil
end
handle = LoopInGameThreadAfterFrames(1, function()
    local ok, problem = pcall(function()
        elapsed = capture.time()
        local row = snapshot()
        row.t = elapsed
        rows[#rows + 1] = row
        if previous then
            if row.health and previous.health and row.health < previous.health then
                hits[#hits + 1] = { t = elapsed, loss = previous.health - row.health }
            end
            if row.mana < previous.mana then
                local spent = previous.mana - row.mana
                mana[#mana + 1] = { t = elapsed, spent = spent }
                deduction = deduction + spent
            end
        end
        previous = row
        if not castAt and elapsed >= 0.2 then
            initial = snapshot()
            castAt = elapsed
            state.capture = capture
            controller:TryCastOffhandSpell(slotIndex)
        end
        if castAt and args.recastAfter and not recastAt and elapsed >= castAt + args.recastAfter then
            recastAt = elapsed
            controller:TryCastOffhandSpell(slotIndex)
        end
        for _, entry in ipairs(capture.objects) do state.observe('frame', entry.object) end
        if args.actorOverlap and not dispatched then
            for _, entry in ipairs(capture.objects) do
                local actor = entry.object
                if valid(actor) and actor:GetClass():GetFName():ToString() == args.actorOverlap then
                    dispatched = { t = elapsed, actor = entry.spawn.actor }
                    actor:ReceiveActorBeginOverlap(enemy)
                    break
                end
            end
        end
        if castAt and elapsed >= castAt + (args.seconds or 3) then
            stopSampling()
            finish({ inputs = inputs, target = target, initial = initial, final = snapshot(), rows = rows, hits = hits,
                mana = mana, manaSpent = deduction, spawns = capture.spawns, castAt = castAt, receives = capture.receives,
                recastAt = recastAt,
                dispatchedActorOverlap = dispatched })
        end
    end)
    if not ok then
        stopSampling()
        finish(nil, problem)
    end
end)
