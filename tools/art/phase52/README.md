# Phase 5.2 isolated weighted-character pilot

This is Gate B work, not an approved whole-valley rebuild. Owner review is mandatory before C, even if engineering tests pass. See `docs/07-PHASE52-ART-DIRECTION.md`.

```powershell
& 'D:\Blender\blender-4.5.15-windows-x64\blender.exe' --background --factory-startup --python tools/art/phase52/build_hero.py
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Invoke-Pilot.ps1 -Task Generate -LogDirectory artifacts/visual-qa/phase-5.2/logs/new-generation
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Test-PilotCopy.ps1 -EvidenceName new-verification
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Invoke-Pilot.ps1 -Task Build -BuildDirectory artifacts/build/phase52-pilot-new -LogDirectory artifacts/visual-qa/phase-5.2/logs/new-build
```

The hero has 25 anatomical/secondary bones, explicitly authored normalized blend weights, 18,864 triangles, four Blender actions (Idle, Walk, TurnLeft/Right), and one native SkinnedMeshRenderer with material submeshes. Weights are authored by code, not painted by a human. Editable `.blend` retains actions via fake users; FBX is mesh/armature interchange without baked actions. JSON contains the explicit vertices/weights/rest transforms and sampled action tracks; Unity writes the Mesh, AnimationClips, prefabs and scenes. No hand-written Unity serialization or new package is involved.

The motion adapter samples authored legacy AnimationClips from actual player displacement and turn rate. A 2.4m stride follows the unchanged 4.5m/s controller, so full-speed locomotion reads as a brisk jog rather than a slow stroll. This adapter never changes the player input/controller or camera. The original rigid visual and its CharacterAnimator reference remain in the isolated scene for comparison, while the new visible character is weighted. The existing footstep system is distance-based and unchanged.

`Phase52PilotBuilder.Generate` copies the existing scenes using Unity, replaces the pilot's visible model, and adds a bounded garden/workshop/north-path environment. Default MainMenu/Valley assets, Hero/NPC prefabs, build scene list, controller, save/gameplay and audio are preserved. Pilot scene basenames still match existing SceneNames, but the explicit pilot build contains only the two separate scene paths. Pilot saves use their own subfolder; full smoke runs retain their existing isolated-save authority.

The supplied original `01` PNG is copied unmodified and shown as **Caretaker information beside the satchel**, never as Mira's speaking portrait. Its semitransparent vignette and simplified 3D counterpart require human edge/identity approval. Other concepts and the desktop baseline captures remain research only. Credits and docs disclose actual use.

For actual movement evidence:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/art/phase52/Capture-PilotMotion.ps1 -PlayerDirectory artifacts/build/phase52-pilot-new -OutputDirectory artifacts/visual-qa/phase-5.2/motion/new720 -Video
```

The optional `-Video` records consecutive real-player framebuffer images via ScreenCapture at WaitForEndOfFrame, preserving monotonic timestamps. Installed FFmpeg encodes those frames at their actual variable intervals; it captures no desktop or audio. The player walks for 36 seconds using the original PlayerController.InputOverride and camera.ApplyLook APIs, never teleporting during the route. Position/phase/frame-time telemetry and source-frame hashes are written separately. Teleports only place initial and post-route diagnostic shots. This is automated input, not human keyboard/mouse evidence. Use `-Baseline` to show the original visual/terrain/trees for matched before images. Use fresh destinations; no previous build or recording is deleted.

Blender/Editor previews are model diagnostics, never Windows-player evidence. The old native exit fault remains historically unresolved. Three-minute performance/human listening/full-world release acceptance belongs to the later owner-gated phases, not this pilot.
