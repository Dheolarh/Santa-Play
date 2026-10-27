using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Flake : MonoBehaviour
{
    public enum FlakeState { Falling, Caught, Melting }

    // Every live flake, so the net can check them without physics.
    public static readonly List<Flake> Active = new List<Flake>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
    }

    [Header("Value")]
    [SerializeField, Min(0)] private int points = 10;
    [Tooltip("Text shown when this flake is caught. {0} becomes the points. Examples: +{0}   or   Gold +{0}")]
    [SerializeField] private string popupFormat = "+{0}";
    [SerializeField] private Color popupColor = new Color(0.96f, 0.69f, 0.18f, 1f);
    [Tooltip("How strongly the net reacts when this flake lands. 1 = normal, 2 = double.")]
    [SerializeField, Min(0f)] private float catchImpact = 1f;

    [Header("Movement")]
    [Header("Shockwave Reaction")]
    [Tooltip("How quickly a shockwave push fades out. Higher = stops sooner.")]
    [SerializeField, Min(0f)] private float nudgeDrag = 3f;
    [Tooltip("How much a push makes the flake spin.")]
    [SerializeField] private float nudgeSpin = 20f;
    [SerializeField, Min(0f)] private float minFallSpeed = 2.5f;
    [SerializeField, Min(0f)] private float maxFallSpeed = 4f;
    [SerializeField] private float maxSpinSpeed = 60f;
    [SerializeField, Min(0f)] private float swayAmount = 0.4f;
    [SerializeField, Min(0f)] private float swaySpeed = 1.5f;

    public int Points => points;
    public Color PopupColor => popupColor;
    public float CatchImpact => catchImpact;
    public FlakeState State { get; private set; } = FlakeState.Falling;
    public float SlotJitter { get; private set; }

    public string PopupText
    {
        get
        {
            try { return string.Format(popupFormat, points); }
            catch (System.FormatException) { return points.ToString(); }
        }
    }

    private const float CaughtPopTime = 0.12f;

    private SpriteRenderer spriteRenderer;
    private float fallSpeed;
    private float spinSpeed;
    private float baseX;
    private float swayOffset;
    private float killY = float.NegativeInfinity;
    private Vector2 nudgeVelocity;

    private Vector3 caughtFromScale;
    private Vector3 caughtToScale;
    private float caughtTimer;

    private float meltTimer;
    private float meltDuration = 0.6f;
    private Vector3 meltFromScale;
    private Color meltFromColor;
    private Color meltTint;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    // Called by the spawner right after creating the flake.
    public void Init(float killLineY)
    {
        killY = killLineY;
    }

    private void Start()
    {
        fallSpeed = Random.Range(minFallSpeed, maxFallSpeed);
        spinSpeed = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        swayOffset = Random.Range(0f, Mathf.PI * 2f);
        baseX = transform.position.x;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        switch (State)
        {
            case FlakeState.Falling: Fall(dt); break;
            case FlakeState.Caught: UpdateCaught(dt); break;
            case FlakeState.Melting: UpdateMelting(dt); break;
        }
    }

        private void Fall(float dt)
    {
        Vector3 pos = transform.position;

        baseX += nudgeVelocity.x * dt;
        pos.y += (nudgeVelocity.y - fallSpeed) * dt;
        pos.x = baseX + Mathf.Sin(Time.time * swaySpeed + swayOffset) * swayAmount;
        transform.position = pos;
        nudgeVelocity *= Mathf.Exp(-nudgeDrag * dt);

        transform.Rotate(0f, 0f, spinSpeed * dt);

        if (pos.y < killY) Destroy(gameObject);
    }

    // Called by a Shockwave. Only falling flakes react; caught ones stay in the net.
    public void Nudge(Vector2 impulse)
    {
        if (State != FlakeState.Falling) return;
        nudgeVelocity += impulse;
        spinSpeed -= impulse.x * nudgeSpin;
    }

    // The net calls this when the flake lands in the bag. The net then positions it.
    public void BeginCatch(float scaleMultiplier, int sortingOrder, float slotJitter)
    {
        State = FlakeState.Caught;
        SlotJitter = slotJitter;
        spriteRenderer.sortingOrder = sortingOrder;

        caughtFromScale = transform.localScale;
        caughtToScale = transform.localScale * scaleMultiplier;
        caughtTimer = 0f;
    }

    private void UpdateCaught(float dt)
    {
        // Quick shrink into the bag, then a slow lazy spin.
        caughtTimer += dt;
        float t = Mathf.Clamp01(caughtTimer / CaughtPopTime);
        float eased = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.Lerp(caughtFromScale, caughtToScale, eased);

        transform.Rotate(0f, 0f, spinSpeed * 0.2f * dt);
    }

    public void StartMelt(float duration, Color tint)
    {
        State = FlakeState.Melting;
        meltDuration = Mathf.Max(0.05f, duration);
        meltTimer = 0f;
        meltFromScale = transform.localScale;
        meltFromColor = spriteRenderer.color;
        meltTint = tint;
    }

    private void UpdateMelting(float dt)
    {
        meltTimer += dt;
        float t = Mathf.Clamp01(meltTimer / meltDuration);

        // Shrink away, turn watery blue, fade out.
        transform.localScale = meltFromScale * (1f - t);

        Color c = Color.Lerp(meltFromColor, meltTint, Mathf.Clamp01(t * 2f));
        c.a = 1f - t;
        spriteRenderer.color = c;

        if (t >= 1f) Destroy(gameObject);
    }
}