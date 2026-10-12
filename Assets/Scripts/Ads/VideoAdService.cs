using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace SantaPlay.Ads
{
    [DefaultExecutionOrder(-90)]
    public class VideoAdService : MonoBehaviour
    {
        public static VideoAdService Instance { get; private set; }

        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private VideoAdConfig _config;

        private readonly List<string> _playlist = new();
        private int _currentIndex;
        private Action _onCompletedCallback;
        private Action<string> _onErrorCallback;
        private RenderTexture _renderTexture;

        public event Action<string> OnAdStarted;
        public event Action OnAdFinished;
        public event Action<string> OnAdError;

        public bool IsPlaying => _videoPlayer != null && _videoPlayer.isPlaying;
        public RenderTexture TargetTexture => _renderTexture;
        public VideoAdConfig Config => _config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayer();
        }

        private void InitializePlayer()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
                if (_videoPlayer == null)
                {
                    _videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
            }

            _videoPlayer.playOnAwake = false;
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.aspectRatio = VideoAspectRatio.FitInside;

            var width = _config != null ? _config.RenderTextureWidth : 1280;
            var height = _config != null ? _config.RenderTextureHeight : 720;
            _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _renderTexture.Create();

            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.prepareCompleted += HandlePrepareCompleted;
            _videoPlayer.loopPointReached += HandlePlaybackFinished;
            _videoPlayer.errorReceived += HandlePlaybackError;
        }

        public void SetAdUrls(IEnumerable<string> urls)
        {
            _playlist.Clear();
            if (urls != null)
            {
                foreach (var url in urls)
                {
                    if (!string.IsNullOrEmpty(url))
                    {
                        _playlist.Add(url);
                    }
                }
            }
            _currentIndex = 0;
        }

        public void AddAdUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                _playlist.Add(url);
            }
        }

        public bool PlayAd(Action onCompleted = null, Action<string> onError = null)
        {
            var url = GetNextUrl();
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("No video ad URLs available");
                return false;
            }

            _onCompletedCallback = onCompleted;
            _onErrorCallback = onError;

            _videoPlayer.Stop();
            _videoPlayer.url = url;
            _videoPlayer.Prepare();
            return true;
        }

        public void SkipAd()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                CompleteAd();
            }
        }

        private string GetNextUrl()
        {
            if (_playlist.Count == 0 && _config != null && _config.FallbackUrls != null)
            {
                SetAdUrls(_config.FallbackUrls);
            }

            if (_playlist.Count == 0) return null;

            var url = _playlist[_currentIndex % _playlist.Count];
            _currentIndex++;
            return url;
        }

        private void HandlePrepareCompleted(VideoPlayer source)
        {
            source.Play();
            OnAdStarted?.Invoke(source.url);
        }

        private void HandlePlaybackFinished(VideoPlayer source)
        {
            CompleteAd();
        }

        private void HandlePlaybackError(VideoPlayer source, string message)
        {
            _onErrorCallback?.Invoke(message);
            OnAdError?.Invoke(message);
            _onErrorCallback = null;
        }

        private void CompleteAd()
        {
            var cb = _onCompletedCallback;
            _onCompletedCallback = null;
            _onErrorCallback = null;
            OnAdFinished?.Invoke();
            cb?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= HandlePrepareCompleted;
                _videoPlayer.loopPointReached -= HandlePlaybackFinished;
                _videoPlayer.errorReceived -= HandlePlaybackError;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }
    }
}
