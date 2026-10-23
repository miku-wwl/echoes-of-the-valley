# 05 — Asset license register

Research date: **2026-10-23 NZDT**. **Zero production assets selected, downloaded, generated, or imported.** These are source candidates, not clearance of individual files or a claim that they are in the game. Asset eligibility and copyright permission are separate checks.

## Candidate register

| ID / kind | Source and proposed use | License evidence / obligations | Jam disposition |
| --- | --- | --- | --- |
| A01, original 3D | Developer-authored hero, NPC variants, chicken, buildings, vegetation after start | Keep authored `.blend` sources and dated exports; original art excluded from source MIT | Preferred; core work must occur during jam |
| A02, environment support | [Poly Haven license](https://polyhaven.com/license): possible base surface/model, heavily style-adapted | Asset library CC0; attribution not legally required by CC0, but provenance and jam credit retained. Website images/logos are not covered by asset CC0 | Candidate; select individual file and avoid complete levels; photoreal assets may undermine cohesion |
| A03, UI/SFX/simple props | [Kenney support](https://kenney.nl/support), [asset catalog](https://kenney.nl/assets) | Asset pages use CC0; inspect included license per pack. Optional author credit Kenney; do not use studio logo | Preferred fallback for small supporting assets, not a gameplay system |
| A04, 3D animals/rigged models | [Quaternius FAQ](https://quaternius.com/faq.html) and [current license](https://quaternius.com/license.html) | CONFLICT: FAQ says CC0; QAL v1.0 dated 2026-08-28 prohibits standalone asset redistribution, including modified assets. QAL is not equivalent to CC0 | **UNVERIFIED / HOLD**. No download history or per-pack earlier CC0 grant exists here; exclude pending exact license and organizer clarification |
| A05, authoring/animation tool | [Blender license](https://www.blender.org/about/license/); [Rigify manual](https://docs.blender.org/manual/en/4.1/addons/rigging/rigify/index.html) | Blender software GPL; owned artwork is usable independently. Rigify GPL tool; specific version, availability, generated script/license/export review still needed | Blender preferred; simple owned armature fallback. Rigify NOT VERIFIED for chosen Blender version |
| A06, font | Nunito: [exact OFL file](https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/OFL.txt), [upstream family](https://github.com/google/fonts/tree/main/ofl/nunito) | SIL OFL 1.1; retain copyright and full license with bundled font, preserve license, obey reserved-name conditions if modifying; do not sell font alone | Candidate open font; inspect exact downloaded files later; record any atlas generation |
| A07, SFX | [Freesound license FAQ](https://freesound.org/help/faq/#licenses), individual sound not selected | Mixed CC0/CC BY/CC BY-NC and legacy licenses. Prefer CC0; CC BY needs author/title/source/license and changes. Exclude NC/unclear material by policy | Per-file **UNVERIFIED** until chosen; never assume site-wide CC0 |
| A08, background music | [Scott Buckley library](https://www.scottbuckley.com.au/library/), track undecided | Library states CC BY 4.0. Preserve track author/title/source/license; indicate edits; verify chosen track and composer instructions | Candidate; confirm open-license fit against jam FAQ before use. Theme fit remains pending |
| A09, official theme audio | [Official jam FAQ](https://itch.io/jam/thatgamejam02/topic/7110933/thatgamejam02-faq) | Submission-specific permission only; see compliance theme-rights row. Composer credit and exact release metadata pending | Optional, unreleased; not for public-source redistribution without explicit permission |
| A10, Unity packages/tools | [6.3 packages](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-core) and installed package notices later | Unity/package-specific licenses; no blanket MIT relicensing; retain applicable notices from exact packages | Public development tools allowed; do not import complete sample gameplay/levels |
| A11, proprietary art/animation marketplaces | No pack selected; includes commercial stock and ready-made character/animation libraries | Paid/free proprietary permission is not automatically public-domain/open-source permission; source redistribution may be restricted | **UNVERIFIED / EXCLUDED by default**; tools and stock content require separate review |

The Quaternius [current terms](https://quaternius.com/license.html) say changes are not retroactive to previously obtained assets. This project has not obtained any; an old FAQ is not proof that a future download is CC0. Never commit QAL raw assets to a public repository on the assumption that a game-use grant allows source redistribution.

## Admission procedure after the gate

Before importing a supporting asset, add an actual-use entry with a unique ID; exact file/pack URL and author; retrieval date; exact license/version and retained license text; source and exported file hashes; project path; modifications; required credit; runtime/build/source distribution permission; and a competition evidence decision. Missing data means HOLD, not silently approved. Do not download assets merely to fill the register in Phase 0.

CC0 is a copyright waiver, not verification that a download lacks trademarks or third-party content. Inspect provenance and content. Avoid fan art, ripped game files, franchise likenesses, voice/music impersonations, and complete gameplay systems. Prefer a small number of coherent sources rather than a large mixed asset dump.

## Release obligations

Ship a human-readable third-party notices file with actual sources/license text and show relevant credits in-game/on the entry page. The organizer's prescribed open-source-material sentence is recorded in [compliance](01-competition-compliance.md); individual credits remain necessary. No actual-use credit list exists yet because no assets are in use.

Source code MIT scope excludes art, audio, fonts, tools, documentation, and all third-party material; see [LICENSE](../LICENSE). A source repository and a game ZIP have different redistribution surfaces. Exclude a raw asset from public Git if its license does not permit source redistribution; exclude it from the game too if jam eligibility remains unresolved. Re-evaluate optional theme audio before any post-jam release.

## Research limitations

The primary Blender license/manual URLs initially failed in the web tool; the official indexed license content and a versioned Rigify manual were accessible. The current Rigify extension listing also showed availability/review warnings, so no modern compatibility claim is made. No asset binary, rig output, font atlas, music excerpt, or attribution package has been tested. Final asset compliance is **NOT VERIFIED** until actual selections exist.
