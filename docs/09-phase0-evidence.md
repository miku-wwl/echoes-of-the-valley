# 09 — Phase 0 evidence and environment preflight

Audit date: **2026-10-23 NZDT**. First live environment timestamp: **2026-10-23T15:59:39+13:00**. Project root: `D:\workshop\oct\echoes-of-the-valley`. **Implementation NOT_STARTED; jam gate CLOSED.**

## Initial repository audit

- Existing branch `main`, tracking `origin/main`; initial working tree clean.
- Initial local and live remote main: `bc9e7c5190df7df8343ae6f61f55f6f78968d2c8` (`Initial commit`).
- Existing tracked files: `README.md` (125 bytes) and `LICENSE` (1086 bytes). No GDD or game project. README concept and existing MIT copyright/text retained, with scope clarified.
- Existing remote preserved: `git@github.com:miku-wwl/echoes-of-the-valley.git`. Live `git ls-remote origin refs/heads/main` succeeded using noninteractive SSH.
- No applicable AGENTS.md was found in workspace or its checked ancestors. No other repository was changed.
- User's Phase 0 brief explicitly authorizes a documentation/configuration commit and push when remote/authentication work. It does not authorize Phase 1.

## Environment evidence

**DETECTED**: registry/path/component evidence only. **VERIFIED**: a command/query succeeded for the stated limited capability. **MISSING**: not detected in the specified bounded search; not proof of absence from every possible portable/custom directory. **NOT_CHECKED**: not exercised. No game/runtime tests are implied by software detection.

| Component | Status | Actual observation and limit |
| --- | --- | --- |
| OS/architecture | VERIFIED | CIM: Windows 11 Home Chinese, 10.0.26200, build 26200, 64-bit |
| CPU | VERIFIED | CIM: AMD Ryzen 7 8845HS with Radeon 780M, 8 cores / 16 logical processors |
| RAM | VERIFIED | CIM usable physical total 31.31 GiB |
| GPU | DETECTED | AMD Radeon 780M; driver 31.0.22042.4001. Honor virtual display also listed; no graphics benchmark. CIM AdapterRAM 512 MiB does not represent all iGPU shared memory |
| Git | VERIFIED | `git version 2.49.0.windows.1`, `D:\Git\cmd\git.exe` |
| Git LFS | VERIFIED | `git-lfs/3.6.1`, windows amd64. Initial `git lfs ls-files` empty; final config checks below |
| GitHub CLI | VERIFIED | `gh 2.92.0`; `gh auth status` reports active miku-wwl account using OS keyring; token line redacted; no credential copied |
| Remote reachability | VERIFIED | Live SSH main ref query succeeded; this proves read access, not push until final delivery verification |
| Unity Hub | MISSING | No PATH/registered install/common-path/shortcut match in bounded searches |
| Unity Editor 6.3 LTS | MISSING | No detected editor; exact installed patch **NOT AVAILABLE** |
| Windows Build Support | MISSING | No matching editor/support installation detected. Mono/IL2CPP player launch/build **NOT_CHECKED** |
| Unity license activation | NOT_CHECKED | No editor/Hub launch or license/terms acceptance performed |
| Blender | MISSING | No PATH/registered install/common-path/shortcut match |
| VS Code | VERIFIED | `D:\Microsoft VS Code\bin\code.cmd --version`: 1.142.0, x64. Registry advertises 1.143.0; command result takes precedence. CLI emitted crashpad registration warning; full GUI/C# integration NOT_CHECKED |
| VS Build Tools | DETECTED | vswhere: VS 2022 Build Tools 17.14.37 / 17.14.37516.0; VC x64 tools component present; MSVC 14.44.35207 cl.exe exists. No compiler build run |
| Windows SDK | DETECTED | Include directory 10.0.26100.0 and x64 SDK registry components. No Unity IL2CPP build run |
| VS C# / Unity extension integration | NOT_CHECKED | No project created; debugger/intellisense/extension readback not exercised |
| Disk C: | VERIFIED | 300.00 GiB total; 22.17 GiB free at snapshot |
| Disk D: | VERIFIED | 629.66 GiB total; 85.03 GiB free at snapshot |
| Python, verification helper | VERIFIED | 3.13.9; not a game runtime dependency |

Bounded installation searches: commands on PATH; HKLM/HKCU uninstall keys (including WOW6432Node); common Unity/Hub/Blender roots under C:\Program Files and D:; matching immediate directories under D:, C:\Program Files and LocalAppData\Programs; Start Menu shortcuts; UnityHub AppData install/settings files if present. No full-drive executable crawl, software download, application install, or Unity project creation occurred.

## Official stack check and exact setup actions

Research confirms the intended 6000.3 LTS line and editor-bound core packages. [Unity 6.3 requirements](https://docs.unity.com/en-us/engine/6000.3/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) cover Windows x64 and recommend at least 8 GB RAM; actual project performance remains untested. IL2CPP needs VS 2019+ C++ tools and Windows SDK 10.0.19041.0+, in addition to Unity support. Existing native components meet the detected version floor, but end-to-end compatibility is NOT_CHECKED.

These are instructions only; **none of the installations below was performed**.

1. Download [Unity Hub from Unity](https://unity.com/download). Install Hub, set editor installation location to `D:\Unity\Hub\Editor`, and use a D:-backed download/cache location if supported. Keep C: headroom for installers and package caches; recheck both disks before downloading. No cleanup/delete is authorized here.
2. In Hub → Installs → Install Editor, choose **Unity 6.3 LTS (6000.3.x)** Windows x64, not an alpha/beta or a different update line. A concrete official archive candidate verified during research is [6000.3.25f1](https://unity.com/releases/editor/whats-new/6000.3.25f1); it is **not asserted to be the latest or installed**. Confirm current 6.3 patch/support/security notes before choosing, record it, and keep it stable through the jam.
3. Verify default Windows desktop Mono support; for optional IL2CPP select **Windows Build Support (IL2CPP)** for that exact editor. If adding later, Hub → Installs → editor Manage → Add modules. [Official Hub instructions](https://learn.unity.com/tutorial/explore-the-unity-hub?version=6.3). Do not install unrelated UWP/server/Android modules. Existing VS native components may be reused after validation.
4. Launch Hub/editor only for software/license verification without creating a project. Complete any account/license action personally as required; this preflight has not activated Unity. Record Hub version, editor exact version, and `Editor\Data\PlaybackEngines\windowsstandalonesupport` contents/module metadata. This supports detection; an actual Windows build in Phase 1 is still required.
5. Download an official supported [Blender LTS Windows x64 package](https://www.blender.org/download/lts/), preferably install/extract to D:. Record exact version and run `blender.exe --version`. Do not create/export a game asset. Confirm compatible rig tooling later; a hand-authored armature is the fallback.
6. Keep current VS Code; follow [official Unity + VS Code setup](https://code.visualstudio.com/docs/other/unity): install Microsoft's Unity extension (which brings C# dependencies). After the start gate, verify Unity's **Visual Studio Editor** package is at least 2.0.20 and select VS Code under Preferences → External Tools. The old Visual Studio Code Editor Unity package is deprecated. VS Installer can Modify Build Tools → Desktop development with C++ if required components are later missing; existing MSVC/SDK are already detected. No additional large install is justified solely by PATH lacking `cl`.
7. **Do not click New Project before the time AND theme gates.** After both, use Universal 3D and verify the packages listed in [architecture](03-technical-architecture.md). No package installation or URP import was executed in Phase 0.

The first failed legacy URP setup URL and redirected Hub manual were replaced by current official package/Unity Learn evidence. The Blender LTS download page returned 402 through the web tool; Unity's download page redirected to inaccessible OAuth onboarding. Their official links are supplied for manual setup, with download/installation NOT VERIFIED. That research verifies intended components, not a working editor configuration.

## Phase 0 verification record

Unity builds, domain/gameplay tests, Windows playtests, FPS measurements, theme listening, and submission are **NOT RUN** and must stay that way in Phase 0.

### Local verification results

| Check | Result | Evidence / precise scope |
| --- | --- | --- |
| Required documents | PASS | All ten numbered documents present, nonempty, with titles; README/LICENSE/configs present |
| Repository structure | PASS | Exactly 14 worktree files outside `.git`: ten docs, README, LICENSE, .gitignore, .gitattributes |
| Markdown relative links | PASS | Python parsed all Markdown inline links; 39 local targets resolved inside repository; zero broken links |
| Text validity | PASS | UTF-8 readable, no replacement characters, balanced code fences |
| Ignore rules | PASS | 14 non-writing `git check-ignore --no-index` probes: caches/builds/secrets ignored; future Assets/.meta/package locks/ProjectVersion kept |
| Attributes | PASS | Seven hypothetical filenames checked with `git check-attr`: PNG/FBX/BLEND/WAV use LFS and binary handling; Unity scene/prefab/meta remain text, no LFS filter |
| LFS setup | PASS | `git lfs install --local` succeeded; local clean/smudge/process/required configuration and pre-push hook read back; no assets/LFS objects tracked |
| Implementation boundary | PASS | Exact file allowlist excludes Unity folders, gameplay source, scenes, prefabs, binaries, images, audio, models and archives; no probe files created |
| Secret scan | PASS, bounded | All 14 text files scanned for private keys, GitHub tokens, AWS access keys, assigned secret values and credential-bearing URLs; zero findings. This is pattern scanning, not a mathematical guarantee |
| Claims review | PASS | Confirmed/Unverified/Assumption separated; prep permission not claimed; FPS/playtests/installations labelled unverified or not run; Quaternius held |
| Whitespace | PASS | `git diff --check` and `git diff --cached --check` exit 0; LF normalization warnings on original CRLF files were informational |
| Exact staged scope | PASS | 14 explicitly named paths staged; staged blob secret scan returned zero findings; all 39 relative links checked again against staged Markdown |
| Game tests | NOT RUN | No game or test implementation exists; no fabricated test count or build result |

Verification helper ran ephemerally via Python standard library over the worktree and Git subprocesses; no test code or tool dependency was added to the repository. Ignore/attribute probes used hypothetical paths and produced no assets. Required-content checks also confirmed competition evidence labels, exact local gate dates, all six planned phases, and blocked kickoff language.

Reproducible read-only command families used: `git status --porcelain=v1 --untracked-files=all`, `git ls-files`, `git diff --check`, `git check-ignore --no-index`, `git check-attr`, `git lfs ls-files`; `Get-CimInstance Win32_OperatingSystem/Win32_Processor/Win32_ComputerSystem/Win32_VideoController/Win32_LogicalDisk`; `Get-Command`; uninstall registry queries; `vswhere -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`; tool version commands; `gh auth status` with token-line redaction; noninteractive `git ls-remote`. Time conversion used `TimeZoneInfo` with New Zealand and Pacific Windows timezone IDs.

### Delivery record

Baseline commit: **`b2873eb8d965552e3ecfaecc6a421504c9f343bd`**, message `docs: establish Phase 0 preflight and repository foundation`. Commit and `git push origin main` both succeeded. At **2026-10-23T16:15:59+13:00**, `git rev-parse HEAD`, `git rev-parse origin/main`, and live `git ls-remote origin refs/heads/main` all returned that full SHA; `git status --short --branch` showed `main...origin/main` with no changed paths.

This delivery receipt is a subsequent documentation-only revision. Its own commit SHA is intentionally not embedded in itself; obtain the final receipt revision from `git log -1` and the final handoff report. The baseline above remains a verifiable ancestor. The existing origin URL and branch are preserved. Authentication remains in the OS keyring/SSH setup and is not stored in project files. No binary/LFS upload was required because no assets exist.

## Readiness decision

**PHASE0_STATUS=PARTIAL.** Preparation can be delivered, but missing Hub/editor/player support/Blender prevent a readiness PASS. Competition sources were accessible; explicit pre-jam-documentation permission, proprietary asset eligibility, and additional prize eligibility details remain unresolved. Quaternius license conflict is recorded and that candidate excluded; no uncertain asset has been imported.

**PHASE1_READINESS=BLOCKED** by time/theme gates and missing tools. Stop after Phase 0. Use the [kickoff](08-phase1-kickoff.md) only in a later authorized execution after its entry conditions are met.

```text
PHASE0_STATUS=PARTIAL
COMPETITION_RULES_STATUS=CONFIRMED_WITH_UNVERIFIED_ITEMS
JAM_START_GATE=CLOSED
GIT_REPOSITORY_STATUS=VERIFIED_MAIN
REMOTE_PUSH_STATUS=VERIFIED_BASELINE_PUSH
UNITY_HUB_STATUS=MISSING_BOUNDED_SEARCH
UNITY_EDITOR_STATUS=MISSING_EXACT_PATCH_UNAVAILABLE
WINDOWS_BUILD_SUPPORT_STATUS=MISSING_NO_EDITOR
BLENDER_STATUS=MISSING_BOUNDED_SEARCH
GIT_LFS_STATUS=VERIFIED_LOCAL_CONFIG_NO_ASSETS
DOCUMENTATION_STATUS=PASS
ART_DIRECTION_STATUS=PASS_PLAN_ONLY
TECHNICAL_ARCHITECTURE_STATUS=PASS_PLAN_ONLY
ASSET_LICENSE_STATUS=PASS_RESEARCH_NO_ASSETS_ADMITTED
AI_DISCLOSURE_PLAN_STATUS=PASS
PHASE1_READINESS=BLOCKED_TIME_THEME_AND_TOOLS
IMPLEMENTATION_STATUS=NOT_STARTED
```
