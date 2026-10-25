# Working agreement for coding agents

Project: **Echoes of the Valley**, `thatgamejam #02` (2026). Read `README.md` and `docs/01-GAME-DESIGN.md` first. Treat all stated environment PASS results as **historical, limited to the blank Unity template**.

## Scope and priorities

1. Meet the **2026-11-08 20:59 NZDT** jam deadline; keep an end-to-end Windows playthrough ahead of additional content.
2. The game is **Silence → Awakening → Resonance → Rest**. Implement the `ResonanceStage` world-feedback mechanic, not generic farming with cosmetic restoration.
3. Ship **one valley, three quests, three recipes, one chicken, three visible restoration stages, one ending**. No combat, fishing, seasons, large animal systems, cloud or multiplayer.
4. Favor repeatable **Unity Editor C# automation + batchmode** for scenes, settings, build and content. Unity's own serialization writes `.unity`/`.prefab`/`.meta`; never invent YAML. Record manual playtest evidence honestly.
5. Preserve this repo and its existing Unity project, if any. Check before creating `game/`; never overwrite/delete work or force-push. Do not push unless the user asks.

## Verification contract

- Unity Editor baseline: `D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe`, Windows x64 Mono/D3D11. Project-specific package/install/build checks remain required.
- Every phase reports `PASS`, `PARTIAL`, or `FAIL` with exact observed tests, logs and build ID. Stock template launch ≠ gameplay PASS. Unobserved GUI, debugger, FPS or sound tests = **NOT VERIFIED**.
- Test inventory capacity, repeat interaction/quest reward, save roundtrip, chicken timers and persistence of S0–S3. Prioritize no loss/dupe/softlock over polish.
- Keep build/caches out of Git. Never stage source files without rights or the unmodified official theme WAV; actual-use licenses and AI disclosure belong in `docs/03-RULES-CREDITS.md`.

## First work after unpacking

Use `tools/Init-Unity.ps1` once **if no actual Unity project exists**. Then generate Boot/Valley, create movement/camera/interact/menu, build a real Windows player, and request/perform real interaction tests. Report P1 status before progressing.
