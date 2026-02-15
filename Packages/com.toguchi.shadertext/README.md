# Shader Text

[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Texture-free text rendering for Unity UI using in-shader bitmap fonts.
Designed for debug overlays with **zero mesh rebuild** on text updates.

<img width="400" alt="ShaderText Example" src="Documentation~/images/preview.png">

## Features

- **Zero Mesh Rebuild** - Text updates only write to a GPU buffer. No vertex recalculation, no Canvas rebuild.
- **Texture-Free** - All glyphs are encoded as bit-packed data inside the shader. No font textures or atlases needed.
- **Unity UI Integration** - Extends `MaskableGraphic`. Works with Canvas layout, RectMask2D, and UI Mask.
- **Anti-Aliased** - Smooth edges via `smoothstep` with screen-space derivatives (`fwidth`).
- **Edit Mode Support** - `[ExecuteAlways]` allows preview in the Scene view without entering Play mode.

## Supported Characters

| Category | Characters |
|----------|-----------|
| Digits | `0 1 2 3 4 5 6 7 8 9` |
| Letters | `A B C D E F G H I J K L M N O P Q R S T U V W X Y Z` |
| Symbols | `(space)` `:` `.` `-` |

Lowercase letters are automatically mapped to uppercase.

## Requirements

- Unity 2021.3 or later
- Shader Model 4.5 (StructuredBuffer support)

## Installation

### Unity Package Manager (Git URL)

Open **Window > Package Manager**, click **+** > **Add package from git URL...**, and enter:

```
https://github.com/toguchi/ShaderText.git?path=Packages/com.toguchi.shadertext
```

### Manual

Clone or download this repository and copy the `Packages/com.toguchi.shadertext` folder into your project's `Packages/` directory.

## Quick Start

1. Create a **Canvas** in your scene (if you don't have one already).
2. Add an empty **GameObject** under the Canvas.
3. Add the **Shader Text Renderer** component (`Add Component > UI > Shader Text Renderer`).
4. Set the **Text** field in the Inspector.

### From Script

```csharp
using ShaderText;

var renderer = gameObject.AddComponent<ShaderTextRenderer>();
renderer.MaxCharacters = 16;
renderer.Text = "FPS:60 16.7MS";

// Updating text is extremely cheap — no mesh rebuild occurs
renderer.Text = "FPS:120 8.3MS";
```

## API Reference

### ShaderTextRenderer

Inherits from `UnityEngine.UI.MaskableGraphic`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | Display text. Only GPU buffer is updated on change. |
| `MaxCharacters` | `int` | `16` | Maximum number of characters. Determines buffer size. |
| `CharacterWidth` | `float` | `14` | Width of each character in pixels. |
| `CharacterHeight` | `float` | `24` | Height of each character in pixels. |
| `CharacterSpacing` | `float` | `2` | Horizontal gap between characters in pixels. |

Standard `MaskableGraphic` properties (`color`, `raycastTarget`, etc.) are also available.

## How It Works

```
┌──────────────────────────────────────────────────────────┐
│  CPU (C#)                                                │
│  ┌─────────────┐    ┌──────────────────┐                 │
│  │ Text = "AB" │───>│ CharToIndex('A') │──> [10, 11]    │
│  └─────────────┘    │ CharToIndex('B') │                 │
│                     └──────────────────┘                 │
│                            │                             │
│                   GraphicsBuffer.SetData()               │
│                            │ (no mesh rebuild)           │
├────────────────────────────┼─────────────────────────────┤
│  GPU (Shader)              ▼                             │
│  ┌─────────────────────────────────────────────┐         │
│  │ StructuredBuffer<uint> _CharIndices          │         │
│  │                                              │         │
│  │ Fragment Shader:                             │         │
│  │   charIndex = _CharIndices[slotIndex]        │         │
│  │   pixel = SampleGlyph(charIndex, uv)         │         │
│  │   alpha = smoothstep(edge, pixel)            │         │
│  └─────────────────────────────────────────────┘         │
│                                                          │
│  Font Data: 5×7 bitmap, 35 bits per glyph (uint2)       │
└──────────────────────────────────────────────────────────┘
```

1. **CPU side** converts each character to a glyph index and writes the array to a `GraphicsBuffer`.
2. **GPU side** reads the index per character slot and samples from a hard-coded 5x7 bitmap font.
3. Anti-aliasing is applied using `smoothstep` with `fwidth`-derived edge thresholds.

## Samples

### Performance Counter

A real-time FPS and frame time display. Import via **Package Manager > Shader Text > Samples > Performance Counter**.

```csharp
// Output example: "FPS:120 8.3MS"
[RequireComponent(typeof(ShaderTextRenderer))]
public class PerformanceCounter : MonoBehaviour
{
    [SerializeField] private float _updateInterval = 0.25f;
    // ...
}
```

## License

This project is licensed under the [MIT License](LICENSE).
