# 08 — Phase 5.2-B2: painted caretaker, canopy camera fix, path restoration

2026-10-28 NZDT. Bounded revision of the **failed Gate B pilot** (`art/phase52-pilot`, commit `5f90a6bf906f95d3c4c344ee940df2e85fcd58a4`, preserved untouched). Work is on the local branch `art/phase52-b2`. **Gate B owner approval: PENDING. World expansion: NOT allowed.** An assistant's own PARTIAL/PASS is not owner approval; see "What still looks bad" below.

## Verified root causes (not assumed)

| Complaint | What the data showed | Evidence (ignored `artifacts/`, copied into the review package) |
| --- | --- | --- |
| Dirt paths replaced by uniform green | The saved `Phase52/PilotTerrain.asset` was **100 % Grass at every texel** (mean layer weights Grass=1.000, all others 0.000). Regenerating the pilot called `EditorUtility.CopySerialized` onto the existing terrain asset, which left three duplicated `SplatAlpha` sub-assets and lost the painted data. The route paint also multiplied every original layer by `1-mask` within 4-8 m of the route, so even a correct paint would have erased the original sand paths and the windmill trail. Net effect: plaza, five paths and the trail disappeared, and the new dirt layer was empty (0 of 133 route samples, 0 of 88 original path samples were dirt). | `diagnostics-before-fix/pilot-asset-audit.txt`, `terrain-foliage-audit.txt`, `terrain-topdown-baseline.png` versus `terrain-topdown-pilot.png`, `dirt-*.png` |
| Giant leaf cards in the camera | Tree meshes used 33-45 leaf cards of **2.2-2.6 m** per tree, the lowest at **0.9-1.9 m** above the ground. The orbit camera sits 6 m from a pivot 1.5 m above the feet (4 m high at the default pitch) and its sphere cast only tests colliders; leaf cards have none, so the camera could end up inside a canopy with a card at the near plane. | `terrain-foliage-audit.txt`; `video-frame-20.png` and `video-frame-30.png` of the Gate B recording |
| White mirror-like glint on the ground (found while reviewing the first B2 player captures) | The two code-painted terrain textures (`RouteMeadow`, `RouteDirt`) were imported as BC7 (`CompressedHQ`, alpha source None). URP's terrain shader treats any diffuse format with an alpha channel as "diffuse alpha = smoothness" and locks the layer smoothness to 1.0 (comment in `TerrainLitPasses.hlsl`), so the sun drew a mirror highlight on the path/meadow whenever the camera looked away from the sun. Bisect with the same Editor camera: the glint disappeared with the sun off, was unchanged with post-processing off and terrain details off, appeared on exactly the two new layers when each layer was shown alone, and never on the older layers (DXT1). Fix: DXT1 (RGB-only) for ground layers plus an EditMode regression test. | `diagnostics-glow/`, `diagnostics-glow2/`, `diagnostics-glow3/` |
| Cream skirt showing through the back and sides of the coat (also present in the first B2 build) | The coat flare and the skirt flare were tuned separately, with different pleat patterns, so the skirt's outer shell crossed the coat's back and side panels (up to ~5 cm). The coat now stands outside the skirt by construction (`skirt_radii` is shared and the coat radius is clamped to skirt radius + 2 cm including both pleat patterns) and the hip belt is measured on the real coat surface. | `model-preview/unity-r2` vs `unity-r2c` (Editor diagnostics, not player evidence) |
| Toy-like hero | The first pilot's authoring (`loft`, `tube`, `box`, 21 flat-colour materials, geometric eyes) has a quality ceiling regardless of triangle count. | `tools/art/phase52b2/README.md` |

## What changed

* **Character art pipeline (substantially redesigned).** Parametric Blender-side surfaces from numpy (`tools/art/phase52b2/`): an anime head profile (wide cheeks, small chin), layered hair clumps (bangs, side locks, a nine-lock ponytail, ribbon, one curled strand), thick cloth sheets for the teal coat, cream skirt, petticoat and scarf, a superellipse torso, boots with cuffs and straps, a satchel with vials, hands. 60 k triangles, **8 painted materials** instead of 21 flat colours. The face (eyes with iris glow and highlights, lashes, brows, blush, mouth) and every garment trim, embroidery band, lacing, stitch line and scarf fringe is **painted by code** into UV atlases; vertex AO is baked in Blender. Everything is original; the supplied concept pack was visual direction only.
* **`Echoes/ToonCharacter` shader** (own HLSL): painted albedo, two-tone cel shading that follows the real sun and shadows, shade tint per material, baked AO, camera fill so faces never fall into backlit grey, rim light, optional toon highlight, inverted-hull outline of constant pixel width, blink swap for the face.
* **Rig and motion.** 43 bones (hair chain, side locks, scarf tails, coat tails, skirt quarters, satchel). `Idle`, `Walk` (a light jog whose cycle is exactly `stride = 4.5 m/s x 0.64 s = 2.88 m`, so the unchanged controller speed is matched by construction), `TurnLeft`/`TurnRight`. The legs use IK so the heel, flat sole and ball of the foot stay planted in the ground frame during stance; arms swing opposite to the legs with bent elbows, the torso counter-rotates, and hair, scarf, coat tails and skirt follow travelling-wave secondary motion. Turn clips play on an upper-body layer (they never scale the legs down). The player controller, speed, camera and collider are unchanged.
* **Foliage and camera.** Three tree silhouettes of ~70-125 small painted leaf clusters each (no card larger than ~1.2 m, canopy kept above 2 m), low ground cover. The `Echoes/Foliage` shader dissolves leaves that are close to the camera and leaves that sit inside a cone between the camera and the hero (published as the global `_EchoesFocus` by `FoliageFadeDriver`). The free camera, its sphere cast and its serialized values are unchanged.
* **Terrain and paths.** `PaintTerrain` now makes a fresh `AssetDatabase.CopyAsset` copy of the original terrain data (heights untouched), keeps stone, re-skins the original grass layers and the original path/trail layers with new painted layers and **adds** the route connectors, at 512 px instead of 128. Texels the original terrain left with no weights (156 along its far southern edge) are filled with Grass in the copy.
* **Small UI fix.** The story banner no longer shows behind the satchel/crafting panel (its timer waits while a panel is open) and an identical toast that is still showing refreshes instead of stacking.

## Verification (what was actually run)

Unity 6000.3.27f1, URP 17.3.0, Windows x64 Mono/D3D11. All player input is **automated** (`PlayerController.InputOverride` and the camera APIs); no human played. Teleports only set up still positions and the whole-game smoke; the timed route has none.

| Check | Result |
| --- | --- |
| EditMode, isolated project copy (`artifacts/visual-qa/phase-5.2/logs/b2-verification3`) | **Passed 219/219** (nine new B2 tests, including the ground-glint regression test) |
| PlayMode, same copy | **Passed 89/89** (two new: turn layer, foliage-focus driver) |
| Windows build `artifacts/build/phase52-b2-r2` | Succeeded, build id `51f3e0a09c6741a4bc1905c9a40a3557`, 0 errors, 0 warnings, 230,909,145 bytes; RC1 and RC2 zip SHA-256 unchanged; earlier pilot build folders not written to |
| Automated 40 s route (175 m, 45 waypoints, canopy detour, orbit and low-pitch legs): recorded 720p, 1080p, RC2-baseline visuals, unrecorded 720p, unrecorded 1080p | all exit 0, 0 exception lines; video 40.04 s, 942 framebuffer frames, no desktop capture, no concept art |
| Whole-game smoke (New Game, three restorations, ending, save/Continue) 720p and 1080p | PASS twice, natural exit 0, 0 exception lines, 40 screenshots each |
| Unrecorded frame time, Radeon 780M, windowed, 60 FPS cap | 720p P99 16.97 ms, 1 % low 57.6 FPS; 1080p P99 16.94 ms, 1 % low 56.9 FPS; RC2-baseline visuals 720p P99 16.92 ms, 1 % low 57.7 FPS. The cap saturates, so no regression is measurable at 60 FPS; headroom above the cap is **NOT VERIFIED**. The recorded run (26.5 FPS) includes framebuffer readback and is not a benchmark. |
| Foot planting on the real skinned vertices at 4.5 m/s | lowest sole vertex -1.1 mm / -0.5 mm; contact-vertex ground speed median 0.001 / 0.006 m/s (p95 0.48, maximum 2.1 m/s at the heel-strike and lift-off frames) |
| Terrain | every texel sums to 1; 140 of 142 original dirt-path samples are still dirt; stone area within 5 % of the original |

Not verified: any human playtest, GUI feel, audio, other GPUs, multi-minute traversal, VRAM and peak memory.

## What still looks bad (author's honest list; Gate B owner approval is PENDING)

* **Face.** Large amber anime eyes, blush and a small smile read as "generic stylised anime", not as the supplied portrait's face; skin shading is nearly flat and the nose and mouth are minimal. The 3D head is a smooth balloon with little structure under the hair.
* **Identity match.** Costume, colours and silhouette follow the portrait (teal coat, cream dress, burgundy scarf, satchel, boots), but the 3D hair is much shorter and straighter than the portrait's long wavy ponytail, the tights are black instead of brown, and the proportions are chibi-leaning (the portrait's legs are far longer).
* **Hair.** Bangs and ponytail are smooth separate ribbons with blunt points. In the extreme near-camera shot the strand texture still reads as wood grain and the painted highlight band is a pale smear. There is no fine strand detail.
* **Hands and arms.** Hands are simple low-detail mitts and the bell sleeves hold a stiff A-pose in idle.
* **Cloth.** The coat and skirt are thick sheets driven by bone chains, not cloth simulation, so tails can look rigid; embroidery is flat painted texture (no normal maps).
* **Animation.** One jog cycle at 4.5 m/s, no walk or run variants and no start/stop transitions; turns are an upper-body layer. Foot planting is proven numerically but nobody has judged how natural it looks.
* **Style mismatch.** Mira, Alder, Wren, Clover and the rest of the valley outside the pilot corridor keep the RC2 low-poly look; next to the painted hero the villagers look blocky (visible in the comparison page).
* **Trees and plants.** Leaf clusters are flat cards with visible edges (a "confetti" look), the willow strands read as green planks, ground flowers look folded from paper, and the path pebbles repeat. The S0 grading (grey-teal) is inherited from RC2.
* **Foliage fade.** The dithered near-camera fade was only seen in still frames; shimmer in motion is NOT VERIFIED.

## Feasibility decision (Option A / Option B)

Option A (improved rigged 3D character) was pursued and delivered; see the results below. Option B (multi-direction 2.5D) was **not built**: with local tooling its eight-direction walk frames could only come from rendering the same 3D model, so it cannot be prettier than Option A, and a hand-painted or AI-generated set of consistent walk frames is outside local, licensable tooling. A static billboard or a fixed camera were never considered; the free third-person camera is unchanged.

## Provenance and AI disclosure

All code, meshes, rig weights, animation, textures (hero, foliage, terrain), shaders and this documentation in B2 were produced with Anthropic Claude Code, directed by the developer. Nothing is downloaded, ripped, sampled or traced; no third-party model, motion capture, stock texture, font or sound is used. The owner-supplied `01-heroine-portrait.png` remains an **unmodified** AI illustration shown only as the labelled Caretaker information; nothing from `02`-`05` is imported. See [03](03-RULES-CREDITS.md).
