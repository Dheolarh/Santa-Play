using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SantaPlay.AssetLoading
{
    [RequireComponent(typeof(Image))]
    public class RemoteImage : MonoBehaviour
    {
        [SerializeField] private string _assetKey;
        [SerializeField] private Sprite _placeholderSprite;
        [SerializeField] private Sprite _fallbackSprite;
        [SerializeField] private bool _loadOnEnable = true;

        private Image _image;
        private CancellationTokenSource _cts;
        private string _loadedKey;

        public string AssetKey
        {
            get => _assetKey;
            set
            {
                _assetKey = value;
                if (isActiveAndEnabled)
                {
                    LoadAsset(_assetKey);
                }
            }
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (_loadOnEnable && !string.IsNullOrEmpty(_assetKey))
            {
                LoadAsset(_assetKey);
            }
        }

        private void OnDisable()
        {
            CancelAndRelease();
        }

        public void LoadAsset(string key)
        {
            if (string.Equals(_loadedKey, key, StringComparison.Ordinal)) return;

            CancelAndRelease();
            _loadedKey = key;
            _cts = new CancellationTokenSource();

            if (_placeholderSprite != null && _image != null)
            {
                _image.sprite = _placeholderSprite;
            }

            _ = LoadInternalAsync(key, _cts.Token);
        }

        private async Task LoadInternalAsync(string key, CancellationToken ct)
        {
            try
            {
                var service = RemoteAssetService.Instance;
                if (service == null)
                {
                    Debug.LogWarning("[RemoteImage] RemoteAssetService instance not found.");
                    return;
                }

                var sprite = await service.LoadSpriteAsync(key, ct);
                if (!ct.IsCancellationRequested && _image != null)
                {
                    _image.sprite = sprite;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoteImage] Failed to load '{key}': {ex.Message}");
                if (!ct.IsCancellationRequested && _image != null && _fallbackSprite != null)
                {
                    _image.sprite = _fallbackSprite;
                }
            }
        }

        private void CancelAndRelease()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (!string.IsNullOrEmpty(_loadedKey))
            {
                RemoteAssetService.Instance?.Release(_loadedKey);
                _loadedKey = null;
            }
        }
    }
}
