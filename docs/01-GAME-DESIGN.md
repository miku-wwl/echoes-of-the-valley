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

## Signature mechanic: Valley Resonance

The *single authoritative* `ResonanceStage` integer (0–3) drives stream animation, plants, light, windmill, NPC reactions and ambient sound. Every scene load rebuilds the correct view from persisted progression; avoid saving dozens of separate scenery booleans. Capture a recognizable before/after clip for each milestone.

## Art/UX direction

Original stylized anime-fantasy 3D: warm wood, pastel foliage, simple rounded geometry, coherent UI; world starts muted but never unreadably dark. **Atelier Ryza** and **QQ Farm/Ranch** are only broad genre/interaction references: never copy characters, costumes, maps, UI or audio. A single small valley with a cohesive look matters more than expensive models. Prefer opaque vegetation, few materials and simple lighting on Radeon 780M. **1080p/30 FPS and 720p/60 FPS are unverified goals**.

**Cut:** fishing, cows, seasons, crop-farm grids, combat, skill trees, large economy, voice acting, multiplayer and copied asset packs.
