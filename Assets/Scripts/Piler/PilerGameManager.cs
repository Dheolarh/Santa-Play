using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SantaPlay.AssetLoading;
using SantaPlay.Core;
using SantaPlay.Core.UI;
using SantaPlay.Ads;

namespace SantaPlay.Piler
{
    public class PilerGameManager : MonoBehaviour
    {
        public static PilerGameManager Instance { get; private set; }

        [Header("Configuration & Systems")]
        [SerializeField] private PilerConfig _config;
        [SerializeField] private PilerStick _stick;
        [SerializeField] private PilerGiftPool _pool;
        [SerializeField] private RectTransform _spawnArea;
        [SerializeField] private Transform _giftsContainer;

        [Header("Scenes")]
        [SerializeField] private string _menuSceneName = "01 Menu";

        public event Action<float, float> OnTimerUpdated;
        public event Action<int, int> OnScoreUpdated;
        public event Action<int, int> OnGameEnded;

        private bool _isPlaying;
        private readonly GameTimer _timer = new();
        private int _currentScore;
        private readonly List<PilerGift> _stackedGifts = new(64);
        private Coroutine _spawnRoutine;

        public bool IsPlaying => _isPlaying;
        public int CurrentScore => _currentScore;
        public int StackedCount => _stackedGifts.Count;
        public GameTimer Timer => _timer;
        public PilerConfig Config => _config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_stick != null && _config != null)
            {
                _stick.Initialize(_config);
            }

            _timer.OnTimeUpdated += (rem, elap) => OnTimerUpdated?.Invoke(rem, elap);
            _timer.OnCompleted += EndGame;
        }

        private async void Start()
        {
            if (_pool != null)
            {
                _pool.Initialize();
                var keys = _pool.GetUniqueAssetKeys();
                if (RemoteAssetService.Instance != null && keys.Count > 0)
                {
                    await RemoteAssetService.Instance.PreloadSpritesAsync(keys);
                }
            }

            StartGame();
        }

        public void StartGame()
        {
            _isPlaying = true;
            _currentScore = 0;
            _stackedGifts.Clear();

            _stick?.ResetPosition();
            _stick?.SetActiveMovement(true);

            var duration = _config != null ? _config.GameDuration : 60f;
            _timer.Start(duration);

            OnScoreUpdated?.Invoke(_currentScore, _stackedGifts.Count);

            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(SpawnRoutine());
        }

        private void Update()
        {
            if (!_isPlaying) return;
            _timer.Tick(Time.deltaTime);
        }

        private IEnumerator SpawnRoutine()
        {
            yield return new WaitForSeconds(0.5f);

            while (_isPlaying)
            {
                SpawnGift();

                var progress = _timer.Progress;
                var initialInterval = _config != null ? _config.InitialSpawnInterval : 2.0f;
                var minInterval = _config != null ? _config.MinSpawnInterval : 0.9f;
                var interval = Mathf.Lerp(initialInterval, minInterval, progress);

                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnGift()
        {
            if (_pool == null) return;

            var gift = _pool.GetRandomGift();
            if (gift == null) return;

            var padding = _config != null ? _config.SpawnPadding : 40f;
            var width = _spawnArea != null ? _spawnArea.rect.width : (_config != null ? _config.FallbackSpawnWidth : 640f);
            var halfWidth = Mathf.Max(0f, (width * 0.5f) - padding);

            var minX = -halfWidth;
            var maxX = halfWidth;
            var spawnY = _spawnArea != null ? _spawnArea.anchoredPosition.y : 0f;

            var randomX = UnityEngine.Random.Range(minX, maxX);
            var spawnPos = new Vector2(randomX, spawnY);

            var progress = _timer.Progress;
            var initialGrav = _config != null ? _config.InitialGravity : 0.8f;
            var maxGrav = _config != null ? _config.MaxGravity : 2.0f;
            var gravity = Mathf.Lerp(initialGrav, maxGrav, progress);

            var parent = _giftsContainer != null ? _giftsContainer : transform;
            gift.Spawn(this, _pool, _config, gravity, spawnPos, parent);
        }

        public void AddStackedGift(PilerGift gift, int points)
        {
            if (!_isPlaying || gift == null) return;

            if (!_stackedGifts.Contains(gift))
            {
                _stackedGifts.Add(gift);
                _currentScore += points;
                OnScoreUpdated?.Invoke(_currentScore, _stackedGifts.Count);
            }
        }

        public void RemoveStackedGift(PilerGift gift)
        {
            if (gift == null) return;

            if (_stackedGifts.Remove(gift))
            {
                _currentScore = Mathf.Max(0, _currentScore - gift.ScoreValue);
                OnScoreUpdated?.Invoke(_currentScore, _stackedGifts.Count);
            }
        }

        private void EndGame()
        {
            _isPlaying = false;
            _stick?.SetActiveMovement(false);

            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            OnGameEnded?.Invoke(_currentScore, _stackedGifts.Count);

            if (_config != null && _config.EnableVideoAds && VideoAdService.Instance != null)
            {
                VideoAdService.Instance.PlayAd();
            }
        }

        public void RestartGame()
        {
            _pool?.ReturnAll();
            StartGame();
        }

        public void LoadMenu()
        {
            SceneManager.LoadScene(_menuSceneName);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
