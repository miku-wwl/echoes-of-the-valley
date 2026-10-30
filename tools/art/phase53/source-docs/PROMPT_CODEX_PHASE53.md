# Codex 执行 Prompt — Echoes of the Valley / Phase 5.3

**推荐模型：GPT-6.1 Sol（High）。** 如果遇到严重跨系统架构、无法复现的贴图/深度排序或关键工程故障，才考虑更强模型；日常切图、编辑器工具、测试使用当前模型即可。

你是《Echoes of the Valley》的 Unity 技术美术与发布质量负责人。**现在开始 Phase 5.3 — 2.5D Character & Pond Visual Reconstruction**。这是真正的视觉重构实施任务，不是只写计划。

## 0. 工作位置 / 刚性授权

- 本地项目：`D:\workshop\oct\echoes-of-the-valley`
- GitHub：https://github.com/miku-wwl/echoes-of-the-valley
- B5 真实审阅证据：https://github.com/miku-wwl/temp-temp （本次用户已经把 B5 ZIP 解压并上传，无需再次要求 ZIP）
- 新源图附件：`Echoes-Phase53-2p5D-Art-Pack.zip`（由 ChatGPT 已生成，用户会把 ZIP 下载到本机；先定位实际路径、校验哈希、解压到项目仓库外独立目录，再按需复制生产素材进入 repo）
- **只在现有 `main` 上直接开发、测试、提交与 push；禁止创建任何新分支、worktree、PR、force push、reset --hard、清除或覆盖未提交用户文件。**
- B5 已知终结 main 参考：`3d5ae5dfbeff10bddc9e6ccce8e7e6dc4f138292`。开始时用 `git status`、`git branch --show-current`、`git rev-parse HEAD`、`git ls-remote origin main` 检查真实当前状态，不得假定它还在此 SHA；有新提交或不洁工作区就先解释并安全保护。
- 保留原 Game、B2/B3/B4/B5 的场景、原图、LFS 对象、测试与日志；可新增 Phase53 隔离 pilot，但最终使用 main-only 实现。**此轮不开始 B6，不扩展世界地图**。

## 1. 美术方向：理解正确才能实施

这是 **2.5D 游戏**：世界地形是真实 3D，有固定 45° yaw、约 48° pitch 的高位斜视角，建筑、树木以 2.5D 绘画卡片呈现。**角色同样必须是从 45° 高位俯视看到的 2.5D 角色，不能用眼平正面立绘当场景人物！**

禁止：
- 把主角改回真实时 3D 网格 / 3D skinned model，作为最终表现；
- 简单把当前正面立绘旋转、拉伸或向摄像机 billboard 以伪装 45°；
- 只有 S/N/W/E 四方向，却谎称八方向；
- 以近似静帧/轻微上下浮动冒充完整走路周期；
- 池塘继续是平坦灰圆柱与六个纯绿圆盘；
- 为了测试通过删除/弱化测试或改变玩法。

**主角、Mira 的新 8 方向源图已由 ChatGPT 提供**。这批是绘画源图，不是具有完美帧边界的正式 atlas。请先从美术质量和来源真实性评审，不能因为有图就无条件显示 PASS。

## 2. Gate A — 源图与 baseline 审计

1. 解压 ZIP 后验证 `SHA256SUMS.txt`、PNG 可解码、Alpha 存在、非空、尺寸和内部构图；读 `README_FIRST_zh.md`、`docs/SOURCES_AND_RIGHTS.md`。
2. 角色源图：
   - `Caretaker_8dir_idle_source.png`: 1536×1024，4×2，方向 S/SW/W/NW、N/NE/E/SE；
   - `Caretaker_8dir_walk_keyposes_source.png`: 1230×1278，4×4，共 8 方向×2 关键姿态，**不是完整 8×6 或 8×8 走路循环**；
   - `Mira_8dir_idle_source.png`: 1536×1024，4×2；
   - 部分角色源图存在行边界内容穿越、碎 alpha、可能的衣物/面容漂移。**不得机械等格裁断角色**。应采用 Alpha 连通区 + 定向近邻 mask/实例核查进行切割，再重排为不重叠 atlas，必要时局部绘制修正。若无法无损切分某格，保留原图并标记 `ART_CUTTING_GATE=PARTIAL`，不可使用损坏帧。
3. 检查 8 个世界方向的头顶可见度、透视缩短、脚点、裙摆、发束、左右手配件、比例一致性，对照 B5 Player 截图。若任一方向明显是眼平立绘，做返修或判 FAIL，不许通过单纯 shader 角度欺骗视觉。
4. 泊岸 S0/S3 源图的 Alpha 外形 IoU 约 0.991；内部反射不同，直接 crossfade 会有鬼影风险。`PondWater_muted_albedo_TILING_CANDIDATE.png` 未严格无缝，需修 seam / 用现有 shader 或重建纹理后才进入生产。
5. 验证基线 B5：262 EditMode / 106 PlayMode 通过、Windows build 0 errors/warnings、60fps cap 附近；这些数据仅是历史记录，必须本轮重测。
6. 找出现有 `Phase52B3Builder`、`SpriteSheet25D`、`CharacterVisual2D5D`、`NpcVisual2D5D`、`FixedFollowCamera25D`、`ValleySceneBuilder.BuildWater()`、`ResonanceView`、`Phase52B5Pilot` 的实现与序列化依赖，先画出最小修改边界。
7. Wren/Alder 原始外观来自仓内 `tools/art/phase52b3/source/02-npc-lineup.png`（LFS 文件）。本 ZIP 没有他们的新 8 方向绘画；**不可臆造他们的身份**。可以基于已经存在的用户原创来源制作高位视角版本；若没有合格新稿则保留旧表现并写 `WREN_ALDER_VISUAL_GATE=PARTIAL`，不得声称全体人物统一完成。

## 3. Gate B — 主角与 NPC 真正的 2.5D 透视统一

1. 创建 Phase53 美术导入/构建路径，原始七张 PNG 字节保留 `Source/`，成品在 `Production/`，审计文档独立。Source/ 的 SHA 写入 Unity EditMode 自动化。
2. 构建可靠的 8 方向渲染：S/SW/W/NW/N/NE/E/SE，方向量化与相机 yaw=45 的世界/屏幕坐标映射一致；有合理 5–8° hysteresis，避免边界来回跳；逐方向用 WASD + 斜向组合实际测试。
3. 主角高位透视绘画和地面脚点标定：屏幕跟随平稳；相同位置、同摄像机下，与树、房子和道路严格统一高视角比例。保持 PlayerController、碰撞器、交互检测和存档身份不变；不能增加第二个可交互角色。
4. 行走动画：从提供的 8×2 关键姿态提炼可用方向、绘画和骨骼运动；必须有左右交替，脚底接触周期合理、减少脚滑、不出现突然翻转/瞬移/糊脸；在非正向方向也能自然运动。若两帧源图不足以完成一个可接受的循环，请如实报告 `WALK_ANIMATION_GATE=PARTIAL`，不要伪造不存在的更多原画。
5. Mira 的 8 方向静止应符合与玩家交谈时的真实朝向，并与 UI dialogue bust 可辨识为同一角色。原有对话 portrait 只用于 UI，**不能当地图站立人物**。
6. Wren/Alder 若无法从 local 原图做符合相同标准的转向图，不要用偏差很大的随意新角色替代；先清楚标记差距与下一步。
7. 角色可见性：B5 工坊遮挡时真实主角本体 0% 可见。实现针对角色与工坊的受控 dither fade /局部屋顶 cutaway /透明过渡，保持建筑美术和场景深度；不可强行把主角浮到所有遮挡物上面或穿墙。默认及近距相机都要可追踪角色真实位置，记录截图与量化数据；原 Stop Rule 3 从 FAIL 改为 PASS 必须有 Player 证据。

## 4. Gate C — 池塘视觉与物理整体重建

1. 原 `BuildWater()` 的 `Pond` 圆柱扁平盘、六个扁平 Cylinder lily pad 全部从最终表现中消失；旧代码/资源保留在可回溯的历史范围，不触碰无关资源。
2. `Pond_S0_dormant_45deg_source.png`、`Pond_S3_restored_45deg_source.png` 提供形状、石块、芦苇、色板和睡莲分布参考；可作为 2.5D 绘画贴片与几何结合，但不允许因为一张贴片把水面做成浮在地面上的纸片。
3. 建真正不规则岸线/内凹水盆/浅水渐深边界；水面 mesh、岸边沉积物、手绘石块与芦苇连接自然。采用与同一场景地面高度一致的实际坐标。若使用水纹 `PondWater_muted_albedo_TILING_CANDIDATE.png`，先做边缘 seam audit & repair 并观察 mipmap/repeat，不允许可见接缝。
4. `LilyPads_6clusters_45deg_source.png` 以无光圈无硬边的正确角度铺在水面，不可使用绿色纯色圆形 Cylinder；要根据 Alpha 切出六种天然簇并验证法线/浮高/遮挡不穿帮。
5. S0 休眠到 S3 恢复阶段：与 `ResonanceView`、保存/Continue 的状态一致；在 S1/S2 可合理插值水色而不鬼影，也可以视觉保守切换。保证桥模型与路径保持，桥端脚底对齐，桥可顺利走过；**玩家不得像截图那样站在深水盘中**，岸边碰撞应阻止非桥路径踏水，原来的小溪出口不能断裂。
6. 做新旧及 S0/S3 的真实 1080p 视角对照（玩家接近、上桥、从桥旁离开、转身），不是 Editor-only 图。

## 5. Gate D — 自动化与真实 Player 完整验收

1. 新增针对材质资源、八方向/斜向切换、走路左右交替、Mira 交谈朝向、所有角色脚点、池塘岸线连通性、桥通行、阻止入水、S0–S3、save/Continue、工坊遮挡、真实 PNG Alpha、Source SHA 的 EditMode/PlayMode 测试。既有测试不可删除、跳过或弱化。
2. 运行全量 EditMode、PlayMode；验收底线至少沿用 **262+106** 项既有范围且 0 failed/0 skipped（新增测试另列）；记录 Unity 版本、退出码、XML 报告与真实统计。
3. Windows x64 非开发 build，0 errors / 0 warnings；从独立目录运行真正的 Player。1080p，记录主角 8 世界方向与斜向移动、Mira 交谈、池塘 S0/S3、桥、工坊背后、Spring/Garden/Chicken/Quests/Ending、Save/Continue。
4. 留存真实 Player 连续录像与截图、帧数/时长/分辨率和运行时 FPS。**录像编码 FPS ≠ 游戏 FPS**。若没有实际播放或人工测试必须标为 NOT VERIFIED / PENDING，不能写 PASS。
5. 同硬件与场景下做 B5 vs 5.3 性能对照（Radeon 780M、1920×1080、60 cap，静态、跑动与池塘）；B4 先前路线阻塞不能当成已经成功的基线。保存 P95/P99 frametime、内存与批次；若无可比条件写 PARTIAL。
6. 视觉停止规则：角色不再像正面纸片竖在斜俯视场景；8 方向正确且无明显缺手缺脚；行走过渡自然；池塘非圆盘、不穿水、桥可通行；工坊不再令主角完全消失；原 Spring/Garden/Tree 的 B5 成果不退化。任何一个明显 FAIL，`PHASE53_VISUAL_GATE=FAIL`。
7. 对艺术源图有任何不确定性，保留完整来源记录，注明 ChatGPT AI-assisted 生成，并提示由 owner 确认赛事 AI 资产披露规则。

## 6. 时间与 Git 交付

- 采用 Gate A→B→C→D 顺序，先完成真实可见的 2.5D hero+Mira+pond 最小纵切，再补动画与完整测试，不进行地图扩张和花哨新玩法。
- 强制 main-only；使用正常非强制 `git add` / `git commit` / `git push origin main`。**不要建任何新分支或 PR**。允许分多个 commit 清晰记录修复阶段。
- 仅当工程测试无回归、构建 PASS、工作树安全且非敏感文件时可推送；视觉 PARTIAL 可保留提交但必须如实标注，严重未解决工程故障不得冒称完成后强推。
- 建议 `docs/12-PHASE53.md`、`artifacts/visual-qa/phase-5.3/`、审计和 Source SHA 清单。生产 PNG 可通过既有 Git LFS 规则存储，先核实远端实际上传成功，不能只提交 pointer 就声称审阅可用；本地大审计包不要无脑 commit。
- 不覆盖 `temp-temp` 审阅仓，除非 owner 明确提出。最后交付可重新访问的证据路径及 SHA。

## 7. 完成时报告

请首先给简洁结论，然后严格输出：

```text
PROJECT=Echoes of the Valley
PHASE=5.3
BRANCH=main
START_HEAD=<sha>
FINAL_HEAD=<sha>
REMOTE_MAIN_HEAD=<sha>
WORKTREE_STATUS=<clean/dirty>
ASSET_ZIP_SHA256=<sha>
SOURCE_IMAGE_COUNT=7
SOURCE_ALPHA_AUDIT=<PASS/PARTIAL/FAIL>
HERO_8D_IDLE=<PASS/PARTIAL/FAIL>
HERO_8D_WALK=<PASS/PARTIAL/FAIL>
MIRA_8D=<PASS/PARTIAL/FAIL>
WREN_ALDER_VISUAL_GATE=<PASS/PARTIAL/FAIL>
POND_SHORELINE=<PASS/PARTIAL/FAIL>
POND_NO_WALKTHROUGH=<PASS/PARTIAL/FAIL>
BRIDGE_ROUTE=<PASS/PARTIAL/FAIL>
S0_S3_ALIGNMENT=<PASS/PARTIAL/FAIL>
WORKSHOP_OCCLUSION=<PASS/PARTIAL/FAIL>
EDITMODE_TESTS=<passed/failed/skipped>
PLAYMODE_TESTS=<passed/failed/skipped>
WINDOWS_BUILD=<PASS/PARTIAL/FAIL>
REAL_PLAYER_1080P=<PASS/PARTIAL/NOT_RUN>
PERFORMANCE_COMPARISON=<PASS/PARTIAL/NOT_RUN>
HUMAN_PLAYTEST=PENDING
OWNER_VISUAL_APPROVAL=PENDING
PHASE53_ENGINEERING_STATUS=<PASS/PARTIAL/FAIL>
PHASE53_VISUAL_GATE=<PASS/PARTIAL/FAIL>
PHASE53_FINAL_STATUS=<PASS/PARTIAL/FAIL>
```

最后列：变更的主要 C#/Shader/资产路径、真实构建运行文件路径、截图/视频证据、失败及已知限制、下一步建议。**不要擅自进入 Phase 5.4/B6。**
