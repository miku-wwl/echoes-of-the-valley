# Echoes of the Valley

A cozy anime-style fantasy RPG about gathering, raising chickens, crafting, and restoring a small valley. One developer; a complete 15–20-minute Windows x64 experience; English interface; no combat.

**GATHER → RAISE → CRAFT → RESTORE**

## Current status — Phase 0.5, 2026-10-23 NZDT

Preparation and development-environment recovery only. **IMPLEMENTATION_STATUS=NOT_STARTED. CORE_CONTENT_GATE=CLOSED.** No Unity project or competition game content has been created. Blender 4.5.15 LTS is installed on D: and passed a background runtime smoke check; the Microsoft Unity extension and C# dependencies are verified in VS Code. Signed Unity Hub 3.22.4 and Unity Editor 6000.3.27f1 installers are ready on D:, but installation, Hub detection, licensing, and Editor/player verification still require user interaction. Current readiness is **PARTIAL**; see the latest Phase 0.5 section of the [evidence and setup actions](docs/09-phase0-evidence.md). Original Phase 0 observations are retained as historical evidence.

## Competition and start gate

Prepared for [thatgamejam #02](https://itch.io/jam/thatgamejam02), subject to the [official FAQ](https://itch.io/jam/thatgamejam02/topic/7110933/thatgamejam02-faq) and the [compliance review](docs/01-competition-compliance.md).

- Start: **2026-10-24 20:00 NZDT** (2026-10-24 07:00 UTC).
- Deadline: **2026-11-08 20:59 NZDT** (2026-11-08 07:59 UTC).
- The musical theme is unreleased in this preflight. Narrative and interpretation remain pending.
- Project policy permits documentation, research, configuration, and tool checks before the start. It prohibits creating the Unity project, implementing gameplay, producing game-ready assets, building levels, importing complete systems, or making a game build.
- The developer reports organizer permission for pre-jam preparation. The private wording has not been reviewed, so this does not establish permission for early core-content development. Public sources do not explicitly settle each preparation activity.
- Phase 1 requires both the official start and theme release, a recorded theme review, and tool readiness. The [kickoff specification](docs/08-phase1-kickoff.md) is instructions for later, not permission to begin now.

## Intended stack

Unity **6.3 LTS / 6000.3.x**, URP and Shader Graph, C#, Unity Input System, Cinemachine 3, Unity Test Framework, Blender, and local JSON saves. Exact editor and package versions will be recorded after installation and the start gate. Windows Mono is the first-build baseline; IL2CPP is a later option requiring its module and native toolchain. No backend, multiplayer, cloud runtime, or Docker runtime dependency.

Visual inspiration: the color, readability, and warmth of anime fantasy RPGs such as Atelier Ryza. Pasture interaction inspiration: QQ Farm / QQ Ranch. All characters, layouts, UI, assets, and music must be original or independently licensed; references are not content to copy.

## Documentation

| Document | Purpose |
| --- | --- |
| [00 — Project charter](docs/00-project-charter.md) | Scope, priorities, boundaries, risks |
| [01 — Competition compliance](docs/01-competition-compliance.md) | Official evidence, dates, unresolved questions |
| [02 — Game design preparation](docs/02-game-design-prep.md) | Systems, progression, onboarding, ending |
| [03 — Technical architecture](docs/03-technical-architecture.md) | Ownership, transactions, events, saves |
| [04 — Art direction](docs/04-art-direction.md) | Solo production and Radeon 780M budgets |
| [05 — Asset license register](docs/05-asset-license-register.md) | Candidate sources and admission rules |
| [06 — AI usage disclosure](docs/06-ai-usage-disclosure.md) | Actual Phase 0 use and future disclosure |
| [07 — Development roadmap](docs/07-development-roadmap.md) | Six phases and evidence gates |
| [08 — Phase 1 kickoff](docs/08-phase1-kickoff.md) | Ordered, blocked foundation work |
| [09 — Phase 0 evidence](docs/09-phase0-evidence.md) | Environment, checks, setup, delivery record |

## Repository and licensing

This baseline contains only Markdown documentation, [LICENSE](LICENSE), [.gitignore](.gitignore), and [.gitattributes](.gitattributes). Binary LFS rules are prepared for future authored assets; no assets are currently stored. Preserve Unity `.meta` files when implementation is authorized. Use Visible Meta Files and Force Text serialization in Phase 1.

The existing MIT text and copyright holder are preserved. Its repository scope is original source code only; art, audio, fonts, third-party packages, and documentation are excluded from that grant. See the [license register](docs/05-asset-license-register.md) before admitting any asset. The simplified three-quest/chicken-only design is explicitly **COMPETITION_MVP**; the broader planned scope remains separate in the [design document](docs/02-game-design-prep.md).
