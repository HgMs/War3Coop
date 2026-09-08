---
name: warcraft3-friendly-ai-handoff
description: Design or audit a Warcraft III campaign conversion where an originally AI-controlled friendly faction may instead be controlled by a human, including startup slot detection, human-leave fallback, and restoration of the original AI mechanisms. Use for classic JASS/GUI maps, especially 1.24e-compatible co-op conversions; do not use for enemy AI that can never become a player faction.
---

# Warcraft III Friendly AI Handoff

Convert one original friendly AI faction into an optional human slot without losing the original AI fallback. Treat this as a control-mode state machine, not as a one-time player-type check.

## Establish The Mission Contract

Before changing the map, identify:

- the exact JASS player index of the converted faction;
- every original mechanism owned by or conditional on that faction;
- which behavior applies when the slot starts as a human, computer, or empty slot;
- which behavior must resume if the human leaves;
- which state must remain unchanged during takeover, especially ownership, diplomacy, quests, and story progress.

When the project has an `ORIGINAL_COOP_MECHANICS.md`, record each affected mechanism there before implementation. Do not infer that starting the original `.ai` script restores all AI behavior: campaign triggers often separately provide resources, issue `CommandAI`, schedule attacks, and stop or restart AI around story phases.

Build a per-mission control matrix with these columns:

| Original mechanism | Source trigger/function | Human at start | Computer/empty at start | Human leaves | Resume basis |
| --- | --- | --- | --- | --- | --- |
| Campaign AI script | `StartCampaignAI` call site | Suppress | Preserve | Restore | Current story phase |
| Trigger-issued AI commands | `CommandAI` call sites | Suppress | Preserve | Restore | Current objective/state |
| AI economy | initial grant/subsidy triggers | Human economy | Preserve | Mission decision | One-time vs periodic |

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
- `PLAYER_SLOT_STATE_PLAYING` excludes an unused/empty slot and a player who has already left.

At mission initialization, after lobby slot settings are available and before any converted-faction AI or economy initialization:

- active human: enter human mode and suppress original friendly-AI-only mechanisms;
- computer or empty: enter AI mode and preserve the original AI path.

The two-condition test is a startup classification. Do not repeatedly query it as the sole runtime source of truth.

## Track Control Mode Explicitly

Keep an explicit per-faction boolean such as `udg_CoopFriendlyAIMode`. All converted-faction AI-only triggers must consult this state or route through helpers that do.

Separate these concerns:

- **control mode**: whether the faction is currently AI-managed;
- **one-time takeover work**: resource reset or other changes made only when a human leaves;
- **campaign AI runtime state**: whether the original `.ai` script should currently be active for the present story phase;
- **recurring systems**: subsidies, assaults, and timers that must not be registered or started twice.

Do not use a single `AIStarted` flag for all four meanings. In particular, an original cinematic or phase transition may call `StopCampaignAI`; the later restart must still be possible.

## Handle A Human Leaving

Register `EVENT_PLAYER_LEAVE` for the converted slot and route it to one idempotent AI-entry function:

```jass
function CoopFriendlyPlayerLeaves takes nothing returns nothing
    call CoopEnableFriendlyAIMode(true)
endfunction

function CoopInitFriendlyControl takes nothing returns nothing
    local trigger leaveTrigger = CreateTrigger()

    call TriggerRegisterPlayerEvent(leaveTrigger, Player(COOP_FRIENDLY_PLAYER_INDEX), EVENT_PLAYER_LEAVE)
    call TriggerAddAction(leaveTrigger, function CoopFriendlyPlayerLeaves)

    if CoopFriendlyPlayerIsActiveHuman() then
        set udg_CoopFriendlyAIMode = false
    else
        call CoopEnableFriendlyAIMode(false)
    endif

    set leaveTrigger = null
endfunction
```

The boolean argument distinguishes a mid-game human departure from computer/empty startup. Use it only for behavior the mission contract says differs, such as a leave-time resource reset.

Inside `CoopEnableFriendlyAIMode`:

1. Set the control-mode boolean before invoking AI side effects.
2. Apply takeover-only work once when the call represents a human departure.
3. Reconcile the original AI with the current story phase.
4. Enable or resume recurring AI-only systems without duplicating triggers or timers.

Make repeated calls harmless. A leave event can race with story triggers, and phase changes may need to call the same reconciliation helper later.

Do not depend on the engine changing controller or slot-state values in a particular order during `EVENT_PLAYER_LEAVE`. The leave handler explicitly changes the stored control mode.

## Restore The Whole Original AI Path

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
- create/start it when AI mode begins, protected by a dedicated initialization flag;
- reconcile it from an explicit current-phase variable when AI mode begins;
- intentionally omit it in human mode, with that decision recorded.

Use the current mission state when a human leaves. Do not replay the opening, recreate dead units, reset completed quests, or restart elapsed story stages unless the mission contract explicitly requires that reset.

Starting the original campaign AI file may assume particular surviving buildings or units. Preserve current assets and allow the original AI to work with them unless evidence shows that controlled reconstruction is required.

## Preserve Independent State Unless Decided Otherwise

AI takeover does not itself authorize changes to:

- unit and building ownership;
- alliances, shared vision, allied victory, or enemy relationships;
- quest progress and cinematic state;
- resources, except for a specifically recorded startup or leave-time rule;
- transfer of the faction to another player.

Keep these decisions separate from control-mode detection. In particular, do not modify diplomacy in the leave handler merely because AI control resumes.

## 1.24e Compatibility

Use classic JASS natives available to the target map. The core detection and event APIs for this pattern are:

- `GetPlayerController`
- `GetPlayerSlotState`
- `TriggerRegisterPlayerEvent`
- `EVENT_PLAYER_LEAVE`
- `StartCampaignAI`, `StopCampaignAI`, and `CommandAI` when used by the original map

For a 1.24e target, do not introduce `Blz*` natives or later patch-only APIs. Preserve the map's generated-JASS conventions when patching GUI-authored maps.

## Verification

Perform static checks before requesting live testing:

- startup detection requires both `MAP_CONTROL_USER` and `PLAYER_SLOT_STATE_PLAYING`;
- the leave event is registered for the correct zero-based player index;
- human mode cannot reach original friendly-AI-only entry points;
- computer/empty startup can reach every retained original AI mechanism;
- leave mode performs each takeover-only action exactly once;
- recurring timers and triggers cannot be duplicated;
- current-phase reconciliation covers early, middle, and late mission states;
- ownership and diplomacy are not mutated by takeover unless explicitly required.

Then record live tests for the target game version. At minimum cover human start, computer start, empty slot, early/mid/late human leave, leave during a cinematic, AI resource cadence, AI assault cadence, and repeated phase transitions after takeover. Empty-slot behavior and engine event timing must remain marked unverified until tested in the actual target client.
