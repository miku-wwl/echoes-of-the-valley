# 02 — Code-first Unity roadmap and acceptance gates

**Deadline:** **2026-11-08 20:59 NZDT**. Internal upload target: Nov 7. Unity Phase 0.5 was a separate **blank-template environment probe**, not a playable game. A real competition project is **not present in the uploaded baseline**.

## Stack and structure

- Unity **6000.3.27f1**, Universal 3D/URP, C#, Input System; Windows x64 **Mono** + D3D11 first. Resolve Cinemachine only when ready; it has not been tested.
- Editor: `D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe`; Blender 4.5.15 LTS. Prefer D: for project, Unity cache and builds; C: was low on space.
- `game/` is the Unity root. `Assets/Echoes/` contains `Scenes`, `Scripts`, `Prefabs`, `Art`, `Audio`, `UI`, `Tests`. Two intended scenes: `Boot` (title/load) and `Valley` (game).
- Generate scenes and prefabs using **Unity Editor C# scripts**, `-batchmode` and `-executeMethod`; never fabricate Unity YAML manually. GUI work is optional for authoring, but **actual player interactions must be manually verified**.
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
| **P1 Foundation · Oct 25–26** | Run code-first bootstrap; generate `Boot`/`Valley`; controllable hero/collision/camera; interaction prompt; title/pause/quit | Actual Editor compile and **Windows player** start, move, look, interact, pause and quit test. |
| **P2 Playable slice · Oct 26–29** | Nodes/inventory/coins, three recipes, hen states, save/load and **first stream repair** | Real player **gather → craft → deliver → S1**, quantity/dupe tests, hen and saved-state roundtrip. |
| **P3 Full greybox · Oct 29–31** | Three quests, S0→S3, clear ending, pacing | Entire fresh-save run, repeat-confirm tests, save/reload at each restoration, no softlock. |
| **P4 Cohesive art · Nov 1–4** | Original hero/hen/village kit, color transitions, light FX, English UI and licensed sound | Styled Windows build; no pink assets; all quests still work; provenance documented. |
| **P5 QA + Release · Nov 5–7** | Polish, performance, credits/AI disclosure, archive, itch.io entry | Unpacked Windows EXE works with its data folder; full ending; upload is visible in jam before Nov 8 20:59 NZDT. |

**PASS** = directly observed evidence; **PARTIAL** = missing evidence/criteria; **FAIL** = build failure, material bug, wrong rights or disqualifying rule violation. End each phase with tests, exact status, commit SHA if committed and remaining blockers. Never label an untouched stock-template player as a game test.

## First next action (Codex prompt contract)

1. Read README and the four short docs; verify installed Unity and repo clean state.
2. Run `tools/Init-Unity.ps1` **only if** `game/ProjectSettings/ProjectVersion.txt` does not exist; otherwise preserve the user's real project and work in place.
3. Add an Editor automation bootstrap for serialized `Boot`/`Valley`, a simple controlled capsule, follow camera, an in-range E prompt, title/pause/quit.
4. Batch import/build to ignored `artifacts/`; manually test the standalone player on Windows. If unavailable, report PARTIAL and explicitly list unverified GUI checks.
5. Only then start P2. Do not push or delete existing project files without instruction.

`Init-Unity.ps1` initializes the project/template **only**. This ZIP contains no gameplay code, scenes or passed Phase 1 build.
