-- Lists the persistent game instance's Completed Achievements, where a taken placed object records its Tag so the
-- object stays gone when its level loads again.
local instance = live('BP_PersistantGameInstance_C')
local tags = {}
instance['Completed Achievements']:ForEach(function(_, element)
    tags[#tags + 1] = element:get():ToString()
end)
return { count = #tags, tags = tags }
