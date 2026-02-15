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

                // Example output: "FPS:120  8.3MS"
                string fps = ((int)_lastFps).ToString();
                string msStr = ms.ToString("F1");
                _renderer.Text = $"FPS:{fps} {msStr}MS";
            }
        }
    }
}
