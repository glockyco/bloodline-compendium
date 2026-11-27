-- lab setup: args.enter calls Enter Event on the live world-map actor of that kind, without walking into it.
-- args.tag instead writes the save tag directly and opens a pine forest camp, bypassing the world-map actor.
-- With neither, observes the active level, the remaining save tag and the event creatures.
local world = UEHelpers.GetWorld()
local instance = assert(live('BP_PersistantGameInstance_C'), 'No live game instance')
local save = instance['Save Game Data']
local saveData = save:IsValid() and save['Player Save Data 2'] or nil
local savePath = '/Game/SaveData2.SaveData2'
local function tag()
  return saveData and field(saveData, savePath, 'OverworldEventTag'):ToString() or nil
end
if args.enter then
  local pawn = requirePlayer()
  assert(pawn:GetClass():GetFName():ToString() == 'TopDownCharacter_Working_C', 'Not on the world map')
  local target
  pawn['Spawned Events']:ForEach(function(_, entry)
    local actor = entry:get()
    if not target and actor:IsValid() and actor['Selected Event']:ToString() == args.enter then target = actor end
  end)
  assert(target, 'No live world map event: ' .. args.enter)
  local name, terrain = target:GetFullName(), target['Nearest Location Type']:ToString()
  target['Enter Event'](target)
  return { actor = name, event = args.enter, terrain = terrain, tag = tag() }
end
if args.tag then
  assert(saveData, 'No live save object')
  local tagName
  StaticFindObject(savePath):ForEachProperty(function(property)
    local name = property:GetFName():ToString()
    if name:match('^OverworldEventTag_%d+_%x+$') then tagName = name end
  end)
  saveData[assert(tagName, 'No overworld event tag property')] = FString(args.tag)
  assert(tag() == args.tag, 'Save event tag did not change')
  local camp = '/Game/Overworld/OpenWorldAreas/Small_Event_Areas/Generic_PineForest_Area'
  statics:OpenLevel(world, FName(camp), true, '')
  return { opened = camp, tag = args.tag }
end
local counts = {}
for _, class in ipairs({ 'SmallEventSpawner_C', 'Bear_C', 'WildWolf_C', 'Undead_Shambler_C', 'Undead_Walker_C',
  'Undead_Bonebag_C', 'Leveled_Bandit_Spawner_C', 'GoblinCampsite_C', 'GreatOgre_C', 'OgreDen_C' }) do
  counts[class] = 0
  for _, actor in ipairs(liveAll(class)) do
    if actor:GetOuter():GetOuter():GetFName():ToString() == world:GetFName():ToString() then
      counts[class] = counts[class] + 1
    end
  end
end
return { world = world:GetFullName(), time = statics:GetTimeSeconds(world), tag = tag(), counts = counts }
