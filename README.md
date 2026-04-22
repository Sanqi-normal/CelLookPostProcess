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

https://www.bilibili.com/video/BV1xMQFBBEES
https://www.bilibili.com/video/BV12uoWBXE9t
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

- Unity 2022.3.17f1c1可用
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

### Global & Pre-Filter
| 参数 | 说明 |
| :--- | :--- |
| Effect Intensity | 整体效果强度 |
| Stencil Mask | 开启后可基于 Stencil ID 屏蔽特定对象（如第一人称武器） |
| Pre-Filter Mode | 预处理模式：None, Kuwahara(去噪), Bilateral(双边平滑) |
| Kuwahara Radius | Kuwahara 滤波半径 (越大越平滑) |
| Bilateral Sigma | 颜色/空间容差参数 (控制边缘平滑度) |

### Color & Luminance Mapping
| 参数 | 说明 |
| :--- | :--- |
| Use Ramp Map | 是否使用自定义 Ramp 贴图进行色彩映射 |
| Cel Steps / Smooth | 分析式色阶分段数及其边缘平滑度 |
| Shadow Threshold | 阴影分割阈值及其平滑度 |
| Saturation / Contrast | 亮部饱和度倍数与整体对比度 |
| Brightness Offset | 整体亮度偏移 |
| Shadow Hue/Sat/Dark | 暗部色相偏移、额外饱和度与压暗控制 |

### Silhouette Mode (剪影)
| 参数 | 说明 |
| :--- | :--- |
| Colors | 分别设置 阴影/中间调/高光 的映射颜色 |
| Thresholds | 设置两个阶段的色彩映射分割阈值 |

### Manga Line Art (线条描边)
| 参数 | 说明 |
| :--- | :--- |
| Line Intensity | 线条整体强度 (0为关闭) |
| Line Thickness | 线条粗细 (像素) |
| Thresholds | 基于深度(Depth)、法线(Normal)与亮度(Color)的边缘检测灵敏度 |
| Depth Falloff | 远处线条淡化距离 |
| Line Color | 描边线条颜色 |

### Pattern Shading (动态网点纸)
| 参数 | 说明 |
| :--- | :--- |
| Pattern Type | 设置为圆点或排线模式 |
| Pattern Scale/Angle | 图案的密度(缩放)与旋转角度 |
| Pattern Intensity | 图案对比强度 |
| Pattern Color | 网点缝隙颜色 (通常为黑色) |
| Pattern Luma Threshold | 图案生成的暗部亮度阈值 |

### Color Grading & Screen FX
| 参数 | 说明 |
| :--- | :--- |
| Final Saturation/Contrast | 最终成图的饱和度与对比度 |
| Shadow Tint | 阴影染色倾向及影响强度 |
| Pixelate | 像素化大小 |
| Retro CRT | 弯曲(Curve)、色差(CA)、扫描线(Scanline)等复古特征 |
| Vaporwave | 噪声贴图、故障(Glitch)频率/速度/撕裂强度、颗粒感 |

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

### Q: 不能正常保存和加载预设
A:之前路径是硬编码，修复一次后此问题应该解决了，如果还遇到可检查Presets路径是否正确

### Q: 场景构建后丢失效果
A: 一般是unity构建时剔除造成的，已添加着色器硬引用，仍不成功则在project settings-graphics-always included shaders中拖入cellookPostProcess.shader
