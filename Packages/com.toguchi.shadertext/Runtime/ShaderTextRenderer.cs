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
    }
}
