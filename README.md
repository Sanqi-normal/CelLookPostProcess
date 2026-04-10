# Cel Look Post Process

Unity URP 后处理着色器，实现卡通/动漫风格的画面效果。

## 简介

Cel Look Post Process 是一款基于 Unity URP 的后处理效果，专注于将 3D 画面转化为卡通/动漫风格。适用于游戏开发中的三渲二（Toon Shading）需求。



### 核心特性

- **Kuwahara 滤镜** - 去除噪点和纹理，保留硬边缘
- **严格二分色块** - 将画面分为亮部和暗部两个层级
- **阴影偏色** - 为暗部添加色相偏移，增加色彩活力
- **漫画线条描边** - 基于深度和法线的边缘检测
- **剪影模式** - 三色阶卡通风格映射
- **像素化风格** - 可选的复古像素效果
- **预设系统** - 内置多种预设，支持自定义保存

## 查看效果

[https://www.bilibili.com/video/BV1xMQFBBEES]

## 文件结构

```
Assets/CelLookPostProcess/
├── Shaders/
│   └── CelLookPostProcess.shader     ← 核心着色器（4个Pass）
├── Scripts/
│   ├── CelLookSettings.cs             ← Volume Component 参数定义
│   ├── CelLookRenderFeature.cs       ← URP Renderer Feature + RenderPass
│   ├── CelLookPreset.cs              ← 预设数据结构
│   ├── CelLookPresetAsset.cs         ← 预设资源
│   └── CelLookPresetSwitcher.cs      ← 运行时预设切换器
├── Editor/
│   └── CelLookSettingsEditor.cs       ← Inspector 编辑器扩展
└── Presets/
    └── *.asset                       ← 内置预设文件
```

## 快速开始

### 环境要求

- Unity 2021.3+
- Universal Render Pipeline (URP) 12.0+

### Step 1：添加 Renderer Feature

1. 在 Project 窗口找到你的 URP Renderer Asset（如 `Assets/render/custom_Renderer.asset`）
2. 点选它，在 Inspector 底部点击 **"Add Renderer Feature"**
3. 选择 **"Cel Look Post Process"**
4. Render Pass Event 保持 **BeforeRenderingPostProcessing**（默认）

### Step 2：在场景中添加 Volume

**全局效果（推荐）**：
1. 新建空 GameObject，命名为 "GlobalVolume"
2. 添加 `Volume` 组件，勾选 **Is Global**
3. 点击 **New** 创建 Profile
4. 点击 **Add Override → Custom → Cel Look Post Process**
5. 将 Effect Intensity 从 0 调至 1 启用效果

**局部效果**：
- 步骤同上，但不勾选 Is Global，配置 Collider 触发范围

### Step 3：启用相机深度+法线输出

线条描边依赖深度和法线纹理，需要在相机或 Pipeline 资产中开启：

**方法 A - 相机设置**：
1. 选择主摄像机
2. 在 Camera Inspector 中勾选 **Depth Texture** 和 **Opaque Texture**

**方法 B - 全局设置**：
1. 打开 URP Pipeline Asset
2. 勾选 **Depth Texture** 和 **Normal Texture**

---

## 参数详解

### Global Settings

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Effect Intensity | 整体效果强度，0=关闭，1=完全生效 | 0~1 |

### Pop Color Block（二分色块）

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Kuwahara Radius | Kuwahara 滤波半径，越大越平滑 | 2~4 |
| Saturation | 亮部饱和度倍数 | 1.2~1.8 |
| Contrast | 整体对比度 | 1.2~1.8 |
| Brightness | 整体亮度偏移 | -0.3~0.3 |

### Shadow Hue Shift（阴影二分）

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Shadow Threshold | 光影二分阈值，低于此值视为暗部 | 0.4~0.6 |
| Shadow Hue Shift | 暗部色相偏移量 | 0.02~0.08 |
| Shadow Sat Boost | 暗部饱和度额外增强 | 0.0~0.5 |
| Shadow Darken | 暗部压暗系数 | 0.5~0.8 |

### Silhouette Mode（剪影模式）

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Enable Silhouette | 启用剪影模式（覆盖阴影二分设置） | - |
| Silhouette Shadow Color | 阴影色 | - |
| Silhouette Mid Color | 中间调色 | - |
| Silhouette High Color | 高光色 | - |
| Silhouette Threshold1 | 阴影→中间调阈值 | 0.3 |
| Silhouette Threshold2 | 中间调→高光阈值 | 0.7 |

### Manga Line Art（漫画线条）

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Enable Lines | 是否开启描边 | true |
| Line Thickness | 描边粗细（像素） | 1.5~2.5 |
| Depth Threshold | 深度边缘检测灵敏度 | 0.005~0.02 |
| Normal Threshold | 法线边缘检测灵敏度 | 0.1~0.3 |
| Line Color | 线条颜色 | 深色 |
| Line Intensity | 线条强度 | 0.8~1.0 |
| Depth Falloff | 远处线条淡化距离 | 30~80 |

### Pop Color Grading（最终调色）

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Final Saturation | 最终饱和度 | 1.0~1.3 |
| Final Contrast | 最终对比度 | 1.0~1.3 |
| Shadow Tint | 阴影染色（偏蓝紫） | (0,0,0.05) |
| Highlight Tint | 高光染色（偏暖黄） | (0.05,0.03,0) |
| Shadow Influence | 阴影染色强度 | 0.3~0.7 |
| Highlight Influence | 高光染色强度 | 0.2~0.5 |

### Pixelate Mode（像素化）

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Enable Pixelate | 启用像素化风格 | false |
| Pixel Size | 像素块大小 | 4~8 |

---

## 预设系统

### 使用内置预设

1. 在 Volume 组件的 Cel Look 设置面板中
2. 找到 Presets 下拉菜单
3. 选择预设（如"漫画"、"卡通"、"空"）
4. 参数将自动应用

### 保存自定义预设

1. 调整好所有参数
2. 在 Presets 区域输入预设名称
3. 点击 **Save** 按钮
4. 预设将保存到 `Assets/CelLookPostProcess/Presets/` 目录

### 运行时切换预设

使用 `CelLookPresetSwitcher` 组件可实现运行时预设切换：

1. 在 Volume 所在的 GameObject 上添加 `CelLookPresetSwitcher` 组件
2. 在 Presets 数组中拖入预设资源
3. 运行时按 **P** 键循环切换预设

---

## 技术原理

### 渲染管线

```
Pass 0: Kuwahara Filter
    └─ 平滑去噪，保留边缘

Pass 1: Color Binarization
    └─ 严格二分法色块化 / 剪影映射

Pass 2: Manga Line Art
    └─ 基于深度+法线的边缘检测描边

Pass 3: Final Grading
    └─ 最终调色 + 与原图混合
```

### 依赖说明

- `_CameraDepthTexture` - 深度纹理（用于边缘检测）
- `_CameraNormalsTexture` - 法线纹理（用于边缘检测）
- URP Blit API - 用于多 Pass 渲染

---

## 常见问题

### Q: 线条不显示怎么办？
A: 检查以下两点：
1. 相机是否开启了 Depth Texture 和 Opaque Texture
2. URP Pipeline Asset 是否开启了 Depth / Normal Texture

### Q: 效果影响了 UI 怎么办？
A: 此效果是全屏后处理，会影响所有渲染到相机的画面。如需排除 UI：
- 将 UI 相机设为 Overlay 模式
- 或使用单独的相机渲染 UI

### Q: 在编辑器 Scene 视图看不到效果？
A: Volume 组件需要在 Scene 视图启用 "Always Refresh"
