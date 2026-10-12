using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SantaPlay.Core.UI;

namespace SantaPlay.Piler
{
    public class PilerUI : MonoBehaviour
    {
        [Header("Manager")]
        [SerializeField] private PilerGameManager _manager;

        [Header("In-Game HUD")]
        [SerializeField] private GameTimerUI _timerUI;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _stackCountText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("Game Over Dialog")]
        [SerializeField] private GameObject _endPanelRoot;
        [SerializeField] private RectTransform _endPanelContent;
        [SerializeField] private TextMeshProUGUI _endTitleText;
        [SerializeField] private TextMeshProUGUI _endSubtitleText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        private void Awake()
        {
            if (_endPanelRoot != null) _endPanelRoot.SetActive(false);

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(OnMenuClicked);
            }

            if (_timerUI == null && _timerText != null)
            {
                _timerUI = _timerText.GetComponent<GameTimerUI>();
            }
        }

        private void Start()
        {
            if (_manager == null)
            {
                _manager = PilerGameManager.Instance;
            }

            if (_manager != null)
            {
                if (_timerUI != null && _manager.Config != null)
                {
                    _timerUI.SetDisplayMode(_manager.Config.TimerDisplayMode);
                }

                _manager.OnTimerUpdated += HandleTimerUpdated;
                _manager.OnScoreUpdated += HandleScoreUpdated;
                _manager.OnGameEnded += HandleGameEnded;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnTimerUpdated -= HandleTimerUpdated;
                _manager.OnScoreUpdated -= HandleScoreUpdated;
                _manager.OnGameEnded -= HandleGameEnded;
            }

            if (_restartButton != null) _restartButton.onClick.RemoveListener(OnRestartClicked);
            if (_menuButton != null) _menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        private void HandleTimerUpdated(float remaining, float elapsed)
        {
            if (_timerUI != null)
            {
                _timerUI.UpdateDisplay(remaining, elapsed);
                return;
            }

            if (_timerText == null) return;

            var mode = _manager != null && _manager.Config != null
                ? _manager.Config.TimerDisplayMode
                : TimerDisplayMode.CountDown;

            var time = mode == TimerDisplayMode.CountDown ? remaining : elapsed;
            var minutes = Mathf.FloorToInt(time / 60f);
            var seconds = Mathf.FloorToInt(time % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void HandleScoreUpdated(int score, int count)
        {
            if (_scoreText != null) _scoreText.text = score.ToString();
            if (_stackCountText != null) _stackCountText.text = count.ToString();
        }

        private void HandleGameEnded(int finalScore, int finalCount)
        {
            if (_endPanelRoot == null) return;

            _endPanelRoot.SetActive(true);

            if (_endTitleText != null)
            {
                _endTitleText.text = "TIME UP!";
            }

            if (_endSubtitleText != null)
            {
                var objectStr = (finalCount == 1) ? "1 object" : $"{finalCount} objects";
                var pointStr = (finalScore == 1) ? "1 point" : $"{finalScore} points";
                _endSubtitleText.text = $"You stacked {objectStr} and scored {pointStr}";
            }

            if (_endPanelContent != null)
            {
                StartCoroutine(PopUpPanelRoutine(_endPanelContent, 0.45f));
            }
        }

        private IEnumerator PopUpPanelRoutine(RectTransform target, float duration)
        {
            target.localScale = Vector3.zero;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                float scaleMultiplier;
                if (t < 0.65f)
                {
                    var p = t / 0.65f;
                    var ease = Mathf.Sin(p * Mathf.PI * 0.5f);
                    scaleMultiplier = Mathf.Lerp(0f, 1.12f, ease);
                }
                else if (t < 0.85f)
                {
                    var p = (t - 0.65f) / 0.20f;
                    scaleMultiplier = Mathf.Lerp(1.12f, 0.96f, p);
                }
                else
                {
                    var p = (t - 0.85f) / 0.15f;
                    scaleMultiplier = Mathf.Lerp(0.96f, 1.0f, p);
                }

                target.localScale = Vector3.one * scaleMultiplier;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private void OnRestartClicked()
        {
            if (_endPanelRoot != null) _endPanelRoot.SetActive(false);
            _manager?.RestartGame();
        }

        private void OnMenuClicked()
        {
            _manager?.LoadMenu();
        }
    }
}
