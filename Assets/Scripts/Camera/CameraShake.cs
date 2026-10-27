using UnityEngine;

// Runs first so it can undo last frame's shake before other scripts read the camera position.
[DefaultExecutionOrder(-1000)]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    [Header("General")]
    [Tooltip("Untick to turn all shaking off (handy for an accessibility option later).")]
    [SerializeField] private bool shakeEnabled = true;
    [Tooltip("Global multiplier on every shake. 1 = as set on the bomb.")]
    [SerializeField, Min(0f)] private float intensityScale = 1f;

    [Header("Feel")]
    [Tooltip("How fast the camera jitters. Higher = more violent.")]
    [SerializeField, Min(1f)] private float frequency = 25f;
    [Tooltip("How much of the shake is up/down compared to sideways. 1 = same.")]
    [SerializeField, Range(0f, 1f)] private float verticalAmount = 1f;
    [Tooltip("Tilt of the camera in degrees at full strength. 0 = no tilt.")]
    [SerializeField, Min(0f)] private float rotationDegrees = 1.5f;
    [Tooltip("X = time (0 = start, 1 = end), Y = strength (0..1). Default hits hard, then fades out.")]
    [SerializeField] private AnimationCurve fadeOut = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -2f, 0f));

    [Header("Safety")]
    [Tooltip("The camera never moves further than this from where it should be.")]
    [SerializeField, Min(0f)] private float maxOffset = 0.8f;

    private Vector3 lastOffset;
    private float lastRotation;
    private float strength;
    private float duration;
    private float timeLeft;
    private Vector2 seed;

    private void Awake()
    {
        Instance = this;
        seed = new Vector2(Random.value * 100f, Random.value * 100f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Easy call from anywhere: CameraShake.Shake(0.4f, 0.5f);
    // Safe if there is no CameraShake in the scene (does nothing).
    public static void Shake(float magnitude, float seconds)
    {
        if (Instance != null) Instance.Play(magnitude, seconds);
    }

    public void Play(float magnitude, float seconds)
    {
        if (!shakeEnabled || magnitude <= 0f || seconds <= 0f) return;

        // A new shake only replaces the current one if it is stronger than what is left of it.
        if (magnitude < CurrentStrength()) return;

        strength = magnitude;
        duration = seconds;
        timeLeft = seconds;
    }

    private float CurrentStrength()
    {
        if (timeLeft <= 0f || duration <= 0f) return 0f;
        float t = 1f - timeLeft / duration;
        return strength * Mathf.Clamp01(fadeOut.Evaluate(t));
    }

    // Undo last frame's shake first, so any other script that moves the camera still works.
    private void Update()
    {
        transform.position -= lastOffset;
        transform.Rotate(0f, 0f, -lastRotation);
        lastOffset = Vector3.zero;
        lastRotation = 0f;
    }

    private void LateUpdate()
    {
        if (timeLeft <= 0f) return;

        float s = CurrentStrength() * intensityScale;
        timeLeft -= Time.unscaledDeltaTime;

        float time = Time.unscaledTime * frequency;
        float nx = Mathf.PerlinNoise(seed.x, time) * 2f - 1f;
        float ny = Mathf.PerlinNoise(seed.y, time) * 2f - 1f;
        float nr = Mathf.PerlinNoise(seed.x + 50f, time + 50f) * 2f - 1f;

        Vector3 offset = new Vector3(nx * s, ny * s * verticalAmount, 0f);
        offset = Vector3.ClampMagnitude(offset, maxOffset);
        float rotation = nr * s * rotationDegrees;

        transform.position += offset;
        transform.Rotate(0f, 0f, rotation);

        lastOffset = offset;
        lastRotation = rotation;
    }
}