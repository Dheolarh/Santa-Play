using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SantaPlay.Ads
{
    public class VideoAdUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private RawImage _videoDisplay;
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private Button _skipButton;
        [SerializeField] private TextMeshProUGUI _skipCountdownText;

        private Coroutine _skipCountdownRoutine;

        private void Awake()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(OnSkipClicked);
                _skipButton.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            var service = VideoAdService.Instance;
            if (service != null)
            {
                service.OnAdStarted += HandleAdStarted;
                service.OnAdFinished += HandleAdFinished;
                service.OnAdError += HandleAdError;
            }
        }

        private void OnDestroy()
        {
            var service = VideoAdService.Instance;
            if (service != null)
            {
                service.OnAdStarted -= HandleAdStarted;
                service.OnAdFinished -= HandleAdFinished;
                service.OnAdError -= HandleAdError;
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveListener(OnSkipClicked);
            }
        }

        public void ShowLoading()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            if (_loadingSpinner != null) _loadingSpinner.SetActive(true);
            if (_skipButton != null) _skipButton.gameObject.SetActive(false);
            if (_skipCountdownText != null) _skipCountdownText.text = string.Empty;
        }

        private void HandleAdStarted(string url)
        {
            var service = VideoAdService.Instance;
            if (service == null) return;

            if (_panelRoot != null) _panelRoot.SetActive(true);
            if (_loadingSpinner != null) _loadingSpinner.SetActive(false);
            if (_videoDisplay != null) _videoDisplay.texture = service.TargetTexture;

            var config = service.Config;
            var skipDelay = config != null ? config.SkipDelaySeconds : 5f;

            if (_skipCountdownRoutine != null) StopCoroutine(_skipCountdownRoutine);

            if (skipDelay > 0f)
            {
                _skipCountdownRoutine = StartCoroutine(SkipCountdownRoutine(skipDelay));
            }
            else
            {
                if (_skipButton != null) _skipButton.gameObject.SetActive(false);
                if (_skipCountdownText != null) _skipCountdownText.text = string.Empty;
            }
        }

        private IEnumerator SkipCountdownRoutine(float seconds)
        {
            if (_skipButton != null) _skipButton.gameObject.SetActive(false);

            var remaining = seconds;
            while (remaining > 0f)
            {
                if (_skipCountdownText != null)
                {
                    _skipCountdownText.text = $"Skip in {Mathf.CeilToInt(remaining)}s";
                }
                yield return new WaitForSecondsRealtime(0.2f);
                remaining -= 0.2f;
            }

            if (_skipCountdownText != null) _skipCountdownText.text = string.Empty;
            if (_skipButton != null) _skipButton.gameObject.SetActive(true);
        }

        private void HandleAdFinished()
        {
            ClosePanel();
        }

        private void HandleAdError(string error)
        {
            ClosePanel();
        }

        private void ClosePanel()
        {
            if (_skipCountdownRoutine != null)
            {
                StopCoroutine(_skipCountdownRoutine);
                _skipCountdownRoutine = null;
            }

            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_videoDisplay != null) _videoDisplay.texture = null;
        }

        private void OnSkipClicked()
        {
            VideoAdService.Instance?.SkipAd();
        }
    }
}
