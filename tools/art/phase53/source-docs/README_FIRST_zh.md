# Echoes of the Valley — Phase 5.3 原创美术资源包

**用途**：把 Phase 5.2-B5 的角色正面立绘式外观与巨大灰色圆盘池塘，升级为与场景统一的 2.5D / 45° 高位斜视艺术表现。

这是 **AI 生成的原创美术源素材包（SOURCE ART）**，不是已经切好帧、进 Unity 运行验收通过的最终游戏资产；Codex 负责自动化整理、切图、边缘处理、导入、场景替换与游戏内验证，主人负责最终视觉与手感验收。

## 七张源图

| 文件 | 内容 | 使用与限制 |
| --- | --- | --- |
| `characters/Caretaker_8dir_idle_source.png` | 主角高位俯视 8 个方向，4×2 | 按 S/SW/W/NW、N/NE/E/SE 顺序；真实透明。部分行界穿越，须智能分割、检查脚底 |
| `characters/Caretaker_8dir_walk_keyposes_source.png` | 主角 8 方向、左右脚各一个行走关键姿态，共 16，4×4 | **不是完整走路循环**。仅供 rig/形变/关键帧重建使用，禁止声称包含 6–8 帧完整动画；行界穿越须修整 |
| `characters/Mira_8dir_idle_source.png` | 村民 Mira 8 个方向，4×2 | 角色身份与既有 UI bust 对照；若不一致必须标明需 owner 审核 |
| `pond/Pond_S0_dormant_45deg_source.png` | 阴郁休眠池塘，不规则岸线 | 参考场景视觉、可做画面贴片或几何材质依据；不是自带碰撞的地形 |
| `pond/Pond_S3_restored_45deg_source.png` | 恢复后的池塘 | S0/S3 alpha mask IoU≈0.991；内部细节并非逐像素完全配准，淡入淡出需检查重影 |
| `pond/LilyPads_6clusters_45deg_source.png` | 6 组天然睡莲，3×2 | 单独分离为不同尺寸与方向的水面装饰，替换硬圆盘 |
| `pond/PondWater_muted_albedo_TILING_CANDIDATE.png` | 可在 3D 水面 mesh 上尝试的绘制水纹 | **尚未严格无缝**：左右端误差 15.4、上下端 11.9（0–255 RGB 平均绝对差），须做 seam repair / 重建 |

## 必须遵守的美术方向

1. 场景是 **2.5D**：固定相机 yaw≈45°、pitch≈48°，树木房屋仍按已有 2.5D 实现。主角、NPC 的动作卡片应是从这个高位斜视视角绘制的世界角色，不得回到直视正面纸片人的表现；**不要改成实时 3D 人物**。
2. 源图已含真正的透明 Alpha；PNG 透明像素可携带无关 RGB，请依 Alpha 做抗锯齿分割、alpha bleed，不能用 RGB 当作不透明背景。
3. 图集并非完美行列可直接 Crop。主角 idle 和 walk 行边界处有内容跨越；不能机械等分后裁掉发梢/披风/靴子，也不能把相邻帧连在一起。需进行帧检测、必要的手工/程序美术返修；返修前称 `SOURCE_ONLY`。
4. 走路只有 **A/B 两个关键姿态**，不是充分动画。可基于原图重建 2D 骨骼与有脚底锁定的局部动作循环，或者在真实游戏内认可的情况下用 A/B 插帧；不许凭空报告 8×6/8 完整源帧。
5. S0/S3 池塘对齐较好，但可以分开显示而非直接逐像素线性混合，以免光照反射错位重影。桥、路径、地形高差、阶段切换、主角不能步行穿水等物理行为要在 Unity 独立修复。
6. 此包只覆盖主角与 Mira。**Wren、Alder 的 8 方向高位绘画没有随本包提供**；优先从现有本地 owner 原始 NPC lineup 做身份一致的创作，无法生产则保持原美术并诚实标记 `NPC_VISUAL_GATE=PARTIAL`，不得伪造或误称都已更新。
7. 旧版视觉、B5 测试和审计证据不能删除或篡改，现有编辑/游玩关卡及存档必须延续。

## 集成顺序建议

1. 读取 `docs/PROMPT_CODEX_PHASE53.md`，先审计现有 main 与旧素材。
2. 对源 PNG 做 SHA256 和 Alpha/轮廓/方向/脚点审计；试点仅做主角 + Mira + 池塘，并用真实 Player 的 1080p 截图对照。
3. 补全动画、避障/桥通行、工坊遮挡、4 方/8 方切换，再做整回归与 Windows build。
4. 所有工程改动 **直接在 main**，不得新建分支或 worktree；视觉 PASS 仍必须由 owner 确认。

## 技术追踪

- 原游戏仓库：https://github.com/miku-wwl/echoes-of-the-valley
- B5 审阅证据：https://github.com/miku-wwl/temp-temp
- B5 完结参考 main：`3d5ae5dfbeff10bddc9e6ccce8e7e6dc4f138292`，若 main 已前进需先审计差异，不能强制回退。
- 测试基线：EditMode 262、PlayMode 106，Windows build 0 error/0 warning；这些是历史 B5 成绩，**不是 Phase 5.3 的成绩**。

## 生成/出处

图像由本次 ChatGPT 内置图像生成功能根据用户授权的游戏截图/设计要求生成。未调用外部素材库；属于 AI 辅助生成内容。详情见 `docs/SOURCES_AND_RIGHTS.md`。
