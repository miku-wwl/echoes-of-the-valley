# 03 — Jam compliance and actual-use credits

Official sources: [jam page](https://itch.io/jam/thatgamejam02) · [FAQ](https://itch.io/jam/thatgamejam02/topic/7110933/thatgamejam02-faq). Confirm again before submission.

## Hard requirements

- Jam begins **2026-10-24 20:00 NZDT**; ends **2026-11-08 20:59 NZDT** (Pacific Time changes from PDT to PST). Upload the **complete Windows or Mac build** using the **itch.io jam entry**, not an external download-only link. Default language **English**. Upload before cutoff; files lock after.
- **Core gameplay/content must be made during the jam**; an existing game, prebuilt complete level/system, or outsourced development is disallowed. The before-start blank Unity probe was strictly an environment test, not authored competition content.
- Composer: **Jason Grace**. Using the official music is optional. If included, the provided recording must be **unaltered** and used **only within the jam submission**: no remix, sampling, public Git upload or later reuse without separate rights.
- Asset rights: original or properly licensed public-domain/open-source resources; keep exact provenance, author/license/credit. Official wording when relevant: **“This game includes authorized open-source materials.”** Do not treat “free download” or commercial-franchise fan assets as cleared.
- **AI permitted, disclosure mandatory:** describe which tools helped and what they generated/assisted in the game's itch.io description, including game-ready assets, code or prose as applicable. Missing AI disclosure can disqualify the entry. The creator bears copyright responsibility.
- All-ages content, no prohibited material. Solo developer eligible. Retain project history and credits. The official pages control on any discrepancy.

## Actual-use register

**No third-party art, audio, model, font or texture file is committed.** Fill the following **for each external asset actually admitted**:

| Local path | Exact source, creator, URL | Exact license / proof | Edits and required credit | Source/Git/game redistribution OK? |
| --- | --- | --- | --- | --- |
| *(none yet)* | — | — | — | — |

**Phase 1 content is code-generated** (`game/Assets/Echoes/Scripts/Editor`): primitive-built characters, scenery and landmarks, flat-colour materials and a procedural terrain. It also uses resources that ship inside Unity itself and are not committed: the built-in primitive meshes, the built-in UI sprite `UI/Skin/UISprite.psd`, the built-in font `LegacyRuntime.ttf` and the procedural skybox shader. Confirm the Unity Personal terms and any font notice before submission.

Hold packs with unclear license (including conflicting Quaternius/QAL claims) until verified. Prefer owned/CC0 art; avoid noncommercial and ripped content. Do not mistake a license allowing game use for permission to publish raw source assets.

## Actual AI record and final template

**Known 2026-10-25 (earlier):** AI assisted planning, documentation, source-rule review and **signal-based** interpretation of the theme track. No generated model, texture, animation or audio was added at that stage.

| Date | Tool | Paths | Purpose | Human review |
| --- | --- | --- | --- | --- |
| 2026-10-25 | Anthropic Claude Code (Claude Sonnet 5.5) | `game/Assets/Echoes/**/*.cs`, `*.asmdef`, `tools/Invoke-Echoes.ps1`, `CLAUDE.md`, doc updates; generated materials, prefabs, scenes and terrain data under `game/Assets/Echoes` | Wrote all Phase 1 gameplay, UI, Editor-automation and test code, ran the Unity batch tooling, and generated the graybox scenes from code | **Pending** - the developer has not yet read or playtested it |

Subsequent AI use must be logged here by date, tool, path, purpose and human review. No AI-generated image, audio, model or texture file is committed; the graybox visuals are produced procedurally by the code above.

The submission description must be edited to reflect the **final** shipping work. Starting wording:

> This game contains AI-assisted content. AI coding assistants (OpenAI Codex for early planning and documentation; Anthropic Claude Code for [replace with the verified final coding/art/text scope]) assisted with research, planning, documentation and code. The final shipped AI-generated assets are [exact list, or an audited statement that none are included].

Replace every bracketed placeholder before upload. Keep source code under the repository MIT license **only for original code**; art, audio, docs, font and theme recording are **not MIT-licensed**. Include actual license notices and credits in the final game/entry.
