-- lab setup: sets a crafting level on the game instance directly, to list what a workbench offers at each level
-- The crafting window that the anvil, the engineer's workbench and the smelting oven open lists its items in one
-- category per tier. For each level in args.levels, sets the game instance's args.field, such as "Weaponsmithing
-- Level", calls the window's own args.fill, such as "Get Player Weaponsmithing Level and Set Available Items", and
-- lists each category's tier, whether the game marks it available, and its items.
-- Run it with a character in the world:
-- lab.mjs eval tools/runtime/checks/crafting-tiers.lua '{"field":"Smelting Level","fill":"Get Player Smelting Level and Set Available Items","levels":[0,1,2]}'
local pawn = requirePlayer()
local gi = live("BP_PersistantGameInstance_C")
local library = StaticFindObject("/Script/UMG.Default__WidgetBlueprintLibrary")
local class = LoadAsset("/Game/BloodlinesAlpha/Crafting.Crafting_C")
local window = library:Create(pawn, class, UEHelpers.GetPlayerController())
local original = gi[args.field]

local function categories()
  local list = {}
  local box = window.ScrollBox_0
  for i = 0, box:GetChildrenCount() - 1 do
    local child = box:GetChildAt(i)
    if child:GetClass():GetFName():ToString():find("CraftingList_LevelCategory", 1, true) then
      local entry = { tier = child["Crafting Level"], available = child["Contents Available to Player?"], items = {} }
      local items = child.Items
      for j = 1, #items do
        local item = items[j]
        entry.items[#entry.items + 1] = item and item:IsValid() and item:GetFName():ToString() or "?"
      end
      list[#list + 1] = entry
    end
  end
  return list
end

-- The function fills the list at once, so each level is read right after the call.
local results = {}
for _, level in ipairs(args.levels) do
  window.ScrollBox_0:ClearChildren()
  gi[args.field] = level
  window[args.fill](window)
  local available = {}
  local found = categories()
  for _, category in ipairs(found) do
    if category.available then available[#available + 1] = category.tier end
  end
  -- The categories and their items are the same at every level, so only the first level lists them.
  results[#results + 1] = { level = level, available = available, categories = #results == 0 and found or nil }
end
gi[args.field] = original
return { field = args.field, original = original, levels = results }
