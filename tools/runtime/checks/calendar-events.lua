-- Lists the live save's generated calendar dates, event names and areas after a new save initializes.
-- args.holidays lists the holiday preset row names in the order the generator checks them.
-- lab setup: args.day sets the live and saved day directly, then opens args.open's level package as travel does.
local instance = assert(live('BP_PersistantGameInstance_C'), 'No live game instance')
local managerPath = '/Game/fantasy_gui_4/widgets/templates/popup/CalendarEventManager'
local manager = live('CalendarEventManager_C')
local datePath = '/Game/Date.Date'
local eventPath = '/Game/Overworld/OpenWorldAreas/CalendarEventStruct.CalendarEventStruct'
local date = instance['Date/Time']
local function readDate(value)
  return { day = field(value, datePath, 'Dayofthemonth'), month = field(value, datePath, 'Month'),
    year = field(value, datePath, 'Year') }
end
if args.holidays then
  local path = '/Game/Overworld/OpenWorldAreas/PresetCalendarEvents.PresetCalendarEvents'
  local library = StaticFindObject('/Script/Engine.Default__DataTableFunctionLibrary')
  local rows, names = {}, {}
  library:GetDataTableRowNames(LoadAsset(path), rows)
  for _, row in ipairs(rows) do names[#names + 1] = row:get():ToString() end
  return { holidays = names }
end
if args.day then
  assert(args.open, 'Pass a level package as args.open')
  local dayName
  StaticFindObject(datePath):ForEachProperty(function(property)
    local name = property:GetFName():ToString()
    if name:match('^Dayofthemonth_%d+_%x+$') then dayName = name end
  end)
  date[assert(dayName, 'No day-of-month property')] = args.day
  local save = instance['Save Game Data']
  assert(save:IsValid(), 'No live save object')
  local playerSavePath = '/Game/PlayerSaveDataaa4.PlayerSaveDataaa4'
  local savedDate = field(save['Player Save Data'], playerSavePath, 'DateTime')
  savedDate[dayName] = args.day
  assert(readDate(field(save['Player Save Data'], playerSavePath, 'DateTime')).day == args.day,
    'Saved day of the month did not change')
  local currentDate = readDate(instance['Date/Time'])
  assert(currentDate.day == args.day, 'Persistent day of the month did not change')
  statics:OpenLevel(UEHelpers.GetWorld(), FName(args.open), true, '')
  return { date = currentDate, opened = args.open }
end
local source = manager or StaticFindObject(managerPath .. '.Default__CalendarEventManager_C')
assert(source:IsValid(), 'The calendar event manager class is not loaded')
local areas, entries = {}, {}
source['All Calendar Event Location Names']:ForEach(function(_, element)
  areas[#areas + 1] = element:get():ToString()
end)
instance['Randomized Calendar Events']:ForEach(function(_, element)
  local event = element:get()
  local title = field(event, eventPath, 'EventName'):ToString()
  local area
  for _, candidate in ipairs(areas) do
    if title:find(candidate, 1, true) then
      assert(not area, 'Two calendar area names in ' .. title)
      area = candidate
    end
  end
  local entry = readDate(field(event, eventPath, 'DateTime'))
  entry.event = field(event, eventPath, 'NameofRandomCitizen'):ToString()
  entry.title, entry.area = title, area
  entries[#entries + 1] = entry
end)
return { date = readDate(date), liveManager = manager and manager:GetFullName() or nil,
  foresight = source['Foresight Range'], areas = areas, entries = entries }
