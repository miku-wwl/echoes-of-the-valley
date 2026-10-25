# Echoes of the Valley

**Unity game jam entry · thatgamejam #02 · 2026**

An original, short, peaceful fantasy adventure about **bringing a silent valley back to life** through gathering, caring for a chicken, alchemy and visible restoration.

> **Silence → Awakening → Resonance → Rest**

**GATHER → CARE → CRAFT → RESTORE → WATCH THE WORLD RESPOND**

## Current truth

- **Competition:** [thatgamejam #02](https://itch.io/jam/thatgamejam02), hosted by thatgamecompany.
- **Start:** 2026-10-24 20:00 NZDT; **deadline:** **2026-11-08 20:59 NZDT**. Internal upload target: 2026-11-07.
- **Phase 1 (foundation): PASS** on 2026-10-25 against its gate (23/23 EditMode, 18/18 PlayMode, Windows build, smoke test). Pushed to `origin/main` as `8849495` and `587ebf4`. Evidence: [02](docs/02-BUILD-PLAN.md#phase-1-status-2026-10-25).
- **Phase 2 (playable slice): automated gate PASS** on 2026-10-25: 111 EditMode and 49 PlayMode tests, a Windows x64 Mono development build and a scripted standalone gameplay smoke test. Committed locally; **not pushed**. Evidence, design numbers and limits: [02](docs/02-BUILD-PLAN.md#phase-2-status-2026-10-25).
- **Manual gameplay QA: PENDING.** No human has played the Windows build yet, so mouse and keyboard feel, real clicks, visuals, audio-free pacing and performance on the Radeon 780M are **not verified**. This is deferred until before final submission.
- **Environment:** Phase 0.5 **PASS** was for a separately tested blank-template probe (see [baseline](docs/04-BASELINE.md)); Phases 1 and 2 exercised the toolchain on this project.
- **Official theme:** released; the supplied 60-second audio's dynamics have been measured and interpreted. The emotional interpretation remains a **design choice**, not a claim of direct human listening.

## What exists now (Phase 2)

A small, complete first loop, built entirely from code. `MainMenu` (**Continue** / **New Game** / Quit over a dusk backdrop) leads into the `Valley`: third-person WASD + mouse in one valley with a ruined landmark, workshop cottage, pond and stream.

1. **Explore.** The opening objective *Discover the Heart of the Valley* (villager, ruined spring, Heart) still warms the light and wakes the crystal.
2. **Gather.** Press **E** at 15 renewable nodes: 6 Moonleaf Herb bushes, 5 Dawn Blossom patches, 4 Echo Ore outcrops. Each regrows (40 s, or 75 s for ore).
3. **Carry.** **I** opens the satchel: 8 slots, up to 9 of each item, and your coins.
4. **Craft.** At the workshop, three recipes: *Spring Tonic* (2 herb + 1 blossom), *Chicken Feed* (1 blossom makes 3) and *Windmill Braces* (4 ore, kept for a later task). Click Craft or press 1-3.
5. **Care.** Feed Clover the hen, wait 15 s, collect the egg.
6. **Restore.** Mira gives the quest *Restore the Stream*; pour a Spring Tonic into the ruined spring. `ResonanceStage` goes from 0 to 1: the murky trickle becomes a wide clear stream, the basin fills, blooms open along the banks, a ring of light runs outward and the valley's light turns golden. You get 25 coins, once.
7. **Save.** Pause menu **Save Game**, plus an autosave when the quest starts and when it completes. **Continue** loads the versioned JSON save (with a backup it falls back to, and it refuses damaged data).

Not built yet: the second and third restorations, the windmill and the ending (Phase 3), purchasing animals, audio, and a human-verified playtest.

## Exact MVP (10–15-minute goal; max 20-minute target)

One compact valley, one original protagonist, three short NPC quests, herbs/ore, a chicken and egg, three recipes, three restoration states, an ending, and local save/load. No combat, fishing, seasons, breeding, online features or large world.

The signature feature is **Valley Resonance**: each important restoration creates a clear, persistent before/after change in color, vegetation, activity and sound. The game does **not** need to be a rhythm game or ship with the official theme audio.

## Docs: four short working references

1. [01 — Game vision and musical theme](docs/01-GAME-DESIGN.md)
2. [02 — Implementation and acceptance gates](docs/02-BUILD-PLAN.md)
3. [03 — Jam rules, provenance and AI disclosure](docs/03-RULES-CREDITS.md)
4. [04 — Verified Phase 0.5 baseline](docs/04-BASELINE.md)

## Unity project (Windows)

`game/` is a Unity **6000.3.27f1** Universal 3D (URP) project: Windows x64 Mono + D3D11, Input System only. It needs the verified Editor at `D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe`, a valid Personal license and free space on D:.

**Open it:** add `game/` in Unity Hub, or run `Unity.exe -projectPath game`. Open `Assets/Echoes/Scenes/MainMenu.unity` (the startup scene) and press Play.

**Commands** (from the repository root; logs, test XML, screenshots and builds go to the git-ignored `artifacts/`):

| Command | What it does |
| --- | --- |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Init-Unity.ps1` | One-time creation of `game/` from Unity's own URP template; refuses to overwrite an existing project. Already done. |
| `.\tools\Invoke-Echoes.ps1 -Task Compile` | Import and compile only (fast check; exit 1 on compiler errors). |
| `.\tools\Invoke-Echoes.ps1 -Task Setup` | Compile, configure player/URP settings, and (re)generate materials, prefabs and the `MainMenu` and `Valley` scenes. Idempotent: it rebuilds the scenes from code, so running it twice does not duplicate anything. Assets come out byte-identical, but Unity renumbers internal object IDs in the two scene files on every save, so `git diff` shows churn; `git checkout` the scenes if you did not mean to change them. |
| `.\tools\Invoke-Echoes.ps1 -Task TestEdit` / `-Task TestPlay` | EditMode / PlayMode tests (batch mode, results in `artifacts\logs`). |
| `.\tools\Invoke-Echoes.ps1 -Task Preview` | Render editor camera shots of both scenes and the HUD, satchel, workshop, pause and title screens at 1280x720 (no Play Mode) into `artifacts\logs\preview-shots`. |
| `.\tools\Invoke-Echoes.ps1 -Task Build` | Windows x64 Mono development build to `artifacts\build\win64\EchoesOfTheValley.exe`. It caps Unity's compiler at 4 threads (`BEE_BUILD_THREADS`) because a full-parallel build ran out of memory on this machine. |
| `.\tools\Invoke-Echoes.ps1 -Task Smoke` | Start that build with `-echoes-smoke`: a scripted playthrough of the whole loop (menu, objective, gathering, satchel, crafting, hen, stream restoration, save, Continue, egg, back to the menu) using the game's real entry points and an isolated save folder, saving screenshots and exiting with code 0 (pass) or 1. It is **not** a human playtest. |

Scenes and prefabs are **generated**: edit `game/Assets/Echoes/Scripts/Editor` (also under the **Echoes** menu in the Editor) and re-run `Setup` rather than editing `.unity`/`.prefab` files by hand.

**Controls:** WASD/arrows move relative to the camera, mouse looks, **E** interacts, **I** opens the satchel, **1-3** craft at the workshop, **Esc** closes a panel or pauses. Saves live in the game's persistent data folder (`%USERPROFILE%\AppData\LocalLow\EchoesJam\Echoes of the Valley\echoes_save.json`, plus `.bak`); tests and the smoke run use throwaway folders.

## Rights and attribution

Original source code is MIT-licensed (see [LICENSE](LICENSE)). That grant does **not** apply to art, audio, fonts, third-party material, documentation or the official theme. **Do not add the official theme WAV to public GitHub.** Maintain an actual-use asset register and make an honest AI-usage disclosure at submission; see [rules](docs/03-RULES-CREDITS.md).
