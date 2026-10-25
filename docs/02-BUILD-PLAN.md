# 02 — Code-first Unity roadmap and acceptance gates

**Deadline:** **2026-11-08 20:59 NZDT**. Internal upload target: Nov 7. Unity Phase 0.5 was a separate **blank-template environment probe**, not a playable game. The real competition project is `game/`, created 2026-10-25; Phase 1 results are recorded [below](#phase-1-status-2026-10-25).

## Stack and structure

- Unity **6000.3.27f1**, Universal 3D/URP, C#, **Input System as the only active input handler** (use `UnityEngine.InputSystem`, never legacy `Input`); Windows x64 **Mono** + D3D11 first. Cinemachine is not used.
- Editor: `D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe`; Blender 4.5.15 LTS. Prefer D: for project, Unity cache and builds; C: was low on space.
- `game/` is the Unity root. `Assets/Echoes/` contains `Scenes`, `Scripts/{Runtime,Editor}`, `Prefabs`, `Art` (materials, terrain data, meshes) and `Tests/{EditMode,PlayMode}`; `Audio` and `UI` folders arrive with content. Two scenes: `MainMenu` (title, **startup scene**; it replaces the originally planned `Boot` name) and `Valley` (game).
- Generate scenes and prefabs using **Unity Editor C# scripts**, `-batchmode` and `-executeMethod`; never fabricate Unity YAML manually. Change `Assets/Echoes/Scripts/Editor` and re-run `tools/Invoke-Echoes.ps1 -Task Setup` (idempotent) instead of editing generated `.unity`/`.prefab` files. GUI work is optional for authoring, but **actual player interactions must be manually verified**.
- Unity's Bee compiler can exhaust memory on this machine (WSL, VS Code and browsers hold most of it): the wrapper caps `BEE_BUILD_THREADS`, and a Windows build that fails with `HRESULT: 0x8007000E` is out-of-memory, not a script error.
- One `GameSession` owns plain-C# game state; MonoBehaviours handle input, views and animation; immutable definitions hold item/recipe/quest IDs. No backend, networking, ECS or imported full gameplay systems.

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
| **P2 Playable slice · Oct 26–29** | Nodes/inventory/coins, three recipes, hen states, save/load and **first stream repair** | Real player **gather → craft → deliver → S1**, quantity/dupe tests, hen and saved-state roundtrip. |
| **P3 Full greybox · Oct 29–31** | Three quests, S0→S3, clear ending, pacing | Entire fresh-save run, repeat-confirm tests, save/reload at each restoration, no softlock. |
| **P4 Cohesive art · Nov 1–4** | Original hero/hen/village kit, color transitions, light FX, English UI and licensed sound | Styled Windows build; no pink assets; all quests still work; provenance documented. |
| **P5 QA + Release · Nov 5–7** | Polish, performance, credits/AI disclosure, archive, itch.io entry | Unpacked Windows EXE works with its data folder; full ending; upload is visible in jam before Nov 8 20:59 NZDT. |

**PASS** = directly observed evidence; **PARTIAL** = missing evidence/criteria; **FAIL** = build failure, material bug, wrong rights or disqualifying rule violation. End each phase with tests, exact status, commit SHA if committed and remaining blockers. Never label an untouched stock-template player as a game test.

## Phase 1 status (2026-10-25)

**`PHASE1_STATUS=PASS`** against the Phase 1 acceptance gate (A-L: project, compile, scenes, movement, camera, interaction framework, three interactables, objective, menus, Windows x64 build, standalone smoke test, no blocking compile errors). The stricter table wording above (a *human* playing the Windows player) is **not yet met**; see the manual QA list. Committed locally only (see `git log`); not pushed.

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

**Known issues:** the "New Objective" and "Objective Complete" banner overlaps the objective panel at the top of the screen (cosmetic; move the banner lower in `ValleySceneBuilder.BuildHud`, which needs a rebuild); the Quit buttons, real-mouse look and Alt-Tab recapture have no automated coverage; there is no audio yet.

## Next action: P2

1. Start from the committed Phase 1 project. Extend the generators; do not hand-edit scenes.
2. Fix the banner overlap, then build the P2 slice: nodes, inventory with capacity checks, three recipes, the hen, save/load, and the first `ResonanceStage` change (stream repair) driven by one authoritative stage value.
3. Keep the invariants above and the build budget in mind: run `Setup`, `TestEdit`, `TestPlay` before any full `Build`, and never push without being asked.
