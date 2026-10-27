using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Shockwave : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("Leave empty to use a generated soft ring. Any sprite works; it is scaled to match the radius.")]
    [SerializeField] private Sprite ringSprite;
    [SerializeField] private Color startColor = new Color(1f, 0.85f, 0.5f, 0.9f);
    [SerializeField] private Color endColor = new Color(1f, 0.35f, 0.1f, 0f);
    [SerializeField] private int sortingOrder = 15;

    [Header("Size & Timing")]
    [SerializeField, Min(0.1f)] private float maxRadius = 4f;
    [SerializeField, Min(0.05f)] private float duration = 0.6f;
    [Tooltip("X = time (0..1), Y = how far the ring has grown (0..1). Default is fast start, slow finish.")]
    [SerializeField] private AnimationCurve expandCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Push Strength")]
    [Tooltip("X = distance from the bomb (0 = centre, 1 = edge of the max radius), Y = strength (0..1).")]
    [SerializeField] private AnimationCurve falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [Tooltip("Tilts the push upward so snow gets blown up and away, not only sideways.")]
    [SerializeField, Range(0f, 1f)] private float upwardBias = 0.35f;

    [Header("Flakes")]
    [SerializeField] private bool pushFlakes = true;
    [Tooltip("Speed given to a flake at the centre (units per second).")]
    [SerializeField, Min(0f)] private float flakeForce = 7f;

    [Header("Other Bombs")]
    [SerializeField] private bool pushBombs = true;
    [SerializeField, Min(0f)] private float bombForce = 7f;

    [Header("Santa")]
    [SerializeField] private bool pushSanta = true;
    [Tooltip("Sideways shove given to Santa (units per second).")]
    [SerializeField, Min(0f)] private float santaShove = 14f;
    [Tooltip("How hard the net bobs when the wave hits Santa (degrees per second).")]
    [SerializeField, Min(0f)] private float santaNetKick = 300f;

    private static Sprite cachedRing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedRing = null;
    }

    private readonly HashSet<Component> pushed = new HashSet<Component>();
    private SpriteRenderer sr;
    private SantaController santa;
    private Vector3 center;
    private float elapsed;
    private bool santaPushed;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = ringSprite != null ? ringSprite : GetDefaultRing();
        sr.sortingOrder = sortingOrder;
    }

    private void Start()
    {
        center = transform.position;
        if (pushSanta) santa = FindFirstObjectByType<SantaController>();
        Apply(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        float radius = Apply(t);
        Push(radius);

        if (t >= 1f) Destroy(gameObject);
    }

    private float Apply(float t)
    {
        float radius = maxRadius * Mathf.Clamp01(expandCurve.Evaluate(t));

        float spriteRadius = Mathf.Max(0.0001f, sr.sprite.bounds.extents.x);
        float s = Mathf.Max(0.0001f, radius / spriteRadius);
        transform.localScale = new Vector3(s, s, 1f);

        sr.color = Color.Lerp(startColor, endColor, t);
        return radius;
    }

    // Each object is pushed once, at the moment the ring reaches it.
    private void Push(float radius)
    {
        if (pushFlakes)
        {
            List<Flake> flakes = Flake.Active;
            for (int i = 0; i < flakes.Count; i++)
            {
                Flake f = flakes[i];
                if (f == null || pushed.Contains(f)) continue;
                if (!TryGetPush(f.transform.position, radius, flakeForce, out Vector2 impulse)) continue;

                pushed.Add(f);
                f.Nudge(impulse);
            }
        }

        if (pushBombs)
        {
            List<Bomb> bombs = Bomb.Active;
            for (int i = 0; i < bombs.Count; i++)
            {
                Bomb b = bombs[i];
                if (b == null || pushed.Contains(b)) continue;
                if (!TryGetPush(b.transform.position, radius, bombForce, out Vector2 impulse)) continue;

                pushed.Add(b);
                b.Nudge(impulse);
            }
        }

        if (pushSanta && santa != null && !santaPushed)
        {
            Vector3 p = santa.transform.position;
            if (TryGetPush(p, radius, 1f, out _))
            {
                santaPushed = true;
                float strength = falloff.Evaluate(Mathf.Clamp01(Vector2.Distance(center, p) / maxRadius));
                float dir = p.x >= center.x ? 1f : -1f;
                santa.Shove(dir * santaShove * strength);
                santa.KickNet(santaNetKick * strength);
            }
        }
    }

    private bool TryGetPush(Vector3 position, float radius, float force, out Vector2 impulse)
    {
        impulse = Vector2.zero;

        Vector2 offset = (Vector2)(position - center);
        float dist = offset.magnitude;
        if (dist > radius) return false;

        Vector2 dir = dist > 0.001f ? offset / dist : Vector2.up;
        dir.y += upwardBias;
        dir.Normalize();

        float strength = falloff.Evaluate(Mathf.Clamp01(dist / maxRadius));
        impulse = dir * (force * strength);
        return true;
    }

    // Soft ring, generated once and reused.
    private static Sprite GetDefaultRing()
    {
        if (cachedRing != null) return cachedRing;

        const int size = 256;
        const float bandCenter = 0.88f;
        const float bandHalf = 0.12f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - bandCenter) / bandHalf);
                a = a * a * (3f - 2f * a);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        cachedRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        return cachedRing;
    }
}