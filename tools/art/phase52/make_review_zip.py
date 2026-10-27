"""Small, curated Gate B evidence/source packet. Never includes the rejected desktop video."""
from pathlib import Path
from datetime import datetime
import json, hashlib, subprocess, zipfile, re
from PIL import Image
root=Path(__file__).resolve().parents[3];qa=root/'artifacts/visual-qa/phase-5.2'
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
parent=subprocess.check_output(['git','rev-parse','HEAD^'],cwd=root,text=True).strip()
assert parent=='d9a8d12f79e17257c4808498f8c4144602cafbc3','Commit the scoped pilot before packaging'
assert not subprocess.check_output(['git','status','--porcelain=v1'],cwd=root,text=True).strip(),'Source must be clean'
tracked=subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().strip('\0').split('\0')
files={}
def add(path,name):
    path=Path(path);assert path.is_file(),str(path);assert name not in files,name;files[name]=path
def evidence(rel):add(qa/rel,'project/artifacts/visual-qa/phase-5.2/'+rel)
for name in tracked:
    include=(name.startswith(('game/Assets/Echoes/Scripts/','game/Assets/Echoes/Tests/','game/Assets/Echoes/Art/Phase52/','tools/art/phase52/')) or name in {'.gitattributes','.gitignore','AGENTS.md','CLAUDE.md','LICENSE','README.md','tools/Invoke-Echoes.ps1','game/Packages/manifest.json','game/Packages/packages-lock.json','game/ProjectSettings/ProjectVersion.txt'} or name in {'docs/01-GAME-DESIGN.md','docs/02-BUILD-PLAN.md','docs/03-RULES-CREDITS.md','docs/05-RELEASE.md','docs/06-PHASE51.md','docs/07-PHASE52-ART-DIRECTION.md'})
    if include:add(root/name,'project/'+name)
for name in ['final-report.md','facts.json','baseline-protection.json','reference-image-audit.json','source-and-build-audit.json','build-player-manifest.json']:
    evidence(name)
for path in sorted((qa/'comparison').iterdir()):
    if path.is_file():evidence(path.relative_to(qa).as_posix())
for name in ['logs/baseline/tests-EditMode.xml','logs/baseline/tests-PlayMode.xml','logs/verification2/tests-EditMode.xml','logs/verification2/tests-PlayMode.xml','logs/verification2/tests-EditMode.log','logs/verification2/tests-PlayMode.log','logs/verification1/tests-EditMode.xml','logs/skin-diagnosis/result.xml','logs/skin-diagnosis/editor.log','logs/generate2/Generate.log','logs/build1/Build.log','logs/build2/Build.log','logs/blender-hero-final.log','after720/player.log','after720/process-result.json','after1080/player.log','after1080/process-result.json']:
    evidence(name)
for resolution in ['720','1080']:
    names=['A1-S0-windmill-hero','A1-S3-windmill-hero','A4-S0-hero-front','A4-S0-hero-side','05-inventory','06-crafting','07-stream-restored','11-garden-restored','15-ending']
    if resolution=='720':names=['03-dialogue','07-stream-restored','11-garden-restored']
    for name in names:evidence('after'+resolution+'/screenshots/'+name+'.png')
for name in ['actual-player-automated-motion.mp4','capture-provenance.json','frame-manifest.json','tour.json','process-result.json','hero-front.png','hero-side.png','hero-rear.png','video-frame-3.png','video-frame-11.png','video-frame-20.png','video-frame-30.png']:
    evidence('motion/after720-2/'+name)
for name in ['tour.json','process-result.json','hero-rear.png']:evidence('motion/before720/'+name)
for name in ['tour.json','process-result.json','hero-rear.png']:evidence('motion/after1080/'+name)
add(qa/'motion/after720-1/process-result.json','history/first-player-exit.json')
add(qa/'motion/after720-1/REJECTED-VIDEO.txt','history/rejected-desktop-capture-note.txt')
task=Path(r'C:\Users\weila\.codex\attachments\86547ad7-8919-451b-9109-0df51f5e0ee6\Pasted text.txt');add(task,'request/phase52-original-request.txt')
pack=Path(r'C:\Users\weila\Music\Echoes-Phase52-Original-Anime-Art-Pack\Echoes-Phase52-Visual-Target-Pack')
add(pack/'05-heroine-turnaround-reference.png','reference-only-NOT-RUNTIME/05-heroine-turnaround-reference.png')
add(pack/'SOURCES_AND_RIGHTS.md','reference-only-NOT-RUNTIME/SOURCES_AND_RIGHTS.md')

readme='''# Phase 5.2 Gate B 独立审阅包

**当前 Gate B=FAIL。禁止据工程 PASS 自动进入全谷扩展。** 新主角已有真实骨骼/蒙皮/动作，但角色完成度不足，连续录像中近景树叶卡片严重遮挡视野。此包保留失败画面，请先评价真实试点，勿把参考立绘当成成果。

阅读顺序：
1. `project/artifacts/visual-qa/phase-5.2/final-report.md`：结果、失败原因、测试/构建/录像方法和限制。
2. `project/artifacts/visual-qa/phase-5.2/motion/after720-2/actual-player-automated-motion.mp4`：36.08 秒实际 Player 帧缓冲连续记录，自动输入；20s/30s 近景叶片遮挡被原样保留。无音频，约 21.34 FPS；同步读回/JPEG 采集有性能开销，不能拿这段记录当纯渲染基准。若网页端不能查看视频，请先本地观看，并单独上传四张 `video-frame-*.png` 与对比 PNG。
3. `project/artifacts/visual-qa/phase-5.2/comparison/index.html`：七组真实前后对比，标签只在画面外。截图像素未改，清单有原文件哈希和 RGB 像素哈希；完整原始 PNG 保留在本机。包内主要以对比图避免重复装入全部基线图片。
4. 包内精选的 720p/1080p 原始截图，包括主角正/侧/背、S0/S3、对话、背包、制作、溪流/花园。完整每分辨率 40 张在本机；本包仅保留代表样张和所有对应运行日志。
5. `request/phase52-original-request.txt`、`project/docs/07-PHASE52-ART-DIRECTION.md`、XML/日志及源码。`reference-only-NOT-RUNTIME/05...png` 是原始建模目标，不是游戏截图；原始 01 画像在 `project/game/Assets/Echoes/Art/Phase52/CaretakerPortrait.png`，仅用作 Caretaker 信息肖像，未冒充 Mira。

`project/` 是**代码上下文和 pilot 增量资产快照**，不是可独立打开的完整 Unity 工程。它包含全部 Echoes C#/测试、新 Phase52 Native 资产/场景、Blender/FBX/JSON 源与工具；以本地基础提交 d9a8d12 为上下文。旧地图素材/音频/缓存/历史 Git/Player 二进制未重复装入。可玩 Windows pilot 在原机 `artifacts/build/phase52-pilot-b2/`；解压发布包验证和正式候选版属于尚未获准的 D 阶段。

首次桌面录像录入了被遮挡的非游戏窗口，已判无效且只保留于本机私有记录，**不在本包**。本包不含该录像或截图、不含带桌面覆盖层的 baseline-frames、官方参考图片、账号凭据或付费素材。日志/原始文档有本机路径。原始 769 张游戏帧留在本机，包内有其时间戳/哈希和编码后视频。

确认事实：基线 203/203、85/85；完整 pilot 回归 210/210、87/87（之后的仅录制分支修改由最终 Player 实测验证）；两轮构建版全流程 smoke PASS，人工视觉/真实键鼠/声音验收 PENDING。Gate A PASS，Gate B FAIL，C/D 未执行。请区分工程验证与艺术判断。

文件完整性：`MANIFEST-SHA256.txt` 列出除自身外每个成员；压缩包侧边校验文件另有整体 SHA-256。
'''
prompt='''请独立审阅此 Phase 5.2 试点。先看真实 Player 连续录像和七组前后对比，再读报告、原始任务、模型参考、源码和原始测试/构建日志。当前作者自评 Gate B=FAIL；请验证而不是照抄。

重点：角色脸/头发/服装是否仍显程序化？动作是否自然，是否明显滑步或穿模？20s/30s 的近景叶片为什么遮挡镜头，是否违反自由镜头/叶片卡片门槛？地面重复 clump 与大块草坡如何改进？把高精立绘与简化 3D 的差距明确指出。

请给出按严重程度排序的问题、具体文件或时间点、可执行的修复方案与验收方法，并建议一个有边界的下一轮角色/树木试点。不要要求立刻铺满整个地图；B 未通过且未获 owner approval，不允许进入 C。

请区分实际观察、作者声明与证据不足。工程测试不能证明美术好看；自动输入/脚本传送不能代替人类键鼠操作；视频无音频不能证明声音质量。若不能打开视频或 PNG，请直接说明未验证，不得编造观看结论。人类两个验收字段保持 PENDING。用中文回答。
'''
patch=subprocess.check_output(['git','diff','--no-ext-diff','--no-color',parent,head,'--','README.md','docs','tools/art/phase52','game/Assets/Echoes/Scripts','game/Assets/Echoes/Tests'],cwd=root)
generated={'README_FIRST_zh.md':readme.encode(),'REVIEW_PROMPT_zh.txt':prompt.encode(),'changes/source.patch':patch,'changes/commit.json':json.dumps({'head':head,'parent':parent,'branch':subprocess.check_output(['git','branch','--show-current'],cwd=root,text=True).strip()},indent=2).encode()}
for name in files:
    assert 'baseline-frames/' not in name and 'reference-study/' not in name and 'art-direction-reference/' not in name
    assert not name.endswith(('.exe','.dll','.wav','.zip','.blend1'))
    assert not ('after720-1' in name)
for row in json.loads((qa/'comparison/image-manifest.json').read_text()):
    with Image.open(qa/'comparison'/(row['view']+'.png')) as sheet:
        for label,x in [('before',0),('after',1280)]:assert hashlib.sha256(sheet.convert('RGB').crop((x,36,x+1280,756)).tobytes()).hexdigest()==row[label+'RgbPixelSha256']
patterns=[re.compile(p) for p in [rb'AKIA[0-9A-Z]{16}',rb'gh[pousr]_[A-Za-z0-9]{30,}',rb'github_pat_[A-Za-z0-9_]{40,}',rb'sk-(?:proj-)?[A-Za-z0-9_-]{35,}',rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',rb'https://[^\s/]+:[^\s/@]+@']]
scanned=0
for name,path in files.items():
    if path.suffix.lower() in {'.png','.mp4','.blend','.fbx'}:continue
    data=path.read_bytes()
    if b'\0' not in data:
        scanned+=1;assert not any(p.search(data) for p in patterns),'Credential-like pattern in '+name
for name,data in generated.items():assert not any(p.search(data) for p in patterns),'Credential-like pattern in '+name
target=root/'artifacts/review/phase52';target.mkdir(parents=True,exist_ok=True)
archive=target/('Echoes-Phase52-GateB-FAIL-Review-'+datetime.now().strftime('%Y%m%d-%H%M%S')+'.zip')
manifest={}
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6,allowZip64=True) as z:
    for name,data in generated.items():z.writestr(name,data);manifest[name]=hashlib.sha256(data).hexdigest()
    for name,path in sorted(files.items()):
        data=path.read_bytes();method=zipfile.ZIP_STORED if path.suffix.lower() in {'.png','.mp4'} else zipfile.ZIP_DEFLATED
        z.writestr(name,data,compress_type=method);manifest[name]=hashlib.sha256(data).hexdigest()
    z.writestr('MANIFEST-SHA256.txt',''.join(f'{h}  {n}\n' for n,h in sorted(manifest.items())).encode())
with zipfile.ZipFile(archive) as z:
    assert len(z.namelist())==len(set(z.namelist()))==len(manifest)+1
    for name,h in manifest.items():assert hashlib.sha256(z.read(name)).hexdigest()==h,name
    assert z.testzip() is None
digest=hashlib.sha256(archive.read_bytes()).hexdigest().upper()
result={'archive':str(archive),'bytes':archive.stat().st_size,'sha256':digest,'files':len(manifest)+1,'textFilesCredentialScanned':scanned+len(generated),'credentialPatternMatches':0,'zipCrc':'PASS','perFileSha256':'PASS','comparisonPixels':'PASS','rejectedDesktopVideoIncluded':False,'gateB':'FAIL','worldExpansionAllowed':False,'gitCommit':head}
archive.with_suffix('.verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8');archive.with_suffix('.sha256.txt').write_text(digest+'  '+archive.name+'\n',encoding='utf-8');archive.with_suffix('.prompt.txt').write_bytes(prompt.encode())
print(json.dumps(result))
