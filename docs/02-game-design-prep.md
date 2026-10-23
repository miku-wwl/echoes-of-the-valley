# 02 — Game design preparation

**Design preparation v0.1 — 2026-10-23 NZDT. No GDD v0.1 existed in the audited repository.** This baseline derives from the user's brief. All numeric tuning, character concepts, quest wording, narrative, and emotional progression require post-theme review. Nothing here is implemented.

## Player fantasy and character

Be a capable young valley caretaker whose ordinary work helps neighbors and changes the surroundings. Proposed protagonist: an original adult traveler with a short practical cape, work gloves, gathered satchel, and readable herb/stone tool silhouettes. Avoid resemblance to any existing RPG protagonist. Exact identity, backstory, final clothing, motivation, and musical response are pending theme review.

Play is peaceful: walk, inspect, gather, care, craft, talk, and deliver. There is no combat, stamina failure, animal death, crop simulation, or real-time waiting outside play.

## World and NPCs

One compact walkable valley, planned traversal radius roughly 60–80 meters: arrival clearing, village hub/workbench, herb meadow, shallow ore outcrop, chicken pen, and a restoration focal point. Paths form a short loop; the player sees the hub from resource routes. No interiors requiring another gameplay scene; no procedural world or platforming dependency.

Three proposed NPC roles, names/dialogue pending: herbalist introduces gathering and the workbench; keeper sells the first chicken and teaches feeding/eggs; restorer requests braces and presents completion. Each has short readable dialogue, one clear objective, progress feedback, and a completed conversation.

## Core loop and inventory

**GATHER → RAISE → CRAFT → RESTORE.** Exploration reveals resources; resources enter inventory; crafting prepares useful goods; NPC delivery changes village state. Chicken care supplies an egg quest and reinforces the caring loop.

Proposed item definitions use stable IDs, English display text, category, icon, stack cap, and whether a quest locks the item. Runtime inventory stores item IDs and integer quantities, separate from static ScriptableObject definitions. Currency is a nonnegative integer, not a tradable item.

| Item ID | Role | Acquisition/use |
| --- | --- | --- |
| herb | Renewable raw material | Meadow; sachets and feed |
| forage_seed | Renewable feed ingredient | Dedicated meadow seed patch |
| ore | Renewable raw material | Outcrop; braces |
| feed | Animal care | Craft two at a time |
| herbal_sachet | First crafted delivery | Herbalist request |
| repair_brace | Restoration delivery | Final request |
| egg | Animal product | Keeper request |

Start with 20 coins and no required consumables. A chicken costs 10 coins; first delivery grants 10. No selling, buying duplicate chickens, or optional currency sinks in P0. A single chicken purchase is checked and persisted. Inventory proposal: 16 stack slots, cap 99 for these items. Required items cannot be discarded in P0; inputs respawn. Capacity failure leaves the source/input unchanged and explains how to proceed.

## Gathering, pasture, and crafting

Herbs, seeds, and ore use one interact action, a brief visual response, and immediate item feedback. Tools are implicit access flags, not breakable or purchasable inventory items. Nodes regenerate after approximately 45–60 seconds of active play and never permanently exhaust a quest supply.

Chicken states: **not owned → chick → fed chick → adult → egg ready → egg collected**. Feeding consumes one feed only when needed; proposed growth is 60 active seconds, followed by an egg after 45 active seconds. One egg may wait in the pen; collection cannot lose it on a full inventory. After collection an adult accepts one feed to start the next egg cycle. Animals cannot die, shrink, or become permanently unhappy. Pause stops timers; loading restores elapsed progress with no offline catch-up or system-clock rewards. These times are design targets, not measured playtest results.

| Recipe ID | Required inputs | Output | Unlock |
| --- | --- | --- | --- |
| herbal_sachet | 2 herb | 1 herbal_sachet | Available at start |
| pasture_feed | 1 herb + 1 forage_seed | 2 feed | Available at start |
| repair_brace | 2 ore | 1 repair_brace | Available at start |

At the hub workbench, show owned/needed counts and one craft button. One craft removes inputs and inserts outputs atomically; an invalid or full-inventory attempt changes nothing. No crafting timers, quality tiers, random success, or minigame. All three recipes must be usable in the final P0 build.

## Quests and restoration

| Quest | Acceptance and objective | Reward and visible change |
| --- | --- | --- |
| Q1, herbalist | Accept; gather 2 herbs; craft and deliver 1 sachet | 10 coins; village state 0 → 1; one planter/path segment restored |
| Q2, keeper | After Q1; buy chick, craft feed, feed/grow, collect and deliver 1 egg | Village state 1 → 2; pen banner/yard restored; Q3 available |
| Q3, restorer | After Q2; mine 4 ore; craft and deliver 2 braces | Village state 2 → 3; focal point restored; ending prompt available |

A quest has locked/available/accepted/ready/completed states, with completion granted once. Readiness is derived from current inventory; gathering before acceptance does not erase progress. Delivery consumes exact goods only on confirmation and commits reward plus restoration in the same operation. Closing dialogue or repeatedly pressing interact cannot repeat a reward. Restored objects are world-state views, not independent sources of progression truth.

The final visual feature (garden, meeting place, or another focal point), narrative rationale, emotional sequence, and ending text remain **pending official theme review**. The numbered restoration states describe functional progression only.

## Onboarding and 15–20-minute structure

| Approximate elapsed play | Player action | Comprehension check |
| --- | --- | --- |
| 0–2 min | Arrival, movement, interact prompt, meet herbalist | Player identifies workbench and objective without external help |
| 2–6 min | Herbs → inventory → sachet → Q1 delivery | First visible change demonstrates the complete slice |
| 6–11 min | Buy chick, gather seed/herb, craft feed, care, egg | Timers overlap with exploration; no idle mandatory wait |
| 11–16 min | Q2 delivery, mine, craft braces, final delivery | Quest tracker communicates counts and location |
| 16–20 min | Inspect restoration, ending, optional revisit/save | Completion is explicit; credits and return to menu work |

This is a proposed pacing budget, **NOT RUN**. Real independent playtests must determine whether travel and animal timing fit it.

## Save, ending, and failure prevention

Save inventory, coins, position, animal progress, node cooldowns, quest states/reward flags, restoration, and ending acknowledgement. Provide New Game, Continue when a valid save exists, Save, Pause, and Quit. Save after successful progression transactions and on explicit save; see [architecture](03-technical-architecture.md).

Win condition: Q1–Q3 completed, state 3 applied, and final interaction acknowledged. Show a short theme-reviewed ending, then allow continued peaceful exploration. No loss screen or irreversible wrong choice. Respawn an out-of-bounds player at a safe hub marker; offer camera reset; ensure keyboard-only UI; do not require audio to understand objectives. Corrupt saves offer recovery without silently overwriting the original.
