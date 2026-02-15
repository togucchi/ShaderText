using UnityEngine;
using UnityEngine.UI;

namespace ShaderText
{
    [AddComponentMenu("UI/Shader Text Renderer")]
    [ExecuteAlways]
    public class ShaderTextRenderer : MaskableGraphic
    {
        private static readonly string ShaderName = "UI/ShaderText";
        private static readonly int CharIndicesProp = Shader.PropertyToID("_CharIndices");

        [SerializeField] private int _maxCharacters = 16;
        [SerializeField] private float _characterWidth = 14f;
        [SerializeField] private float _characterHeight = 24f;
        [SerializeField] private float _characterSpacing = 2f;
        [SerializeField] private string _text = "";

        private Material _runtimeMaterial;
        private GraphicsBuffer _charIndexBuffer;
        private uint[] _charIndices;
        private string _appliedText;
        private bool _bufferDirty;

        public string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                ApplyCharData();
            }
        }

        public int MaxCharacters
        {
            get => _maxCharacters;
            set
            {
                value = Mathf.Max(1, value);
                if (_maxCharacters == value) return;
                _maxCharacters = value;
                RebuildBuffer();
                SetVerticesDirty();
            }
        }

        public float CharacterWidth
        {
            get => _characterWidth;
            set
            {
                if (Mathf.Approximately(_characterWidth, value)) return;
                _characterWidth = value;
                SetVerticesDirty();
            }
        }

        public float CharacterHeight
        {
            get => _characterHeight;
            set
            {
                if (Mathf.Approximately(_characterHeight, value)) return;
                _characterHeight = value;
                SetVerticesDirty();
            }
        }

        public float CharacterSpacing
        {
            get => _characterSpacing;
            set
            {
                if (Mathf.Approximately(_characterSpacing, value)) return;
                _characterSpacing = value;
                SetVerticesDirty();
            }
        }

        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void Awake()
        {
            base.Awake();
            EnsureInitialized();
        }

        protected override void OnEnable()
        {
            EnsureInitialized();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseBuffer();
            if (_runtimeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_runtimeMaterial);
                else
                    DestroyImmediate(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            _maxCharacters = Mathf.Max(1, _maxCharacters);
            _characterWidth = Mathf.Max(1f, _characterWidth);
            _characterHeight = Mathf.Max(1f, _characterHeight);
            _characterSpacing = Mathf.Max(0f, _characterSpacing);

            if (isActiveAndEnabled)
            {
                EnsureInitialized();
                SetVerticesDirty();
            }

            base.OnValidate();
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            float advance = _characterWidth + _characterSpacing;

            for (int i = 0; i < _maxCharacters; i++)
            {
                float x0 = rect.xMin + i * advance;
                float x1 = x0 + _characterWidth;
                float y0 = rect.yMin;
                float y1 = y0 + _characterHeight;

                // Stop if exceeding rect width
                if (x1 > rect.xMax + 0.5f)
                    break;

                int vi = vh.currentVertCount;

                // UV0.xy = glyph-local coords (0-1), UV0.z = slot index
                vh.AddVert(new Vector3(x0, y0), color, new Vector4(0, 0, i, 0));
                vh.AddVert(new Vector3(x0, y1), color, new Vector4(0, 1, i, 0));
                vh.AddVert(new Vector3(x1, y1), color, new Vector4(1, 1, i, 0));
                vh.AddVert(new Vector3(x1, y0), color, new Vector4(1, 0, i, 0));

                vh.AddTriangle(vi, vi + 1, vi + 2);
                vh.AddTriangle(vi, vi + 2, vi + 3);
            }
        }

        private void EnsureInitialized()
        {
            if (_runtimeMaterial == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    Debug.LogError($"[ShaderText] Shader '{ShaderName}' not found.");
                    return;
                }
                _runtimeMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                material = _runtimeMaterial;
            }

            if (_charIndexBuffer == null || _charIndices == null || _charIndices.Length != _maxCharacters)
            {
                RebuildBuffer();
            }

            ApplyCharData();
        }

        private void RebuildBuffer()
        {
            ReleaseBuffer();

            _charIndices = new uint[_maxCharacters];
            for (int i = 0; i < _maxCharacters; i++)
                _charIndices[i] = 255u;

            _charIndexBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                _maxCharacters,
                sizeof(uint)
            );
            _charIndexBuffer.SetData(_charIndices);

            if (_runtimeMaterial != null)
                _runtimeMaterial.SetBuffer(CharIndicesProp, _charIndexBuffer);

            _appliedText = null;
        }

        private void ReleaseBuffer()
        {
            if (_charIndexBuffer != null)
            {
                _charIndexBuffer.Release();
                _charIndexBuffer = null;
            }
        }

        private void ApplyCharData()
        {
            if (_charIndices == null || _charIndexBuffer == null)
                return;

            if (_appliedText == _text)
                return;

            _appliedText = _text;

            int len = _text != null ? Mathf.Min(_text.Length, _maxCharacters) : 0;
            for (int i = 0; i < len; i++)
                _charIndices[i] = CharToIndex(_text[i]);
            for (int i = len; i < _maxCharacters; i++)
                _charIndices[i] = 255u;

            _charIndexBuffer.SetData(_charIndices);
        }

        private static uint CharToIndex(char c)
        {
            if (c >= '0' && c <= '9') return (uint)(c - '0');
            if (c >= 'A' && c <= 'Z') return (uint)(c - 'A' + 10);
            if (c >= 'a' && c <= 'z') return (uint)(c - 'a' + 10);
            switch (c)
            {
                case ' ': return 36u;
                case ':': return 37u;
                case '.': return 38u;
                case '-': return 39u;
                default:  return 255u;
            }
        }

        // ── Zero-GC API ─────────────────────────────────────

        /// <summary>
        /// Set a single character at the given slot. Does not upload to GPU.
        /// </summary>
        public void SetChar(int index, char c)
        {
            if (_charIndices == null || (uint)index >= (uint)_charIndices.Length)
                return;
            _charIndices[index] = CharToIndex(c);
            _bufferDirty = true;
        }

        /// <summary>
        /// Fill slots from <paramref name="startIndex"/> onward with blank (255).
        /// </summary>
        public void ClearChars(int startIndex = 0)
        {
            if (_charIndices == null) return;
            for (int i = Mathf.Max(0, startIndex); i < _charIndices.Length; i++)
                _charIndices[i] = 255u;
            _bufferDirty = true;
        }

        /// <summary>
        /// Upload buffer changes to the GPU.
        /// </summary>
        public void Apply()
        {
            if (!_bufferDirty || _charIndices == null || _charIndexBuffer == null)
                return;
            _charIndexBuffer.SetData(_charIndices);
            _bufferDirty = false;
            _appliedText = null;
        }

        /// <summary>
        /// Write an integer without string allocation. Returns the number of characters written.
        /// </summary>
        public int WriteInt(int value, int startIndex = 0)
        {
            if (_charIndices == null) return 0;

            int pos = startIndex;
            int len = _charIndices.Length;

            if (value < 0)
            {
                if ((uint)pos < (uint)len)
                    _charIndices[pos] = CharToIndex('-');
                pos++;
                // Handle int.MinValue: -(int.MinValue) overflows, use long
                long abs = -(long)value;
                pos = WritePositiveLong(abs, pos, len);
            }
            else if (value == 0)
            {
                if ((uint)pos < (uint)len)
                    _charIndices[pos] = CharToIndex('0');
                pos++;
            }
            else
            {
                pos = WritePositiveLong(value, pos, len);
            }

            _bufferDirty = true;
            return pos - startIndex;
        }

        /// <summary>
        /// Write a float without string allocation. Returns the number of characters written.
        /// </summary>
        public int WriteFloat(float value, int decimals, int startIndex = 0)
        {
            if (_charIndices == null) return 0;

            int pos = startIndex;
            int len = _charIndices.Length;

            if (value < 0f)
            {
                if ((uint)pos < (uint)len)
                    _charIndices[pos] = CharToIndex('-');
                pos++;
                value = -value;
            }

            // Multiply to get integer representation with desired decimal places
            long multiplier = 1;
            for (int d = 0; d < decimals; d++)
                multiplier *= 10;

            long scaled = (long)(value * multiplier + 0.5f);
            long intPart = scaled / multiplier;
            long fracPart = scaled % multiplier;

            // Write integer part
            if (intPart == 0)
            {
                if ((uint)pos < (uint)len)
                    _charIndices[pos] = CharToIndex('0');
                pos++;
            }
            else
            {
                pos = WritePositiveLong(intPart, pos, len);
            }

            // Write decimal point and fractional part
            if (decimals > 0)
            {
                if ((uint)pos < (uint)len)
                    _charIndices[pos] = CharToIndex('.');
                pos++;

                // Write fractional digits with leading zeros
                for (int d = decimals - 1; d >= 0; d--)
                {
                    long divisor = 1;
                    for (int p = 0; p < d; p++)
                        divisor *= 10;
                    long digit = (fracPart / divisor) % 10;
                    if ((uint)pos < (uint)len)
                        _charIndices[pos] = (uint)(digit);
                    pos++;
                }
            }

            _bufferDirty = true;
            return pos - startIndex;
        }

        /// <summary>
        /// Write each character of a string. Returns the number of characters written.
        /// </summary>
        public int WriteString(string text, int startIndex = 0)
        {
            if (_charIndices == null || text == null) return 0;

            int len = _charIndices.Length;
            int count = text.Length;
            for (int i = 0; i < count; i++)
            {
                int idx = startIndex + i;
                if ((uint)idx >= (uint)len) break;
                _charIndices[idx] = CharToIndex(text[i]);
            }

            _bufferDirty = true;
            return count;
        }

        private int WritePositiveLong(long value, int pos, int bufferLen)
        {
            // Count digits
            int digitCount = 0;
            long tmp = value;
            while (tmp > 0)
            {
                digitCount++;
                tmp /= 10;
            }

            // Write digits in reverse order
            int endPos = pos + digitCount;
            for (int i = endPos - 1; i >= pos; i--)
            {
                if ((uint)i < (uint)bufferLen)
                    _charIndices[i] = (uint)(value % 10);
                value /= 10;
            }

            return endPos;
        }
    }
}
