# Echoes of the Valley

**Unity game jam entry · thatgamejam #02 · 2026**

An original, short, peaceful fantasy adventure about **bringing a silent valley back to life** through gathering, caring for a chicken, alchemy and visible restoration.

> **Silence → Awakening → Resonance → Rest**

**GATHER → CARE → CRAFT → RESTORE → WATCH THE WORLD RESPOND**

## Current truth

- **Competition:** [thatgamejam #02](https://itch.io/jam/thatgamejam02), hosted by thatgamecompany.
- **Start:** 2026-10-24 20:00 NZDT; **deadline:** **2026-11-08 20:59 NZDT**. Internal upload target: 2026-11-07.
- **Phase 1 (foundation): PASS** against its acceptance gate on 2026-10-25: 23/23 EditMode and 18/18 PlayMode tests, a Windows x64 Mono development build, and a scripted standalone-player smoke test that exited 0. A **human playtest of the Windows player has not been done yet**. Exact evidence, known issues and the manual QA checklist: [02](docs/02-BUILD-PLAN.md#phase-1-status-2026-10-25).
- **Environment:** Phase 0.5 **PASS** was for a separately tested blank-template probe (see [baseline](docs/04-BASELINE.md)); Phase 1 exercised the toolchain on this project.
- **Official theme:** released; the supplied 60-second audio's dynamics have been measured and interpreted. The emotional interpretation remains a **design choice**, not a claim of direct human listening.
- **Remote:** Phase 1 work is committed **locally only**; nothing has been pushed.

## What exists now (Phase 1)

A small playable exploration slice, built entirely from code: `MainMenu` (Play / Quit over a dusk backdrop) → `Valley` (third-person WASD + mouse, one valley with a ruined landmark, workshop cottage, pond and stream, trees and flowers). Press **E** at four interactables (the villager, the alchemy workshop, the ruined spring, the Heart of the Valley) to complete *Discover the Heart of the Valley*, which warms the light and wakes the crystal. **Esc** opens the pause menu (Resume / Main Menu / Quit).

Not built yet: gathering, inventory, crafting, the chicken, save/load, `ResonanceStage` restoration and audio (Phase 2+). The workshop and ending text say so in-game.

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
| `.\tools\Invoke-Echoes.ps1 -Task Setup` | Compile, configure player/URP settings, and (re)generate materials, prefabs and the `MainMenu` and `Valley` scenes. Idempotent: it rebuilds the scenes from code, so running it twice does not duplicate anything. Assets come out byte-identical, but Unity renumbers internal object IDs in the two scene files on every save, so `git diff` shows churn; `git checkout` the scenes if you did not mean to change them. |
| `.\tools\Invoke-Echoes.ps1 -Task TestEdit` / `-Task TestPlay` | EditMode / PlayMode tests (batch mode, results in `artifacts\logs`). |
| `.\tools\Invoke-Echoes.ps1 -Task Preview` | Render editor camera shots of both scenes (no Play Mode, no UI overlay). |
| `.\tools\Invoke-Echoes.ps1 -Task Build` | Windows x64 Mono development build to `artifacts\build\win64\EchoesOfTheValley.exe`. |
| `.\tools\Invoke-Echoes.ps1 -Task Smoke` | Start that build with `-echoes-smoke`: a scripted pass through menu, movement, all interactions, the objective, pause and back to the menu, saving screenshots and exiting with code 0 (pass) or 1. It calls the game's public entry points; it is **not** a human playtest. |

Scenes and prefabs are **generated**: edit `game/Assets/Echoes/Scripts/Editor` (also under the **Echoes** menu in the Editor) and re-run `Setup` rather than editing `.unity`/`.prefab` files by hand.

**Controls:** WASD/arrows move relative to the camera, mouse looks, **E** interacts, **Esc** pauses.

## Rights and attribution

Original source code is MIT-licensed (see [LICENSE](LICENSE)). That grant does **not** apply to art, audio, fonts, third-party material, documentation or the official theme. **Do not add the official theme WAV to public GitHub.** Maintain an actual-use asset register and make an honest AI-usage disclosure at submission; see [rules](docs/03-RULES-CREDITS.md).
