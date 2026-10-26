using UnityEngine;

public class NetBag : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The HoopCenter object (a sibling of this bag under the Net). The bag stretches down from this point.")]
    [SerializeField] private Transform anchor;

    [Header("Weight (sag)")]
    [Tooltip("How much longer the bag gets when the net is full. 0.5 = 50% longer.")]
    [SerializeField, Min(0f)] private float stretchAtFull = 0.5f;
    [Tooltip("How much narrower the bag gets when full. 0.1 = 10% narrower.")]
    [SerializeField, Range(0f, 0.5f)] private float narrowAtFull = 0.1f;

    [Header("Bounce")]
    [Tooltip("Bounces per second.")]
    [SerializeField, Min(0.1f)] private float bounceFrequency = 3.5f;
    [Tooltip("Lower = bouncier, higher = settles faster.")]
    [SerializeField, Range(0.05f, 1.5f)] private float bounceDamping = 0.3f;

    [Header("Sway")]
    [Tooltip("Sideways swing added per catch.")]
    [SerializeField, Min(0f)] private float swayPerJiggle = 40f;
    [SerializeField, Min(0.1f)] private float swayFrequency = 2.5f;
    [SerializeField, Range(0.05f, 1.5f)] private float swayDamping = 0.25f;
    [SerializeField, Min(0f)] private float maxSwayAngle = 15f;

    private float load;              // 0 = empty, 1 = full
    private float stretch;           // extra length, 0 = resting
    private float stretchVelocity;
    private float angle;
    private float angleVelocity;

    // Read by the NetCatcher so snow follows the bag.
    public Vector2 CurrentScale { get; private set; } = Vector2.one;
    public float CurrentAngle => angle;

    public void SetLoad(float value01)
    {
        load = Mathf.Clamp01(value01);
    }

    // Called on every catch. Bigger strength = bigger bounce.
    public void Jiggle(float strength)
    {
        stretchVelocity += strength;
        float direction = Random.value < 0.5f ? -1f : 1f;
        angleVelocity += direction * swayPerJiggle * strength;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.033f);

        float target = load * stretchAtFull;
        Spring(ref stretch, ref stretchVelocity, target, bounceFrequency, bounceDamping, dt);
        stretch = Mathf.Clamp(stretch, -0.25f, stretchAtFull * 1.6f + 0.25f);

        Spring(ref angle, ref angleVelocity, 0f, swayFrequency, swayDamping, dt);
        angle = Mathf.Clamp(angle, -maxSwayAngle, maxSwayAngle);

        Apply();
    }

    private static void Spring(ref float value, ref float velocity, float target,
                               float frequency, float damping, float dt)
    {
        float omega = 2f * Mathf.PI * frequency;
        float accel = -omega * omega * (value - target) - 2f * damping * omega * velocity;
        velocity += accel * dt;
        value += velocity * dt;
    }

    private void Apply()
    {
        float scaleY = 1f + stretch;
        float scaleX = 1f;
        if (stretchAtFull > 0.0001f)
        {
            scaleX = 1f - narrowAtFull * Mathf.Clamp(stretch / stretchAtFull, -0.5f, 1.5f);
        }

        Vector3 scale = new Vector3(scaleX, scaleY, 1f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // Scale and rotate around the hoop, so the top of the bag stays attached to the ring.
        Vector3 p = anchor != null ? anchor.localPosition : Vector3.zero;
        transform.localScale = scale;
        transform.localRotation = rotation;
        transform.localPosition = p - rotation * Vector3.Scale(scale, p);

        CurrentScale = new Vector2(scaleX, scaleY);
    }
}