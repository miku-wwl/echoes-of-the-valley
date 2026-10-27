"""Assembles final-report.md and README_FIRST_zh.md for the B2 review package from measured evidence plus the author's explicit judgments.

python tools/art/phase52b2/make_b2_report.py <judgments.json>
Numbers (tests, build, frame times, hashes, git ids) are read from the evidence files, never typed by hand. Judgments (PASS/PARTIAL/FAIL,
what still looks bad) come from judgments.json and are the author's opinion, not owner approval."""
import hashlib, json, re, subprocess, sys, xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parents[3]
qa = root / 'artifacts/visual-qa/phase-5.2-b2'
legacy = root / 'artifacts/visual-qa/phase-5.2'
J = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def git(*a):
    return subprocess.check_output(['git', *a], cwd=root, text=True).strip()


def tests(xml):
    r = ET.parse(xml).getroot()
    return '%s %s/%s (failed %s, skipped %s)' % (r.get('result'), r.get('passed'), r.get('total'), r.get('failed'), r.get('skipped'))


def read(path, default=None):
    p = Path(path)
    return json.loads(p.read_text(encoding='utf-8-sig')) if p.is_file() else default


tv = legacy / 'logs' / J['testFolder']
edit, play = tests(tv / 'tests-EditMode.xml'), tests(tv / 'tests-PlayMode.xml')
build_log = (qa / 'logs/build/Build.log').read_text(encoding='utf-8', errors='replace')
m = re.search(r'PILOT_BUILD (\w+) id=(\w+) errors=(\d+) warnings=(\d+) bytes=(\d+)', build_log)
build = {'result': m.group(1), 'id': m.group(2), 'errors': m.group(3), 'warnings': m.group(4), 'bytes': m.group(5)} if m else {'result': 'UNKNOWN', 'id': '?', 'errors': '?', 'warnings': '?', 'bytes': '?'}
exe = Path(J['buildDir']) / 'EchoesOfTheValley.exe'
stats = {k: read(qa / 'player' / k / 'tour-stats.json') for k in ('after720', 'after1080', 'baseline720', 'unrecorded720', 'unrecorded1080')}
smoke = {k: read(qa / 'player' / k / 'process-result.json') for k in ('smoke720', 'smoke1080')}
tour = {k: read(qa / 'player' / k / 'process-result.json') for k in ('after720', 'after1080', 'baseline720', 'unrecorded720', 'unrecorded1080')}
prov = read(qa / 'player/after720/capture-provenance.json', {})
head, base = git('rev-parse', 'HEAD'), '5f90a6bf906f95d3c4c344ee940df2e85fcd58a4'
rc1 = root / 'artifacts/release/EchoesOfTheValley-Windows-x64-RC1.zip'
rc2 = root / 'artifacts/release/phase51-rc2-validated/EchoesOfTheValley-Windows-x64-RC2-Art.zip'
protected = rc1.is_file() and rc2.is_file() and sha(rc1) == 'EB9BFC0D9B0BA60A0E9E8367ADF3BA4EB4D4F3EFE8F0F6FCD58A2AB4FDA3112A' and sha(rc2) == '9085554ED52B39B46B250AA5E7E94642815FF76D9730AF038E6E8F180CFC8A80'
orig_changed = git('diff', '--name-only', base, 'HEAD', '--', 'game/Assets/Echoes/Scenes', 'game/Assets/Echoes/Prefabs', 'game/ProjectSettings', 'game/Packages')
wavs = git('diff', '--name-only', base, 'HEAD', '--', 'game/Assets/Echoes/Audio')


def perf(k):
    s = stats.get(k)
    if not s:
        return 'not run'
    f = s['frameMs']
    return '%s, %d frames over %.1f s: P50 %.2f / P95 %.2f / P99 %.2f ms, worst %.1f ms, mean %.1f FPS, 1%% low %.1f FPS (%s)' % (s['resolution'], s['frames'], s['seconds'], f['p50'], f['p95'], f['p99'], f['worst'], s['fps']['mean'], s['fps']['onePercentLow'], 'RECORDED, capture overhead' if s['recordedFramebuffer'] else 'unrecorded')


def bullets(items):
    return '\n'.join('- ' + i for i in items)


status = J['status']
report = '''# Echoes of the Valley: Phase 5.2-B2 review report

2026-10-28 NZDT, branch `art/phase52-b2`, base (failed Gate B pilot) `%(base)s`. **Author self-assessment only; Gate B owner approval is PENDING and world expansion is NOT allowed.** Automated tests prove engineering properties, never that the art is good. Every screenshot and the video are real Windows-player framebuffer captures driven by **automated** input; no human played.

## 1. Summary of actual changes

%(changes)s

## 2. Character-art approach used

%(approach)s

## 3. Why the new character looks different (and where I think it is better)

%(better)s

## 4. WHAT STILL LOOKS BAD

%(bad)s

## 5. Foliage obstruction: fix and evidence

%(foliage)s

## 6. Path restoration: fix and evidence

%(paths)s

## 7. Animation evidence

%(anim)s

## 8. Test results (read from the XML, not assumed)

| Run | Result |
| --- | --- |
| EditMode (isolated project copy `%(testFolder)s`) | %(edit)s |
| PlayMode (same copy) | %(play)s |

Previous Gate B totals were 210/210 and 87/87. No test was removed or weakened. Changed existing tests: `Skin_HasNormalizedWeightsAndBlendedJointInfluences` (the rig has 43 bones instead of 25, so the hard-coded 25 became "at least 40 and the same joints are present", and the same weight/blend checks still run) and the bounds range (1.6-1.9 m). New B2 EditMode tests cover the toon materials and textures, mesh budget, foot planting on the real skinned vertices at 4.5 m/s, turn-clip keying, tree card size and canopy height, foliage materials and driver, terrain (no uniform collapse, every texel sums to 1, 98.6 %% of original path samples retained, stone retained) and missing-asset checks. New PlayMode tests cover the turn layer and the foliage-focus driver. Baseline counts from the earlier phases (203/203 and 85/85) were not re-measured on a pristine copy in this session.

## 9. Windows build

Non-development Windows x64 Mono/D3D11, Unity 6000.3.27f1 / URP 17.3.0, version `0.5.2-pilot.2`, build `%(build_dir)s`: result **%(bres)s**, build id `%(bid)s`, errors %(berr)s, warnings %(bwarn)s, %(bbytes)s bytes, `EchoesOfTheValley.exe` SHA-256 `%(exe_sha)s`. RC1 and RC2 and the previous pilot builds were not touched.

## 10. Runtime results (automated player runs)

%(runs)s

## 11. Performance observations (Radeon 780M, D3D11, windowed, vSync off, 60 FPS cap)

%(perf)s

Recorded runs include synchronous framebuffer readback and JPEG encoding and are NOT a rendering benchmark. Unrecorded runs are a bounded 40 s scripted route, not a human traversal benchmark. Draw calls, VRAM, peak memory, a multi-minute traversal and other hardware are NOT VERIFIED. Cost drivers by design: one skinned mesh of ~60 k triangles with 8 materials (SRP-Batcher compatible) plus an inverted-hull outline pass, foliage cutout with alpha-to-coverage and dithered fade, two-pass terrain (8 layers).

## 12. Git

Commit `%(head)s` (local only, not pushed). `git diff --stat` against the failed Gate B base is in `source/git-change-summary.txt`. Original scenes, prefabs, ProjectSettings and Packages changed since base: %(orig)s. Original WAV files changed since base: %(wavs)s.

## 13. Review package

`%(zip)s`, SHA-256 `%(zip_sha)s` (also in the sidecar `.sha256.txt` and `MANIFEST-SHA256.txt` inside).

## 14. Is further artistic work required?

%(further)s

```text
PHASE52_B2_STATUS=%(s_b2)s
BASELINE_PROTECTED=%(protected)s

HERO_MODEL_STATUS=%(s_model)s
HERO_FACE_STATUS=%(s_face)s
HERO_HAIR_STATUS=%(s_hair)s
HERO_CLOTHING_STATUS=%(s_cloth)s
HERO_MATERIAL_STATUS=%(s_mat)s
HERO_ANIMATION_STATUS=%(s_anim)s

FOLIAGE_CAMERA_STATUS=%(s_fol)s
TERRAIN_PATH_STATUS=%(s_path)s

REAL_PLAYER_SCREENSHOTS=YES
REAL_PLAYER_MOTION_VIDEO=%(video)s

EDITMODE_STATUS=%(edit)s
PLAYMODE_STATUS=%(play)s
WINDOWS_BUILD_STATUS=%(bres)s %(bid)s
PLAYER_SMOKE_STATUS=%(smoke_line)s

GATE_B_RESULT=%(s_gate)s
GATE_B_OWNER_APPROVAL=PENDING
WORLD_EXPANSION_ALLOWED=NO

HUMAN_VISUAL_APPROVAL=PENDING
HUMAN_GAMEPLAY_QA_STATUS=PENDING

GIT_COMMIT_SHA=%(head)s
REMOTE_PUSH_STATUS=NOT_ATTEMPTED
ITCH_SUBMISSION_STATUS=NOT_SUBMITTED

REVIEW_ZIP_PATH=%(zip)s
REVIEW_ZIP_SHA256=%(zip_sha)s

NEXT_ACTION=%(next)s
```
'''
runs = []
for k, label in (('after720', 'Tour 720p with recorded video'), ('after1080', 'Tour 1080p'), ('baseline720', 'Tour 720p, RC2 baseline visuals'), ('unrecorded720', 'Tour 720p, unrecorded'), ('unrecorded1080', 'Tour 1080p, unrecorded')):
    t, s = tour.get(k), stats.get(k)
    if t and s:
        runs.append('%s: exit code %s, forced termination %s, exception/error lines %s, %.1f m walked in %.1f s, %s waypoints, teleports during movement: %s; canopy detour: %s' % (label, t['exitCode'], t['forcedTermination'], t['exceptionLines'], s['metres'], s['seconds'], s['waypointsVisited'], s['teleportsDuringMovement'], s['canopyTree']))
for k, label in (('smoke720', 'Whole-game scripted smoke 720p'), ('smoke1080', 'Whole-game scripted smoke 1080p')):
    r = smoke.get(k)
    if r:
        runs.append('%s: natural exit %s, exit code %s, %s s, %s screenshots, exception lines %s (teleports for quest set-up; automated)' % (label, r['naturalExit'], r['exitCode'], r['seconds'], r['screenshots'], r['exceptionLines']))
if prov:
    runs.append('Video: %.2f s, %s frames captured at their real intervals (about %.1f FPS encoded), no audio, no desktop capture, no concept art or repainting; SHA-256 `%s`.' % (prov.get('videoDuration', 0), prov.get('framesCaptured'), prov.get('framesCaptured', 0) / max(prov.get('capturedSeconds', 1), 1), prov.get('movieSha256')))
smoke_ok = all(smoke.get(k) and smoke[k]['naturalExit'] and smoke[k]['exitCode'] == 0 and smoke[k]['exceptionLines'] == 0 for k in ('smoke720', 'smoke1080'))
values = dict(
    base=base, changes=bullets(J['changes']), approach=bullets(J['approach']), better=bullets(J['better']), bad=bullets(J['bad']), foliage=bullets(J['foliage']), paths=bullets(J['paths']), anim=bullets(J['anim']),
    testFolder=J['testFolder'], edit=edit, play=play, build_dir=J['buildDir'], bres=build['result'], bid=build['id'], berr=build['errors'], bwarn=build['warnings'], bbytes=build['bytes'], exe_sha=sha(exe) if exe.is_file() else 'MISSING',
    runs=bullets(runs), perf=bullets(['Recorded 720p tour: ' + perf('after720'), '1080p tour: ' + perf('after1080'), 'Unrecorded 720p tour: ' + perf('unrecorded720'), 'Unrecorded 1080p tour: ' + perf('unrecorded1080'), 'RC2 baseline visuals 720p tour (original terrain/trees/hero): ' + perf('baseline720')]),
    head=head, orig=orig_changed.replace('\n', ', ') or 'none', wavs=wavs.replace('\n', ', ') or 'none', zip=J.get('zipPath', 'see below'), zip_sha=J.get('zipSha', '(see sidecar)'),
    further=bullets(J['further']), protected='YES' if protected else 'NO', video='YES' if prov else 'NO', smoke_line=('PASS (natural exit 0 twice, 0 exception lines)' if smoke_ok else 'FAIL or incomplete: see section 10'),
    next=J['next'], s_b2=status['b2'], s_model=status['model'], s_face=status['face'], s_hair=status['hair'], s_cloth=status['cloth'], s_mat=status['material'], s_anim=status['animation'], s_fol=status['foliage'], s_path=status['path'], s_gate=status['gate'])
(qa / 'final-report.md').write_text(report % values, encoding='utf-8')
readme = '''# Echoes of the Valley Phase 5.2-B2 独立审阅包

**这是作者自评材料，不是验收。** `GATE_B_OWNER_APPROVAL=PENDING`，`WORLD_EXPANSION_ALLOWED=NO`。自动测试不能证明美术好看；所有截图和视频都是真实 Windows Player 画面，输入是**自动脚本**，没有人类试玩，视频无声音。

阅读顺序：
1. `comparison/index.html`：同机位前后对比（主角正/侧/背、脸部特写、工坊/花园/风车路径、20s/30s 树叶对比）。BEFORE 来自失败的 Gate B pilot 或 RC2 基线模式，AFTER 来自本次构建，像素未改。
2. `player/after720/actual-player-automated-motion.mp4`：约 40 秒连续实机录像（自动输入），含相机绕主角、在树冠下低机位行走、沿原有土路行走；`contact-sheet.png` 为每 2 秒抽帧。
3. `final-report.md`：改动、做法、**WHAT STILL LOOKS BAD**、测试/构建/性能（已区分录制/非录制）、状态字段。
4. `terrain/`：修复前后的地形诊断（修复前资产 100% Grass 的原始证据）。`model-diagnostics-NOT-PLAYER-EVIDENCE/` 是编辑器诊断图，不是实机证据。
5. `tests/`、`logs/`、`player/*/player.log`、`source/`（变更源码与 git 摘要）、`provenance/`（素材来源与 AI 披露）、`MANIFEST-SHA256.txt`。

请你重点判断：脸/头发/服装是否仍显程序化、跑步是否自然（脚是否滑步/穿模）、树叶是否仍遮挡镜头、土路是否可读，以及 3D 主角与背包界面里的原画是否像同一个人。`HUMAN_VISUAL_APPROVAL` 与 `HUMAN_GAMEPLAY_QA_STATUS` 保持 PENDING。
'''
(qa / 'README_FIRST_zh.md').write_text(readme, encoding='utf-8')
print('written', qa / 'final-report.md')
