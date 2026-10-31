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

1. Observe every claim the entry makes in the running game on the build it names, as the [runtime-checks skill](../runtime-checks/SKILL.md) describes under "What an observation must show". Every item the entry notes needs its own observation, because a condition in the data can include items that behave correctly. A script that checks them all in one run, such as `tools/runtime/checks/noted-armor.mjs`, keeps that cheap. Add the steps to `tools/runtime/checks/issues.sh`, so a recheck repeats them.
2. Write the entry. `effect` and `workaround` speak to players and come first. `steps` are what a player can do without mods, each with what they should see. `observed` states what the runs showed. `cause` is derived from the game's code and data, and `citations` name the functions as `package:function`, as `records/code/index.json` lists them. `builds` lists the Steam builds the runs used. A new entry has `"report": "notReported"` and `"reportUrl": null`.
3. Make the catalog note the affected pages. A tooltip cause in `src/Bloodline/Catalog/TooltipCheck.cs` marked as an issue notes every page where its difference concerns a shown value. Other defects need a detector in `src/Bloodline/Catalog/KnownIssues.cs` that lists only items a run observed.
4. Run `catalog` and check the notes on the Known Issues page and on an affected item. The catalog fails when a note or tooltip cause names an entry that does not exist, and when an entry cites a function that the build's code index lacks or did not parse completely.
5. Name the runs that reproduced the defect in the commit body, not in the entry.

## Recheck after a game update

After extracting and cataloging a new build, run the tooltip sweep and every check in `tools/runtime/checks/` once on the new build. Add the build to the `builds` of each entry whose result did not change. Look into every result that changed: remove the entry together with its tooltip cause or detector when the defect is gone, and repeat the check when its result can vary. Coverage then lists any tooltip difference that is left.

## Report to the developer

`bun scripts/issue-report.mjs <issue-id>` prints a report for the game's Discord from the entry. Without an ID it prints every entry that is not reported yet. Ask before posting a report. After posting, set `report` to `reported` and `reportUrl` to the message link.
