-- lab setup: with args.open, opens a level by its package path through OpenLevel, as travel does, without the
-- journey. With args.take, calls the named placed object's own Take Item, the handler its interaction runs.
-- Without either, lists every live placed pickup object in the loaded level, so a run can compare them with the
-- level's placements: its actor name, item, amount, and whether it is hidden or lets the player collide.
-- args.classes overrides the class paths to list.
if args.open then
    statics:OpenLevel(UEHelpers.GetWorld(), FName(args.open), true, '')
    return { opened = args.open }
end
if args.take then
    for _, actor in ipairs(liveAll('TagBasedInteractionObject_C')) do
        if actor:GetFName():ToString() == args.take then
            actor['Take Item'](actor)
            return { took = actor:GetFullName() }
        end
    end
    error('No live placed object named ' .. args.take)
end

local classes = args.classes or {
    '/Game/BloodlinesAlpha/Interactables/TagBasedInteractionObject.TagBasedInteractionObject_C',
    '/Game/BloodlinesAlpha/Interactables/InteractableObject.InteractableObject_C',
    '/Game/BloodlinesAlpha/Interactables/QuestBasedInteractionObject.QuestBasedInteractionObject_C',
    '/Game/BloodlinesAlpha/Interactables/LootItem_Pickup.LootItem_Pickup_C',
}
local world = UEHelpers.GetWorld()
local seen, found = {}, {}
for _, path in ipairs(classes) do
    local class = LoadAsset(path)
    for _, actor in ipairs(liveAll(class:GetFName():ToString())) do
        local name = actor:GetFName():ToString()
        if not seen[name] and actor:IsValid() then
            seen[name] = true
            local item = nil
            pcall(function() item = classPath(actor.Item) end)
            local amount = nil
            pcall(function() amount = actor['Amount of Items'] end)
            local location = actor:K2_GetActorLocation()
            found[#found + 1] = {
                name = name,
                class = actor:GetClass():GetFName():ToString(),
                item = item,
                amount = amount,
                hidden = actor.bHidden,
                collision = actor:GetActorEnableCollision(),
                location = { math.floor(location.X), math.floor(location.Y), math.floor(location.Z) },
            }
        end
    end
end
return { world = world:GetFullName(), count = #found, actors = found }
