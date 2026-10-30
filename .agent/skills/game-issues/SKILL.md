---
name: game-issues
description: Decide who owns a disagreement between the catalog, the game's code and the running game, record reproduced game defects in issues/known-issues.json, recheck them after a game update, and draft reports for the game's developer. Use when a tooltip, a fact or an observation disagrees with another, when Coverage lists a tooltip difference, after a game update, or before reporting a defect.
---

# Game issues

A known issue is a defect in The Bloodline itself that a lab run reproduced. The registry `issues/known-issues.json` holds every entry. `issues/known-issues.schema.json` is generated from `KnownIssue` in `src/Bloodline/SiteData/SiteDataModel.cs` by `bun run types`. The catalog publishes the entries as `issues.json`, notes them on the item pages whose shown values they affect, and the site lists them on its Known Issues page.

## Who owns a disagreement

Decide the owner before recording anything, as the `verification/game-claims` spec requires:

| Finding | Owner | Where it goes |
| --- | --- | --- |
| The catalog publishes a value that the game does not apply | The catalog | A fix where the catalog produces the value, with a regression test on invented data |
| A tool misreads behaviour that is correct | The tool | A fix in the tool |
| The game contradicts its own code, data or interface, reproduced in a lab run | The game | An entry in `issues/known-issues.json` |
| Code suggests a defect that no run has reproduced | Open | An open question in the change's design, not an entry |
| No cause is known | Open | Coverage, which lists tooltip differences without a cause by itself |

Item pages always show the value the game applies. A tooltip that shows another value is a known issue, not the fact.

## Add an entry

1. Reproduce the defect twice, in separate runs on fresh saves, with the [runtime-checks skill](../runtime-checks/SKILL.md), in `launch render`. Each run needs a screenshot of what a player sees at every step, such as the tooltip, the character sheet's Armor line, the Health bar or the status effects, and the game's own values next to it. Look at every screenshot. A claim about a tooltip line needs a screenshot that shows the line, because the widget may keep it hidden. Add the steps to `tools/runtime/checks/issues.sh`, so a recheck repeats them.
2. Write the entry. `effect` and `workaround` speak to players and come first. `steps` are what a player can do without mods, each with what they should see. `observed` states what the runs showed. `cause` is derived from the game's code and data, and `citations` name the functions as `package:function`, as `records/code/index.json` lists them. `builds` lists the Steam builds the runs used. A new entry has `"report": "notReported"` and `"reportUrl": null`.
3. Make the catalog note the affected pages. A tooltip cause in `src/Bloodline/Catalog/TooltipCheck.cs` marked as an issue notes every page where its difference concerns a shown value. Other defects need a detector in `src/Bloodline/Catalog/KnownIssues.cs`. Every page a note reaches needs its own observation: `tools/runtime/checks/noted-armor.mjs` covers the armor issues, and a detector for other items lists only items that a run showed.
4. Run `catalog` and check the notes on the Known Issues page and on an affected item. The catalog fails when a note or tooltip cause names an entry that does not exist, and when an entry cites a function that the build's code index lacks or did not parse completely.
5. Name the runs that reproduced the defect in the commit body, not in the entry.

## Recheck after a game update

After extracting and cataloging a new build, repeat every entry's steps in the lab on the new build, twice. If the defect still reproduces, add the build to the entry's `builds`. If it no longer does, remove the entry together with its tooltip cause or detector. Coverage then lists any tooltip difference that is left.

## Report to the developer

`bun scripts/issue-report.mjs <issue-id>` prints a report for the game's Discord from the entry. Without an ID it prints every entry that is not reported yet. Ask before posting a report. After posting, set `report` to `reported` and `reportUrl` to the message link.
