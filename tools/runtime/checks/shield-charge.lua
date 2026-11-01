-- lab setup: supplies Shield Charge through AddNewSkill, as giving a weapon supplies its inventory actor.
-- Casting uses TryUseSkill with the real equipment, mana and availability checks. Charge flags and damage are not edited.
local p = requirePlayer()
local world = UEHelpers.GetWorld()
if args.action == 'supply' then
    p:AddNewSkill(LoadAsset('/Game/UltimateSkillSystem/SkillSystemSkill/ShieldCharge.ShieldCharge_C'), FName('BLADES'))
    return { supplied = true }
end
local skill = live('ShieldCharge_C')
local enemy = _G.StaffIssueCheck and _G.StaffIssueCheck.enemy
local function snapshot()
    return { charge = p['Shield Charge'], performing = p['Performing Skill'],
        mana = p['Current Mana'], using = skill['Using Skill'],
        health = enemy and enemy:IsValid() and enemy.Health or nil,
        weaponType = p['Equipped Weapon Type'], shield = p.ShieldEquipped }
end
if args.action == 'read' then return snapshot() end
statics:SetGamePaused(world, false)
local initial = snapshot()
local result = {}
skill:TryUseSkill(result)
local success = result['Success?']
local finish = defer()
local started = statics:GetTimeSeconds(world)
local rows, previous = {}, nil
local handle
handle = LoopInGameThreadAfterFrames(1, function()
    local ok, problem = pcall(function()
        local row = snapshot()
        local key = tostring(row.charge) .. ':' .. tostring(row.performing) .. ':' .. tostring(row.using) .. ':' .. tostring(row.health)
        if key ~= previous then
            row.t = statics:GetTimeSeconds(world) - started
            rows[#rows + 1] = row
            previous = key
        end
        if statics:GetTimeSeconds(world) - started > (args.seconds or 4) then
            CancelDelayedAction(handle)
            finish({ initial = initial, success = success, final = snapshot(), changes = rows })
        end
    end)
    if not ok then CancelDelayedAction(handle); finish(nil, problem) end
end)
