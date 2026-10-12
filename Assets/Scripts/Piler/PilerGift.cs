using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using SantaPlay.AssetLoading;

namespace SantaPlay.Piler
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PilerGift : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string _assetKey;
        [SerializeField] private int _scoreValue = 50;

        private RectTransform _rectTransform;
        private Rigidbody2D _rigidbody2D;
        private Image _image;
        private PilerGameManager _manager;
        private PilerGiftPool _pool;
        private PilerConfig _config;
        private PilerStick _stick;

        private bool _isStacked;
        private bool _hasRegistered;
        private CancellationTokenSource _cts;

        public string AssetKey => _assetKey;
        public int ScoreValue => _scoreValue;
        public bool IsStacked => _isStacked;
        public PilerStick Stick => _stick;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _image = GetComponent<Image>();
        }

        public void Spawn(PilerGameManager manager, PilerGiftPool pool, PilerConfig config, float gravityScale, Vector2 position, Transform parent)
        {
            _manager = manager;
            _pool = pool;
            _config = config;
            _stick = null;
            _isStacked = false;
            _hasRegistered = false;

            transform.SetParent(parent, false);
            _rectTransform.anchoredPosition = position;
            _rectTransform.localRotation = Quaternion.identity;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
                _rigidbody2D.gravityScale = gravityScale;
                _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            }

            gameObject.SetActive(true);
            BindRemoteSprite();
        }

        public void SetFallGravity(float gravity)
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.gravityScale = gravity;
            }
        }

        private void BindRemoteSprite()
        {
            if (string.IsNullOrEmpty(_assetKey) || _image == null) return;

            var service = RemoteAssetService.Instance;
            if (service == null) return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _ = LoadSpriteInternalAsync(service, _cts.Token);
        }

        private async Task LoadSpriteInternalAsync(RemoteAssetService service, CancellationToken ct)
        {
            try
            {
                var sprite = await service.LoadSpriteAsync(_assetKey, ct);
                if (!ct.IsCancellationRequested && _image != null && sprite != null)
                {
                    _image.sprite = sprite;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PilerGift] Failed to load sprite for '{_assetKey}': {ex.Message}");
            }
        }

        private void Update()
        {
            if (_config == null || _pool == null) return;

            if (_isStacked && transform.parent != null)
            {
                if (_rectTransform.localPosition.y < _config.ToppleDeathY)
                {
                    _manager?.RemoveStackedGift(this);
                    _pool.ReturnGift(this);
                }
            }
            else
            {
                if (_rectTransform.anchoredPosition.y < _config.FallDeathY)
                {
                    _pool.ReturnGift(this);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isStacked || _hasRegistered) return;

            if (collision.gameObject.TryGetComponent<PilerStick>(out var stick))
            {
                StackOnto(stick);
            }
            else if (collision.gameObject.TryGetComponent<PilerGift>(out var otherGift) && otherGift.IsStacked)
            {
                if (otherGift.Stick != null)
                {
                    StackOnto(otherGift.Stick);
                }
            }
        }

        private void StackOnto(PilerStick stick)
        {
            _stick = stick;
            _isStacked = true;
            _hasRegistered = true;

            transform.SetParent(stick.transform, true);
            _manager?.AddStackedGift(this, _scoreValue);
        }

        public void ResetForPool()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _isStacked = false;
            _hasRegistered = false;
            _stick = null;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
