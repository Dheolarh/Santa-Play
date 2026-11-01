using TMPro;
using UnityEngine;

// Counts down the round, shows it as mm:ss, swells the clock in the last seconds,
// and announces "time warning" (30s) and "game over" (0s) through GameEvents.
public class GameTimer : MonoBehaviour
{
    [Header("Time")]
    [Tooltip("Round length in seconds. 120 = 02:00.")]
    [SerializeField, Min(1f)] private float duration = 120f;
    [Tooltip("Start counting as soon as the scene starts. Turn off if a Start button will call StartTimer().")]
    [SerializeField] private bool startOnPlay = true;

    [Header("References")]
    [Tooltip("Text_Timer")]
    [SerializeField] private TMP_Text timerText;
    [Tooltip("Icon_Clock. This is the icon that swells.")]
    [SerializeField] private RectTransform clockIcon;

    [Header("Warning (last seconds)")]
    [Tooltip("The warning starts when this many seconds are left: the text turns red, the clock swells every second, and the warning sound loop starts.")]
    [SerializeField, Min(1)] private int warningSeconds = 30;
    [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.25f, 1f);
    [Tooltip("How big the clock gets on each beat. 1 = no swell.")]
    [SerializeField, Min(1f)] private float pulseSwell = 1.25f;
    [Tooltip("How long one beat takes.")]
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.35f;
    [Tooltip("In the last X seconds the beat gets bigger.")]
    [SerializeField, Min(0)] private int finalSeconds = 10;
    [SerializeField, Min(1f)] private float finalPulseSwell = 1.45f;

    public float Remaining => remaining;
    public bool IsRunning => running;
    public bool IsFinished => finished;

    private string[] labels;      // "02:00", "01:59", ... built once, so no garbage while playing
    private float remaining;
    private bool running;
    private bool finished;
    private bool warned;
    private int shownSeconds;

    private float pulseTimer;
    private float currentSwell;

    private Color textBaseColor = Color.white;
    private Vector3 textBaseScale = Vector3.one;
    private Vector3 clockBaseScale = Vector3.one;

    private void Awake()
    {
        BuildLabels();

        if (timerText != null)
        {
            textBaseColor = timerText.color;
            textBaseScale = timerText.rectTransform.localScale;
        }
        if (clockIcon != null) clockBaseScale = clockIcon.localScale;

        ResetTimer();
    }

    private void Start()
    {
        if (startOnPlay) StartTimer();
    }

    // ---- Call these from anywhere (start button, restart, pause) ----

    public void StartTimer()
    {
        if (finished) return;
        running = true;
        CheckWarning(shownSeconds); // in case the round is shorter than the warning time
    }

    public void StopTimer()
    {
        running = false;
    }

    public void ResetTimer()
    {
        remaining = duration;
        running = false;
        finished = false;
        warned = false;
        pulseTimer = 0f;

        ShowSeconds(Mathf.CeilToInt(duration));

        if (timerText != null)
        {
            timerText.color = textBaseColor;
            timerText.rectTransform.localScale = textBaseScale;
        }
        if (clockIcon != null) clockIcon.localScale = clockBaseScale;
    }

    // ---- Countdown ----

    private void Update()
    {
        if (running)
        {
            remaining -= Time.deltaTime;

            if (remaining <= 0f)
            {
                remaining = 0f;
                running = false;
                finished = true;

                ShowSeconds(0);
                CheckWarning(0);
                StartPulse(0);
                GameEvents.RaiseGameOver();
            }
            else
            {
                int secs = Mathf.CeilToInt(remaining);
                if (secs != shownSeconds)
                {
                    ShowSeconds(secs);
                    CheckWarning(secs);
                    if (secs <= warningSeconds) StartPulse(secs);
                }
            }
        }

        AnimatePulse();
    }

    private void BuildLabels()
    {
        int max = Mathf.CeilToInt(duration);
        labels = new string[max + 1];
        for (int i = 0; i <= max; i++)
            labels[i] = (i / 60).ToString("00") + ":" + (i % 60).ToString("00");
    }

    // Only touches the text when the second really changes.
    private void ShowSeconds(int secs)
    {
        shownSeconds = secs;
        if (timerText != null)
            timerText.text = labels[Mathf.Clamp(secs, 0, labels.Length - 1)];
    }

    private void CheckWarning(int secs)
    {
        if (warned || secs > warningSeconds) return;

        warned = true;
        if (timerText != null) timerText.color = warningColor;
        GameEvents.RaiseTimeWarningStarted();
    }

    // ---- Clock beat ----

    private void StartPulse(int secs)
    {
        currentSwell = secs <= finalSeconds ? finalPulseSwell : pulseSwell;
        pulseTimer = pulseDuration;
    }

    private void AnimatePulse()
    {
        if (pulseTimer <= 0f) return;

        pulseTimer -= Time.unscaledDeltaTime;
        float t = 1f - Mathf.Clamp01(pulseTimer / pulseDuration);

        // Quick swell, then a slower settle back: 0 -> 1 -> 0.
        float k = Mathf.Sin(Mathf.Sqrt(t) * Mathf.PI);

        if (clockIcon != null)
            clockIcon.localScale = clockBaseScale * (1f + (currentSwell - 1f) * k);

        // The numbers swell a little too, at half strength.
        if (timerText != null)
            timerText.rectTransform.localScale = textBaseScale * (1f + (currentSwell - 1f) * 0.5f * k);

        if (pulseTimer <= 0f)
        {
            if (clockIcon != null) clockIcon.localScale = clockBaseScale;
            if (timerText != null) timerText.rectTransform.localScale = textBaseScale;
        }
    }
}
