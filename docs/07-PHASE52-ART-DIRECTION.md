# Phase 5.2 — character-first pilot and owner gate

2026-10-27 NZDT. The owner **rejected Phase 5.1 character and environment quality after playing the Windows build**. Earlier engineering PASS labels do not override that decision. Human visual/gameplay acceptance for the new pilot remains PENDING. This phase is authorized through **A and B only**; whole-valley expansion and release-candidate work require explicit owner approval of B.

## Baseline and references

Initial HEAD `d9a8d12f79e17257c4808498f8c4144602cafbc3`, clean worktree; parent `099b0b4e45f09ae805922ed108d2ce17a60e8bb0`. Work is isolated on local branch `art/phase52-pilot`. Every initially tracked file was hashed before changes. Baseline tests run on a fresh copy of tracked `game/` files at `D:\Unity\Temp\Echoes\phase52-baseline-20261027\game` so generator tests cannot renumber the owner's original scenes.

RC1 SHA256 `EB9BFC0D9B0BA60A0E9E8367ADF3BA4EB4D4F3EFE8F0F6FCD58A2AB4FDA3112A`; validated RC2 SHA256 `9085554ED52B39B46B250AA5E7E94642815FF76D9730AF038E6E8F180CFC8A80`. Both are protected and retained. Tool paths: Unity `D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe`, Blender `D:\Blender\blender-4.5.15-windows-x64\blender.exe`. URP is 17.3.0. Initial protection/GUIDs and reference-file hashes: ignored `artifacts/visual-qa/phase-5.2/baseline-protection.json`, `baseline-tracked-hashes.json`, `reference-image-audit.json`.

Read all supplied direction documents and inspected all five original PNGs at native resolution and all ten actual baseline frames. Pack: `C:\Users\weila\Music\Echoes-Phase52-Original-Anime-Art-Pack\Echoes-Phase52-Visual-Target-Pack`. The ten frames include desktop/sharing/notification overlays: internal evidence only. The generated PNGs are design targets, not runtime evidence. No target valley/UI still is used as a fake level or screenshot.

| Reference | Observation and implementable delta |
| --- | --- |
| 01, 1024x1536 RGBA | Adult brunette alchemist, ponytail, expressive amber eyes, fitted teal/cream layers, burgundy scarf, distinct leather satchel. Original picture may be tried unmodified as a correctly labelled caretaker information portrait; it is not Mira. Alpha is present (0–254), with semitransparent painted/vignette regions that require runtime edge/fit review. |
| 02, 1672x941 | Distinct Mira robe/herbs, Wren gardening workwear and Alder tools. Reference only until B approval; no NPC rebuilding in this pilot. |
| 03, 1672x941 | Connected canopy masses, foreground plants, path edges and cool distance frame the architecture. Apply only to the existing garden/plaza/north-path route; no extra towns, lake or copied background picture. |
| 04, 1672x941 | Parchment/material hierarchy and portraits are useful principles. Its example heroine labelled Mira is not authoritative. Whole UI redesign belongs to C; preserve English names/recipes and controls. |
| 05, 1536x1024 | Useful front/side/back silhouette, coat/scarf/satchel arrangement. Viewpoint seams and accessories are inconsistent; define one coherent model rather than tracing every view. |

## Gap matrix and authority

| Area | Observed problem / source | Controlled response |
| --- | --- | --- |
| Hero | `PilotArt.Human`, `build_pilot.py`, `CharacterAnimator`: separate rigid arm/leg pivots; no elbows/knees or skin weights; simplistic face | New Blender armature and weighted surfaces, native skinned mesh and real idle/walk/turn clips, shoulder/elbow/knee continuity, coherent costume and face. Keep the existing player/controller/collider/camera contract. |
| Mira/Wren/Alder | Same rigid anatomical base and simple face; `Characters` -> `PilotArt.Human`, `NpcIdle` | Leave existing quest actors intact for B; establish a hero construction method before applying it to them. |
| Clover | Simple shaped bird, `GameplayPieces.Hen` / PilotArt geometry | Preserve Body/Egg/state anchors and timer; defer art refinement to C. |
| Trees/plants | Reused branch model and angular separated solid leaves; `Pieces.Tree`, `PilotArt.Tree`, `WorldArt.TreeLine` | Three original pilot tree silhouettes with connected alpha-cutout foliage clusters; near-ground layering only along selected route. Preserve authoritative collision and quest vegetation groups. |
| Hills/path | Large uniform green slopes, seeded scatter; `ValleyLayout`, terrain generator, `PilotArt.Path` | Local route dressing/path transition and atmospheric depth in an isolated scene; no terrain height or navigation rewrite. |
| Windmill surroundings | Improved architecture but empty context; `Phase3Pieces`, `PilotArt.Dress` | Retain functional mill and sails; frame approach with controlled canopy/ground planting, keep Alder/door/path clear. |
| Workshop | Detailed roof but simple pot and sparse surroundings; `Landmarks`, `WorldArt.Cottage` | Keep existing structure/interactions; pilot route connects existing garden/workshop vicinity to north path. Larger prop rework waits for C. |
| UI | Opaque rectangular panels, repeated toasts and banners under panels; `GameplayHud`, `HudController`, `GameplayUiBuilder` | Correctly identified caretaker portrait in the pilot; no heroine portrait on NPC speech. Full layout/toast redesign is explicitly deferred to C. |

## Selected route and architecture

**HYBRID**, retaining fully free third-person mouse look. One original skinned 3D playable hero, an optional caretaker information portrait, real 3D architecture and original cutout foliage. No directional-billboard replacement and no camera locking. A genuine armature/weight/clip path is feasible with installed Blender and Unity native SkinnedMeshRenderer/AnimationClip APIs; no external model/rig/package or paid service is required.

Pilot assets/scenes are separate from existing Hero/Valley/MainMenu. Clone the current playable scene using Unity APIs, preserve its gameplay references and terrain/colliders, and replace only the player's visible model plus a bounded route dressing group. Original rigid visual remains available for matched before captures. Existing scene generators and default build scenes continue to describe RC2 until owner approval. Never hand-write serialized Unity YAML.

The motion adapter consumes existing movement speed/actual displacement and turn rate. It must not change movement speed, acceleration, input, collision, interaction or the camera. Idle/walk/turn and cloth/hair motion use actual bones and skin weights; no rigid cylinder swing or concept sprite is accepted as the moving hero.

## Definition of done and limits

- Real Windows player: close front, both sides/rear, walk and natural turns; real joint bending, foot contact, shadows and no severe clothing penetrations. 720p/1080p matched camera and stage images.
- At least 30 seconds continuous movement recording from that player. Label automated input as automated, record controls/route, and never count teleports as walking. Keep the original captured frames/video distinct from references.
- Confirm baseline 203 EditMode + 85 PlayMode results afresh; run relevant skin-weight, clip-binding, route/collision and preserved-gameplay checks. No tests assign aesthetic scores.
- Pilot build and evidence use fresh paths; no RC1/RC2 overwrite. Full release phase D is not authorized before C.
- Provide a small review ZIP containing true before/after, continuous actual-player video, changed source, tests, logs, source/asset hashes and explicit limitations.
- **Stop at B for independent review even on self-rated PASS. PARTIAL/FAIL cannot authorize expansion.** `GATE_B_OWNER_APPROVAL=PENDING`, `WORLD_EXPANSION_ALLOWED=NO` until the owner clearly approves.

Primary risks: believable face/topology and deformation; long coat/satchel clipping; foot sliding at the unchanged 4.5m/s controller speed; cutout-card shape and overdraw under a free camera; user-provided portrait edges/identity. Work prioritizes the hero and motion over scattering more objects. No commercial-quality promise, no new gameplay, no push/upload, no claim of human testing.

## Current evidence

Gate A source/target mapping and protection: complete. Baseline test results, model/export details, build budget, actual pilot measurements, B assessment and local commit are appended after observation. Existing Phase 5.1 art is **REJECTED by owner**; new human acceptance remains PENDING.

## Observed pilot outcome — stop before C

**Gate A PASS; Gate B FAIL.** The weighted hero is functional, but facial/costume finish remains partial and the continuous actual-player film exposes intrusive near-camera foliage cards (around 20s and 30s), repeated ground clumps and an unfinished environment. The strict visual target is not met. `GATE_B_OWNER_APPROVAL=PENDING`, `WORLD_EXPANSION_ALLOWED=NO`. No whole-valley expansion or Phase D release candidate was performed.

Fresh baseline tests: **203/203 EditMode, 85/85 PlayMode**. Full isolated pilot-source regression: **210/210 EditMode, 87/87 PlayMode**; no old test removed or weakened. A new conservative-bounds floor assertion was corrected to check actual baked vertices with the same threshold: actual floor +0.00554m, knee bend 86.77°, average vertex change 0.0776m. The full suite preceded the later capture-only recording fix; that branch was exercised in the final Windows player and two further whole-game smoke runs.

Final non-development **pilot** build: `b4e97ba7c8174917b786ddb2f252584d`, version `0.5.2-pilot.1`, zero errors/warnings. Two built-player smokes pass in 70/75 seconds with 40 actual screenshots each and normal exits 0. Original sources/scenes/prefabs/settings and 36 WAVs remain byte-identical; only existing README/docs 03/06 gain current acceptance/provenance notes. RC1/RC2 hashes remain intact.

The 25-bone hero has 18,864 triangles / 10,848 vertices, four native skeletal clips and one skinned renderer with 21 material submeshes. The original 01 PNG is an unmodified caretaker-information portrait, not NPC speaking art. The route uses three tree types, local copied-terrain paint and visual dressing; all other world/actors remain original pending approval.

Valid motion evidence: `artifacts/visual-qa/phase-5.2/motion/after720-2/actual-player-automated-motion.mp4`, 36.08 seconds, 769 consecutive actual framebuffer samples encoded at their real intervals (about 21.34 FPS), automated controller/camera input and no in-route teleport. No audio. Framebuffer readback/JPEG capture adds substantial overhead; this is not a clean FPS benchmark. The earlier occluded desktop-grab video is rejected/private/excluded. Five Blender runs, three scene-generation attempts (one new-test API compile error), two successful Windows builds; no hidden retries.

Seven matched comparisons, original PNGs, XML/logs, hashes and the detailed B assessment are in the ignored `artifacts/visual-qa/phase-5.2/final-report.md` and `comparison/index.html`. Static 8s and 36s route samples are documented there without a general FPS claim. Three-minute traversal, peak memory and full human input/audio acceptance are NOT VERIFIED. Independent owner/ChatGPT review is the next action; B failure cannot be overridden by engineering PASS.

Technical API references used for the native bake: [Unity Skinned Mesh Renderer](https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/asset-types/mesh/components-reference/class-skinned-mesh-renderer), [Mesh bindposes](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/mesh/bindposes), [AnimationClip](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/animationclip). No reference-source code was imported.

## B2 revision (2026-10-28)

The Gate B failure above is historical and preserved. The bounded B2 revision (new painted toon character, canopy/camera fix, path restoration and the measured root causes) is documented in [08](08-PHASE52-B2.md). `GATE_B_OWNER_APPROVAL=PENDING` and `WORLD_EXPANSION_ALLOWED=NO` still apply.
