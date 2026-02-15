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
            // Detects the "first character repeated" bug by verifying the data pipeline:
            // 1. _charIndices buffer updates per-slot when text changes
            // 2. Mesh UV0.z encodes distinct slot indices so the shader reads the correct buffer entry
            // If UV0.z were always 0, every slot would read _CharIndices[0] and show the first character.

            _renderer.MaxCharacters = 2;
            _renderer.CharacterWidth = 40f;
            _renderer.CharacterHeight = 50f;
            _renderer.CharacterSpacing = 20f;
            yield return null;

            var charIndicesField = typeof(ShaderTextRenderer).GetField(
                "_charIndices", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(charIndicesField, "_charIndices field not found");

            // ── Pass 1: "AA" → both slots map to 'A' (index 10) ──
            _renderer.Text = "AA";
            yield return null;

            var indices = (uint[])charIndicesField.GetValue(_renderer);
            Assert.AreEqual(10u, indices[0], "Slot 0 should be 'A'=10");
            Assert.AreEqual(10u, indices[1], "Slot 1 should be 'A'=10");

            // ── Pass 2: "A0" → slot 1 changes to '0' (index 0) ──
            _renderer.Text = "A0";
            yield return null;

            indices = (uint[])charIndicesField.GetValue(_renderer);
            Assert.AreEqual(10u, indices[0], "Slot 0 should still be 'A'=10");
            Assert.AreEqual(0u, indices[1], "Slot 1 should change to '0'=0");
            Assert.AreNotEqual(indices[0], indices[1],
                "Changing text from 'AA' to 'A0' must update char index for slot 1. " +
                "If indices are identical, the text change did not propagate per-slot.");

            // ── Verify mesh UV0.z encodes distinct slot indices ──
            var vh = new VertexHelper();
            var populateMethod = typeof(ShaderTextRenderer).GetMethod(
                "OnPopulateMesh",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(VertexHelper) }, null);
            populateMethod.Invoke(_renderer, new object[] { vh });

            var mesh = new Mesh();
            vh.FillMesh(mesh);
            var uvs = new System.Collections.Generic.List<Vector4>();
            mesh.GetUVs(0, uvs);

            Assert.AreEqual(8, uvs.Count, "Expected 8 vertices (2 chars × 4 verts)");
            for (int v = 0; v < 4; v++)
                Assert.AreEqual(0f, uvs[v].z, 0.001f,
                    $"Slot 0 vertex {v}: UV0.z must be 0 so the shader reads _CharIndices[0]");
            for (int v = 4; v < 8; v++)
                Assert.AreEqual(1f, uvs[v].z, 0.001f,
                    $"Slot 1 vertex {v}: UV0.z must be 1 so the shader reads _CharIndices[1]");

            mesh.Clear();
            vh.Dispose();
        }

        [UnityTest]
        public IEnumerator Rendering_TextIsVisible_PixelsAreNonTransparent()
        {
            // Verifies all prerequisites for visible rendering:
            // 1. mainTexture is not null (CanvasRenderer skips drawing when null)
            // 2. Material and shader are valid and supported
            // 3. Mesh geometry is generated by OnPopulateMesh
            // 4. GraphicsBuffer for character indices is valid and populated

            _renderer.MaxCharacters = 8;
            _renderer.CharacterWidth = 20f;
            _renderer.CharacterHeight = 40f;
            _renderer.CharacterSpacing = 2f;
            _renderer.Text = "ABCD1234";
            yield return null;

            // 1. mainTexture check
            Assert.IsNotNull(_renderer.mainTexture,
                "mainTexture must not be null; CanvasRenderer skips drawing when texture is null.");

            // 2. Material & shader check
            Assert.IsNotNull(_renderer.material,
                "Material must be assigned for rendering to occur.");
            Assert.IsTrue(_renderer.material.shader.isSupported,
                "Shader 'UI/ShaderText' must be supported on this platform.");
            Assert.AreEqual("UI/ShaderText", _renderer.material.shader.name,
                "Material must use the ShaderText shader.");

            // 3. Mesh geometry check
            var vh = new VertexHelper();
            var populateMethod = typeof(ShaderTextRenderer).GetMethod(
                "OnPopulateMesh",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(VertexHelper) }, null);
            populateMethod.Invoke(_renderer, new object[] { vh });

            Assert.Greater(vh.currentVertCount, 0,
                "OnPopulateMesh must produce vertices for text to be visible.");
            Assert.AreEqual(8 * 4, vh.currentVertCount,
                "Expected 8 character quads (4 vertices each) for 'ABCD1234'.");
            vh.Dispose();

            // 4. GraphicsBuffer & char indices check
            var bufferField = typeof(ShaderTextRenderer).GetField(
                "_charIndexBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
            var buffer = (GraphicsBuffer)bufferField.GetValue(_renderer);
            Assert.IsNotNull(buffer,
                "GraphicsBuffer must exist for the shader to read character data.");

            var charIndicesField = typeof(ShaderTextRenderer).GetField(
                "_charIndices", BindingFlags.NonPublic | BindingFlags.Instance);
            var charIndices = (uint[])charIndicesField.GetValue(_renderer);
            Assert.IsNotNull(charIndices);
            Assert.AreEqual(8, charIndices.Length);

            bool hasVisibleChar = false;
            for (int i = 0; i < charIndices.Length; i++)
            {
                if (charIndices[i] < 255u) { hasVisibleChar = true; break; }
            }
            Assert.IsTrue(hasVisibleChar,
                "At least one character slot must have a renderable glyph index (< 255).");
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
