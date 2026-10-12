using UnityEngine;

namespace SantaPlay.Piler
{
    [CreateAssetMenu(fileName = "PilerConfig", menuName = "SantaPlay/Piler/Config")]
    public class PilerConfig : ScriptableObject
    {
        [Header("Fall & Progression")]
        [SerializeField] private float _initialGravity = 0.8f;
        [SerializeField] private float _maxGravity = 2.0f;
        [SerializeField] private float _initialSpawnInterval = 2.0f;
        [SerializeField] private float _minSpawnInterval = 0.9f;

        [Header("Game Rules")]
        [SerializeField] private float _gameDuration = 60f;
        [SerializeField] private float _fallDeathY = -250f;
        [SerializeField] private float _toppleDeathY = -120f;

        [Header("Spawn Boundaries")]
        [SerializeField] private float _spawnPadding = 40f;
        [SerializeField] private float _fallbackSpawnWidth = 640f;

        [Header("Stick Movement")]
        [SerializeField] private float _minStickX = -380f;
        [SerializeField] private float _maxStickX = 380f;
        [SerializeField] private float _stickKeyboardSpeed = 800f;
        [SerializeField] private float _stickSmoothSpeed = 25f;

        [Header("Stick Sinking")]
        [Tooltip("Local Y height of stacked gifts on the stick that triggers sinking.")]
        [SerializeField] private float _sinkTriggerHeight = 250f;

        [Tooltip("Speed in units/sec at which the stick sinks.")]
        [SerializeField] private float _sinkSpeed = 40f;

        [Tooltip("Minimum anchored Y position the stick can sink to.")]
        [SerializeField] private float _minStickY = -450f;

        [Header("Display & Ads")]
        [SerializeField] private SantaPlay.Core.UI.TimerDisplayMode _timerDisplayMode = SantaPlay.Core.UI.TimerDisplayMode.CountDown;
        [SerializeField] private bool _enableVideoAds = true;

        public float SpawnPadding => _spawnPadding;
        public float FallbackSpawnWidth => _fallbackSpawnWidth;

        public float InitialGravity => _initialGravity;
        public float MaxGravity => _maxGravity;
        public float InitialSpawnInterval => _initialSpawnInterval;
        public float MinSpawnInterval => _minSpawnInterval;
        public float GameDuration => _gameDuration;
        public float FallDeathY => _fallDeathY;
        public float ToppleDeathY => _toppleDeathY;
        public float MinStickX => _minStickX;
        public float MaxStickX => _maxStickX;
        public float StickKeyboardSpeed => _stickKeyboardSpeed;
        public float StickSmoothSpeed => _stickSmoothSpeed;
        public float SinkTriggerHeight => _sinkTriggerHeight;
        public float SinkSpeed => _sinkSpeed;
        public float MinStickY => _minStickY;
        public SantaPlay.Core.UI.TimerDisplayMode TimerDisplayMode => _timerDisplayMode;
        public bool EnableVideoAds => _enableVideoAds;
    }
}
