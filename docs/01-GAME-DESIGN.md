# 01 — Game design and musical theme

**Decision · 2026-10-25 NZDT:** `Echoes of the Valley` is a **cozy fantasy restoration adventure** for a solo developer. Windows x64, English-first, approximately 10–15 minutes (20-minute ceiling). No combat or online dependency.

## From music to player actions

The official jam theme is a **60-second composition by Jason Grace** ([official page](https://itch.io/jam/thatgamejam02)). The user's original WAV was analyzed structurally:

| Music | Measured contrast | Interpretation and gameplay |
| --- | --- | --- |
| 0–20s | Quieter, sparser opening | **Silence**: desaturated, inactive valley; arrival and exploration |
| ~20–25s | Pronounced increase in energy and rhythmic density | **Awakening**: first meaningful repair sends out a visible pulse |
| ~25–54s | Sustained denser, more forceful section | **Resonance**: activity, care, light and color spread |
| ~54–60s | Sharp reduction and ending | **Rest**: repaired village, warm resolution |

**Evidence caveat:** duration, waveform and spectral changes were measured. Individual instruments were not reliably heard by the assistant. “Silence → Awakening → Resonance → Rest” is a **design reading**, not an official label or proven emotion. The developer should directly listen, especially around 20s, before locking dialogue/audio. This is **not** a rhythm game and actions are not time-locked to the track.

## Player promise

**Every meaningful act of care makes the world visibly answer back.** The player sees the first unmistakable restoration within 3–5 minutes. Gathering and crafting are a means of changing the village, not grinding inventory counters.

## Competition MVP — no expansion until a full playable ending

- **World:** one compact walkable valley: hub, herb meadow, ore outcrop, stream, hen pen, windmill; three original NPCs.
- **Hero:** one original anime-inspired fantasy caretaker, simple walk/idle/interact; WASD/mouse/E/I/Escape.
- **Resources:** herbs, renewable seeds, ore; stack inventory and simple coins.
- **Crafting:** exactly three recipes: Spring Tonic, Chicken Feed, Windmill Braces.
- **Livestock:** buy one chicken, feed, short active-time wait, collect egg; no death, breeding or long idle timers.
- **Quests:** three deliveries, persistent visual restoration stages, clear ending; local save/load.

| Stage | Player journey | Mandatory audiovisual change |
| --- | --- | --- |
| S0 → S1 | Gather **2 herbs**, craft/deliver **Spring Tonic** to herbalist | Stream begins flowing; plants regain color; outward pulse |
| S1 → S2 | Buy **1 chicken** (starting money sufficient); craft/feed **Chicken Feed**; deliver **1 egg** to keeper | Garden blooms; hen yard and NPC behaviors awaken |
| S2 → S3 | Mine **4 ore**; craft/deliver **Windmill Braces** to restorer | Windmill turns; lighting and ambient life return |
| Ending | Speak to final NPC, enjoy restored valley | Peaceful resolution, credits, optional exploration |

Numbers are starting **tuning proposals**, not tested balance. Essential inputs regenerate; crafting/quest confirmations must never duplicate rewards or consume unavailable items. Chicken timers pause during menus and never force excessive waiting.

**Deliberate deviation (Phase 3):** the table above proposed *buying* a chicken for S1 → S2. Phase 2 had already shipped Clover as a resident hen with a working feed, wait and collect cycle, so the second quest keeps her and simply asks for an egg: there is no purchase step, no second chicken and no shop (coins are only a reward, see below). This keeps the working Phase 2 hen untouched and removes a place where progression could stall.

## As implemented (Phase 3)

| Stage | `ResonanceStage` | Who and what | What the valley shows |
| --- | --- | --- | --- |
| S0 → S1 | `AwakeningStream` | Mira offers *Restore the Stream*; pour a Spring Tonic into the ruined spring | Murky trickle becomes a wide clear stream, the basin fills with a fountain, 18 flower patches and 4 saplings bloom along the banks, a ring of light, golden light |
| S1 → S2 | `BloomingGarden` | Wren, the Garden Keeper, wants one of Clover's eggs | The withered garden (dry soil, drooping stalks, bare vines) flowers in many colours, the hen yard gets flower pots and bunting, pollen drifts over the garden, a larger ring of light, warmer light, the three villagers stand up and sway |
| S2 → S3 | `RestoredValley` | Alder, the Windmill Restorer, needs Windmill Braces (4 Echo Ore) | The broken windmill (snapped sails, fallen beams) is mended with glowing ore braces and its sails turn, lanterns and flower patches appear through the village, drifting fireflies, a pulse that crosses the whole valley, the low golden-hour sun, the villagers bounce and turn about |
| Ending | (unchanged) | Mira's closing conversation | A screen over the restored valley: title, *The valley has found its voice again.*, Silence · Awakening · Resonance · Rest, credits, **Continue Exploring** and **Main Menu** |

- **Everything derives from one integer.** `ResonanceView` draws the world from `GameSession.Resonance` alone; nothing about the garden, the windmill or the lanterns is saved separately. A loaded stage appears at once with no replayed reward or animation; a stage reached while playing runs a short transition. Stage 3 keeps everything stages 1 and 2 showed.
- **Rewards (coins, once each):** 25, 30, 40. Coins have no sink yet; they are a reward counter, not an economy.
- **Anti-softlock rules:** every ingredient comes from a renewable node or from Clover; holding the egg or the braces *before* accepting the quest is fine (the delivery only looks at the satchel); a delivery without the item changes nothing and says what is missing; each delivery is rejected the second time; the satchel has one slot per item type, so no stack can block another.
- **Guidance:** the objective panel is a pure function of the session (`QuestLog`) and always names who to talk to, what is still needed, where to gather, where to craft and where to deliver.
- **People:** Mira (opening guide, stream quest, the closing conversation), Wren (garden), Alder (windmill). Names and all dialogue are original and AI-written, to be reviewed by the developer.
- **The ending is real state:** `EndingSeen` is saved. Loading never replays it; Mira afterwards only chats. The screen never traps the player: Enter, E or Esc continue exploring, M or the button leaves for the title.

## Signature mechanic: Valley Resonance

The *single authoritative* `ResonanceStage` integer (0–3) drives stream animation, plants, light, windmill, NPC reactions and ambient sound. Every scene load rebuilds the correct view from persisted progression; avoid saving dozens of separate scenery booleans. Capture a recognizable before/after clip for each milestone.

## Art/UX direction

Original stylized anime-fantasy 3D: warm wood, pastel foliage, simple rounded geometry, coherent UI; world starts muted but never unreadably dark. **Atelier Ryza** and **QQ Farm/Ranch** are only broad genre/interaction references: never copy characters, costumes, maps, UI or audio. A single small valley with a cohesive look matters more than expensive models. Prefer opaque vegetation, few materials and simple lighting on Radeon 780M. **1080p/30 FPS and 720p/60 FPS are unverified goals**.

**Cut:** fishing, cows, seasons, crop-farm grids, combat, skill trees, large economy, voice acting, multiplayer and copied asset packs.
