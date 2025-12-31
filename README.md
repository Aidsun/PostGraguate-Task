# 沉浸式红色文化数字展馆 (Immersive Red Culture Digital Exhibition)

> **基于 Unity3D 引擎开发的数字化历史漫游体验项目** > 融合高保真场景还原、全景视频高清渲染与交互式多媒体技术，打造寓教于乐的沉浸式学习平台。

------

## 📖 项目简介 (Introduction)

本项目是一款独立开发的3D沉浸式数字展馆，以“红色文化”为主题。项目旨在突破传统参观的时空限制，利用虚拟仿真技术重构历史场景。

作为核心开发者，我独立完成了从 **场景搭建、C#核心逻辑编写、UI交互系统** 到 **性能优化** 的全流程开发。项目重点解决了全景视频高清渲染、跨场景数据持久化及输入系统状态管理等工程难题，展现了扎实的工程落地能力与逻辑思维。

------

## ✨ 核心功能 (Key Features)

### 1. 沉浸式漫游系统

- **双视角切换**：支持 **第一人称 (FPS)** 与 **第三人称 (TPS)** 视角的实时无缝切换，满足不同用户的观察习惯。
- **自由探索**：基于 CharacterController 的物理移动系统，支持跳跃、奔跑及碰撞检测。

### 2. 智能交互机制

- **交互热点**：基于 **Raycast (射线检测)** 技术，当玩家靠近展品时自动触发高亮反馈（Shader实现）。
- **多媒体联动**：点击展品即可呼出图文详情或触发全息语音解说。

### 3. 高清全景影院

- **高清渲染优化**：利用 **RenderTexture (4K/8K)** 技术重构视频渲染管线，有效解决了 Unity 默认 VideoPlayer 播放全景视频模糊的问题。
- **沉浸式天空盒**：支持逻辑控制 Skybox 材质动态切换，实现360度全景历史影像回放。

### 4. 模块化系统设置

- **全局控制面板**：按 `Tab` 呼出/隐藏设置面板，集成了 **AudioMixer** 音频管理（BGM/音效分离）、画质调节及键位映射。
- **状态机管理**：实现了完善的 `Time.timeScale` 时间流速控制与鼠标光标（Cursor Lock/Unlock）状态机，解决了UI操作与角色控制的冲突。

### 5.场景展示

![image-20251231210202313](C:\Users\Aidsu\AppData\Roaming\Typora\typora-user-images\image-20251231210202313.png)

##### 													**开始界面**

![image-20251231210332754](C:\Users\Aidsu\AppData\Roaming\Typora\typora-user-images\image-20251231210332754.png)

##### 													**主场景**

------

## 🛠️ 技术架构 (Technical Architecture)

本项目采用模块化设计，代码结构清晰，遵循面向对象编程（OOP）原则。

- **Design Pattern (设计模式)**:
  - **Singleton (单例模式)**: 用于 `GameData` (全局数据) 和 `AudioManager`，确保跨场景数据持久化 (`DontDestroyOnLoad`)。
  - **Observer/Event (事件机制)**: 使用 UnityEvents 和 C# 委托处理 UI 交互与游戏逻辑的解耦。
- **Data Management**:
  - `GameData.cs`: 集中管理全局状态（如是否播放过片头、音量设置、玩家位置记忆）。
  - `SceneLoading.cs`: 基于 `AsyncOperation` 的异步场景加载系统，包含进度条与动态背景。
- **Optimization**:
  - 全景视频采用 `RenderTexture` + `InternalTime` 模式，确保视频帧与游戏逻辑帧同步，并大幅提升清晰度。

------

## 🎮 操作指南 (Controls)

| **动作**     | **按键/操作**         | **说明**                  |
| ------------ | --------------------- | ------------------------- |
| **移动**     | `W` / `A` / `S` / `D` | 前后左右移动              |
| **跳跃**     | `Space`               | 跨越障碍                  |
| **视角**     | 鼠标移动              | 控制镜头朝向              |
| **交互**     | 鼠标左键              | 点击物品查看详情/播放视频 |
| **切换视角** | `T`                   | 切换第一/第三人称         |
| **跳过片头** | `E` / 左键            | 在开场视频播放时快速跳过  |
| **系统菜单** | `Tab`                 | 暂停游戏并打开设置面板    |

------

## 📂 目录结构 (Directory Structure)

Plaintext

```
Assets/
├── _Scripts/           # 核心代码逻辑
│   ├── Managers/       # GameData, AudioManager, SceneLoading
│   ├── UI/             # SettingPanel, StartGame, HelpPanel
│   ├── Player/         # FirstPersonController, Interactions
│   └── Video/          # PanoramaController
├── _Scenes/            # StartGame, LoadingScene, Museum_Main
├── Resources/          # 动态加载的配置资源
├── Materials/          # 材质球 (包含高清全景 Skybox Material)
└── RenderTextures/     # 用于全景视频的高清渲染纹理 (4096+)
```

------

## 🚀 快速开始 (Getting Started)

1. **环境要求**:
   - Unity 2021.3 LTS 或更高版本。
   - Visual Studio 2019/2022 (推荐)。
2. **安装步骤**:
   - 克隆本仓库: `git clone https://github.com/YourUsername/RedCulture-Exhibition.git`
   - 使用 Unity Hub 打开项目文件夹。
   - 打开 `_Scenes/StartGame` 场景作为入口运行。

------

## 📝 待办事项 / 开发计划 (Roadmap)

- [x] 基础漫游与交互系统
- [x] 4K全景视频播放器优化
- [x] 全局设置面板与数据持久化
- [ ] 接入 AIGC 智能导游 (LLM Agent integration)
- [ ] VR 设备适配 (Oculus/Pico)