using TMPro;
using UnityEngine;

public class ScorePopup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty if the TextMeshPro component is on this same object.")]
    [SerializeField] private TMP_Text label;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
    [Tooltip("Fading starts at this point of the lifetime (0 = start, 1 = end).")]
    [SerializeField, Range(0f, 1f)] private float fadeStart = 0.55f;

    [Header("Motion")]
    [Tooltip("How far the text floats up.")]
    [SerializeField] private float riseDistance = 1.3f;
    [Tooltip("Random sideways drift while floating (+/-).")]
    [SerializeField] private float driftX = 0.25f;
    [Tooltip("Random tilt (+/- degrees), like the slanted text in your design.")]
    [SerializeField] private float tiltDegrees = 8f;

    [Header("Pop")]
    [Tooltip("Size over the popup's life. X = 0..1 of the lifetime, Y = size multiplier. Default pops big, then settles.")]
    [SerializeField] private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0.2f),
        new Keyframe(0.15f, 1.25f),
        new Keyframe(0.3f, 1f),
        new Keyframe(1f, 1f));

    [Header("Layering")]
    [SerializeField] private int sortingOrder = 20;

    private Vector3 startPosition;
    private float drift;
    private Vector3 baseScale = Vector3.one;
    private float elapsed;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();

        Renderer r = GetComponent<Renderer>();
        if (r != null) r.sortingOrder = sortingOrder;

        baseScale = transform.localScale;
    }

    // Called right after the popup is created.
    public void Play(string message, Color color)
    {
        if (label != null)
        {
            label.text = message;
            label.color = color;
        }

        startPosition = transform.position;
        drift = Random.Range(-driftX, driftX);

        float tilt = Random.Range(tiltDegrees * 0.5f, tiltDegrees) * (Random.value < 0.5f ? -1f : 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, tilt);

        elapsed = 0f;
        transform.localScale = baseScale * scaleCurve.Evaluate(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        // Float up fast, then slow down.
        float rise = 1f - (1f - t) * (1f - t);
        transform.position = startPosition + new Vector3(drift * rise, riseDistance * rise, 0f);
        transform.localScale = baseScale * scaleCurve.Evaluate(t);

        if (label != null)
        {
            float fade = t < fadeStart ? 1f : 1f - (t - fadeStart) / Mathf.Max(0.001f, 1f - fadeStart);
            label.alpha = Mathf.Clamp01(fade);
        }

        if (elapsed >= lifetime) Destroy(gameObject);
    }
}