# 08 — Phase 1 kickoff specification

**BLOCKED — JAM_START_GATE=CLOSED. Prepared 2026-10-23 NZDT. DO NOT EXECUTE IN PHASE 0.** This specification is actionable future work, not an instruction to start creating the game now.

## Entry conditions

All must be satisfied and recorded before competition-project creation. The separately authorized external Phase 0.5 environment probe does not satisfy or bypass this gate:

1. Auckland time is at or after **2026-10-24 20:00 NZDT / 07:00 UTC**. Confirm against official schedule; do not open merely because another timezone says October 24.
2. Official musical theme is actually released through an official source. Record URL, timestamp, title/composer/license as published; listen to the authentic track. No invented placeholder theme or claimed listening.
3. Write a short theme review: observed musical characteristics, candidate player actions, and chosen design adjustments. Narrative/emotional interpretation starts here; revisit the provisional GDD rather than assert it already matches.
4. Unity Hub and a **6000.3.x LTS** Windows x64 Editor are installed; exact patch and compatible packages are recorded; desktop player support is present. Complete license activation legitimately. No assumed license/terms acceptance.
5. Blender is installed and version launch checked, or an explicit accepted art-tool alternative is documented. Current Phase 0 reports no such alternative.
6. Recheck [compliance](01-competition-compliance.md), current rules, Git branch/remote, and [preflight blockers](09-phase0-evidence.md). Exclude unapproved stock assets/systems; resolve any new organizer restriction before relying on this preparation.

If start time arrives without theme release, remain blocked. If theme is released before the start time, remain blocked. This Phase 0 run will not wait for or trigger this gate automatically.

## Ordered work packages — approximately 6 focused hours

| Order / timebox | Action after gate | Concrete exit evidence |
| --- | --- | --- |
| 1 / 30 min | Record gate/theme review; confirm minimal post-theme scope; no P1 additions | Dated theme note and revised design decisions |
| 2 / 45 min | Create `game/` with Unity Hub Universal 3D template using verified 6000.3 LTS; set English product name, Windows x64, Force Text and Visible Meta Files | ProjectVersion, manifest/lock, settings readback; clean compile |
| 3 / 45 min | Verify URP/Shader Graph/Test Framework; resolve Input System and Cinemachine 3; choose New Input handling; create owned Boot/Valley scenes | Package versions, no imported gameplay demos, scenes in build profile |
| 4 / 90 min | Greybox short safe valley route and implement original CharacterController movement/follow camera; one owned interaction marker/prompt | Editor movement/collision/range checks and no red console errors |
| 5 / 45 min | Title → play, pause/resume, camera reset, English controls, keyboard focus, Quit; disable gameplay actions in modal UI | Repeated menu/pause transitions with no duplicate session/input |
| 6 / 45 min | Create Windows x64 Mono build to ignored `artifacts/phase1/windows/`; launch full player folder outside Editor | Actual build log and observed player launch; path and SHA |
| 7 / 60 min | Run acceptance list; fix critical issues; audit files, update evidence and commit exact scope | Checked results, test actor, timestamp, commit, clean worktree |

Do not install arbitrary package versions from web examples. Use released packages compatible with the chosen patch; retain manifest and lock. Do not create a second project to hide an unsuccessful first attempt. Do not bring in full controllers/levels as a shortcut.

## Foundation specification

Camera-relative keyboard movement: WASD/arrow keys; mouse look when gameplay owns focus; E interact; I reserved for later inventory; Escape pause; visible camera reset action. Exact speeds and controls may be adjusted after observing play. Basic ground collision, safe spawn, and camera obstruction behavior are required; jump, sprint stamina, combat, gamepad, and animation polish are not Phase 1 dependencies.

Interaction marker displays a short English prompt only in range. For this phase it produces a reversible acknowledgement, not gathered goods, currency, quest progress, or a claimed complete slice. Domain gameplay belongs in Phases 2–3. No actual production asset is required to verify foundation controls.

Use one session object per play start, explicit teardown on return to title, and one camera/audio listener. Keep Unity integration separated from later domain rules as described in [architecture](03-technical-architecture.md), without building a general framework. No cloud, network, Docker, paid SDK, telemetry, or authentication runtime.

## Acceptance checklist — all NOT RUN in Phase 0

| Test | Expected result |
| --- | --- |
| F01, editor load | Exact pinned editor opens project; packages resolve; no compile errors |
| F02, start/restart | Title starts Valley; returning/restarting creates one session and one camera |
| F03, movement | WASD/arrows move relative to camera; collisions prevent walking through required barriers |
| F04, camera | Follow/look works; reset recovers usable angle; no critical route obscured |
| F05, interaction | In-range prompt and E acknowledgement work; out-of-range activation rejected; held key does not spam |
| F06, modal input | Pause stops movement/timers; UI keyboard focus works; resume restores expected control |
| F07, recovery | Out-of-bounds player returns to safe spawn; Escape can recover from UI |
| F08, Windows build | x64 Mono player launches outside Editor; full folder retained; title/play/pause/quit work |
| F09, hygiene | No secrets, caches, builds, copied assets, or complete imported systems staged; `.meta` and lockfiles tracked |

Retain editor/build logs, actual Windows observations/screenshots with test IDs, exact versions, timestamp, tester identity, and commit SHA. Do not label automated file existence as successful interactive play. Performance measurements are deferred; no FPS claim is needed for foundation PASS.

## Exit and failure handling

PASS requires F01–F09 with evidence. PARTIAL if the Editor works but player launch or a required observation is absent. FAIL if the gate was bypassed, compilation/build fails, or core input/menu behavior prevents play. When a required tool/license is unavailable, stop dependent project work and report the blocker. When foundation passes, schedule Phase 2 separately; Phase 0 authorization never executes this specification.
