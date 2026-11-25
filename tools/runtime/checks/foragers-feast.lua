-- lab setup: opens the live cooking fire directly. Recipe selection and cooking use the window's bound controls.
local pawn = requirePlayer()
local function cookingWindow()
  for _, candidate in ipairs(liveAll('CookingWindow_C')) do
    if candidate:IsInViewport() then return candidate end
  end
end
if args.action == 'open' then
  local fire = assert(live('CookingFire_C'), 'no cooking fire in this level')
  fire['Open Crafting Menu'](fire)
  local finish = defer()
  local deadline = statics:GetRealTimeSeconds(UEHelpers.GetWorld()) + 5
  local function after()
    if cookingWindow() then
      finish({ fire = fire:GetFullName(), inventory = items(pawn.Inventory) })
    elseif statics:GetRealTimeSeconds(UEHelpers.GetWorld()) >= deadline then
      finish(nil, 'the cooking fire did not open its window')
    else
      ExecuteInGameThreadAfterFrames(5, after)
    end
  end
  after()
  return
end
if args.action == 'inventory' then return { inventory = items(pawn.Inventory) } end
local window = assert(cookingWindow(), 'no cooking window in viewport')
local function state()
  return {
    paused = statics:IsGamePaused(UEHelpers.GetWorld()),
    button = window.Cook_BTN:GetVisibility(),
    enabled = window.Cook_BTN:GetIsEnabled(),
    ingredients = { classPath(window['Ing 1 Class']), classPath(window['Ing 2 Class']),
      classPath(window['Ing 3 Class']), classPath(window['Ing 4 Class']) },
    cooking = window['Cooking In Progress'],
    meal = classPath(window['Meal Being Cooked']),
    inventory = items(pawn.Inventory),
  }
end
if args.action == 'state' then return state() end
if args.action == 'wait' then
  local finish = defer()
  local deadline = statics:GetRealTimeSeconds(UEHelpers.GetWorld()) + 25
  local function after()
    if not window['Cooking In Progress'] or statics:GetRealTimeSeconds(UEHelpers.GetWorld()) >= deadline then
      finish(state())
    else
      ExecuteInGameThreadAfterFrames(15, after)
    end
  end
  after()
  return
end
if args.action == 'add' then
  local grid = window.IngredientGrid
  for i = 0, grid:GetChildrenCount() - 1 do
    local slot = grid:GetChildAt(i)
    if slot:GetClass():GetFName():ToString() == 'Cooking_Ingredient_Button_C'
      and (args.item and classPath(itemInfo(slot.ItemInfo, 'ItemClass')) == args.item
        or args.name and itemInfo(slot.ItemInfo, 'ItemName'):ToString() == args.name) then
      assert(slot.ItemButton:GetIsEnabled(), 'ingredient button is disabled')
      return press(slot, 'ItemButton')
    end
  end
  error('no ingredient ' .. (args.item or args.name))
elseif args.action == 'cook' then
  local before = state()
  local click = press(window, 'Cook_BTN')
  return { before = before, click = click, after = state() }
elseif args.action == 'close' then
  return press(window, 'Close_BTN')
end
error('unknown action: ' .. tostring(args.action))
