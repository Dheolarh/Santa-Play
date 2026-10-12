using TMPro;
using UnityEngine;

namespace SantaPlay.Core.UI
{
    public enum TimerDisplayMode
    {
        CountDown,
        CountUp
    }

    public class GameTimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TimerDisplayMode _displayMode = TimerDisplayMode.CountDown;

        public TimerDisplayMode DisplayMode
        {
            get => _displayMode;
            set => _displayMode = value;
        }

        private void Awake()
        {
            if (_timerText == null)
            {
                _timerText = GetComponent<TextMeshProUGUI>();
            }
        }

        public void SetDisplayMode(TimerDisplayMode mode)
        {
            _displayMode = mode;
        }

        public void UpdateDisplay(float remaining, float elapsed)
        {
            if (_timerText == null) return;

            var time = _displayMode == TimerDisplayMode.CountDown ? remaining : elapsed;
            var minutes = Mathf.FloorToInt(time / 60f);
            var seconds = Mathf.FloorToInt(time % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
