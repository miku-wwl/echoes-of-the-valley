# 05 — Release candidate, submission copy and human QA

**Status (2026-10-26): automated release preparation only.** Nothing here was played, listened to or judged by a person, and **nothing has been uploaded or submitted to itch.io.** The developer decides when and what to submit. `MANUAL_GAMEPLAY_QA_STATUS=PENDING`, `ACTUAL_PLAYER_DURATION_STATUS=NOT_VERIFIED`, `SUBMISSION_STATUS=NOT_SUBMITTED`.

| Field | Value |
| --- | --- |
| Package | `artifacts/release/EchoesOfTheValley-Windows-x64-RC1.zip` (git-ignored; rebuild with `tools\Invoke-Echoes.ps1 -Task Release`) |
| Size / SHA-256 | 64,679,296 bytes (61.7 MB) / `EB9BFC0D9B0BA60A0E9E8367ADF3BA4EB4D4F3EFE8F0F6FCD58A2AB4FDA3112A` |
| Contents | 181 files, 137.7 MB unpacked: the player, `EchoesOfTheValley_Data`, `MonoBleedingEdge`, `README.txt` |
| Build | Windows x64, Mono, D3D11, **non-development** (no watermark, no profiler, no script debugging), Unity 6000.3.27f1, build id `008df7e74ad8443faa7db6a90d2f4776`, version `1.0.0-rc1`, 0 errors, 0 warnings, 144,398,106 bytes |
| Source | built from git `04c7a04` plus the Phase 5 changes committed right after it (`build: prepare Windows release candidate`); the working tree was not clean when it was built, which `release-metadata.json` records as `gitWorkingTreeDirty` |
| Machine | AMD Radeon 780M / Ryzen 7 8845HS / Windows 11 (10.0.26100), the target hardware |

## What was verified by machine (`-Task VerifyRelease`, results in `artifacts/release/release-metadata.json`)

| Check | Observed |
| --- | --- |
| Zip integrity | all 181 entries open and read to their full size; every entry name uses forward slashes and sits under one top folder (`EchoesOfTheValley-Windows-x64-RC1/`) |
| Extraction | extracted to a temporary folder: 181 files, 0 hash mismatches against the staged copy |
| Required files | the exe, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `README.txt`, `globalgamemanagers`, `level0`, `level1`, `Echoes.Runtime.dll` and the Mono runtime are present; no `.pdb`, Burst debug folder or log file ships |
| Local paths and names | every file scanned (ASCII and UTF-16) for `D:\workshop`, `D:/workshop`, `weila`, `mingyue`, `echoes-of-the-valley` and `Unity\Downloads`: **0 hits**. A first scan found the build machine's PDB path inside `Echoes.Runtime.dll`; the package now keeps only the bare file name there (our own assembly only: Unity's and Microsoft's code-signed binaries are untouched) |
| Cold start | the extracted exe launched with no arguments: still running after 12 s, "MainMenu ready" logged, 0 error lines |
| Full-game smoke on the **extracted** player | 1 attempt(s) (up to 3 allowed); the last: exit code 0, natural exit, `PASS` in 65 s with 20 screenshots, 0 exception lines, and the player reported `build: release player` (not a Development Build). It reached the ending, and its save, Continue, Continue Exploring and New Game-question steps passed. Process exit faults after a logged PASS: 0 |
| Network | 0 remote connections seen (`netstat` sampled every 5 s while the player ran); the game has no network code |
| Frame time in the extracted release player (scripted, vSync off, windowed) | restored valley with every effect and sound: 1280x720: 4.1 ms average (243 FPS), worst frame 16.8 ms; 1920x1080: 4.5 ms average (223 FPS), worst frame 16.7 ms. During the restorations at 1280x720: 3.0 ms, 2.6 ms, 3.8 ms, 2.8 ms average |

**Not verified:** a play by a person (mouse feel, real clicks, Alt-Tab, whether the visuals and music are *good*, the length of a real play); that the sound is audible (the run reports that the director started 10 sources and the audio device was 48 kHz stereo, nobody listened); start-up with the network disconnected (the player has no network code and made no remote connection while it ran, but it was never run offline on purpose); other machines and graphics cards; a second Windows installation.

**Known issues**

1. **Intermittent exit fault.** In one of the three Phase 4 development-build smoke runs the process exited with `0xC000041D` *after* the run had logged PASS: Unity's window teardown (`UiaDisconnectAllProviders` pumping messages) dispatched a message into its own window procedure and read a null pointer inside `UnityPlayer.dll`. It did not repeat in the other runs and is not in game code, but a player quitting could in principle meet it (the crash handler would write a dump to the temp folder and the window would close). The verification repeats the scripted run up to three times, records every attempt, and counts such exits as 0 in this run.
2. **Colour.** The restored valley is a strong yellow-green with a lot of bloom in the stills the assistant read; a human may want it toned down (it is two numbers in `ValleyLighting` and the golden filter).
3. **Scripted frame times** (above) were measured with the player standing near the spawn; they are not a promise for every view. One-off hitches of 50-100 ms appear in the first seconds of an effect.
4. The Unity **splash screen** is left on (default). The font is Unity's built-in `LegacyRuntime`; inspecting the build data shows it holds no font file (it names the operating system's Arial), so no font ships in the package, but that is the assistant's reading of the binary, not documentation.

5. **Generator hygiene.** `SceneKit.CreateVolume` leaves orphaned `VolumeComponent` sub-objects in the two post-processing profile assets (the committed files hold 28 objects where 4 are needed, and every Setup run or generator test adds 3 more). Nothing references them, so nothing changes at run time and only the asset files carry the dead weight; the fix (rewrite the profile asset from scratch) was left out so the verified release candidate was not regenerated. After running Setup or the EditMode tests, `git checkout` the generated scenes, prefabs and `Art/Settings` assets unless you mean to change them (the scenes also get renumbered internal IDs).

## Decisions only the developer can make (open items)

1. **Creator display name (open, required).** The credits (title screen, ending card, `README.txt`) say *"Created by the developer of this entry"* because no public name has been confirmed. Set `GameCredits.CreatorDisplayName` in `game/Assets/Echoes/Scripts/Runtime/Core/GameCredits.cs`, change the line in `tools/release-readme.txt`, then run `-Task Release` again, and use the same name on the itch.io page.
2. **Play it** with the checklist below, with a stopwatch, and listen with headphones and with laptop speakers. Only a person can say whether the music is pleasant and balanced, the visuals are attractive and the game is fun.
3. **Official theme track.** The game ships its **own** synthesised music and does not use the official recording. If you want it included it must be the unmodified file, only inside the jam entry, never in the public repository. Nothing in this package needs it.
4. **Engine and font notices.** Confirm that the Unity Personal terms and the built-in font behaviour above are acceptable for the entry ([03](03-RULES-CREDITS.md)).
5. **AI-disclosure wording** on the itch.io page (below) must match what you decide to ship.
6. **Pushing.** Nothing has been pushed; the Phase 3, 4 and 5 commits are local.

## Human QA checklist (Windows, 1280x720 and 1920x1080)

Tick each item yourself; none is verified.

- [ ] Unzip to a folder with no admin rights and run `EchoesOfTheValley.exe`: starts without errors, no "Development Build" watermark.
- [ ] Title screen: the welcome music plays, the volume sliders work, **Credits** opens and Esc/Close returns, **Quit** closes the game.
- [ ] **New Game**: WASD/arrows move, the mouse turns the camera, the cursor is hidden in play, **Esc** pauses, **Resume** works, **Alt-Tab** away and back (cursor and look recover).
- [ ] Follow the objective panel through all **three quests** by walking (no shortcuts): Mira, the ruined spring, the Heart; gather herbs, blossoms and ore with **E**; open the satchel with **I**; craft at the workshop with the buttons and with **1-3**; feed Clover; collect the egg; give it to Wren; bring braces to Alder.
- [ ] Each restoration is **memorable**: you can see and hear the before and after (stream, garden, windmill); the effects feel good rather than loud or confusing.
- [ ] **Audio:** S0 is sparse and quiet, S1 adds water and harp, S2 birds, marimba and chimes, S3 the melody and sails; the ending music is a gentle resolution; no clicks, no harshness, no clipping; the music and sound sliders behave; footsteps are not annoying.
- [ ] Read all dialogue and objective text: readable, no overflow, no overlap (the banner never covers the ending title).
- [ ] **Ending**: Mira's closing talk opens the ending card; **Continue Exploring** (button, Enter, E, Esc) and **Main Menu** (button, M) work; reloading never replays the ending.
- [ ] **Save / Continue**: save from the pause menu, quit, relaunch, **Continue** restores the stage, items, coins and the hen's timer; **New Game** asks before replacing a save.
- [ ] Wait at the hen: Clover lays an egg after about 15 seconds of game time.
- [ ] Look for pink or missing materials, flicker, clipping, floating objects and anything slow.
- [ ] **Time a full play** from New Game to the ending (target 10-15 minutes, 20 at most) and note it here.
- [ ] Performance on the Radeon 780M at 1280x720 (goal 60 FPS) and 1920x1080 (goal 30 FPS), compared with the scripted measurements above.

## Submission copy (draft; edit before use)

**Title:** Echoes of the Valley

**Short description (one line):** A short, peaceful adventure: gather, tend a hen, brew potions and listen as a silent valley finds its voice again.

**Full description (English):**

> **Echoes of the Valley** is a short, gentle adventure about bringing a silent valley back to life. You are the new caretaker of a quiet village whose stream has gone dry, whose garden has withered and whose windmill has stopped turning. Gather herbs, blossoms and ore, look after Clover the hen, mix potions at the alchemy workshop, and help three villagers (Mira, Wren and Alder) restore the stream, the garden and the windmill.
>
> Every restoration changes the world you can see and hear: **Silence → Awakening → Resonance → Rest.** Colour, light, water, plants, birds and a quietly growing piece of music answer your care, until the valley hums with you inside the song. A complete play takes roughly 10 to 15 minutes. There is no combat and no timer.
>
> **Controls:** WASD / arrow keys move, mouse looks, **E** talks, gathers and uses, **I** opens the satchel, **1-3** craft at the workshop, **Esc** pauses. Music and sound volume sliders are on the title screen and in the pause menu. The game saves automatically at every story step.
>
> **Install:** download the zip, unzip it anywhere and run `EchoesOfTheValley.exe` (Windows 10/11, 64-bit, DirectX 11). Nothing to install, no internet connection needed.
>
> **AI disclosure:** this game was made with AI assistance. Anthropic Claude Code (Claude Sonnet 5.5), directed by the developer, wrote all of the game's code, its dialogue, quest and story text, its 3D art (simple shapes assembled by code, with procedural textures) and all of its music and sound effects (original audio synthesised by code; nothing is sampled or taken from a library). OpenAI Codex helped with early planning and documentation. No external art, music, sound or font files were used; the interface uses the font and sprites built into Unity. The official theme recording is not used.
>
> **Credits:** created by **[creator name: to be confirmed by the developer]** for thatgamejam #02. Built with Unity 6.

**Tags (suggestion):** cozy, relaxing, atmospheric, 3d, singleplayer, short, exploration, crafting, fantasy.

**Screenshots to attach:** choose after looking at them yourself from `artifacts/release/submission-package/screenshots/` (captured by the scripted run of the release player: the title screen, the silent valley, the restored stream, the blooming garden, the golden-hour windmill, the ending card).

## Trailer plan (video optional; none has been recorded)

About 45 seconds, 1080p, no voice, the game's own music:

1. 0-6 s: title card over the dusk title screen (fireflies, the cottage).
2. 6-14 s: the silent valley (muted colour, sparse bells and wind), the caretaker walks to the Heart.
3. 14-22 s: gathering a herb, brewing at the cauldron, feeding Clover, the egg.
4. 22-32 s: the three restorations cut together: the stream running clear, the garden blooming, the windmill's sails starting to turn, each with its chime.
5. 32-42 s: the golden hour, the ending card; the final bell arpeggio rings out.
6. 42-45 s: "Echoes of the Valley · thatgamejam #02" and the AI-disclosure line.

**Capture:** run the player at 1920x1080 windowed, record with any screen recorder, and play it through once by hand (the scripted run teleports and is only a test). Use the ending card's drifting camera for the final shot.

## Rebuilding and verifying

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Invoke-Echoes.ps1 -Task Release
```

`BuildRelease` makes the non-development build; `Package` stages it (without the Burst debug folder or any `.pdb`), replaces the build machine's PDB path inside `Echoes.Runtime.dll` with its bare file name, adds `README.txt` (`tools/release-readme.txt`) and zips it with a SHA-256; `VerifyRelease` reads every zip entry, extracts to a temporary folder, compares every file, checks the required files, scans every file for local paths and user names, starts the extracted player cold, and runs the full-game smoke test on it with `-echoes-expect-release` (up to three attempts, all recorded). The result is written to `artifacts/release/release-metadata.json`. `-Task Smoke` runs the same scripted game on the development build.
