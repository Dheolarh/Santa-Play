using System;
using UnityEngine;

namespace SantaPlay.Ads
{
    [CreateAssetMenu(fileName = "VideoAdConfig", menuName = "SantaPlay/Ads/Video Config")]
    public class VideoAdConfig : ScriptableObject
    {
        [Header("Ad Behavior")]
        [Tooltip("Seconds before the skip button is enabled. 0 or negative = unskippable.")]
        [SerializeField] private float _skipDelaySeconds = 5f;

        [Header("Video Render Resolution")]
        [SerializeField] private int _renderTextureWidth = 1280;
        [SerializeField] private int _renderTextureHeight = 720;

        [Header("Fallback Video URLs")]
        [SerializeField] private string[] _fallbackUrls = Array.Empty<string>();

        public float SkipDelaySeconds => _skipDelaySeconds;
        public int RenderTextureWidth => _renderTextureWidth;
        public int RenderTextureHeight => _renderTextureHeight;
        public string[] FallbackUrls => _fallbackUrls;
    }
}
