# 🏛️ RedGeine - 3D沉浸式红色主题数字展馆

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3%20LTS-green?style=flat-square&logo=unity" alt="Unity Version">
  <img src="https://img.shields.io/badge/C%23-9.0-blue?style=flat-square&logo=csharp" alt="C# Version">
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=flat-square" alt="License">
</p>

> 🎯 这是一款独立开发的3D沉浸式数字展馆，以"红色阅兵"为主题，利用虚拟仿真技术重构历史场景，突破传统参观的时空限制。

---

## ✨ 核心特性

### 1. 🚶 沉浸式漫游系统

- **双视角切换**：支持 **第一人称 (FPS)** 与 **第三人称 (TPS)** 实时无缝切换
- **自由探索**：基于 CharacterController 的物理移动系统，支持跳跃、奔跑及碰撞检测
- ** Cinemachine 摄像机**：专业级摄像机控制，平滑跟随

### 2. 🎯 智能交互系统

- **射线检测 (Raycast)**：玩家靠近展品时自动触发高亮反馈
- **Shader 高亮效果**：自定义高亮材质，视觉反馈更直观
- **多媒体联动**：点击展品即可呼出图文详情或触发全息语音解说

### 3. 🎬 高清全景影院

- **4K/8K RenderTexture**：重构视频渲染管线，解决 Unity 默认 VideoPlayer 播放全景视频模糊问题
- **沉浸式天空盒**：逻辑控制 Skybox 材质动态切换
- **360° 全景回放**：历史影像沉浸式体验

### 4. 📝 答题互动系统

- **随机抽题**：从题库中随机抽取 7 道题目
- **印章收集**：答题正确获得展馆印章
- **奖励机制**：集齐印章可领取纪念奖励

### 5. 🎧 模块化音频系统

- **AudioMixer 集成**：BGM/音效/视频音频/解说音频分离控制
- **跨场景持久化**：使用 `DontDestroyOnLoad` 确保音频状态无缝过渡
- **场景音频状态机**：自动根据场景切换控制音频播放/暂停

### 6. ⚙️ 全局设置面板

- **快捷键 `Tab` 呼出**：暂停游戏并打开设置面板
- **音量独立调节**：BGM、视频、语音、按钮音效分别控制
- **画质调节**：可选画质预设
- **键位映射**：自定义按键配置

---

## 🛠️ 技术架构

### 设计模式

| 模式 | 应用场景 | 优势 |
|:---|:---|:---|
| **单例模式 (Singleton)** | `GameData`、`AudioManager` | 全局唯一访问点，跨场景数据持久化 |
| **观察者模式 (Observer)** | UnityEvents / C# Delegate | UI 与游戏逻辑解耦 |
| **状态机 (State Machine)** | 视角切换、鼠标状态管理 | 清晰的状态流转逻辑 |

### 核心模块

```
RedGeine/
├── Assets/
│   ├── Scripts/                    # 核心代码逻辑 (23个脚本)
│   │   ├── GameData.cs             # 全局数据管理 & 存档系统
│   │   ├── AudioManager.cs         # 音频管理器 (AudioMixer集成)
│   │   ├── SceneLoading.cs         # 异步场景加载系统
│   │   ├── PanoramaExhibition.cs  # 全景视频展示
│   │   ├── VideoExhibition.cs      # 视频展示
│   │   ├── ImageExhibition.cs      # 图片展示
│   │   ├── QuestionManager.cs     # 答题系统
│   │   ├── GuideNPC.cs             # 导览员系统
│   │   ├── QuizPoint.cs            # 答题点触发
│   │   ├── RouteLines.cs           # 路线指示
│   │   ├── SwitchViews.cs         # 视角切换
│   │   ├── PlayerInteraction.cs   # 玩家交互
│   │   ├── StartGame.cs           # 开始界面
│   │   └── SettingPanel.cs        # 设置面板
│   │
│   ├── Scenes/                     # 场景文件
│   │   ├── StartGame.unity         # 开始界面
│   │   ├── LoadingScene.unity     # 加载场景
│   │   ├── Museum_Main.unity       # 主展馆
│   │   ├── VideoContent.unity      # 视频内容
│   │   ├── PanoramaContent.unity   # 全景内容
│   │   └── ImageContent.unity      # 图片内容
│   │
│   ├── Resources/                  # 动态加载资源
│   │   └── Question_Ansower.txt    # 题库文件
│   │
│   ├── Audios/                     # 音频资源
│   ├── Images/                     # 图片资源
│   ├── Videos/                     # 视频资源
│   ├── Models/                     # 3D模型
│   ├── Materials/                  # 材质球
│   ├── Textures/                   # 纹理贴图
│   └── Prefabs/                     # 预制体
│
├── Packages/                       # Unity 包管理
└── ProjectSettings/                 # 项目设置
```

### 技术栈

| 类别 | 技术/版本 | 说明 |
|:---|:---|:---|
| **引擎** | Unity 2022.3 LTS | 长期支持版本 |
| **语言** | C# 9.0 | .NET Standard 2.1 |
| **摄像机** | Cinemachine 2.10.5 | 专业摄像机系统 |
| **UI** | TextMeshPro 3.0.7 | 高质量文本渲染 |
| **模型加载** | glTFast | 高效 glTF 模型加载 |
| **输入系统** | Unity Input System 1.14.0 | 新一代输入系统 |
| **可视化编程** | Visual Scripting 1.9.4 | Bolt 可视化脚本 |

---

## 🎮 操作指南

### 基础控制

| 操作 | 按键/方式 | 说明 |
|:---|:---|:---|
| **移动** | `W` / `A` / `S` / `D` | 前后左右移动 |
| **跳跃** | `Space` | 跨越障碍物 |
| **视角控制** | 鼠标移动 | 控制镜头朝向 |
| **交互** | 鼠标左键 | 点击物品查看详情/播放视频 |
| **切换视角** | `T` | 第一人称 ↔ 第三人称 |
| **跳过片头** | `E` / 左键 | 开场视频时快速跳过 |
| **系统菜单** | `Tab` | 暂停游戏并打开设置 |

### 游戏流程

```
开始游戏 → 片头视频 → 主展馆漫游 → [交互展品] → [答题挑战] → 收集印章 → 领取奖励
     ↓
  设置面板 (Tab键)
```

---

## 📊 开发历程

| 版本 | 日期 | 更新内容 |
|:---|:---|:---|
| **v1.0** | 2026-03-20 | 完善第一版，图片/视频/全景视频功能齐全 |
| **v0.9** | 2026-03-19 | 完善导览员功能 |
| **v0.8** | 2026-03-16 | 添加7个答题点，实现印章计数系统 |
| **v0.7** | 2026-01-28 | 修复音频系统，重构加载逻辑 |
| **v0.6** | 2026-01-27 | 添加路线指示、答题功能 |
| **v0.5** | 2026-01-25 | 室外场景布置完成 |
| **v0.4** | 2026-01-14 | UI优化，增加音频延迟效果 |
| **v0.3** | 2026-01-03 | 增加开屏视频和控制面板 |
| **v0.2** | 2025-12-27 | 核心逻辑完成，AudioMixer集成 |
| **v0.1** | 2025-12-26 | 代码架构重构，项目基础奠定 |

> 📌 共经历 **40+** 次提交，从零构建完整的数字展馆系统

---

## 🚀 快速开始

### 环境要求

| 配置 | 最低要求 | 推荐配置 |
|:---|:---|:---|
| **Unity** | 2021.3 LTS | 2022.3 LTS |
| **VS** | VS 2019 | VS 2022 |
| **内存** | 8 GB | 16 GB |
| **显卡** | GTX 1060 | RTX 3060+ |

### 安装步骤

1. **克隆仓库**
   ```bash
   git clone https://github.com/Aidsun/RedGeine.git
   cd RedGeine
   ```

2. **使用 Unity Hub 打开**
   - 打开 Unity Hub
   - 点击 "Open" → 选择 `RedGeine` 文件夹
   - 等待 Unity 导入包和编译脚本

3. **运行项目**
   - 在 Unity Editor 中打开 `Assets/Scenes/StartGame.unity`
   - 按 `Play` 按钮或 `Ctrl+P` 运行

---

## 📁 项目结构详解

### 脚本分类

| 分类 | 数量 | 主要功能 |
|:---|:---|:---|
| **管理器** | 3 | 数据存储、音频控制、场景加载 |
| **展示系统** | 3 | 图片、视频、全景视频播放 |
| **交互系统** | 4 | 射线检测、热点交互、答题点、路线 |
| **玩家控制** | 3 | 视角切换、移动控制、足音 |
| **UI系统** | 5 | 开始界面、设置面板、引导、交互按钮 |
| **辅助工具** | 5 | 文本滚动、摄像机控制、Shader参数 |

### 数据流

```
玩家输入 → PlayerInteraction (射线检测)
           ↓
    触发热点 → 展品脚本 (PanoramaExhibition/VideoExhibition/ImageExhibition)
           ↓
    保存状态 → GameData (跨场景数据持久化)
           ↓
    场景切换 → SceneLoading (异步加载 + 进度条)
           ↓
    状态恢复 → SwitchViews (视角 & 位置恢复)
```

---

## 🔧 扩展开发

### 添加新的展品

1. 在对应场景中创建展品对象
2. 添加对应的 Exhibition 脚本
3. 配置 Inspector 参数（标题、视频/图片资源、解说音频）
4. 添加 Collider 和 InteractionButton

### 添加新题目

编辑 `Resources/Question_Ansower.txt`：

```
题目内容#选项A#选项B#选项C#选项D#正确答案
```

格式说明：`#` 分隔，正确答案为 A/B/C/D

### 自定义高亮效果

修改 `GameData.cs` 中的 `HighlightColor` 字段，或在材质中自定义 Shader

---

## 📄 许可证

本项目基于 **MIT 许可证** 开源，欢迎学习和交流，但禁止商用。

---



## 📧 联系作者

- **GitHub**: [Aidsun](https://github.com/Aidsun)
- **Email**: Aidsun_552@163.com

---

<p align="center">
  <sub>Built with ❤️ by Aidsun | 独立开发作品</sub>
</p>
