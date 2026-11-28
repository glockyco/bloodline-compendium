-- lab setup: places a still enemy in front of the player, applies a debuff through the enemy's own
-- EnemyStatusEffectManager functions instead of casting the skill that applies it, and deals the enemy's health as
-- damage through its own Recieve Damage.
-- args.debuff is "Divine Bond", "Soulsap" or nil for none. args.enemy names the enemy class. Returns the inventory
-- counts of Divine Favor and Spirit Essence before and after the kill, to
-- compare with what creature pages say an enemy gives when it dies while debuffed.
local world = UEHelpers.GetWorld()
local pawn = requirePlayer()
local function count(name)
  local total = 0
  for _, entry in ipairs(items(pawn.Inventory)) do
    if entry.item:find(name, 1, true) then total = total + (entry.quantity or 1) end
  end
  return total
end
local before = { favor = count('DivineFavor'), essence = count('SpiritEssence') }
local forward, location = pawn:GetActorForwardVector(), pawn:K2_GetActorLocation()
local transform = {
  Rotation = { X = 0, Y = 0, Z = 0, W = 1 },
  Translation = { X = location.X + forward.X * 300, Y = location.Y + forward.Y * 300, Z = location.Z },
  Scale3D = { X = 1, Y = 1, Z = 1 },
}
local enemy = statics:BeginDeferredActorSpawnFromClass(world,
  LoadAsset(args.enemy or '/Game/AIBehaviorToolkit/Blueprints/GoblinHeavy2Handed.GoblinHeavy2Handed_C'), transform, 1, nil)
statics:FinishSpawningActor(enemy, transform)
enemy.CharacterMovement:DisableMovement()
if enemy.BehaviorComponent:IsValid() then enemy.BehaviorComponent:SetComponentTickEnabled(false) end
-- Applying a special debuff adds an icon to the health bar, which exists only once the bar has been shown.
enemy['Show Health Bar'](enemy)
local manager = enemy.EnemyStatusEffectManager
if args.debuff == 'Divine Bond' then
  manager['Apply Divine Bond Debuff'](manager, 30)
elseif args.debuff == 'Soulsap' then
  manager['Apply Soulsap Debuff'](manager, 30)
end
local health = enemy.Health
local finish = defer()
local frames = 0
local handle
handle = LoopInGameThreadAfterFrames(1, function()
  frames = frames + 1
  -- The debuff binds its death handler when it begins, which is after Apply returns, so the kill waits.
  if frames == 60 then
    -- Armor absorbs a whole hit before health takes damage, so it is cleared to kill with one call.
    enemy.Armor = 0
    enemy['Recieve Damage'](enemy, 100000, enemy:K2_GetActorLocation(), 0)
  end
  if frames < 180 then return end
  CancelDelayedAction(handle)
  finish({
    debuff = args.debuff,
    health = health,
    effects = manager['Active Special Status Effects'] and #manager['Active Special Status Effects'] or nil,
    before = before,
    after = { favor = count('DivineFavor'), essence = count('SpiritEssence') },
  })
end)
