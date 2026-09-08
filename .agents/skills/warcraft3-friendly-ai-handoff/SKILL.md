---
name: warcraft3-friendly-ai-handoff
description: Design or audit a Warcraft III campaign conversion where an originally AI-controlled friendly faction becomes an optional human slot, with one-time startup detection and preservation of the original AI for computer or empty starts. Use for classic JASS/GUI maps, especially 1.24e-compatible co-op conversions; do not use for enemy AI or mid-game player-leave takeover.
---

# Warcraft III Friendly AI Startup Handoff

Convert one original friendly AI faction into an optional human slot without losing the original AI fallback. Classify the slot once while the map initializes; do not change control mode when a human later leaves.

## Establish The Mission Contract

Before changing the map, identify:

- the exact JASS player index of the converted faction;
- every original mechanism owned by or conditional on that faction;
- which behavior applies when the slot starts as an active human, computer, empty, or not joined;
- which state must remain unchanged during takeover, especially ownership, diplomacy, quests, and story progress.

When the project has an `ORIGINAL_COOP_MECHANICS.md`, record each affected mechanism there before implementation. Do not infer that preserving the original `.ai` entry preserves all AI behavior: campaign triggers often separately provide resources, issue `CommandAI`, schedule attacks, and stop or restart AI around story phases.

Build a per-mission startup matrix with these columns:

| Original mechanism | Source trigger/function | Human at start | Computer/empty/not joined at start |
| --- | --- | --- | --- |
| Campaign AI script | `StartCampaignAI` call site | Suppress | Preserve |
| Trigger-issued AI commands | `CommandAI` call sites | Suppress | Preserve |
| AI economy | initial grant/subsidy triggers | Human economy | Preserve |

Extend the matrix for every discovered mechanism. Values in the example are categories, not mission-specific decisions.

## Detect An Active Human At Startup

For Warcraft III 1.24e-compatible JASS, classify the converted slot as an active human only when both conditions are true:

```jass
function CoopFriendlyPlayerIsActiveHuman takes nothing returns boolean
    return GetPlayerController(Player(COOP_FRIENDLY_PLAYER_INDEX)) == MAP_CONTROL_USER and GetPlayerSlotState(Player(COOP_FRIENDLY_PLAYER_INDEX)) == PLAYER_SLOT_STATE_PLAYING
endfunction
```

Use a constant integer for `COOP_FRIENDLY_PLAYER_INDEX`, set to the mission's actual zero-based JASS player index.

Do not use either condition alone:

- `MAP_CONTROL_USER` distinguishes user control from computer control.
- `PLAYER_SLOT_STATE_PLAYING` excludes an unused or empty slot.

At mission initialization, after lobby slot settings are available and before any converted-faction AI or economy initialization:

- active human: enter human mode and suppress original friendly-AI-only mechanisms;
- computer, empty, or not joined: enter AI mode and preserve the original AI path.

The two-condition test is a one-time startup classification. Save its result and do not query live controller or slot state again to change behavior.

## Store The Startup Control Mode

Keep an explicit per-faction boolean such as `udg_CoopFriendlyAIMode`. All converted-faction AI-only triggers must consult this state or route through helpers that do.

Keep campaign AI runtime state and recurring-system initialization separate from the fixed control-mode flag. An original cinematic or phase transition may call `StopCampaignAI`; a later story-driven restart in the AI startup branch must still be possible without reclassifying the slot.

## Keep The Mode Fixed

The startup classification is immutable for the rest of the match. Do not:

- register `EVENT_PLAYER_LEAVE` for takeover;
- switch a human-start faction back to AI mode after a disconnect or departure;
- reset resources, transfer assets, enable shared control, or add victory/defeat behavior because a player left;
- add live tests for custom leave handling.

Mid-game human departure uses Warcraft III's default engine behavior and is outside the project's custom feature and acceptance scope. Runtime player-type or slot-state changes must not accidentally activate AI-only callbacks.

## Preserve The Whole Original AI Path

Trace and classify all original friendly-faction AI dependencies, including:

- `StartCampaignAI` and `StopCampaignAI`;
- `CommandAI` and other trigger-issued orders;
- initial AI resources, periodic grants, handicap, upkeep, and tech restrictions;
- difficulty-dependent attack timers and assault composition;
- rebuild, rescue, transport, or guard behavior implemented outside the `.ai` file;
- cinematic pause/stop/restart behavior and skip branches;
- quest or story-phase gates that change the AI plan;
- victory, defeat, hero-death, and player-specific event registration;
- campaign-cache or cross-level state used by the faction.

For each mechanism, choose one of these implementation shapes:

- initialize once and gate each callback by AI mode;
- create or start it only in the AI startup branch, protected by a dedicated initialization flag;
- preserve story-phase stop and restart behavior while remaining in the fixed AI mode;
- intentionally omit it in human mode, with that decision recorded.

Starting the original campaign AI file may assume particular surviving buildings or units. Preserve current assets and allow the original AI to work with them unless evidence shows that controlled reconstruction is required.

## Preserve Independent State Unless Decided Otherwise

AI takeover does not itself authorize changes to:

- unit and building ownership;
- alliances, shared vision, allied victory, or enemy relationships;
- quest progress and cinematic state;
- resources, except for a specifically recorded startup rule;
- transfer of the faction to another player.

Keep these decisions separate from startup control-mode detection.

## 1.24e Compatibility

Use classic JASS natives available to the target map. The core startup detection APIs for this pattern are:

- `GetPlayerController`
- `GetPlayerSlotState`
- `StartCampaignAI`, `StopCampaignAI`, and `CommandAI` when used by the original map

For a 1.24e target, do not introduce `Blz*` natives or later patch-only APIs. Preserve the map's generated-JASS conventions when patching GUI-authored maps.

## Verification

Perform static checks before requesting live testing:

- startup detection requires both `MAP_CONTROL_USER` and `PLAYER_SLOT_STATE_PLAYING`;
- controller and slot state are classified once before affected systems initialize;
- the saved control mode, not a live slot query, gates later behavior;
- human mode cannot reach original friendly-AI-only entry points;
- computer, empty, and not-joined starts can reach every retained original AI mechanism;
- the map registers no custom `EVENT_PLAYER_LEAVE` takeover path;
- recurring timers and triggers cannot be duplicated;
- ownership and diplomacy are not mutated by takeover unless explicitly required.

Then record live tests for the target game version. At minimum cover active-human start, computer start, and empty/not-joined start, including AI resources, AI assault cadence, cinematics, tasks, victory, and defeat. Empty-slot engine behavior must remain marked unverified until tested in the actual target client.
