using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ShaderText.Tests
{
    [TestFixture]
    public class ShaderTextRendererTests
    {
        private GameObject _canvasGo;
        private GameObject _textGo;
        private ShaderTextRenderer _renderer;

        private static readonly MethodInfo CharToIndexMethod =
            typeof(ShaderTextRenderer).GetMethod("CharToIndex", BindingFlags.NonPublic | BindingFlags.Static);

        [SetUp]
        public void Setup()
        {
            _canvasGo = new GameObject("TestCanvas");
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasGo.AddComponent<CanvasScaler>();

            _textGo = new GameObject("ShaderText");
            _textGo.transform.SetParent(_canvasGo.transform);
            var rt = _textGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 100f);

            _renderer = _textGo.AddComponent<ShaderTextRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_textGo != null) Object.DestroyImmediate(_textGo);
            if (_canvasGo != null) Object.DestroyImmediate(_canvasGo);
        }

        // ── Property Tests ──────────────────────────────────

        [Test]
        public void Text_SetAndGet_ReturnsAssignedValue()
        {
            _renderer.Text = "HELLO";
            Assert.AreEqual("HELLO", _renderer.Text);
        }

        [Test]
        public void Text_SameValue_DoesNotReapply()
        {
            _renderer.Text = "ABC";
            _renderer.Text = "ABC";
            Assert.AreEqual("ABC", _renderer.Text);
        }

        [Test]
        public void MaxCharacters_SetAndGet_ReturnsAssignedValue()
        {
            _renderer.MaxCharacters = 32;
            Assert.AreEqual(32, _renderer.MaxCharacters);
        }

        [Test]
        public void MaxCharacters_BelowMinimum_ClampedToOne()
        {
            _renderer.MaxCharacters = 0;
            Assert.AreEqual(1, _renderer.MaxCharacters);

            _renderer.MaxCharacters = -10;
            Assert.AreEqual(1, _renderer.MaxCharacters);
        }

        [Test]
        public void CharacterWidth_SetAndGet_ReturnsAssignedValue()
        {
            _renderer.CharacterWidth = 20f;
            Assert.AreEqual(20f, _renderer.CharacterWidth, 0.001f);
        }

        [Test]
        public void CharacterHeight_SetAndGet_ReturnsAssignedValue()
        {
            _renderer.CharacterHeight = 30f;
            Assert.AreEqual(30f, _renderer.CharacterHeight, 0.001f);
        }

        [Test]
        public void CharacterSpacing_SetAndGet_ReturnsAssignedValue()
        {
            _renderer.CharacterSpacing = 5f;
            Assert.AreEqual(5f, _renderer.CharacterSpacing, 0.001f);
        }

        [Test]
        public void MainTexture_ReturnsNonNull()
        {
            Assert.IsNotNull(_renderer.mainTexture,
                "mainTexture must not be null; CanvasRenderer skips drawing when texture is null");
        }

        // ── CharToIndex Tests (via Reflection) ─────────────

        private static uint InvokeCharToIndex(char c)
        {
            return (uint)CharToIndexMethod.Invoke(null, new object[] { c });
        }

        [TestCase('0', 0u)]
        [TestCase('5', 5u)]
        [TestCase('9', 9u)]
        public void CharToIndex_Digits_ReturnCorrectIndex(char c, uint expected)
        {
            Assert.AreEqual(expected, InvokeCharToIndex(c));
        }

        [TestCase('A', 10u)]
        [TestCase('M', 22u)]
        [TestCase('Z', 35u)]
        public void CharToIndex_UpperCase_ReturnCorrectIndex(char c, uint expected)
        {
            Assert.AreEqual(expected, InvokeCharToIndex(c));
        }

        [TestCase('a', 10u)]
        [TestCase('m', 22u)]
        [TestCase('z', 35u)]
        public void CharToIndex_LowerCase_MapsToUpperCase(char c, uint expected)
        {
            Assert.AreEqual(expected, InvokeCharToIndex(c));
        }

        [TestCase(' ', 36u)]
        [TestCase(':', 37u)]
        [TestCase('.', 38u)]
        [TestCase('-', 39u)]
        public void CharToIndex_SpecialChars_ReturnCorrectIndex(char c, uint expected)
        {
            Assert.AreEqual(expected, InvokeCharToIndex(c));
        }

        [TestCase('!')]
        [TestCase('@')]
        [TestCase('#')]
        [TestCase('$')]
        public void CharToIndex_UnsupportedChars_Returns255(char c)
        {
            Assert.AreEqual(255u, InvokeCharToIndex(c));
        }

        // ── Lifecycle Tests ─────────────────────────────────

        [UnityTest]
        public IEnumerator Component_EnableDisableEnable_DoesNotThrow()
        {
            _renderer.Text = "TEST";
            yield return null;

            _renderer.enabled = false;
            yield return null;

            Assert.DoesNotThrow(() => _renderer.enabled = true);
            yield return null;

            Assert.AreEqual("TEST", _renderer.Text);
        }

        [UnityTest]
        public IEnumerator Text_UpdatedAfterEnable_AppliesCorrectly()
        {
            _renderer.enabled = false;
            yield return null;

            _renderer.enabled = true;
            yield return null;

            _renderer.Text = "UPDATED";
            yield return null;

            Assert.AreEqual("UPDATED", _renderer.Text);
        }

        // ── Mesh Generation Tests ───────────────────────────

        [UnityTest]
        public IEnumerator OnPopulateMesh_GeneratesCorrectVertexCount()
        {
            _renderer.MaxCharacters = 4;
            _renderer.CharacterWidth = 14f;
            _renderer.CharacterSpacing = 2f;
            yield return null;

            var mesh = new Mesh();
            var canvasRenderer = _textGo.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null && canvasRenderer.materialCount > 0)
            {
                // MaskableGraphic populates the mesh via CanvasRenderer
                // We test via VertexHelper directly
            }

            // Use VertexHelper to test OnPopulateMesh directly
            var vh = new VertexHelper();
            var populateMethod = typeof(ShaderTextRenderer).GetMethod(
                "OnPopulateMesh",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(VertexHelper) },
                null);

            Assert.IsNotNull(populateMethod, "OnPopulateMesh method not found");
            populateMethod.Invoke(_renderer, new object[] { vh });

            // 4 vertices per character quad
            Assert.AreEqual(4 * 4, vh.currentVertCount, "Expected 4 quads with 4 vertices each");

            vh.Dispose();
        }

        [UnityTest]
        public IEnumerator OnPopulateMesh_ExceedingRectWidth_StopsGenerating()
        {
            var rt = _textGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50f, 100f);

            _renderer.MaxCharacters = 10;
            _renderer.CharacterWidth = 14f;
            _renderer.CharacterSpacing = 2f;
            yield return null;

            var vh = new VertexHelper();
            var populateMethod = typeof(ShaderTextRenderer).GetMethod(
                "OnPopulateMesh",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(VertexHelper) },
                null);

            populateMethod.Invoke(_renderer, new object[] { vh });

            // advance = 14 + 2 = 16, rect width = 50
            // char0: x0=0, x1=14  -> fits (14 <= 50.5)
            // char1: x0=16, x1=30 -> fits (30 <= 50.5)
            // char2: x0=32, x1=46 -> fits (46 <= 50.5)
            // char3: x0=48, x1=62 -> does NOT fit (62 > 50.5)
            Assert.AreEqual(3 * 4, vh.currentVertCount, "Expected 3 quads fitting within 50px width");

            vh.Dispose();
        }

        // ── Buffer Rebuild Tests ────────────────────────────

        [UnityTest]
        public IEnumerator MaxCharacters_Changed_RebuildBuffer()
        {
            _renderer.MaxCharacters = 8;
            _renderer.Text = "ABCD";
            yield return null;

            _renderer.MaxCharacters = 16;
            yield return null;

            // Buffer should have been rebuilt; text should still be accessible
            Assert.AreEqual(16, _renderer.MaxCharacters);
            Assert.AreEqual("ABCD", _renderer.Text);
        }

        [UnityTest]
        public IEnumerator Text_LongerThanMaxCharacters_IsTruncated()
        {
            _renderer.MaxCharacters = 4;
            yield return null;

            _renderer.Text = "ABCDEFGH";
            yield return null;

            // Text property stores full string but only first 4 chars are applied to buffer
            Assert.AreEqual("ABCDEFGH", _renderer.Text);

            // Verify via internal buffer using reflection
            var charIndicesField = typeof(ShaderTextRenderer).GetField(
                "_charIndices", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(charIndicesField);

            var charIndices = (uint[])charIndicesField.GetValue(_renderer);
            Assert.IsNotNull(charIndices);
            Assert.AreEqual(4, charIndices.Length);

            // A=10, B=11, C=12, D=13
            Assert.AreEqual(10u, charIndices[0], "Index 0 should be 'A'=10");
            Assert.AreEqual(11u, charIndices[1], "Index 1 should be 'B'=11");
            Assert.AreEqual(12u, charIndices[2], "Index 2 should be 'C'=12");
            Assert.AreEqual(13u, charIndices[3], "Index 3 should be 'D'=13");
        }

        [UnityTest]
        public IEnumerator Text_ShorterThanMax_PadsWithBlank()
        {
            _renderer.MaxCharacters = 8;
            yield return null;

            _renderer.Text = "AB";
            yield return null;

            var charIndicesField = typeof(ShaderTextRenderer).GetField(
                "_charIndices", BindingFlags.NonPublic | BindingFlags.Instance);
            var charIndices = (uint[])charIndicesField.GetValue(_renderer);
            Assert.IsNotNull(charIndices);

            // A=10, B=11
            Assert.AreEqual(10u, charIndices[0]);
            Assert.AreEqual(11u, charIndices[1]);
            // Remaining should be 255 (blank)
            for (int i = 2; i < 8; i++)
            {
                Assert.AreEqual(255u, charIndices[i], $"Index {i} should be 255 (blank)");
            }
        }

        [UnityTest]
        public IEnumerator Text_EmptyString_AllBlank()
        {
            _renderer.MaxCharacters = 4;
            yield return null;

            _renderer.Text = "";
            yield return null;

            var charIndicesField = typeof(ShaderTextRenderer).GetField(
                "_charIndices", BindingFlags.NonPublic | BindingFlags.Instance);
            var charIndices = (uint[])charIndicesField.GetValue(_renderer);
            Assert.IsNotNull(charIndices);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(255u, charIndices[i], $"Index {i} should be 255 (blank)");
            }
        }

        // ── Mesh UV1 Slot Index Tests ────────────────────────

        [Test]
        public void OnPopulateMesh_UV0zContainsCorrectSlotIndex()
        {
            _renderer.MaxCharacters = 4;
            _renderer.CharacterWidth = 14f;
            _renderer.CharacterSpacing = 2f;

            var vh = new VertexHelper();
            var populateMethod = typeof(ShaderTextRenderer).GetMethod(
                "OnPopulateMesh",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(VertexHelper) },
                null);

            populateMethod.Invoke(_renderer, new object[] { vh });

            var mesh = new Mesh();
            vh.FillMesh(mesh);

            var uvs = new System.Collections.Generic.List<Vector4>();
            mesh.GetUVs(0, uvs);

            Assert.AreEqual(16, uvs.Count, "Expected 16 vertices (4 chars x 4 verts)");

            // Each character quad's 4 vertices should have UV0.z == slot index
            for (int slot = 0; slot < 4; slot++)
            {
                for (int v = 0; v < 4; v++)
                {
                    int vertIndex = slot * 4 + v;
                    Assert.AreEqual((float)slot, uvs[vertIndex].z, 0.001f,
                        $"Vertex {vertIndex} (slot {slot}, vert {v}): UV0.z should be {slot} " +
                        "so the shader can look up the correct character from _CharIndices buffer");
                }
            }

            mesh.Clear();
            vh.Dispose();
        }

        // ── Slot Index Channel Tests ─────────────────────────

        // ── Rendering Tests ──────────────────────────────────

        [UnityTest]
        public IEnumerator Rendering_SecondSlotReflectsTextChange()
        {
            // This test detects the "first character repeated" bug:
            // If UV1 (slot index) is not passed to the shader, every slot reads
            // _CharIndices[0], so changing only the 2nd character has no visual effect.
            //
            // Approach: render "AA" then "A0". Compare full-frame pixel hashes.
            // With working UV1: hashes differ (slot 1 switches from 'A' to '0').
            // With broken UV1: hashes are identical (both look like "AA").

            // Clean up the shared Setup objects — this test uses its own hierarchy
            Object.DestroyImmediate(_textGo);
            Object.DestroyImmediate(_canvasGo);
            _textGo = null;
            _canvasGo = null;

            const int rtWidth = 256;
            const int rtHeight = 64;
            var rt = new RenderTexture(rtWidth, rtHeight, 24);
            rt.Create();

            // Camera
            var camGo = new GameObject("TestCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = rtHeight * 0.5f;
            cam.nearClipPlane = -1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.targetTexture = rt;

            // Canvas (ScreenSpace - Camera)
            _canvasGo = new GameObject("RenderTestCanvas");
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // ShaderTextRenderer
            _textGo = new GameObject("RenderTestText");
            _textGo.transform.SetParent(_canvasGo.transform, false);
            var rectTransform = _textGo.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(200f, 60f);

            var renderer = _textGo.AddComponent<ShaderTextRenderer>();
            renderer.color = Color.white;
            renderer.MaxCharacters = 2;
            renderer.CharacterWidth = 40f;
            renderer.CharacterHeight = 50f;
            renderer.CharacterSpacing = 20f;

            // ── Pass 1: render "AA" ──
            renderer.Text = "AA";
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < 3; i++) yield return null;
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rtWidth, rtHeight, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var pixelsAA = tex.GetPixels32();
            long hashAA = 0;
            for (int i = 0; i < pixelsAA.Length; i++)
                hashAA += pixelsAA[i].a * (long)(i + 1);

            // ── Pass 2: render "A0" (only slot 1 changes) ──
            renderer.Text = "A0";
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < 3; i++) yield return null;
            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var pixelsA0 = tex.GetPixels32();
            long hashA0 = 0;
            for (int i = 0; i < pixelsA0.Length; i++)
                hashA0 += pixelsA0[i].a * (long)(i + 1);

            // Cleanup
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);

            Assert.AreNotEqual(hashAA, hashA0,
                "Changing text from 'AA' to 'A0' must change the rendered output. " +
                "If the output is identical, the slot index (UV1.x) is always 0 — " +
                "every slot reads _CharIndices[0] and shows the first character only.");
        }

        [UnityTest]
        public IEnumerator Rendering_TextIsVisible_PixelsAreNonTransparent()
        {
            // Clean up the shared Setup objects — this test uses its own hierarchy
            Object.DestroyImmediate(_textGo);
            Object.DestroyImmediate(_canvasGo);
            _textGo = null;
            _canvasGo = null;

            const int rtWidth = 256;
            const int rtHeight = 128;
            var rt = new RenderTexture(rtWidth, rtHeight, 24);
            rt.Create();

            // Camera
            var camGo = new GameObject("TestCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = rtHeight * 0.5f;
            cam.nearClipPlane = -1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.targetTexture = rt;

            // Canvas (ScreenSpace - Camera)
            _canvasGo = new GameObject("RenderTestCanvas");
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // ShaderTextRenderer
            _textGo = new GameObject("RenderTestText");
            _textGo.transform.SetParent(_canvasGo.transform, false);
            var rectTransform = _textGo.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var renderer = _textGo.AddComponent<ShaderTextRenderer>();
            renderer.color = Color.white;
            renderer.MaxCharacters = 8;
            renderer.CharacterWidth = 20f;
            renderer.CharacterHeight = 40f;
            renderer.CharacterSpacing = 2f;
            renderer.Text = "ABCD1234";

            // Wait several frames for Canvas rebuild + rendering
            for (int i = 0; i < 4; i++)
                yield return null;

            // Force a camera render
            cam.Render();

            // Read back pixels
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rtWidth, rtHeight, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            // Count non-transparent pixels
            var pixels = tex.GetPixels32();
            int nonTransparentCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 10)
                    nonTransparentCount++;
            }

            // Cleanup
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);

            Assert.Greater(nonTransparentCount, 0,
                "ShaderTextRenderer should render visible pixels. " +
                "If mainTexture returns null, CanvasRenderer skips drawing entirely.");
        }

        // ── Mixed Character Tests ───────────────────────────

        [UnityTest]
        public IEnumerator Text_MixedContent_CorrectIndices()
        {
            _renderer.MaxCharacters = 16;
            yield return null;

            _renderer.Text = "HP:100 A-Z.";
            yield return null;

            var charIndicesField = typeof(ShaderTextRenderer).GetField(
                "_charIndices", BindingFlags.NonPublic | BindingFlags.Instance);
            var charIndices = (uint[])charIndicesField.GetValue(_renderer);

            // H=17, P=25, :=37, 1=1, 0=0, 0=0, space=36, A=10, -=39, Z=35, .=38
            Assert.AreEqual(17u, charIndices[0],  "H");
            Assert.AreEqual(25u, charIndices[1],  "P");
            Assert.AreEqual(37u, charIndices[2],  ":");
            Assert.AreEqual(1u,  charIndices[3],  "1");
            Assert.AreEqual(0u,  charIndices[4],  "0");
            Assert.AreEqual(0u,  charIndices[5],  "0");
            Assert.AreEqual(36u, charIndices[6],  "space");
            Assert.AreEqual(10u, charIndices[7],  "A");
            Assert.AreEqual(39u, charIndices[8],  "-");
            Assert.AreEqual(35u, charIndices[9],  "Z");
            Assert.AreEqual(38u, charIndices[10], ".");
        }

        [UnityTest]
        public IEnumerator Text_LowerCaseInput_MapsToUpperCaseIndices()
        {
            _renderer.MaxCharacters = 4;
            yield return null;

            _renderer.Text = "aBcD";
            yield return null;

            var charIndicesField = typeof(ShaderTextRenderer).GetField(
                "_charIndices", BindingFlags.NonPublic | BindingFlags.Instance);
            var charIndices = (uint[])charIndicesField.GetValue(_renderer);

            // a->A=10, b->B=11, c->C=12, d->D=13
            Assert.AreEqual(10u, charIndices[0]);
            Assert.AreEqual(11u, charIndices[1]);
            Assert.AreEqual(12u, charIndices[2]);
            Assert.AreEqual(13u, charIndices[3]);
        }
    }
}
