# Phase 5.2-B2 caretaker: painted anime character pipeline

Everything here is original and code-authored (no downloaded model, texture, shader, font or motion clip). The owner's supplied AI concept
pack (`01` portrait, `05` turnaround) was used as visual direction only; no pixel of it is sampled or traced for the textures or meshes.

## Why this pipeline (and what it replaces)

The first pilot built the hero from lofted tubes and boxes with 21 flat-colour materials; it read as an assembled doll. B2 changes the
method, not just the numbers:

| Layer | First pilot | B2 |
| --- | --- | --- |
| Surfaces | `loft`/`tube`/`box`, voxel-remesh blouse | parametric head profile, thick cloth sheets (outer/inner shell/edges), superellipse lofts, flattened hair clumps, rounded boxes |
| Face | eyes/lids as small geometry on a bare head | painted 1024 px face (eyes with iris glow and highlights, brows, blush, mouth) on the head UVs, plus a closed-eye twin for blinking |
| Materials | 21 flat colours | 8 painted atlases (Skin, Hair, Teal, Cream, Burgundy, Leather, Tights, Trinket) |
| Shading | URP Lit | `Echoes/ToonCharacter`: two-tone cel shading that follows the sun and shadows, baked vertex AO, rim light, camera fill, inverted-hull outline |
| Rig | 25 bones | 43 bones: hair chain, side locks, scarf tails, coat tails, skirt quarters, satchel |
| Animation | simple IK walk | IK jog whose heel, flat sole and ball stay planted at 4.5 m/s, arm swing, torso counter-rotation, travelling-wave secondary motion, upper-body turn layer |

## Files

* `kit.py` numpy surface kit; `atlas.py` shared UV layout; `design.py` / `design_body.py` the model; `rig_spec.py` / `rig.py` skeleton and skin-weight rules.
* `anim.py` clips; `skinning.py` forward kinematics and linear-blend skinning; `validate_anim.py` numeric foot-planting check on the real skinned vertices.
* `paint.py`, `textures.py`, `textures_garments.py` hero textures; `foliage_textures.py` leaf, ground-plant and terrain textures.
* `build_hero_b2.py` Blender: objects, armature, vertex groups, baked AO, actions, `hero-skin.json` for Unity, `.blend` and `.fbx` in `source/`.
* `blender_preview.py` Workbench geometry previews; `Run-UnityMethod.ps1`, `Run-PilotSmoke.ps1` helpers.

## Reproduce

```powershell
python tools/art/phase52b2/textures.py all
python tools/art/phase52b2/foliage_textures.py
& 'D:\Blender\blender-4.5.15-windows-x64\blender.exe' --background --factory-startup --python tools/art/phase52b2/build_hero_b2.py
python tools/art/phase52b2/validate_anim.py
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Invoke-Pilot.ps1 -Task Generate -LogDirectory artifacts/visual-qa/phase-5.2-b2/logs/generate
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Test-PilotCopy.ps1 -EvidenceName b2-verification
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Invoke-Pilot.ps1 -Task Build -BuildDirectory artifacts/build/phase52-b2 -LogDirectory artifacts/visual-qa/phase-5.2-b2/logs/build
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Capture-PilotMotion.ps1 -PlayerDirectory artifacts/build/phase52-b2 -OutputDirectory artifacts/visual-qa/phase-5.2-b2/player/after720 -Video
```

Blender previews and the Unity model stills (`Phase52HeroPreview`, `Phase52PilotPreview`) are diagnostics, never Windows-player evidence.
