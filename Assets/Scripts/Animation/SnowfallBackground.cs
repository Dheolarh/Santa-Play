using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SnowfallBackground : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Leave empty to use the Main Camera.")]
    [SerializeField] private Camera targetCamera;

    [Header("Amount")]
    [Tooltip("New flakes per second. Higher = denser snow.")]
    [SerializeField, Min(0f)] private float flakesPerSecond = 6f;
    [SerializeField, Min(1)] private int maxFlakes = 300;

    [Header("Look")]
    [SerializeField, Min(0.01f)] private float minSize = 0.08f;
    [SerializeField, Min(0.01f)] private float maxSize = 0.35f;
    [Tooltip("Faintest a flake can be. 1 = all flakes fully white.")]
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.4f;
    [SerializeField] private float maxSpinDegreesPerSecond = 45f;

    [Header("Motion")]
    [SerializeField, Min(0.1f)] private float minFallSpeed = 1f;
    [SerializeField, Min(0.1f)] private float maxFallSpeed = 2.5f;
    [Tooltip("How far flakes drift sideways.")]
    [SerializeField, Min(0f)] private float swayStrength = 0.3f;
    [Tooltip("How quickly the drift changes direction.")]
    [SerializeField, Min(0f)] private float swayFrequency = 0.3f;

    [Header("Layering")]
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Background is -10, Santa is 5. Snow sits in between.")]
    [SerializeField] private int orderInLayer = -5;

    [Header("Area")]
    [Tooltip("How far above/below and beside the screen flakes live, so none pop in or out.")]
    [SerializeField, Min(0f)] private float offscreenMargin = 1f;

    private ParticleSystem ps;
    private Camera cam;
    private float lastAspect = -1f;
    private float lastSize = -1f;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        cam = targetCamera != null ? targetCamera : Camera.main;

        if (cam == null)
        {
            Debug.LogError("SnowfallBackground: no camera found. Tag your camera as MainCamera.", this);
            return;
        }

        // Duration and prewarm can only be changed while stopped.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ConfigureSystem();
        UpdateArea();

        ps.Play();
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // Refit only if the camera shape changed (e.g. the browser window was resized).
        if (!Mathf.Approximately(cam.aspect, lastAspect) ||
            !Mathf.Approximately(cam.orthographicSize, lastSize))
        {
            UpdateArea();
        }
    }

    private void ConfigureSystem()
    {
        float lowSpeed = Mathf.Min(minFallSpeed, maxFallSpeed);
        float highSpeed = Mathf.Max(minFallSpeed, maxFallSpeed);

        // Main
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.playOnAwake = true;
        main.prewarm = true; // screen is already full of snow on frame 1
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.startSpeed = 0f;
        main.maxParticles = maxFlakes;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, minAlpha), Color.white);

        // Emission
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = flakesPerSecond;

        // Shape: a wide strip above the screen
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.rotation = Vector3.zero;

        // Fall speed (each flake picks its own speed)
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-highSpeed, -lowSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Sideways drift
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = swayStrength;
        noise.strengthY = 0f;
        noise.strengthZ = 0f;
        noise.frequency = swayFrequency;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Low;

        // Spin
        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        float spin = maxSpinDegreesPerSecond * Mathf.Deg2Rad;
        rotation.z = new ParticleSystem.MinMaxCurve(-spin, spin);

        // Renderer
        ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortingLayerName = sortingLayerName;
        psRenderer.sortingOrder = orderInLayer;
    }

    // Sizes and positions the snow strip to match the camera.
    private void UpdateArea()
    {
        lastAspect = cam.aspect;
        lastSize = cam.orthographicSize;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        // Sit just above the top edge of the screen.
        transform.rotation = Quaternion.identity;
        transform.position = new Vector3(camPos.x, camPos.y + halfHeight + offscreenMargin, 0f);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.scale = new Vector3((halfWidth + offscreenMargin) * 2f, 0.1f, 0.1f);

        // Live exactly long enough to cross the screen at the slowest speed.
        float slowest = Mathf.Max(0.1f, Mathf.Min(minFallSpeed, maxFallSpeed));
        float travel = halfHeight * 2f + offscreenMargin * 2f;

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = travel / slowest;
    }
}