using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    // Every live bomb, so the net can check them without physics.
    public static readonly List<Bomb> Active = new List<Bomb>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
    }

    [Header("Penalty")]
    [SerializeField, Min(0)] private int scoreLoss = 20;
    [Tooltip("Text shown when the bomb is caught. {0} becomes the score loss.")]
    [SerializeField] private string popupFormat = "-{0}";
    [SerializeField] private Color popupColor = new Color(0.95f, 0.15f, 0.15f, 1f);
    [Tooltip("How strongly the net reacts when the bomb lands. 1 = same as a normal flake.")]
    [SerializeField, Min(0f)] private float catchImpact = 3f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float minFallSpeed = 2.5f;
    [SerializeField, Min(0f)] private float maxFallSpeed = 4f;
    [Tooltip("Keep this low if your spark particles are a child of the bomb.")]
    [SerializeField] private float maxSpinSpeed = 20f;
    [SerializeField, Min(0f)] private float swayAmount = 0.3f;
    [SerializeField, Min(0f)] private float swaySpeed = 1.2f;

    [Header("Shockwave Reaction (when ANOTHER bomb's wave hits this one)")]
    [SerializeField, Min(0f)] private float nudgeDrag = 3f;
    [SerializeField] private float nudgeSpin = 20f;

    [Header("Explosion Effects")]
    [Tooltip("Prefab with the Shockwave script on it.")]
    [SerializeField] private Shockwave shockwavePrefab;
    [Tooltip("Optional particle burst (sparks, smoke). Spawned at the bomb's position.")]
    [SerializeField] private GameObject explosionVfxPrefab;
    [Tooltip("Seconds before the explosion VFX object is removed.")]
    [SerializeField, Min(0.1f)] private float explosionVfxLifetime = 2f;

    [Header("Camera Shake")]
    [Tooltip("How far the camera jolts. 0 = no shake.")]
    [SerializeField, Min(0f)] private float shakeStrength = 0.35f;
    [Tooltip("How long the shake lasts, in seconds.")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.45f;

    [Header("Safety")]
    [Tooltip("Removes the bomb after this many seconds even if it never leaves the screen.")]
    [SerializeField, Min(1f)] private float maxLifetime = 30f;

    public int ScoreLoss => scoreLoss;
    public Color PopupColor => popupColor;
    public float CatchImpact => catchImpact;
    public bool Detonated { get; private set; }

    public string PopupText
    {
        get
        {
            try { return string.Format(popupFormat, scoreLoss); }
            catch (System.FormatException) { return "-" + scoreLoss; }
        }
    }

    private float fallSpeed;
    private float spinSpeed;
    private float baseX;
    private float swayOffset;
    private float killY = float.NegativeInfinity;
    private float age;
    private Vector2 nudgeVelocity;

    private void OnEnable() { Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }

    // Called by the spawner right after creating the bomb (same as Flake).
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
        if (Detonated) return;

        float dt = Time.deltaTime;
        age += dt;

        Vector3 pos = transform.position;
        baseX += nudgeVelocity.x * dt;
        pos.y += (nudgeVelocity.y - fallSpeed) * dt;
        pos.x = baseX + Mathf.Sin(Time.time * swaySpeed + swayOffset) * swayAmount;
        transform.position = pos;
        nudgeVelocity *= Mathf.Exp(-nudgeDrag * dt);

        transform.Rotate(0f, 0f, spinSpeed * dt);

        if (pos.y < killY || age > maxLifetime) Destroy(gameObject);
    }

    // Called by a Shockwave.
    public void Nudge(Vector2 impulse)
    {
        if (Detonated) return;
        nudgeVelocity += impulse;
        spinSpeed -= impulse.x * nudgeSpin;
    }

    // Called by the NetCatcher when the bomb lands in the net.
    public void Detonate()
    {
        if (Detonated) return;
        Detonated = true;
        Active.Remove(this);

        Vector3 pos = transform.position;
        CameraShake.Shake(shakeStrength, shakeDuration);

        if (shockwavePrefab != null)
            Instantiate(shockwavePrefab, pos, Quaternion.identity);

        if (explosionVfxPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVfxPrefab, pos, Quaternion.identity);
            Destroy(vfx, explosionVfxLifetime);
        }

        Destroy(gameObject);
    }
}