-- lab setup: supplies and equips the requested staff, restores mana between checks, and spawns a Goblin Brute
-- at a measured distance with movement, actor and behavior ticks disabled. Attacks use the live input bindings.
-- Damage, health, armor, combo and proficiency are not edited. Pausing preserves visible bars for screenshots.
local p = requirePlayer()
local world = UEHelpers.GetWorld()
local controller = UEHelpers.GetPlayerController()
local state = _G.StaffIssueCheck or {}
_G.StaffIssueCheck = state

local function input(phase)
    local calls = {}
    for _, target in ipairs({ controller, p }) do
        target:GetClass().DynamicBindingObjects:ForEach(function(_, element)
            local binding = element:get()
            if binding:GetClass():GetFName():ToString() ~= 'EnhancedInputActionDelegateBinding' then return end
            binding.InputActionDelegateBindings:ForEach(function(_, entry)
                local b = entry:get()
                if b.InputAction:IsValid() and b.InputAction:GetFName():ToString() == 'IA_Attack' and b.TriggerEvent == phase then
                    local name = b.FunctionNameToBind:ToString()
                    target[name](target, { Value = { X = phase == 1 and 1 or 0, Y = 0, Z = 0 }, ValueType = 0 }, 0, 0)
                    calls[#calls + 1] = name
                end
            end)
        end)
    end
    if #calls == 0 then error('No live Attack binding for phase ' .. phase) end
    return calls
end

local function snapshot()
    local enemy = state.enemy
    local weapon = p['Equipped Weapon']
    return {
        mana = p['Current Mana'], maximumMana = p['Maximum Mana'],
        enemyHealth = enemy and enemy:IsValid() and enemy.Health or nil,
        enemyArmor = enemy and enemy:IsValid() and enemy.Armor or nil,
        weapon = weapon:IsValid() and weapon:GetFullName() or nil,
        combo = p['Halberd Combo Counter'], sheathed = p['Has Weapon Sheathed'],
        damageMultiplier = p['Damage Multiplier'],
    }
end

if not state.hook then
    local function observe(phase, actor)
        local object = actor:get()
        if not object or not object:IsValid() then return end
        local class = object:GetClass():GetFullName()
        if not class:find('Projectile', 1, true) then return end
        local ok, damage = pcall(function() return object.Damage end)
        state.spawns[#state.spawns + 1] = {
            phase = phase, class = class, damage = ok and damage or nil,
            enemyHealth = state.enemy and state.enemy:IsValid() and state.enemy.Health or nil,
        }
    end
    state.spawns = {}
    local a, b = RegisterHook('/Script/Engine.GameplayStatics:FinishSpawningActor',
        function(_, actor) observe('beforeFinish', actor) end,
        function(_, actor) observe('afterFinish', actor) end)
    state.hook = { a, b }
end

commands['close-tutorials']({})
statics:SetGamePaused(world, false)

if args.action == 'equip' then
    if state.enemy and state.enemy:IsValid() then state.enemy:K2_DestroyActor() end
    state.enemy = nil
    if state.tooltip and state.tooltip:IsValid() then state.tooltip:RemoveFromParent() end
    state.tooltip = nil
    commands.give({ items = { args.item }, count = 1 })
    commands['open-sheet']({})
    commands.equip({ item = args.item })
    p['Open/Close Character Sheet'](p)
    if not p['First Person?'] then p['Set camera to First Person'](p) end
    if p['Has Weapon Sheathed'] then p['Draw Weapon'](p) end
    p['Current Mana'] = p['Maximum Mana']
    return snapshot()
elseif args.action == 'tooltip' then
    commands.give({ items = { args.item }, count = 1 })
    commands['open-sheet']({})
    -- Inventory rebuilds after adding an item. Hover the shown slot after that frame has finished.
    local finish = defer()
    local start = statics:GetRealTimeSeconds(world)
    local handle
    handle = LoopInGameThreadAfterFrames(1, function()
        if statics:GetRealTimeSeconds(world) - start < 0.5 then return end
        CancelDelayedAction(handle)
        local ok, result = pcall(function()
            local slot = inventorySlot(args.item)
            slot:OnMouseEnter({}, {})
            state.tooltip = slot.ToolTipWidget
            state.tooltip:AddToViewport(100)
            state.tooltip:SetPositionInViewport({ X = 450, Y = 20 }, true)
            local shown, hidden = texts(state.tooltip)
            return { shown = shown, hidden = hidden, values = snapshot() }
        end)
        if ok then finish(result) else finish(nil, result) end
    end)
    return
elseif args.action == 'close-tooltip' then
    if state.tooltip and state.tooltip:IsValid() then state.tooltip:RemoveFromParent() end
    state.tooltip = nil
    p['Open/Close Character Sheet'](p)
    return snapshot()
elseif args.action == 'target' then
    if state.enemy and state.enemy:IsValid() then state.enemy:K2_DestroyActor() end
    local rotation = controller:GetControlRotation()
    controller:SetControlRotation({ Pitch = 0, Yaw = rotation.Yaw, Roll = 0 })
    local forward, location = p:GetActorForwardVector(), p:K2_GetActorLocation()
    local transform = {
        Rotation = { X = 0, Y = 0, Z = 0, W = 1 },
        Translation = { X = location.X + forward.X * args.distance, Y = location.Y + forward.Y * args.distance, Z = location.Z },
        Scale3D = { X = 1, Y = 1, Z = 1 },
    }
    local enemy = statics:BeginDeferredActorSpawnFromClass(world,
        LoadAsset('/Game/AIBehaviorToolkit/Blueprints/GoblinHeavy2Handed.GoblinHeavy2Handed_C'), transform, 1, nil)
    statics:FinishSpawningActor(enemy, transform)
    enemy.CharacterMovement:DisableMovement()
    enemy.BehaviorComponent:SetComponentTickEnabled(false)
    enemy:SetActorTickEnabled(false)
    enemy['Show Health Bar'](enemy)
    state.enemy = enemy
    return { distance = args.distance, values = snapshot() }
end

if args.action == 'prime' and state.enemy and state.enemy:IsValid() then
    state.enemy:K2_DestroyActor()
    state.enemy = nil
end

local finish = defer()
local started = statics:GetTimeSeconds(world)
local rows, calls = {}, {}
local pressed, released, hitAt = false, false, nil
local initial = snapshot()
state.spawns = {}
local handle
handle = LoopInGameThreadAfterFrames(1, function()
    local ok, problem = pcall(function()
        local t = statics:GetTimeSeconds(world) - started
        if args.action == 'prime' then
            if t > 0.6 and not p.IsAttacking then
                if p['Halberd Combo Counter'] == 1 then
                    CancelDelayedAction(handle)
                    finish({ values = snapshot(), swings = calls })
                    return
                end
                -- This is the same completed Attack handler used by a short normal release.
                calls[#calls + 1] = input(5)
            end
            if t > 8 then error('Could not reach the second normal combo attack through swings') end
        else
            if args.action == 'attack' and not pressed and t > 0.2 and not p.IsAttacking then
                calls[#calls + 1] = input(1)
                pressed = t
            end
            if pressed and not released and t > pressed + 0.08 then
                calls[#calls + 1] = input(5)
                released = t
            end
            local row = snapshot()
            row.t = t
            rows[#rows + 1] = row
            if row.enemyHealth and initial.enemyHealth and row.enemyHealth < initial.enemyHealth and not hitAt then hitAt = t end
            local stop = t >= (args.seconds or 2)
                or (args.pauseOnHit and hitAt and t >= hitAt + 0.12)
                or (args.pauseDuring and released and t >= released + 0.25)
            if stop then
                CancelDelayedAction(handle)
                if args.pauseOnHit or args.pauseDuring then statics:SetGamePaused(world, true) end
                finish({ initial = initial, final = snapshot(), rows = rows, calls = calls, spawns = state.spawns, hitAt = hitAt })
            end
        end
    end)
    if not ok then CancelDelayedAction(handle); finish(nil, problem) end
end)
