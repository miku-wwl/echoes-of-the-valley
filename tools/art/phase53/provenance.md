# Phase 5.3 actual-use provenance

Owner archive SHA-256: `80C763E0AF1037BC5A377119976B27331633C1D96C54F095B4BCC930F555037C`.
The supplied SHA256SUMS verifies seven PNGs. Originals are byte-identical in `game/Assets/Echoes/Art/Phase53/Source/`.
Full source hashes are in `Production/SourceHashes.json`, independently pinned in `Phase53Tests`.

| Source | Actual use / modification |
| --- | --- |
| Caretaker_8dir_idle_source.png | Eight complete alpha-connected instances, fringe removal, edge RGB bleed, body scale and common foot pivot. New map character only, no old portrait rotated. |
| Caretaker_8dir_walk_keyposes_source.png | Sixteen complete instances = TWO original contact drawings per direction. Eight derived local lower-leg deformation frames per direction. Animation quality PARTIAL; no claim of eight original drawings. |
| Mira_8dir_idle_source.png | Eight complete alpha instances and shared ground pivots, actual player-facing dialogue orientation. UI bust retained; identity consistency needs owner review. |
| Pond_S0_dormant_45deg_source.png | Cleaned low-alpha fringe; source-image gap for the existing stream inlet; projected onto a real world-space basin/shore mesh, not a floating camera card. |
| Pond_S3_restored_45deg_source.png | Same geometry/UV registration as S0. Conservative stage switch and tint instead of a ghosting crossfade. |
| LilyPads_6clusters_45deg_source.png | Six alpha-cleaned clusters on a combined world water-plane mesh, slight elevation, no green cylinders. |
| PondWater_muted_albedo_TILING_CANDIDATE.png | Periodic edge correction, wrap edges 0/0 error; optional/reference production tile, not used to mislabel the static painted pond as animated water. |

Generation: ChatGPT image generation, supplied by the owner under the current task authorization. No new illustrations were generated
by the implementation agent. OpenAI Codex wrote/adapted the processing, C#/Unity build tools, tests and evidence tools. Earlier B3/B4/B5
art and pipelines retain their existing provenance and AI disclosures. Windows/Python/Unity/FFmpeg are local processing tools; no new
marketplace, stock, ripped or franchise asset is introduced. No official theme WAV is added.

Code is MIT as in the repository. That grant does not cover the supplied AI-assisted illustrations. The source pack's rights statement
does not guarantee copyright, exclusivity or jam acceptance. Owner confirmation of the current jam's AI disclosure is PENDING before
release. Human playtest and visual approval remain PENDING. Wren/Alder have no new supplied turnaround art and retain their old visuals,
explicitly PARTIAL. Source docs are preserved under `source-docs/`.
