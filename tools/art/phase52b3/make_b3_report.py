"""Writes final-report.md and README_FIRST_zh.md for the Phase 5.2-B3 review package.

python tools/art/phase52b3/make_b3_report.py <judgments.json>
Numbers (tests, build id, frame times, distances, exit codes, hashes, git ids) are read from the evidence files, never typed by hand.
Judgments (statuses, what still looks bad) come from judgments.json and are the author's opinion, not owner approval.
"""
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parents[3]
J = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
ev = root / J['evidenceRoot']
tv = root / J['testFolder']


def read(path, default=None):
    p = Path(path)
    return json.loads(p.read_text(encoding='utf-8-sig')) if p.is_file() else default


def tests(xml):
    r = ET.parse(xml).getroot()
    return '%s %s/%s (failed %s, skipped %s)' % (r.get('result'), r.get('passed'), r.get('total'), r.get('failed'), r.get('skipped'))


def b3_cases(xml):
    r = ET.parse(xml).getroot()
    return ['%s: %s' % (c.get('name'), c.get('result')) for c in r.iter('test-case') if 'Phase52B3' in (c.get('classname') or '')]


def git(*a):
    return subprocess.check_output(['git', *a], cwd=root, text=True).strip()


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()


edit, play = tests(tv / 'tests-EditMode.xml'), tests(tv / 'tests-PlayMode.xml')
cases = b3_cases(tv / 'tests-EditMode.xml') + b3_cases(tv / 'tests-PlayMode.xml')
blog = (root / J['buildLog']).read_text(encoding='utf-8', errors='replace')
m = re.search(r'PILOT_BUILD (\w+) id=(\w+) errors=(\d+) warnings=(\d+) bytes=(\d+)', blog)
build = dict(result=m.group(1), id=m.group(2), errors=m.group(3), warnings=m.group(4), bytes=m.group(5)) if m else dict(result='UNKNOWN', id='?', errors='?', warnings='?', bytes='?')
exe = root / J['buildDir'] / 'EchoesOfTheValley.exe'
runs = {k: (read(ev / 'player' / k / 'process-result.json'), read(ev / 'player' / k / 'tour-stats.json')) for k in ('tour720-video', 'tour1080', 'tour1080-legacy-b2-hero', 'tour720-unrecorded', 'tour1080-unrecorded')}
smoke = {k: read(ev / 'player' / k / 'process-result.json') for k in ('smoke720', 'smoke1080')}
prov = read(ev / 'player/tour720-video/capture-provenance.json', {})
head = git('rev-parse', 'HEAD')
rc1 = root / 'artifacts/release/EchoesOfTheValley-Windows-x64-RC1.zip'
rc2 = root / 'artifacts/release/phase51-rc2-validated/EchoesOfTheValley-Windows-x64-RC2-Art.zip'
protected = rc1.is_file() and rc2.is_file() and sha(rc1) == 'EB9BFC0D9B0BA60A0E9E8367ADF3BA4EB4D4F3EFE8F0F6FCD58A2AB4FDA3112A' and sha(rc2) == '9085554ED52B39B46B250AA5E7E94642815FF76D9730AF038E6E8F180CFC8A80'
b2_intact = git('rev-parse', 'art/phase52-b2') == 'aef87c09cd6ceee8ed183e59608c7b833268e481'


def bullets(items):
    return '\n'.join('- ' + i for i in items)


def run_line(label, key):
    r, s = runs[key]
    if not r or not s:
        return '%s: not run' % label
    f = s['frameMs']
    return ('%s: exit code %s (natural exit %s), exception/error lines %s; route %s m in %s s, route pass %s, failures %s; %s, %d frames, P50 %.2f / P95 %.2f / P99 %.2f ms, worst %.1f ms, mean %.1f FPS, 1%% low %.1f FPS (%s)'
            % (label, r['exitCode'], r['naturalExit'], r['exceptionLines'], s['metres'], s['routeSeconds'], s['routePass'], s['failures'] or 'none', s['resolution'], s['frames'], f['p50'], f['p95'], f['p99'], f['worst'], s['fps']['mean'], s['fps']['onePercentLow'], s['note']))


run_lines = [run_line('Recorded route 720p (video)', 'tour720-video'), run_line('Route 1080p (stills)', 'tour1080'), run_line('Route 1080p, B2 3D hero under the B3 camera (comparison)', 'tour1080-legacy-b2-hero'),
             run_line('Unrecorded route 720p', 'tour720-unrecorded'), run_line('Unrecorded route 1080p', 'tour1080-unrecorded')]
for k, label in (('smoke720', 'Whole-game smoke 720p'), ('smoke1080', 'Whole-game smoke 1080p')):
    r = smoke[k]
    run_lines.append('%s: %s' % (label, ('natural exit %s, exit code %s, %s s, %s screenshots, exception lines %s, %s' % (r['naturalExit'], r['exitCode'], r['seconds'], r['screenshots'], r['exceptionLines'], '; '.join(r.get('passLines') or []))) if r else 'not run'))
video_line = ('%.2f s, %s real framebuffer frames at their captured intervals (about %.1f FPS), 1280x720, no audio, no desktop capture, no concept compositing; SHA-256 `%s`' % (prov['videoDuration'], prov['framesCaptured'], prov['framesCaptured'] / max(1e-6, prov['capturedSeconds']), prov['movieSha256'])) if prov else 'not produced'
vs = runs['tour720-video'][1] or {}
skate = vs.get('runPhaseVsDistance', {})
S = J['status']
smoke_ok = all(smoke[k] and smoke[k]['naturalExit'] and smoke[k]['exitCode'] == 0 and smoke[k]['exceptionLines'] == 0 for k in smoke)
video_ok = bool(prov) and 45 <= prov.get('videoDuration', 0) <= 90

report = '''# Echoes of the Valley: Phase 5.2-B3 (2.5D pivot) review report

2026-10-28 NZDT, branch `art/phase52-b3-2d5d` (local only), from the B2 commit `aef87c09cd6ceee8ed183e59608c7b833268e481`. **Author self-assessment only. Gate B owner approval, human visual approval and human gameplay QA are PENDING; world expansion is NOT allowed.** Every screenshot and the video are real Windows-player framebuffer captures driven by **automated** input (no human played).

## 1. What was built

%(built)s

## 2. Art source and pipeline (the blocker, stated plainly)

%(pipeline)s

## 3. Camera and controls (owner-authorised constraint)

%(camera)s

## 4. WHAT STILL LOOKS BAD

%(bad)s

## 5. Visual gate (author's judgment of the real-player screenshots)

%(gate)s

## 6. Tests (read from the XML)

| Run | Result |
| --- | --- |
| EditMode, isolated project copy `%(tv)s` | %(edit)s |
| PlayMode, same copy | %(play)s |

B3 tests: %(cases)s. No existing test was removed or weakened; `DialogueBox` gained a read-only `Speaker` property only.

## 7. Windows build

Non-development Windows x64 Mono/D3D11, Unity 6000.3.27f1 / URP 17.3.0, version `0.5.2-pilot.3`, `%(bdir)s`: **%(bres)s**, build id `%(bid)s`, errors %(berr)s, warnings %(bwarn)s, %(bbytes)s bytes, `EchoesOfTheValley.exe` SHA-256 `%(exe)s`. RC1/RC2 zips unchanged: %(prot)s. B2 branch still at its commit: %(b2)s.

## 8. Real-player runs (automated)

%(runs)s

Video: %(video)s. Run-cycle check on the recorded route: the presenter's run phase advanced by distance / stride (%(stride).3f m) with median error %(skmed)s and p95 error %(skp95)s of a cycle per frame (the planted foot cannot skate by construction).

## 9. Performance

Radeon 780M, D3D11, windowed, 60 FPS cap, vSync off. The unrecorded runs above are the measurement; the recorded run includes synchronous framebuffer readback and JPEG encoding and is not a benchmark. Headroom above the cap, VRAM, peak memory, multi-minute traversal and other GPUs are NOT VERIFIED. New cost: one 4096 x 4096 BC7 caretaker atlas (about 21 MB with mips), one 4096 x 2048 villager atlas, four transparent cards and four contact shadows.

## 10. Git

Commit `%(head)s` (local only; not pushed).

```text
PHASE52_B3_STATUS=%(s_b3)s
ART_PIPELINE=%(s_pipe)s
CHARACTER_DIRECTION_COUNT=%(s_dirs)s
CHARACTER_IDLE_STATUS=%(s_idle)s
CHARACTER_LOCOMOTION_STATUS=%(s_loco)s
CHARACTER_IDENTITY_CONSISTENCY=%(s_ident)s
CHARACTER_VISUAL_STATUS=%(s_vis)s
CAMERA_2D5D_STATUS=%(s_cam)s
DEPTH_SORTING_STATUS=%(s_depth)s
FOLIAGE_CAMERA_STATUS=%(s_fol)s
TERRAIN_PATH_STATUS=%(s_path)s
PORTRAIT_MATCH_STATUS=%(s_port)s
EDITMODE_STATUS=%(edit)s
PLAYMODE_STATUS=%(play)s
WINDOWS_BUILD_STATUS=%(bres)s %(bid)s
ACTUAL_PLAYER_VIDEO_STATUS=%(s_video)s
PERFORMANCE_STATUS=%(s_perf)s
ASSET_LICENSE_STATUS=%(s_lic)s
AI_DISCLOSURE_STATUS=%(s_ai)s
GATE_B_RESULT=%(s_gate)s
GATE_B_OWNER_APPROVAL=PENDING
HUMAN_VISUAL_APPROVAL=PENDING
HUMAN_GAMEPLAY_QA_STATUS=PENDING
WORLD_EXPANSION_ALLOWED=NO
REMOTE_PUSH_STATUS=NOT_ATTEMPTED
ITCH_SUBMISSION_STATUS=NOT_SUBMITTED
GIT_COMMIT_SHA=%(head)s
PLAYER_SMOKE_STATUS=%(smoke)s
REVIEW_ZIP=see the sidecar .sha256.txt next to the zip (a zip cannot contain its own hash)
NEXT_ACTION=%(next)s
```
''' % dict(built=bullets(J['built']), pipeline=bullets(J['pipeline']), camera=bullets(J['camera']), bad=bullets(J['bad']), gate=bullets(J['gate']),
           tv=J['testFolder'], edit=edit, play=play, cases=', '.join(cases) or 'none found', bdir=J['buildDir'], bres=build['result'], bid=build['id'], berr=build['errors'],
           bwarn=build['warnings'], bbytes=build['bytes'], exe=sha(exe) if exe.is_file() else 'MISSING', prot='YES' if protected else 'NO', b2='YES' if b2_intact else 'NO',
           runs=bullets(run_lines), video=video_line, stride=skate.get('stride', 0), skmed=skate.get('medianError'), skp95=skate.get('p95Error'), head=head,
           s_b3=S['b3'], s_pipe=S['pipeline'], s_dirs=S['directions'], s_idle=S['idle'], s_loco=S['locomotion'], s_ident=S['identity'], s_vis=S['visual'], s_cam=S['camera'],
           s_depth=S['depth'], s_fol=S['foliage'], s_path=S['path'], s_port=S['portrait'], s_video=('PASS (automated, %.1f s)' % prov['videoDuration']) if video_ok else 'FAIL or incomplete',
           s_perf=S['performance'], s_lic=S['license'], s_ai=S['ai'], s_gate=S['gate'], smoke=('PASS (720p and 1080p, natural exit 0, 0 exception lines)' if smoke_ok else 'FAIL or incomplete: see section 8'), next=J['next'])
(ev / 'final-report.md').write_text(report, encoding='utf-8')
readme = '''# Echoes of the Valley · Phase 5.2-B3（2.5D 转向）独立审阅包

**这是作者自评材料，不是验收。** `GATE_B_OWNER_APPROVAL=PENDING`、`HUMAN_VISUAL_APPROVAL=PENDING`、`HUMAN_GAMEPLAY_QA_STATUS=PENDING`、`WORLD_EXPANSION_ALLOWED=NO`。所有截图和视频都来自真实 Windows Player，输入是**自动脚本**，没有人类试玩，视频无声音。

阅读顺序：
1. `comparison/index.html`：同一相机、同一位置的 B2 3D 主角 vs B3 立绘精灵；四个朝向与对角；对话头像/背包立绘一致性；各地点环境对比。每张图都标了类别（CONCEPT_ART / PRODUCTION_SPRITES / EDITOR_PREVIEW / REAL_PLAYER_SCREENSHOT）。
2. `motion/actual-player-2d5d-motion.mp4`：约一分钟连续实机录像（自动输入）：站立、起步、四个对角方向、转身、停下、与 Mira 对话、在柳树树冠下绕行、采集花朵、风车、回到花园。`motion/contact-sheet.png` 每 2 秒一帧，`motion/selected-frames/` 按步骤和朝向挑出的真实帧。
3. `final-report.md`：做了什么、美术来源与流程、相机约束、**WHAT STILL LOOKS BAD**、测试/构建/性能、状态字段。
4. `character-source/`：原始概念图（业主提供的 AI 插画，未修改）、抠图后的三视图、游戏实际使用的精灵图集与头像。
5. `provenance/`（素材清单、AI 披露、许可说明）、`tests/`、`logs/`、`source/`、`MANIFEST-SHA256.txt`、`LABELS.md`。

重要说明：本环境**没有图像生成能力**，所以没有画新画；所有精灵都是从你提供的 AI 插画（01/02/05）抠图后用 2D 骨骼变形做的动画，没有任何一像素是代码画出来的。要更好的待机/跑步，需要真正逐帧绘制或生成的新原画（见报告第 4 节）。

请重点判断：角色在游戏里是否好看、是否像同一个人（世界里的精灵、对话头像、背包立绘）、跑步/转身是否可以接受、固定 30° 俯视相机是否可以接受、以及立绘角色与低多边形世界的风格反差。
'''
(ev / 'README_FIRST_zh.md').write_text(readme, encoding='utf-8')
print('written', ev / 'final-report.md')
