# 00 — Project charter

Prepared 2026-10-23 NZDT. **Phase 0: PREP ONLY. Implementation not started.**

## Product promise

Echoes of the Valley is an original, colorful anime fantasy RPG in which small acts of gathering, caring, and making visibly improve a village. The player should always understand one useful next action, enjoy a readable landscape, and reach a satisfying ending within 15–20 minutes. No combat or punishment loop is planned.

One developer owns design, code, art integration, tests, and release. Windows x64 is the only competition target. English is the default interface. The design is provisional until the official musical theme is available; plot, emotional arc, names, dialogue, and musical interpretation are pending review.

## Priorities

| Tier | Scope | Cut policy |
| --- | --- | --- |
| Competition P0 | One small 3D valley; one controllable anime protagonist; interaction/dialogue; inventory/currency; herbs/mining; chicken purchase, feeding, growth, eggs; three recipes; three complete NPC quests; visible restoration; save/load; English; Windows build; beginning-to-ending playthrough | Preserve all; simplify counts, geometry, UI, and animations first |
| P1, optional | Fishing, cows/sheep, more recipes/quests, cinematic effects, additional animations | Admit only after all P0 gates pass with schedule buffer |
| P2, after competition | Expanded world, full crop farming, seasons, breeding, large relationship systems, multiplayer | Excluded from the jam build |

The first complete vertical slice is **movement → resource gathering → inventory → crafting → NPC delivery → village restoration**. Chicken care must also be included before declaring the full P0 experience complete. A feature list or an Editor scene alone is not a playable slice.

## Constraints and decisions

- Stack: Unity 6.3 LTS, URP/Shader Graph, C#, Blender, Input System, Cinemachine, Unity Test Framework, JSON saves. No backend, cloud services, networking, or Docker runtime.
- Reference machine: Windows 11, Ryzen 7 8845HS, Radeon 780M, approximately 32 GB installed-class memory. Actual usable memory is in the [preflight](09-phase0-evidence.md).
- Performance goals: 1080p medium 30 FPS; 720p low 60 FPS. Both are **targets, NOT VERIFIED**.
- No complete imported levels or gameplay frameworks. Prefer small owned systems and one coherent art palette.
- No copied Atelier/QQ characters, layouts, UI, music, animations, or reference images in the repository.
- Preserve existing README intent and MIT copyright. License only original source code under MIT; do not imply MIT covers third-party material.

## Phase boundary and authority

Before 2026-10-24 20:00 NZDT **and** theme release: documentation/configuration/research/tool checks only. No Unity project, gameplay source, game-ready art, scene, prefab, level, or executable. This remains the boundary even if preparation finishes early. Pre-jam documentation eligibility is not explicitly settled by official sources; see [compliance](01-competition-compliance.md).

After the gate: record source/time of theme release, listen to the official track, review the design, and follow the [Phase 1 specification](08-phase1-kickoff.md). This Phase 0 execution stops before that work. No automated timer will start development.

## Principal risks

| Risk | Mitigation and decision trigger |
| --- | --- |
| Tool installation consumes the first development session | Install/verify required tools separately; no project creation during setup; exact steps in preflight |
| P0 exceeds one developer's capacity | One valley scene, three NPCs, one chicken species; make the full slice by Phase 3 before visual expansion |
| Theme changes the design | Keep mechanical proposals provisional; use Phase 1 review to change motivation and feedback before implementation |
| Unclear asset eligibility or license | Originals/CC0 first; record exact per-file permission; exclude uncertain candidates |
| iGPU fill rate, vegetation, and shadows | Opaque stylization, baked light, one sun, low foliage overdraw; profile a Windows player |
| Economy or save bugs block the ending | Renewable inputs, unsellable progression goods, atomic operations, persisted reward flags, backup saves |
| Submission freeze or upload failure | Package early; set internal release cutoff one day before official deadline; verify downloaded build |

## Completion contract

Phase 0 is complete as a preparation deliverable when the required documents and config are present and locally checked. Its reported readiness remains PARTIAL if critical tools or blockers remain. Phase 6 completion requires a genuine Windows playthrough and submission evidence; none exists in Phase 0. See the [roadmap](07-development-roadmap.md) for phase-specific PASS/PARTIAL/FAIL rules.
