# 07 — Development roadmap

**Planning only, 2026-10-23 NZDT. Phases 1–6 NOT STARTED.** One developer, Windows x64, complete P0 before optional expansion. Dates are Auckland NZDT and effort budgets assume roughly 5–6 focused hours on working days; actual availability is unconfirmed. Reduce visual detail and extras when capacity falls short.

## Schedule and common gate meanings

| Phase | Planned window | Effort ceiling / stop rule |
| --- | --- | --- |
| 1 — Foundation and first build | Oct 24 after 20:00, then Oct 25 | ~6 h; first player build before feature work |
| 2 — Gathering/inventory/pasture/crafting | Oct 26–28 | ~16 h; all three recipes and chicken loop |
| 3 — Quests/restoration | Oct 29–30 | ~10 h; complete playable slice and P0 greybox ending |
| 4 — Anime visual production | Oct 31–Nov 3 | ~20 h; no new gameplay features |
| 5 — Test/polish/performance | Nov 4–6 | ~15 h; fix completion blockers before visual polish |
| 6 — Release/submission | Nov 7–8 | ~6 h plus upload buffer; internal cutoff Nov 7 20:00 |

Official deadline is Nov 8 **20:59** NZDT. Internal cutoff leaves approximately 25 hours for package/upload recovery. Optional P1 features require all P0 gates passed and ≥one working day of spare capacity; otherwise they remain cut. P2 is excluded regardless of spare time.

**PASS** means all phase acceptance checks have direct evidence. **PARTIAL** means some deliverables work but a criterion or evidence is missing; it is not permission to claim completion. **FAIL** means a critical criterion fails or a phase boundary/compliance violation occurs. Block dependent work on a critical failure; nonblocking cosmetic work may continue only if it cannot conceal that failure.

## Phase 1 — Foundation and first Windows build

- Scope: theme review after both gates; exact Unity/package lock; Universal 3D project; Boot/Valley greybox; original basic movement/follow camera; one interaction prompt; title/pause/quit; Windows x64 Mono player.
- Deliverables: minimal tracked project with `.meta` files, package lock, scene/build configuration, English controls, first runnable full player folder, setup/readme evidence. No inventory, livestock, quest, or purchased gameplay framework yet.
- Dependencies: [kickoff prerequisites](08-phase1-kickoff.md), Hub/editor/player support, accessible theme, resolved policy concerns for anything to import.
- Technical risks: editor/package mismatch; license activation; camera clipping; missing Mono support; UI action map traps.
- Acceptance tests: clean open/compile; title → valley; camera-relative motion/collision; interact prompt enters/leaves range; pause/resume/focus; Quit; launch Windows player outside Editor.
- Evidence: theme review time/source; exact editor/package versions; Editor console; build log; actual player screenshots/interaction notes with timestamp and SHA; ignored build path. Record who tested it.
- PASS: all tests succeed in genuine Windows player. PARTIAL: Editor works but build/player evidence missing or small noncritical check incomplete. FAIL: project cannot compile/launch, missing required gate, or imported full gameplay system.

## Phase 2 — Gathering, inventory, pasture, and crafting

- Scope: herb/seed/ore nodes, stack inventory and coins, chicken purchase/feed/growth/egg, three recipes, transaction/save foundations. One species, no fishing/crops.
- Deliverables: functional loop in greybox, inventory/crafting/pen UI, stable definition IDs, versioned save/load for these systems, domain and integration checks.
- Dependencies: Phase 1 Windows build; [design quantities](02-game-design-prep.md); interaction and session architecture.
- Technical risks: full-inventory losses, repeated presses, currency softlock, timers advancing while paused, save inconsistency.
- Acceptance tests: gather all raw types; craft all three recipes; buy once; feed once; grow and collect egg once; repeat clicks/full bag cause no duplication or loss; pause/load preserve timer; quit/load restores exact amounts.
- Evidence: EditMode/PlayMode reports with counts; before/after quantities; timer/pause/save cases; Windows smoke log and SHA. Do not substitute a mocked livestock sequence for actual play.
- PASS: every system works together and state survives roundtrip. PARTIAL: some work but chicken, recipe, or persistence remains incomplete; block claiming P0 slice. FAIL: reproducible duplication, loss, permanent softlock, or broken Phase 1 build.

## Phase 3 — NPC quests and world restoration

- Scope: three complete quests, short dialogue, prerequisites, three visible changes, onboarding and explicit ending; complete functional P0 greybox.
- Deliverables: movement → gather → inventory → craft → NPC delivery → restoration slice; chicken quest; all three quests from acceptance to completion; save/load of progression and ending.
- Dependencies: Phase 2 complete transaction model and items; post-theme-reviewed motivation/dialogue; world-state views.
- Technical risks: out-of-order quest lock, duplicated rewards on load, visuals disagreeing with progress, animal wait breaking pacing.
- Acceptance tests: fresh-save beginning-to-ending run; collect inputs before acceptance; decline/cancel/reopen dialogue; repeatedly confirm delivery; save after each restoration and reload; finish with exact reward counts and no blocked progression.
- Evidence: continuous real playthrough or timestamped manual steps, starting save state, timings, transaction checks, reload screenshots, final state/build SHA. Target 15–20 minutes but report actual duration.
- PASS: full slice plus all P0 systems and ending work in Windows build. PARTIAL: slice works but a quest/ending/save check remains; no art-expansion claim of completion. FAIL: ending unreachable, duplication persists, or state corrupts.

## Phase 4 — Anime 3D visual production

- Scope: replace greybox with cohesive original/licensed hero, shared NPCs, chicken, small valley kit, vegetation, shaders/UI/audio and restoration visuals. Keep functional design stable.
- Deliverables: [minimum art set](04-art-direction.md), owned sources/exports, exact actual-use license entries and credits, readable English UI, a styled complete build.
- Dependencies: Phase 3 end-to-end working build; tested style sample; Blender/export pipeline; verified per-asset rights and eligibility.
- Technical risks: solo art workload, rig/export incompatibility, materials/foliage overdraw, incompatible stock styles.
- Acceptance tests: no missing/pink materials; no copied references; all interactables/quest states visually readable; original hero controllable; animation remains functional; save restored visuals match; all admitted asset files match register.
- Evidence: before/after screenshots, actual asset provenance/license files, import settings, source/export hashes, Windows regression playthrough, profiler sample. No claim that visual polish implies FPS target passed.
- PASS: minimum coherent set and rights checks complete with no P0 regression. PARTIAL: greybox remains or source/rights evidence incomplete; uncertain assets excluded. FAIL: unlicensed content, copied content, broken progression, or unavailable readable visuals.

## Phase 5 — Testing, polish, and performance

- Scope: completion bugs, save recovery, keyboard/focus/readability, audio levels, collision, pacing, both performance presets, clean-machine-style build launch. No late feature expansion.
- Deliverables: release candidate; issue triage; automated meaningful domain/integration tests; manual P0 checklist; measured player performance; updated how-to-play and notices.
- Dependencies: complete styled P0 build; stable project/package versions; representative 780M hardware.
- Technical risks: shader/load spikes, worst-view foliage, corrupt save, modal focus, unobserved player assumptions.
- Acceptance tests: new and continued runs, duplicate reward/capacity cases, corrupt primary/backup recovery, no-keyboard-trap UI, out-of-bounds recovery, two preset traversals. Prefer a second human tester; record if only developer/agent observations exist.
- Evidence: exact test counts/results/logs; actual 15–20-minute run timing; unresolved issue severity; build SHA; frame-time captures with settings/driver/power mode and warm-up. Use ≥3-minute representative traversal per preset and report median/p95/p99/stalls.
- PASS: all P0 checks pass, no completion/data-loss blockers; median ≥30 FPS at 1080p medium and ≥60 at 720p low with p95 frame time ≤33.3/16.7 ms respectively; explain outlier stalls. PARTIAL: playable but pacing/performance/recovery evidence misses a target. FAIL: crash, lost progress, unreachable ending, or material rights failure.

## Phase 6 — Release and submission

- Scope: final archive/credits/disclosure; upload to itch.io jam entry; final version checks before cutoff. Submission itself is outside Phase 0.
- Deliverables: ZIP of complete Windows player folder, English controls/credits/notices, game page with AI explanation, final game files and entry linkage, immutable SHA-256 and commit record.
- Dependencies: Phase 5 result reviewed; all P0 complete; platform access; current official-rule recheck; source/asset permission resolution.
- Technical risks: missing player data, archive corruption, upload time, wrong entry platform, undocumented AI use, inadvertent judging-period update.
- Acceptance tests: unpack archive to another folder; launch actual EXE with companion data; play beginning/ending and Continue; verify downloaded uploaded package hash; inspect page English/credits/AI disclosure; verify jam entry lists intended build before deadline.
- Evidence: ZIP hash/size, clean checkout commit, launch/playtest log, actual upload/entry URLs and timestamp, downloaded artifact check. Platform submission is NOT VERIFIED unless observed.
- PASS: valid entry and downloaded build verified before deadline. PARTIAL: package ready but upload/entry or download checks missing. FAIL: late/missing entry, unlaunchable package, or disqualifying content. After deadline, do not update/replace/delete entry until results permit it.

## Daily decision rule

End each working day with a runnable Windows build, exact current issues, and next smallest dependency. If Phase 3 slips, use remaining feature budget to finish the greybox ending before art expansion. If Phase 5 misses performance, reduce rendering cost rather than add systems. No future planned test in this document has been executed during Phase 0.
