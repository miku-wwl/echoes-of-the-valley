# 09 — Phase 0 evidence and environment preflight

Audit date: **2026-10-23 NZDT**. First live environment timestamp: **2026-10-23T15:59:39+13:00**. Project root: `D:\workshop\oct\echoes-of-the-valley`. **Implementation NOT_STARTED; jam gate CLOSED.**

**Historical snapshot notice:** the Phase 0 sections below retain their original results. The appended **Phase 0.5 — development environment recovery** section is the current environment record; it supersedes the old missing-Blender and unchecked-VS-Code-extension observations. No Unity implementation is authorized by either phase.

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

## Phase 0.5 — development environment recovery

Recovery date: **2026-10-23 NZDT**. Start repository commit `2700f459dfd53897879a9b3c6f4df6e82afd204a`; branch main and initial working tree clean. The developer reports external Phase 0 review and organizer permission for pre-jam preparation. The private permission text was not provided or interpreted as permission for early core content. **PREP_GATE_STATUS=OPEN_ENVIRONMENT_ONLY; CORE_CONTENT_GATE=CLOSED.** No game project, gameplay code, level, character, or game-ready asset was created.

### Reinspection and version selection

Repeated PATH, Windows uninstall registry, common installation roots, Start Menu/app inspection, disk, and executable checks. No Unity Hub, Unity Editor, or Blender installation was detected at the start of recovery. VS Code 1.142.0 and Microsoft C#/C# Dev Kit were already present; the Unity extension was missing. .NET SDKs 9.0.304 and 10.0.300 were detected. No unrelated software was removed or replaced.

Selected **Unity 6000.3.27f1**, the released LTS patch returned by the official release service, published **2026-10-20**, revision **672d562a42a7**. [Official release notes](https://unity.com/releases/editor/whats-new/6000.3.27f1) and [official release API query](https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3&limit=10&order=RELEASE_DATE_DESC) were read. This is a stable release-channel selection, not a claim that it has no bugs. Relevant known issues include repeated asynchronous scene switching, high-polling-rate mouse Editor framerate, and simultaneous Bee backends; avoid parallel builds and retest scene transitions during the later implementation phase. The patch includes an FBX SDK security update.

Selected **Blender 4.5.15 LTS**, released 2026-10-22; the 4.5 line is supported until July 2027. [Official 4.5 LTS release page](https://www.blender.org/releases/4-5/) and [official download directory](https://download.blender.org/release/Blender4.5/) were checked. An initial 4.5.14 archive was downloaded before the newer patch's security/correctness fixes were reviewed; it was not installed, and the retained archive was not deleted. Only 4.5.15 was extracted and run.

### Download and integrity evidence

All downloads, checksums, metadata and local logs are outside Git under **`D:\Unity\Phase05Recovery\20261023`**. Nothing from that directory is staged or committed. Full file checks:

| Package | Actual bytes | Integrity / signer | Result |
| --- | --- | --- | --- |
| UnityHubSetup-x64.exe, product version 3.22.4 | 193385952 | Authenticode **Valid**, Unity Technologies SF | Download verified; not installed |
| UnitySetup64-6000.3.27f1.exe | 4235893440 | Official API MD5 `7B9412B4F0970814DAF1AB6469944836`; Authenticode **Valid**, Unity Technologies SF | Download verified; not installed |
| blender-4.5.15-windows-x64.zip | 398649826 | SHA-256 matches official `.sha256` manifest; extracted blender.exe signature **Valid**, Blender Foundation | Installed and background runtime verified |

```text
Hub SHA256     D61D152C271211962AF1904B57BE3479FC03251F8C36D6561316A881A6CEC69E
Editor SHA256  15236D7DBE692EE683CC06E31D5F708AB15439A5C5E8CD5FF5EDD82A24434055
Blender SHA256 52DD02FD74877FA08E74B67D7854CF32468A232C02AAA19B6844C91285574F8D
```

Unity metadata integrity `md5-e5QStPCXCBTa8atkaZRINg==` decodes to the MD5 printed above. MD5 here is an upstream transfer-integrity check, supplemented by valid Authenticode and a locally recorded SHA-256. The initial legacy Hub filename `UnityHubSetup.exe` returned 404; the architecture-specific official CDN filename `UnityHubSetup-x64.exe` succeeded. Blender/Unity public downloads worked through HTTPS requests despite earlier web-reader retrieval failures; no authentication or safety barrier was bypassed.

### Blender installation and actual runtime evidence

Portable installation: **`D:\Blender\blender-4.5.15-windows-x64\blender.exe`**. The destination did not exist before extraction. Created a new Start Menu shortcut `Blender 4.5 LTS.lnk` pointing to that executable; did not overwrite an existing shortcut or change system PATH.

Commands actually run:

```powershell
& 'D:\Blender\blender-4.5.15-windows-x64\blender.exe' --version
& 'D:\Blender\blender-4.5.15-windows-x64\blender.exe' --background --factory-startup --python-expr 'import bpy; bpy.ops.wm.read_factory_settings(use_empty=True); print("BLENDER_RUNTIME_SMOKE=PASS"); print("VERSION="+bpy.app.version_string); print("SCENE_OBJECTS="+str(len(bpy.data.objects)))'
```

Actual output, **exit code 0**:

```text
Blender 4.5.15 LTS
build date: 2026-10-22
build hash: 5afeabba6097
build branch: blender-v4.5-release
build platform: Windows

BLENDER_RUNTIME_SMOKE=PASS
VERSION=4.5.15 LTS
SCENE_OBJECTS=0
Blender 4.5.15 LTS (hash 5afeabba6097 built 2026-10-22 01:35:27)
Blender quit
```

This checks executable/runtime startup, Python/bpy initialization and clean shutdown. No file was saved, rendered or exported. **GUI viewport/graphics operation NOT VERIFIED** because native desktop control was unavailable; no claim of visually observing Blender is made.

### VS Code configuration and limitations

Ran `code --install-extension visualstudiotoolsforunity.vstuc`, which exited 0 and reported successful installation. Subsequent installed-extension readback:

```text
visualstudiotoolsforunity.vstuc@1.3.2
ms-dotnettools.csdevkit@3.40.215
ms-dotnettools.csharp@2.160.4
ms-dotnettools.vscode-dotnet-runtime@3.2.0
9.0.304 [C:\Program Files\dotnet\sdk]
10.0.300 [C:\Program Files\dotnet\sdk]
```

Unity extension manifest was read from `C:\Users\weila\.vscode\extensions\visualstudiotoolsforunity.vstuc-1.3.2`: publisher `visualstudiotoolsforunity`, VS Code requirement `^1.76.0`, dependencies C# Dev Kit and .NET runtime extension. No unrelated extension was changed. CLI emitted an existing crashpad registration warning; installation and readback still exited successfully.

**Verified:** extension installed and dependencies present. **NOT VERIFIED:** Unity project integration, IntelliSense, attach debugging, and Unity's External Script Editor preference. Those need a real Editor/project session; installed extension paths alone do not establish that workflow. [Microsoft's official setup](https://code.visualstudio.com/docs/other/unity) requires Unity's **Visual Studio Editor** package 2.0.20 or newer and VS Code as external editor. Do not use the deprecated Visual Studio Code Editor Unity package. No package manifest or game project was created to claim this check.

### D: cache and capacity configuration

Created the previously absent user file **`C:\Users\weila\.upmconfig.toml`**, with these values; parsed it using Python `tomllib` and checked the D: directory exists:

```toml
cacheRoot = "D:\\Unity\\Cache\\upm"
maxCacheSize = 5000000000
```

Readback: `UPM_CONFIG_PARSE=PASS`, cacheRoot `D:\Unity\Cache\upm`, maxCacheSize `5000000000`. No existing package cache or user config was moved, deleted or overwritten. No global TEMP/TMP or security settings were changed. [Unity 6.3 cache configuration](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/package-manager-caches/upm-config-cache), [user config location](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/upm-config/files). Actual UPM consumption remains NOT VERIFIED until Editor runs.

At **2026-10-23T16:47:20+13:00**, C: **22.23 GiB free**, D: **79.27 GiB free**. Start of recovery: C: 22.00 GiB, D: 85.02 GiB; small background fluctuations are not attributed to this task. Editor installation metadata estimates another **8464143666 bytes (7.88 GiB)** on D:, leaving roughly **71 GiB** before further imports/caches/builds. Reserve approximately 20 GiB for project Library/imports, 10 GiB for build/temp work, and the configured ≤5 GB UPM cache. These are planning allowances, not measured project sizes. Recheck free space before installation and after first imports; pause optional downloads if either drive falls below its working reserve. Suggested operating reserves: C: ≥15 GiB, D: ≥30 GiB. Do not delete existing files/apps to meet them without approval.

Large application, archives, editor destination, download destination and future repository-local Library/builds are on D:. VS Code's existing extensions and SDKs remain in their current locations to preserve the user's setup. Hub download/editor-path preferences still need to be set in Hub; creating D: destination directories does not verify those preferences.

### Interactive Unity handoff — current blocker

The environment is not elevated. A command to start the signed Hub installer was rejected by automatic approval review with **`blocked by policy`**; no more specific reason was returned, and no bypass was attempted. Download/integrity checks succeeded separately. Native Computer Use initialization succeeded, but app inspection returned **`Computer Use native pipe is unavailable ... os error 2`**, so there is no supported desktop window control/readback in this session.

The user was asked to install/login/activate in Hub and separately whether an external empty verification project is permitted. Until answered, no project is created and no authentication, EULA acceptance, UAC approval, or license activation is automated. The private organizer permission is not used to infer authorization for an environment-test project.

Concrete handoff instructions:

1. Manually run **`D:\Unity\Phase05Recovery\20261023\UnityHubSetup-x64.exe`**. Review terms yourself, handle UAC as required, and select **`D:\Unity\Hub`** as the install location. Installer version/signature was verified above; installation has not been observed.
2. Start Hub, sign in personally, and use the license appropriate to your eligibility. If Personal applies, use Settings → Licenses → Add license → Get a free personal license and personally review/accept its terms if needed. Do not share passwords, license files or keys in chat. [Official license management](https://docs.unity.com/en-us/hub/manage-license)
3. Set Hub Editor installation destination to **`D:\Unity\Hub\Editor`** and download destination to **`D:\Unity\Downloads`** where the installed Hub supports it. Do not create a game project.
4. Either install **6000.3.27f1** in Hub, or run the already verified **`D:\Unity\Phase05Recovery\20261023\UnitySetup64-6000.3.27f1.exe`** and select **`D:\Unity\Hub\Editor\6000.3.27f1`**, reviewing its terms/UAC yourself. Do not install a duplicate copy. If using the standalone installer, add **`D:\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe`** using Hub's Locate/add-existing-editor action afterward.
5. On Windows the desktop Mono player support is bundled with the Windows Editor: the exact official Windows module list offers `windows-il2cpp`, UWP and Windows Server as additional choices, but no separate Windows Mono add-on. **Do not install unrelated platform modules.** After installation, inspect `Editor\Data\PlaybackEngines\windowsstandalonesupport`, its Win64 Mono player files and `UnityEditor.WindowsStandalone.Extensions.dll`; then verify actual availability through Editor before calling support operational. IL2CPP is optional and is not needed for the initial Mono backend.
6. Verify Hub lists the exact patch/path, Licenses shows a valid active license, `Unity.exe -version` returns 6000.3.27f1, and launch logs show successful licensing/Editor startup. No such Editor or license success is currently claimed. If an empty test project is explicitly authorized, perform only external environment/startup/Mono checks and keep all results outside the competition repository; otherwise record the remaining integration/build checks as NOT RUN.

### Phase 0.5 acceptance assessment

| Requirement | Result | Evidence or blocker |
| --- | --- | --- |
| Reinspect existing installations | PASS | Repeated live command/registry/path checks |
| Select stable 6.3 LTS patch | PASS | Official LTS metadata and release notes, 6000.3.27f1 |
| Hub install/detection | PARTIAL | Valid 3.22.4 installer staged on D:; user installation/readback pending |
| Editor install/launch | PARTIAL | Valid exact-patch installer staged on D:; not installed or launched |
| Windows x64 Mono support | NOT VERIFIED | Bundled support researched; installed files/Editor capability/build not exercised |
| License readiness | BLOCKED | User login/eligible license activation required; no credential collection |
| Supported Blender LTS | PASS for background runtime | 4.5.15 LTS, official checksum/signature, real runtime exit 0; GUI not observed |
| VS Code Unity/C# | PARTIAL | Unity extension and dependencies verified; Editor integration not tested |
| D: storage/cache preparation | PASS configuration | Large files/application on D:; TOML parsed; actual UPM/Hub use still unverified |
| Design reconciliation | PASS | COMPETITION_MVP and BROADER_PLANNED_SCOPE separated; original GDD still unavailable |
| No core-content work | PASS | No Unity project/gameplay/assets or runtime dependency added to repository |

Raw local evidence files: `hub-installer-verification.json`, `unity-installer-verification.json`, `unity-release-6000.3.27f1.json`, `blender-archive-verification.json`, `blender-version.txt`, `blender-runtime-smoke.txt`, `vscode-unity-extensions.txt`, `vscode-unity-extension-manifest.json`, `dotnet-sdks.txt`, `disk-after-downloads.json`. These remain local; sanitized command outputs above supply the committed evidence. Installer files, caches, logs, SDKs and user settings are excluded from the documentation-only Git delivery.

Final local Phase 0.5 documentation validation returned:

```text
REPOSITORY_FILES=14
CHANGED_DOCUMENTS=4
RELATIVE_LINKS=40
DOCUMENT_SCOPE=PASS
SECRET_SCAN=PASS
CORE_CONTENT_ABSENT=PASS
DESIGN_RECONCILIATION=PASS
UNITY_INSTALLER_METADATA_MATCH=PASS
BLENDER_LOG_CHECK=PASS
UPM_CONFIG=PASS
git diff --check: exit 0
```

Exact changed paths: README.md, docs/01-competition-compliance.md, docs/02-game-design-prep.md, docs/09-phase0-evidence.md. Validation parsed all local Markdown targets, checked the repository file allowlist and UTF-8/fences, scanned credential patterns, matched saved Unity bytes/MD5/signature results against official metadata, checked Blender's actual runtime log and archive verification, and parsed the real UPM TOML. These are environment/document checks, not game tests. Commit/push is performed only after staged-content checks; the final commit SHA and live remote comparison are supplied in the handoff rather than embedded self-referentially in this file.

**PHASE05_STATUS=PARTIAL.** Independent recovery work and an exact installation handoff are complete; Hub installation/detection, Editor installation/launch, licensing, Windows Mono availability and full VS Code Unity integration are not verified. Stop at Phase 0.5. Core-content development remains CLOSED even after software setup succeeds.

```text
PHASE05_STATUS=PARTIAL
UNITY_HUB_STATUS=SIGNED_INSTALLER_READY_INSTALLATION_PENDING
UNITY_EDITOR_VERSION=NOT_INSTALLED_TARGET_6000.3.27f1
UNITY_EDITOR_LAUNCH_STATUS=NOT_RUN
WINDOWS_BUILD_SUPPORT_STATUS=NOT_VERIFIED_MONO_BUNDLED_WITH_WINDOWS_EDITOR
UNITY_LICENSE_STATUS=USER_LOGIN_AND_ACTIVATION_REQUIRED
BLENDER_VERSION=4.5.15_LTS
BLENDER_LAUNCH_STATUS=PASS_BACKGROUND_RUNTIME_GUI_NOT_VERIFIED
VSCODE_UNITY_STATUS=EXTENSIONS_VERIFIED_EDITOR_INTEGRATION_NOT_RUN
C_DRIVE_FREE=22.23_GiB
D_DRIVE_FREE=79.27_GiB
PREP_GATE_STATUS=OPEN_ENVIRONMENT_ONLY_USER_REPORTED_PERMISSION
CORE_CONTENT_GATE=CLOSED
BLOCKERS=UNITY_INSTALLATION_LOGIN_LICENSE_AND_EDITOR_WORKFLOW_VERIFICATION
```
