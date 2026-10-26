# Original Phase 5.1 art pipeline

No external mesh, texture or image is used in these assets. `build_pilot.py` authors surfaces, loop normals and UVs in Blender 4.5 LTS. This is original AI-assisted work directed by the developer; see `docs/03-RULES-CREDITS.md` for scope and rights. Source-code MIT licensing does not license the resulting art outputs.

```powershell
& 'D:\Blender\blender-4.5.15-windows-x64\blender.exe' --background --factory-startup --python tools/art/build_pilot.py
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-Echoes.ps1 -Task Setup
```

Editable source/interchange: `source/echoes-pilot.blend`, `source/echoes-pilot.fbx`. The FBX is not the runtime import path. Blender exports `game/Assets/Echoes/Art/PilotSource/pilot-meshes.json`; `PilotArt.Bake` turns explicit metre-space positions, UVs, normals and winding into native Unity meshes, and produces shared URP Lit materials with small original procedural color/normal textures in `Art/Pilot/`. Unity creates all asset and meta serialization. Run Setup after changing Blender source, not just Compile.

The caretaker is about 1.65m / 6.7 heads, with rigid pivots matching the existing procedural animator. It has shaped jaw/neck, separate sleeves/forearms/hands/fingers, boots, clothing trim, scarf, hair and satchel. This is **not a skinned humanoid rig**. Elbows/knees do not bend independently; close-up character fidelity needs visual review.

The new mill shell retains the original tower collision envelopes and node names. SailsPivot / IntactSails / BrokenSails / SupportsBroken / Braces still drive the existing stage and rotation behavior. The pilot's opaque branch/leaf meshes and prop placements are visual-only, with the original terrain and gameplay positions unchanged. Whole-world expansion is subject to the pilot quality gate.

An optional `-echoes-art-audit` argument to the existing `-echoes-smoke` run adds fixed real-player screenshots at S0 and S3. No quest state is injected; progression still comes from real gathering/crafting/delivery entry points. These are automated screenshots, not human gameplay or artistic approval.

Blender JSON should reproduce byte-for-byte. Blender/FBX files can contain exporter metadata; assess their content, not timestamp-dependent byte equality. Unity scene IDs can change on every Setup; review semantic content as well as the generated diff.

## Whole-valley expansion

The Blender export now contains 164 mesh/material groups and 53,040 triangles across the caretaker, three distinct villager profiles, Clover, the mill shell, trees, plants and reusable props. NPCs share an anatomical base with role-specific robes/aprons/hats/hair/beard and tools; they are not capsule recolors. `WorldArt.cs` adds native rough stones, cottage shingles/framing/shelves, stream-bank detail, a distant tree line and seven item icons. `InventoryPanel` selects each icon by stable item id, so consuming a stack cannot leave another item's icon on that row.

The full valley replaces the former tree/flower prefabs and resource visuals while preserving their gameplay parents, positions and collision. The hen keeps Body/Egg/HungryIcon/ProducingIcon and the existing 15-second state machine. All source/export assets are original and AI-assisted. Profiles, saves, recipes, audio and controls retain the existing authority.

Geometry checks compare triangle winding with native Unity meshes. This caught an early double reversal during coordinate export. A fresh export also reproduces the JSON SHA256 exactly; editable Blender/FBX metadata itself is not claimed byte deterministic. Never use a file's existence or a passing test as an artistic approval.
