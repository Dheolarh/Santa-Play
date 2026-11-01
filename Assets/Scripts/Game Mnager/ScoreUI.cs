using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the score and animates it:
//  - catching a flake (+10 / Gold +30): the number counts up, the star swells and wobbles, the text glows gold
//  - catching a bomb (-20): the text flashes red and shakes, the star shrinks and turns red
// Put this on Widget_Score.
public class ScoreUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Text_Score")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("Icon_Star")]
    [SerializeField] private RectTransform starIcon;
    [Tooltip("Leave empty to find the NetCatcher automatically.")]
    [SerializeField] private NetCatcher netCatcher;

    [Header("Count Up")]
    [Tooltip("Slowest the number rolls, in points per second.")]
    [SerializeField, Min(1f)] private float minCountSpeed = 40f;
    [Tooltip("The number always catches up within about this many seconds, even after a big jump.")]
    [SerializeField, Min(0.05f)] private float catchUpTime = 0.25f;

    [Header("Gain (+10 / +30)")]
    [SerializeField] private Color gainColor = new Color(1f, 0.85f, 0.25f, 1f);
    [Tooltip("How big the score text swells. 1 = no swell.")]
    [SerializeField, Min(1f)] private float gainTextSwell = 1.3f;
    [Tooltip("How big the star swells. 1 = no swell.")]
    [SerializeField, Min(1f)] private float gainStarSwell = 1.45f;
    [Tooltip("How far the star wobbles, in degrees.")]
    [SerializeField] private float gainStarWobble = 25f;
    [SerializeField, Min(0.05f)] private float gainDuration = 0.35f;
    [Tooltip("Points that count as a normal catch. A bigger catch (like Gold +30) swells more.")]
    [SerializeField, Min(1f)] private float normalPoints = 10f;
    [Tooltip("Most a big catch can strengthen the animation. 1.6 = 60% stronger.")]
    [SerializeField, Min(1f)] private float maxIntensity = 1.6f;

    [Header("Loss (bomb)")]
    [SerializeField] private Color lossColor = new Color(1f, 0.2f, 0.2f, 1f);
    [Tooltip("How small the star gets. 1 = no shrink.")]
    [SerializeField, Range(0.3f, 1f)] private float lossStarScale = 0.7f;
    [Tooltip("How far the text shakes sideways (canvas units).")]
    [SerializeField, Min(0f)] private float lossShake = 8f;
    [SerializeField, Min(0.05f)] private float lossDuration = 0.5f;

    private int targetScore;
    private float shownScore;
    private int lastShownInt = -1;

    private float gainTimer;
    private float gainStrength = 1f;
    private float lossTimer;

    private Color textBaseColor = Color.white;
    private Vector3 textBaseScale = Vector3.one;
    private Vector2 textBasePosition;
    private Vector3 starBaseScale = Vector3.one;
    private Graphic starGraphic;
    private Color starBaseColor = Color.white;

    // Number -> text, remembered so rolling the score creates no garbage.
    private readonly string[] numberCache = new string[5000];

    private void Awake()
    {
        if (scoreText != null)
        {
            textBaseColor = scoreText.color;
            textBaseScale = scoreText.rectTransform.localScale;
            textBasePosition = scoreText.rectTransform.anchoredPosition;
        }

        if (starIcon != null)
        {
            starBaseScale = starIcon.localScale;
            starGraphic = starIcon.GetComponent<Graphic>();
            if (starGraphic != null) starBaseColor = starGraphic.color;
        }

        if (netCatcher == null) netCatcher = FindFirstObjectByType<NetCatcher>();
    }

    private void OnEnable()
    {
        if (netCatcher != null)
        {
            netCatcher.ScoreChanged += OnScoreChanged;
            targetScore = netCatcher.TotalScore;
        }
        GameEvents.BombCaught += OnBombCaught;

        shownScore = targetScore;
        ShowNumber(targetScore);
    }

    private void OnDisable()
    {
        if (netCatcher != null) netCatcher.ScoreChanged -= OnScoreChanged;
        GameEvents.BombCaught -= OnBombCaught;
    }

    // (new total, change). Positive = flake. The bomb is handled by OnBombCaught.
    private void OnScoreChanged(int total, int change)
    {
        targetScore = total;

        if (change > 0)
        {
            gainTimer = gainDuration;
            gainStrength = Mathf.Clamp(change / normalPoints, 1f, maxIntensity);
        }
        else if (change == 0)
        {
            // e.g. ResetScore(): jump straight to the new number.
            shownScore = total;
            ShowNumber(total);
        }
    }

    // Fires on every bomb hit, even when the score was already 0 and nothing could be lost.
    private void OnBombCaught(Bomb bomb, int loss)
    {
        lossTimer = lossDuration;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateCount(dt);
        Animate(dt);
    }

    // ---------- Number ----------

    private void UpdateCount(float dt)
    {
        if (Mathf.Approximately(shownScore, targetScore)) return;

        float diff = Mathf.Abs(targetScore - shownScore);
        float speed = Mathf.Max(minCountSpeed, diff / catchUpTime);
        shownScore = Mathf.MoveTowards(shownScore, targetScore, speed * dt);

        ShowNumber(Mathf.RoundToInt(shownScore));
    }

    // Only touches the text when the number really changes.
    private void ShowNumber(int value)
    {
        if (value == lastShownInt) return;
        lastShownInt = value;

        if (scoreText == null) return;

        if (value >= 0 && value < numberCache.Length)
        {
            if (numberCache[value] == null) numberCache[value] = value.ToString();
            scoreText.text = numberCache[value];
        }
        else
        {
            scoreText.text = value.ToString();
        }
    }

    // ---------- Animations ----------

    private void Animate(float dt)
    {
        bool gaining = gainTimer > 0f;
        bool losing = lossTimer > 0f;
        if (!gaining && !losing) return;

        // Gain: quick swell then settle (0 -> 1 -> 0), plus a wobble of the star.
        float gainK = 0f;
        float wobble = 0f;
        if (gaining)
        {
            gainTimer -= dt;
            float t = 1f - Mathf.Clamp01(gainTimer / gainDuration);
            gainK = Mathf.Sin(Mathf.Sqrt(t) * Mathf.PI) * gainStrength;
            wobble = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * gainStarWobble * gainStrength;
        }

        // Loss: instantly strong, then fades out, with a sideways shake.
        float lossK = 0f;
        float shake = 0f;
        if (losing)
        {
            lossTimer -= dt;
            float t = 1f - Mathf.Clamp01(lossTimer / lossDuration);
            lossK = 1f - t;
            shake = Mathf.Sin(t * 40f) * lossK * lossShake;
        }

        if (starIcon != null)
        {
            float starScale = 1f + (gainStarSwell - 1f) * gainK - (1f - lossStarScale) * lossK;
            starIcon.localScale = starBaseScale * starScale;
            starIcon.localRotation = Quaternion.Euler(0f, 0f, wobble);
        }

        if (scoreText != null)
        {
            scoreText.rectTransform.localScale = textBaseScale * (1f + (gainTextSwell - 1f) * gainK);
            scoreText.rectTransform.anchoredPosition = textBasePosition + new Vector2(shake, 0f);

            Color c = Color.Lerp(textBaseColor, gainColor, Mathf.Clamp01(gainK));
            scoreText.color = Color.Lerp(c, lossColor, lossK);
        }

        if (starGraphic != null)
            starGraphic.color = Color.Lerp(starBaseColor, lossColor, lossK * 0.6f);
    }
}
