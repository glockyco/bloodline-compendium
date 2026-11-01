-- lab setup: wakes the staff-issues check's Goblin Brute, holds the ordinary block input and samples
-- player block charges, Health and Stamina. Ready restores stamina through FoodManager between comparisons.
local p = requirePlayer()
local world = UEHelpers.GetWorld()
local controller = UEHelpers.GetPlayerController()
local function input(phase)
    for _, target in ipairs({ controller, p }) do
        target:GetClass().DynamicBindingObjects:ForEach(function(_, element)
            local binding = element:get()
            if binding:GetClass():GetFName():ToString() ~= 'EnhancedInputActionDelegateBinding' then return end
            binding.InputActionDelegateBindings:ForEach(function(_, entry)
                local b = entry:get()
                if b.InputAction:IsValid() and b.InputAction:GetFName():ToString() == 'IA_AltFire' and b.TriggerEvent == phase then
                    target[b.FunctionNameToBind:ToString()](target, { Value = { X = phase == 1 and 1 or 0, Y = 0, Z = 0 }, ValueType = 0 }, 0, 0)
                end
            end)
        end)
    end
end
local function snapshot()
    return { health = p.Health, stamina = p.Stamina, shield = p.ShieldEquipped,
        blocking = p.IsBlocking, charges = p['Current Absolute Block Shields'],
        maximumCharges = p['Maximum Absolute Block Shields'] }
end
if args.action == 'ready' then
    if p['Is Character Sheet Open'] then p['Open/Close Character Sheet'](p) end
    local food = live('FoodManager_C')
    food['Replenish Stamina'](food, 250)
    return snapshot()
end
if args.action == 'read' then return snapshot() end
local state = _G.ShieldBlockCheck or { blocks = {} }
_G.ShieldBlockCheck = state
if not state.hook then
    state.hook = { RegisterHook('/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2.FirstPersonCharacter2_C:Block Attack',
        function(_, damage)
            state.blocks[#state.blocks + 1] = { damage = damage:get(), values = snapshot() }
        end) }
end
local enemy = _G.StaffIssueCheck.enemy
if not enemy or not enemy:IsValid() then error('Spawn a target with staff-issues first') end
statics:SetGamePaused(world, false)
enemy.CharacterMovement:SetMovementMode(1, 0)
enemy.BehaviorComponent:SetComponentTickEnabled(true)
enemy:SetActorTickEnabled(true)
state.blocks = {}
if args.block ~= false then input(1) end
local initial, previous = snapshot(), snapshot()
local finish = defer()
local started = statics:GetTimeSeconds(world)
local changes, losses = {}, 0
local handle
handle = LoopInGameThreadAfterFrames(1, function()
    local ok, problem = pcall(function()
        local now = snapshot()
        local t = statics:GetTimeSeconds(world) - started
        if now.stamina < previous.stamina or now.health < previous.health or now.charges ~= previous.charges then
            changes[#changes + 1] = { t = t, before = previous, after = now,
                staminaLost = previous.stamina - now.stamina, healthLost = previous.health - now.health }
            if now.stamina < previous.stamina then losses = losses + 1 end
        end
        previous = now
        if t > (args.seconds or 30) or losses >= (args.losses or 3) or now.health < 50 then
            CancelDelayedAction(handle)
            if args.block ~= false then input(5) end
            enemy:SetActorTickEnabled(false)
            enemy.BehaviorComponent:SetComponentTickEnabled(false)
            enemy.CharacterMovement:DisableMovement()
            finish({ initial = initial, final = snapshot(), changes = changes, blocks = state.blocks })
        end
    end)
    if not ok then CancelDelayedAction(handle); finish(nil, problem) end
end)
