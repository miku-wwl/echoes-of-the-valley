# 02 — Code-first Unity roadmap and acceptance gates

**Deadline:** **2026-11-08 20:59 NZDT**. Internal upload target: Nov 7. Unity Phase 0.5 was a separate **blank-template environment probe**, not a playable game. The real competition project is `game/`, created 2026-10-25; Phase 1 results are recorded [below](#phase-1-status-2026-10-25).

## Stack and structure

- Unity **6000.3.27f1**, Universal 3D/URP, C#, **Input System as the only active input handler** (use `UnityEngine.InputSystem`, never legacy `Input`); Windows x64 **Mono** + D3D11 first. Cinemachine is not used.
- Editor: `D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe`; Blender 4.5.15 LTS. Prefer D: for project, Unity cache and builds; C: was low on space.
- `game/` is the Unity root. `Assets/Echoes/` contains `Scenes`, `Scripts/{Runtime,Editor}`, `Prefabs`, `Art` (materials, terrain data, meshes) and `Tests/{EditMode,PlayMode}`; `Audio` and `UI` folders arrive with content. Two scenes: `MainMenu` (title, **startup scene**; it replaces the originally planned `Boot` name) and `Valley` (game).
- Generate scenes and prefabs using **Unity Editor C# scripts**, `-batchmode` and `-executeMethod`; never fabricate Unity YAML manually. Change `Assets/Echoes/Scripts/Editor` and re-run `tools/Invoke-Echoes.ps1 -Task Setup` (idempotent) instead of editing generated `.unity`/`.prefab` files. GUI work is optional for authoring, but **actual player interactions must be manually verified**.
- Unity's Bee compiler can exhaust memory on this machine (WSL, VS Code and browsers hold most of it): the wrapper caps `BEE_BUILD_THREADS`, and a Windows build that fails with `HRESULT: 0x8007000E` is out-of-memory, not a script error.
- One `GameSession` owns plain-C# game state; MonoBehaviours handle input, views and animation; immutable definitions hold item/recipe/quest IDs. No backend, networking, ECS or imported full gameplay systems. Implemented in Phase 2 under `Scripts/Runtime/Gameplay` (items, `Inventory`, `RecipeBook`, `GameSession`, `SaveCodec`/`SaveSystem`) with no scene dependency, so the tests drive it directly; scene scripts only forward input to it and draw its state.

## Invariants

1. Gather/buy/feed/craft/deliver: **validate → one atomic commit → notify**, including projected inventory capacity and repeat-press defense.
2. `ResonanceStage` 0–3 changes only with committed quest rewards, once; save/reload reconstructs views without replaying rewards.
3. No negative item/coin counts, no consumed reward with full inventory, no nonrenewable progression dependency, no stalled chicken wait.
4. Versioned local JSON save with stable IDs, input validation, temporary write and backup. A corrupt save must fail safely; no fabricated durability guarantee.
5. Controls/UI: WASD/arrows, mouse follow/look, E interact, I inventory, Esc pause; English menus, keyboard focus and safe spawn.

## Phases

| Phase / target | Deliverable | PASS evidence |
| --- | --- | --- |
| **P1 Foundation · Oct 25–26** | Run code-first bootstrap; generate `MainMenu`/`Valley`; controllable hero/collision/camera; interaction prompt; title/pause/quit | Actual Editor compile and **Windows player** start, move, look, interact, pause and quit test. **Status: gate A–L PASS, human playtest pending - see [below](#phase-1-status-2026-10-25).** |
| **P2 Playable slice · Oct 26–29** | Nodes/inventory/coins, three recipes, hen states, save/load and **first stream repair** | Real player **gather → craft → deliver → S1**, quantity/dupe tests, hen and saved-state roundtrip. **Status: automated gate A-N PASS, human playtest pending - see [below](#phase-2-status-2026-10-25).** |
| **P3 Full greybox · Oct 29–31** | Three quests, S0→S3, clear ending, pacing | Entire fresh-save run, repeat-confirm tests, save/reload at each restoration, no softlock. |
| **P4 Cohesive art · Nov 1–4** | Original hero/hen/village kit, color transitions, light FX, English UI and licensed sound | Styled Windows build; no pink assets; all quests still work; provenance documented. |
| **P5 QA + Release · Nov 5–7** | Polish, performance, credits/AI disclosure, archive, itch.io entry | Unpacked Windows EXE works with its data folder; full ending; upload is visible in jam before Nov 8 20:59 NZDT. |

**PASS** = directly observed evidence; **PARTIAL** = missing evidence/criteria; **FAIL** = build failure, material bug, wrong rights or disqualifying rule violation. End each phase with tests, exact status, commit SHA if committed and remaining blockers. Never label an untouched stock-template player as a game test.

## Phase 1 status (2026-10-25)

**`PHASE1_STATUS=PASS`** against the Phase 1 acceptance gate (A-L: project, compile, scenes, movement, camera, interaction framework, three interactables, objective, menus, Windows x64 build, standalone smoke test, no blocking compile errors). The stricter table wording above (a *human* playing the Windows player) is **not yet met**; see the manual QA list. Commits `8849495` and `587ebf4` are pushed to `origin/main`.

| Evidence | Observed result |
| --- | --- |
| Project and compile | `game/` from Unity's Universal 3D template (6000.3.27f1, URP 17.3.0, Input System 1.20.1, active input handler = new). `Setup` batch run exits 0 with no compiler errors. |
| Generation | `Setup` rebuilds materials, 10 prefabs, `MainMenu` and `Valley` from code. An EditMode test re-runs it and confirms the same scene list and object counts. Checked on the committed tree: materials, prefabs, terrain and meshes regenerate byte-identical; the two scene files differ only by renumbered internal IDs (sorted, ID-normalised lines identical, same per-type object counts). |
| EditMode tests | **23/23 passed**: objective state machine, interaction range and selection, build settings and startup scene, scene wiring, no missing scripts/references/non-URP shaders, generator idempotency. |
| PlayMode tests | **18/18 passed** in the real scenes: grounding, camera-relative walking, smooth turning, obelisk collision, respawn, camera distance/pitch/ground clearance, interaction range, dialogue freezing movement, objective order, completes once and new-game reset, light/crystal/sparkle response, pause/resume/main-menu time scale, and **virtual-keyboard Esc, W and E**. |
| Windows build | **Succeeded**: Windows x64, Mono, D3D11, Development Build, Unity build id `b67006044b0a4a9ebd6ac58d372f47cb`, 0 errors, 0 warnings, 179,156,453 bytes, scenes MainMenu then Valley, output `artifacts/build/win64/EchoesOfTheValley.exe` (git-ignored). A first attempt failed with `HRESULT 0x8007000E` (out of memory in Unity's Bee compiler, caused by the machine, not the scripts); the retry capped `BEE_BUILD_THREADS=4`. |
| Player smoke test | `-echoes-smoke`: **natural exit code 0**, `[Echoes][SMOKE] PASS`, no exception lines, AMD Radeon 780M / Direct3D11. Scripted through menu, Valley, a 4.83 m walk, out-of-range rejection, workshop, villager, early-heart rejection, spring, heart, single completion, pause/resume and back to the menu. Five screenshots were reviewed by the assistant. |
| Frame time | One measured data point only: 3.1 ms average (326 FPS), worst frame 16.6 ms over 5 s at 1280x720 windowed, vSync off, while the completion effect played near the Heart. **Not** a performance claim for the whole map or for other hardware. |

The smoke run and PlayMode tests drive public entry points (and a virtual keyboard in the Editor), so they are **automated**, not a human playtest.

**NOT VERIFIED - manual QA on Windows** (build or open `MainMenu`):

1. Click **Play**; the Valley loads and the cursor is captured.
2. Walk with WASD and arrows and look with the **real mouse**: the camera follows without spinning, never dips below the ground and keeps a sensible vertical angle.
3. Walk to Mira (east, by the cottage): `[E] Talk to Mira` appears in reach and vanishes when you step away; each E press advances one line; the objective text and banner update.
4. Check the workshop message (alchemy comes later), then the ruined spring (north-west, by the pond), then the Heart (centre): the banner, warmer light, glowing crystal and sparkles appear once.
5. Press **Esc**: the menu appears and the cursor is free. Resume works by button and by Esc. **Main Menu**, then Play again: the objective starts over.
6. **Quit** from the pause menu and from the title screen closes the player (the buttons were not exercised by any test).
7. Alt-Tab away and back: the cursor is recaptured and the mouse still turns the camera.
8. Look for pink or missing materials, flicker, clipping, and anything slow on the Radeon 780M.

**Known issues at the end of Phase 1:** the banner overlapped the objective panel (**fixed in Phase 2**, with a layout test); the Quit buttons, real-mouse look and Alt-Tab recapture have no automated coverage; there was no audio.

## Phase 2 status (2026-10-25)

**`PHASE2_STATUS=PASS`** against the automated gate below. **`MANUAL_GAMEPLAY_QA_STATUS=PENDING`**: the developer deferred human play until before submission, so nothing here was played by a person, real mouse clicks, visual quality and pacing are **not verified**, and this is not release-ready. Phase 2 is committed locally and **not pushed**.

| Gate | Observed evidence |
| --- | --- |
| A Phase 1 intact | All 23 Phase 1 EditMode and 18 PlayMode tests pass. One assertion changed on purpose: the workshop used to say "coming later" and now opens the crafting panel. Two Phase 1 UI facts were corrected: the banner moved below the HUD panels, and `Play` became the `New Game` button (same field and method names). |
| B Gathering | 15 renewable nodes (6 herb, 5 blossom, 4 ore) gathered with E: in-range grant, out-of-range rejection, cooldown with a stripped/regrowing look, regrowth, no duplicate on repeated presses, a full stack refuses without burning the node (EditMode and PlayMode). |
| C Inventory | `Inventory`: 8 slots, 9 per stack, atomic remove-then-add transactions, one notification per commit, capacity, invalid-quantity and insufficient-quantity tests, no scene needed. |
| D, E Recipes and crafting | 3 recipes, crafted through the panel buttons and the 1-3 keys: ingredients validated, output room validated, consumed once, repeated presses rejected, nothing consumed on failure. |
| F Hen | One hen, Clover: Hungry -> Producing (15 s) -> EggReady. Feeding spends one feed, cannot be repeated, the egg cannot be collected twice, a full egg stack leaves the egg in the nest, state shown by a bubble, a star and an egg, and in the HUD. |
| G, H Quest and `ResonanceStage` | *Restore the Stream*: Mira offers it after the opening objective; it only completes through `TryDeliverSpringTonic` with a tonic in the satchel (one consumed, 25 coins once); stage goes 0 -> 1 exactly once. Rejected without the quest, without the item, and on repeat. |
| I World change | Stage 1 swaps the murky trickle for a wide clear stream, fills the basin, clears the pond, shows 18 flower patches and 4 saplings, fires a pulse ring and a fountain, and adds a golden-hour lift through `ValleyLighting` on top of the opening Heart payoff (either order works). |
| J Save and load | JSON, `version` 1, stable ids, FNV-1a integrity field, temp file -> read-back verification -> replace, previous good save kept as `.bak`, fallback to the backup only with a visible notice, never to damaged data. Continue, New Game and pause-menu Save Game; autosave when the quest starts and completes. A completed restoration reloads shown at once with no second reward. |
| K EditMode | **111/111 passed** (23 Phase 1 + 88 new): inventory, crafting, gathering, hen, quest, softlock cases, save handling (garbage and truncated JSON, missing fields, tampered values, unsupported version, 18 invalid-value cases, temp/backup/recovery and a failed write), scene wiring and layout, generator idempotency. |
| L PlayMode | **49/49 passed** (18 Phase 1 + 31 new) in the real scenes, including gather -> craft -> deliver -> world change, save -> Continue, the menu's Continue states, and virtual-keyboard I, E, 1 and Esc. |
| M Windows build | **Succeeded**: Windows x64, Mono, D3D11, Development Build, Unity build id `7435af060e7c4147889e394609619352`, 0 errors, 0 warnings, 179,570,721 bytes, scenes MainMenu then Valley, output `artifacts/build/win64/EchoesOfTheValley.exe` (git-ignored). |
| N Player smoke | `-echoes-smoke`: **natural exit code 0**, `[Echoes][SMOKE] PASS`, 0 exception lines, AMD Radeon 780M / Direct3D11. It played New Game -> opening objective -> Mira's quest -> gathered 2 herbs and 2 blossoms by interaction -> satchel -> crafted Spring Tonic and Chicken Feed through the buttons -> fed the hen -> poured the tonic (stage 1, world changed) -> saved from the pause menu -> main menu -> Continue (state, node timers, hen and the restored world all back) -> collected the egg -> main menu. 10 screenshots were reviewed by the assistant. |

Frame time, two data points only: 3.1 ms average (319 FPS, worst 23.6 ms) during the Heart effect and 3.2 ms (311 FPS, worst 16.8 ms) during the stream restoration. The player window opened at **800x600**, not the requested 1280x720, with vSync off, so these say nothing about 1080p or other views. The 1280x720 layouts were checked separately with editor renders.

**Design numbers** (first-pass tuning, not playtested balance; `docs/01` fixes only "2 herbs" for the tonic and "4 ore" for the braces):

| Item | Value |
| --- | --- |
| Items | Moonleaf Herb, Dawn Blossom, Echo Ore, Chicken Feed, Hen's Egg, Spring Tonic, Windmill Braces |
| Satchel | 8 slots (7 item types, so a full stack of one item never blocks another), 9 per stack |
| Recipes | Spring Tonic = 2 herb + 1 blossom; Chicken Feed = 1 blossom -> 3 feed; Windmill Braces = 4 ore |
| Nodes | herb and blossom regrow in 40 s, ore in 75 s; they always regrow, so nothing required can run out |
| Hen | 15 s from feed to egg, counted in game time: the pause menu and a closed game never advance it (the satchel and workshop panels do not pause it) |
| Coins | 20 at the start, +25 once for restoring the stream; there is no shop yet |
| Save | `%USERPROFILE%\AppData\LocalLow\EchoesJam\Echoes of the Valley\echoes_save.json` (+ `.bak`); stores coins, stacks, node and hen timers in milliseconds, exploration step, quest stage and `ResonanceStage`, not the player's position |

**Known limitations:** Windmill Braces and eggs have no use until Phase 3, and coins have one source and no sink; there is no hen purchase, audio, or stage 2 and 3; the "flowing" stream is a swap to a wider clear ribbon plus particles, not an animated shader; props and effects are primitives and have only been seen as still frames; autosave only fires when the quest starts or completes, so gathering since the last manual save is lost on quit; the checksum catches accidental damage, not deliberate editing; Quit buttons, real-mouse look, Alt-Tab recapture and real clicks on UI buttons have no automated coverage (tests invoke `onClick`); scene files get new internal IDs on every regeneration.

**Manual QA deferred (human, Windows, before submission):** play from New Game to the restored stream with the real keyboard and mouse; check that every prompt, panel and toast reads well at 1280x720 and 1920x1080; click every button (Continue, New Game, Craft, Close, Save Game, Resume, Main Menu, Quit); Alt-Tab during play and with a panel open; close the game mid-egg and Continue; look for pink or missing materials, clipping and pacing; judge the stream restoration effect in motion; check performance on the Radeon 780M.

## Next action: P3

1. Start from the committed Phase 2 project. Extend the generators; do not hand-edit scenes. Reuse `GameSession`: add stages 2 and 3 to `ResonanceStage`, the other two quests, the windmill (Windmill Braces and eggs finally get a use) and the ending.
2. Keep the invariants above. Run `Compile`, `Setup`, `TestEdit` and `TestPlay` before any `Build`, build once, and never push without being asked.
