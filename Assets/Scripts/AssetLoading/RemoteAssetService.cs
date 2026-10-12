using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SantaPlay.AssetLoading
{
    [DefaultExecutionOrder(-100)]
    public class RemoteAssetService : MonoBehaviour, IImageAssetLoader
    {
        private class CacheEntry
        {
            public Sprite Sprite;
            public Texture2D Texture;
            public int RefCount;
        }

        public static RemoteAssetService Instance { get; private set; }

        [SerializeField] private AssetManifest _manifest;

        private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Task<Sprite>> _inFlight = new(StringComparer.Ordinal);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetManifest(AssetManifest manifest)
        {
            _manifest = manifest;
        }

        public Task<Sprite> LoadSpriteAsync(string key, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Task.FromException<Sprite>(new ArgumentException("Asset key cannot be null or empty", nameof(key)));
            }

            var url = (_manifest != null && _manifest.TryGetUrl(key, out var resolvedUrl)) ? resolvedUrl : key;

            if (_cache.TryGetValue(url, out var entry))
            {
                entry.RefCount++;
                return Task.FromResult(entry.Sprite);
            }

            if (_inFlight.TryGetValue(url, out var inFlightTask))
            {
                return AwaitInFlightAsync(url, inFlightTask, ct);
            }

            var downloadTask = DownloadSpriteInternalAsync(url, ct);
            _inFlight[url] = downloadTask;
            return downloadTask;
        }

        public async Task PreloadSpritesAsync(IEnumerable<string> keys, CancellationToken ct = default)
        {
            if (keys == null) return;

            var tasks = new List<Task<Sprite>>();
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    tasks.Add(LoadSpriteAsync(key, ct));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        public void Release(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            var url = (_manifest != null && _manifest.TryGetUrl(key, out var resolvedUrl)) ? resolvedUrl : key;
            if (_cache.TryGetValue(url, out var entry))
            {
                entry.RefCount = Mathf.Max(0, entry.RefCount - 1);
            }
        }

        public void UnloadUnused()
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.RefCount <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                var key = keysToRemove[i];
                var entry = _cache[key];
                if (entry.Sprite != null) Destroy(entry.Sprite);
                if (entry.Texture != null) Destroy(entry.Texture);
                _cache.Remove(key);
            }
        }

        private async Task<Sprite> AwaitInFlightAsync(string url, Task<Sprite> inFlightTask, CancellationToken ct)
        {
            var sprite = await inFlightTask;
            ct.ThrowIfCancellationRequested();

            if (_cache.TryGetValue(url, out var entry))
            {
                entry.RefCount++;
            }

            return sprite;
        }

        private async Task<Sprite> DownloadSpriteInternalAsync(string url, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<Sprite>(TaskCreationOptions.RunContinuationsAsynchronously);
            var uwr = UnityWebRequestTexture.GetTexture(url);

            var reg = ct.Register(() =>
            {
                uwr.Abort();
                tcs.TrySetCanceled(ct);
            });

            var op = uwr.SendWebRequest();
            op.completed += _ =>
            {
                reg.Dispose();
                if (tcs.Task.IsCompleted)
                {
                    uwr.Dispose();
                    return;
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    var errorMsg = uwr.error;
                    uwr.Dispose();
                    tcs.TrySetException(new Exception($"Failed to download '{url}': {errorMsg}"));
                    return;
                }

                var texture = DownloadHandlerTexture.GetContent(uwr);
                uwr.Dispose();

                if (texture == null)
                {
                    tcs.TrySetException(new Exception($"Failed to decode texture from '{url}'"));
                    return;
                }

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect
                );

                tcs.TrySetResult(sprite);
            };

            try
            {
                var sprite = await tcs.Task;
                _cache[url] = new CacheEntry
                {
                    Sprite = sprite,
                    Texture = sprite.texture,
                    RefCount = 1
                };
                return sprite;
            }
            finally
            {
                _inFlight.Remove(url);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            foreach (var entry in _cache.Values)
            {
                if (entry.Sprite != null) Destroy(entry.Sprite);
                if (entry.Texture != null) Destroy(entry.Texture);
            }
            _cache.Clear();
            _inFlight.Clear();
        }
    }
}
