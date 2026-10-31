using UnityEngine;

public class GameSpeed : MonoBehaviour
{
    public static GameSpeed Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    // Read by Flake and Bomb every frame. 1 if there is no GameSpeed in the scene.
    public static float FlakeMultiplier => Instance != null ? Instance.flakeMultiplier : 1f;
    public static float BombMultiplier => Instance != null ? Instance.bombMultiplier : 1f;

    [Header("Starting Speed (1 = normal)")]
    [SerializeField, Min(0.1f)] private float flakeStart = 1f;
    [SerializeField, Min(0.1f)] private float bombStart = 1f;

    [Header("Limits")]
    [SerializeField, Min(0.1f)] private float flakeMax = 3f;
    [SerializeField, Min(0.1f)] private float bombMax = 3.5f;

    [Header("Auto Ramp (gets faster as you play)")]
    [SerializeField] private bool autoRamp = true;
    [Tooltip("Seconds before the speed starts climbing.")]
    [SerializeField, Min(0f)] private float rampDelay = 10f;
    [Tooltip("Speed added per second. 0.01 = +0.6 per minute.")]
    [SerializeField, Min(0f)] private float flakeRampPerSecond = 0.01f;
    [SerializeField, Min(0f)] private float bombRampPerSecond = 0.015f;

    [Header("Live Values (you can edit these while playing)")]
    [SerializeField, Min(0.1f)] private float flakeMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float bombMultiplier = 1f;

    private float elapsed;

    private void Awake()
    {
        Instance = this;
        ResetSpeed();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        if (flakeMax < flakeStart) flakeMax = flakeStart;
        if (bombMax < bombStart) bombMax = bombStart;
    }

    private void Update()
    {
        if (!autoRamp) return;

        elapsed += Time.deltaTime;
        if (elapsed < rampDelay) return;

        flakeMultiplier = Mathf.Min(flakeMultiplier + flakeRampPerSecond * Time.deltaTime, flakeMax);
        bombMultiplier = Mathf.Min(bombMultiplier + bombRampPerSecond * Time.deltaTime, bombMax);
    }

    // ---- Call these from anywhere (a level system, score milestones, a restart button) ----

    public void ResetSpeed()
    {
        elapsed = 0f;
        flakeMultiplier = flakeStart;
        bombMultiplier = bombStart;
    }

    public void SetSpeed(float flake, float bomb)
    {
        flakeMultiplier = Mathf.Clamp(flake, 0.1f, flakeMax);
        bombMultiplier = Mathf.Clamp(bomb, 0.1f, bombMax);
    }

    public void AddSpeed(float flake, float bomb)
    {
        SetSpeed(flakeMultiplier + flake, bombMultiplier + bomb);
    }

    public void SetAutoRamp(bool value)
    {
        autoRamp = value;
    }
}