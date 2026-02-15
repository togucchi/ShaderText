using UnityEngine;
using ShaderText;

namespace ShaderText.Samples
{
    /// <summary>
    /// FPS and render time counter using ShaderTextRenderer.
    /// Attach to a GameObject that has a ShaderTextRenderer component.
    /// </summary>
    [RequireComponent(typeof(ShaderTextRenderer))]
    public class PerformanceCounter : MonoBehaviour
    {
        [SerializeField] private float _updateInterval = 0.25f;

        private ShaderTextRenderer _renderer;
        private float _elapsed;
        private int _frameCount;
        private float _lastFps;

        private void Awake()
        {
            _renderer = GetComponent<ShaderTextRenderer>();
            _renderer.MaxCharacters = 16;
        }

        private void Update()
        {
            _frameCount++;
            _elapsed += Time.unscaledDeltaTime;

            if (_elapsed >= _updateInterval)
            {
                _lastFps = _frameCount / _elapsed;
                _frameCount = 0;
                _elapsed = 0f;

                float ms = 1000f / Mathf.Max(_lastFps, 0.001f);

                // Zero-GC text update — no string allocation
                // Example output: "FPS:120 8.3MS"
                int pos = 0;
                pos += _renderer.WriteString("FPS:", pos);
                pos += _renderer.WriteInt((int)_lastFps, pos);
                pos += _renderer.WriteString(" ", pos);
                pos += _renderer.WriteFloat(ms, 1, pos);
                pos += _renderer.WriteString("MS", pos);
                _renderer.ClearChars(pos);
                _renderer.Apply();
            }
        }
    }
}
