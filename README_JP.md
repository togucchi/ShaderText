# Shader Text

[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Unity UI 向けのテクスチャ不要テキストレンダリング。シェーダー内ビットマップフォントを使用します。
テキスト更新時に**メッシュの再構築がゼロ**なデバッグオーバーレイ向け設計です。

<img width="400" alt="ShaderText Example" src="Documentation~/images/preview.png">

## 特徴

- **メッシュ再構築ゼロ** - テキスト更新時は GPU バッファへの書き込みのみ。頂点の再計算も Canvas の再構築も不要です。
- **テクスチャ不要** - すべてのグリフはシェーダー内にビットパックデータとしてエンコード。フォントテクスチャやアトラスは必要ありません。
- **Unity UI 統合** - `MaskableGraphic` を継承。Canvas レイアウト、RectMask2D、UI Mask と連携します。
- **アンチエイリアス** - スクリーンスペース微分 (`fwidth`) を用いた `smoothstep` による滑らかなエッジ。
- **Edit Mode 対応** - `[ExecuteAlways]` により Play モードに入らずに Scene ビューでプレビュー可能。

## 対応文字

| カテゴリ | 文字 |
|---------|------|
| 数字 | `0 1 2 3 4 5 6 7 8 9` |
| 英字 | `A B C D E F G H I J K L M N O P Q R S T U V W X Y Z` |
| 記号 | `(スペース)` `:` `.` `-` |

小文字は自動的に大文字にマッピングされます。

## 動作要件

- Unity 2021.3 以降
- Shader Model 4.5（StructuredBuffer サポート）

## インストール

### Unity Package Manager (Git URL)

**Window > Package Manager** を開き、**+** > **Add package from git URL...** をクリックして以下を入力:

```
https://github.com/toguchi/ShaderText.git?path=Packages/com.toguchi.shadertext
```

### 手動インストール

このリポジトリをクローンまたはダウンロードし、`Packages/com.toguchi.shadertext` フォルダをプロジェクトの `Packages/` ディレクトリにコピーしてください。

## クイックスタート

1. シーンに **Canvas** を作成します（まだない場合）。
2. Canvas の下に空の **GameObject** を追加します。
3. **Canvas Renderer** コンポーネントを追加します。
4. **Shader Text Renderer** コンポーネントを追加します（`Add Component > UI > Shader Text Renderer`）。
5. Inspector で **Text** フィールドを設定します。

> **注意:** `ShaderTextRenderer` が正しく描画されるには、同じ GameObject に `CanvasRenderer` コンポーネントが必要です。

### スクリプトから使用

```csharp
using ShaderText;

gameObject.AddComponent<CanvasRenderer>();
var renderer = gameObject.AddComponent<ShaderTextRenderer>();
renderer.MaxCharacters = 16;
renderer.Text = "FPS:60 16.7MS";

// テキスト更新は非常に軽量 — メッシュの再構築は発生しません
renderer.Text = "FPS:120 8.3MS";
```

## API リファレンス

### ShaderTextRenderer

`UnityEngine.UI.MaskableGraphic` を継承しています。

| プロパティ | 型 | デフォルト | 説明 |
|-----------|------|---------|------|
| `Text` | `string` | `""` | 表示テキスト。変更時は GPU バッファのみ更新されます。 |
| `MaxCharacters` | `int` | `16` | 最大文字数。バッファサイズを決定します。 |
| `CharacterWidth` | `float` | `14` | 各文字の幅（ピクセル単位）。 |
| `CharacterHeight` | `float` | `24` | 各文字の高さ（ピクセル単位）。 |
| `CharacterSpacing` | `float` | `2` | 文字間の水平方向の間隔（ピクセル単位）。 |

| メソッド | 戻り値 | 説明 |
|---------|--------|------|
| `SetChar(int index, char c)` | `void` | 指定スロットに1文字をセット。GPU にはアップロードされません。 |
| `ClearChars(int startIndex = 0)` | `void` | `startIndex` 以降のスロットを空白で埋めます。 |
| `Apply()` | `void` | バッファの変更を GPU にアップロードします。 |
| `WriteInt(int value, int startIndex = 0)` | `int` | 文字列アロケーションなしで整数を書き込みます。書き込んだ文字数を返します。 |
| `WriteFloat(float value, int decimals, int startIndex = 0)` | `int` | 文字列アロケーションなしで浮動小数点数を書き込みます。書き込んだ文字数を返します。 |
| `WriteString(string text, int startIndex = 0)` | `int` | 文字列の各文字を書き込みます。書き込んだ文字数を返します。 |

標準の `MaskableGraphic` プロパティ（`color`、`raycastTarget` など）も使用できます。

## Zero-GC での使用

`Text` プロパティは `string` を受け取るため、毎フレーム `ToString()` や文字列補間を使用すると GC アロケーションが発生します。パフォーマンスが重要なシナリオ（例：毎フレーム更新される FPS カウンター）では、Zero-GC API を使用してください:

```csharp
// ❌ 毎回文字列をアロケーション（GC 負荷あり）
renderer.Text = $"FPS:{fps} {ms:F1}MS";

// ✅ Zero-GC — GPU バッファに直接書き込み
int pos = 0;
pos += renderer.WriteString("FPS:", pos);
pos += renderer.WriteInt(fpsValue, pos);
pos += renderer.WriteString(" ", pos);
pos += renderer.WriteFloat(msValue, 1, pos);
pos += renderer.WriteString("MS", pos);
renderer.ClearChars(pos);   // 残りのスロットを空白に
renderer.Apply();            // GPU にアップロード
```

### パターン: SetChar + Apply

1文字ずつ更新する場合:

```csharp
renderer.SetChar(0, 'A');
renderer.SetChar(1, '1');
renderer.ClearChars(2);
renderer.Apply();
```

### パターン: WriteInt + WriteFloat

`WriteInt` と `WriteFloat` は書き込んだ文字数を返すため、呼び出しを簡単にチェーンできます:

```csharp
int pos = 0;
pos += renderer.WriteInt(score, pos);      // 例: "12345"
pos += renderer.WriteString(" ", pos);     // セパレータ
pos += renderer.WriteFloat(time, 2, pos);  // 例: "3.14"
renderer.ClearChars(pos);
renderer.Apply();
```

## 仕組み

```
┌──────────────────────────────────────────────────────────┐
│  CPU (C#)                                                │
│  ┌─────────────┐    ┌──────────────────┐                 │
│  │ Text = "AB" │───>│ CharToIndex('A') │──> [10, 11]    │
│  └─────────────┘    │ CharToIndex('B') │                 │
│                     └──────────────────┘                 │
│                            │                             │
│                   GraphicsBuffer.SetData()               │
│                            │ (メッシュ再構築なし)          │
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
│  フォントデータ: 5×7 ビットマップ, 1グリフ35ビット (uint2) │
└──────────────────────────────────────────────────────────┘
```

1. **CPU 側** で各文字をグリフインデックスに変換し、配列を `GraphicsBuffer` に書き込みます。
2. **GPU 側** で文字スロットごとにインデックスを読み取り、ハードコードされた 5x7 ビットマップフォントからサンプリングします。
3. `fwidth` 由来のエッジしきい値を用いた `smoothstep` でアンチエイリアスが適用されます。

## サンプル

### Performance Counter

GC アロケーションゼロのリアルタイム FPS & フレームタイム表示。**Package Manager > Shader Text > Samples > Performance Counter** からインポートできます。

```csharp
// 出力例: "FPS:120 8.3MS"  (GC アロケーションゼロ)
int pos = 0;
pos += _renderer.WriteString("FPS:", pos);
pos += _renderer.WriteInt((int)fps, pos);
pos += _renderer.WriteString(" ", pos);
pos += _renderer.WriteFloat(ms, 1, pos);
pos += _renderer.WriteString("MS", pos);
_renderer.ClearChars(pos);
_renderer.Apply();
```

## ライセンス

このプロジェクトは [MIT License](LICENSE) の下で公開されています。
