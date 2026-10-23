# 04 — Art direction and solo production

**Production plan only, 2026-10-23 NZDT. No images, models, textures, animations, or other game-ready art produced or downloaded.** All theme-dependent mood, musical timing, story symbolism, and final palette accents remain pending.

## Visual language

Original bright anime fantasy 3D: soft sunlight, warm village surfaces, readable color blocks, gently painterly texture variation, rounded foliage clusters, and cozy asymmetrical roof silhouettes. Borrow only general lessons of color/readability from genre references. Do not copy a franchise character, costume, environment composition, animation, logo, or interface.

Proposed base palette: warm cream plaster, honey wood, muted red roofs, mint/sage foliage, lavender shadow tint, and restrained cyan interaction accents. Keep interactable herbs distinct in value and silhouette, not color alone. Character face/hair/clothing must read at the normal camera distance. Use a simple shared light response rather than a photoreal environment with a mismatched toon hero.

## Minimum production set after the gate

| Set | Solo budget and method | Acceptance |
| --- | --- | --- |
| Hero | One original adult model, one outfit, simple face, one rig | Recognizable silhouette; clean walk/idle and interaction pose |
| NPCs | Three role variants from an owned shared body/rig | Distinct apron/hat/color silhouettes, readable dialogue position |
| Buildings | Three modular building silhouettes, workbench, pen, path kit | Consistent scale, roof/plaster palette, no inaccessible critical interaction |
| Vegetation/resources | Two tree shapes, two bush shapes, two herbs, seed patch, ore node | Opaque leaf clusters where practical; gathering state readable |
| Animal | One chicken model with chick/adult scale or mesh states | Feeding/growth/egg feedback distinct, no elaborate animation dependency |
| Restoration | Three staged visual changes using small owned props | Before/after state visible from play camera; survives load |
| UI | Inventory, recipe panel, quest tracker, dialogue, pause/title, credits | Shared spacing, rounded panels, English labels, keyboard focus |
| Audio | Minimal UI, gather, craft, chicken, delivery sounds; optional music | Independent license proof; no theme commitment before release |

Order: greybox whole slice first; validate one hero, one prop, one plant, and one building as the style sample; then reuse palette/materials across remaining content. Timebox a hero/rig experiment to half a working day after the gate; simplify face detail, hair geometry, and animation before sacrificing a playable ending. No cinematic is required for P0.

## Blender → Unity workflow

Model at a consistent meter scale, apply transforms, use clear object/material names, and export a small FBX sample for verification after kickoff. Keep original `.blend` files and export provenance. Author short in-place idle/walk/interact clips; hand-key simple poses if rig tooling costs more than it saves. Bake deformation into an export rig, verify bone count and skinning, and avoid copying animation data from commercial games.

A simple owned armature is the fallback. Rigify is a candidate tool, not an imported production dependency; its compatibility/generated-script distribution still needs review. See [license register](05-asset-license-register.md). Avoid embedding authoring add-on Python in runtime assets. Use one opaque Shader Graph material family with configurable palette/shadow tint, minimal texture reads, and no expensive screen-space outlines initially.

## Radeon 780M rendering budget

Performance goals are **1080p medium: 30 FPS** and **720p low: 60 FPS**. Neither has been tested; these correspond to 33.3 ms and 16.7 ms total frame budgets. Shared system memory makes the CIM-reported dedicated memory an unreliable total graphics-memory measure.

Initial content caps are hypotheses to profile: hero ≤15k triangles; NPC ≤8k each; chicken ≤2k; visible scene ≤250k triangles; most textures 512–1024 px, hero atlas ≤2048 px; typically one or two materials per prop; environment atlas reuse. These are working budgets, not guarantees or measured limits.

| Setting | Medium starting point | Low starting point |
| --- | --- | --- |
| Lighting | Baked environment plus one realtime sun | Same, reduce shadow distance/resolution |
| Shadows | Approximately 35 m; 2 cascades; 1024 map candidate | Approximately 20 m; 1 cascade; 512 candidate |
| Render scale | 1.0 at 1080p | 1.0 at 720p; reduce only if measured necessary |
| Anti-aliasing | 2× MSAA candidate | Off or 2× based on profiling |
| Vegetation | Clustered opaque geometry; simple LODs | Lower density/distance; same essential cues |
| Post effects | Restrained color/bloom if budget allows | Disable bloom and optional effects |
| Additional lights | Baked or unshadowed, sparse | No extra realtime shadowed lights |

Do not add volumetric fog, SSR, heavy SSAO, tessellation, dense transparent grass, or dynamic weather to P0. Use baked ambient/occlusion, simple gradients, and silhouette composition. Test static batching/instancing with actual materials; no claim of savings before profiling.

## Validation and cut sequence

Capture the same village, meadow, and pen camera paths in a Windows player at both target presets on the 780M. Log resolution, preset, driver, AC/power mode, build SHA, warm-up, frame-time distribution, and traversal duration; exclude loading from steady-state measurements but report it separately. Inspect GPU/CPU profiler evidence and frame spikes; Editor FPS alone is insufficient.

If slow: reduce shadow cost, vegetation overdraw/distance, extra lights/post effects, then geometry/material count. If visual production slips: cut extra NPC costumes, secondary props, facial animation, and cinematics; keep one original hero, readable interactions, and three visible restoration steps. Verify English contrast/focus at both resolutions. Source candidates and uncertainties are recorded in the [asset register](05-asset-license-register.md).
