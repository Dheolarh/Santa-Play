using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class SantaController : MonoBehaviour
{
    public enum DragMode
    {
        Relative, // Santa moves by how far you drag. Best for touch (finger doesn't cover him).
        Absolute  // Santa goes to wherever your finger/mouse is.
    }

    [Header("References")]
    [Tooltip("Leave empty to use the Main Camera.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("The Net object. It must be a child of Santa.")]
    [SerializeField] private Transform net;

    [Header("Movement")]
    [SerializeField] private DragMode dragMode = DragMode.Relative;
    [Tooltip("1 = Santa moves exactly as far as your finger. Higher = faster.")]
    [SerializeField, Min(0.1f)] private float dragSensitivity = 1f;
    [Tooltip("Lower = snappier, higher = floatier.")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.08f;
    [SerializeField, Min(1f)] private float maxSpeed = 40f;
    [Tooltip("Extra gap kept between Santa and the screen edges.")]
    [SerializeField] private float edgePadding = 0.1f;

    [Header("Net Side")]
    [Tooltip("Tick if your net image has its hoop on the right (as in your design).")]
    [SerializeField] private bool netArtFacesRight = true;
    [Tooltip("Santa must move faster than this (units/sec) before the net switches sides.")]
    [SerializeField, Min(0f)] private float sideSwitchSpeed = 0.6f;
    [Tooltip("How long the net takes to swing to the other side.")]
    [SerializeField, Min(0.01f)] private float flipTime = 0.12f;

    [Header("Net Swing")]
    [Tooltip("Degrees of tilt per unit/sec of Santa's speed.")]
    [SerializeField] private float tiltPerSpeed = 1.2f;
    [SerializeField] private float maxTilt = 25f;
    [SerializeField, Min(0.01f)] private float tiltSmoothTime = 0.1f;
    [Tooltip("Tiny constant sway so the net never looks frozen.")]
    [SerializeField] private float idleSwingAngle = 2f;
    [SerializeField] private float idleSwingSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private Camera cam;

    // Movement state
    private float targetX;
    private float velocityX;
    private bool dragging;
    private float dragStartPointerX;
    private float dragStartSantaX;
    private bool controlEnabled = true;

    // Net state
    private Vector3 netBaseLocalPosition;
    private Vector3 netBaseScale;
    private float side = 1f;        // current side, animates between 1 (right) and -1 (left)
    private float targetSide = 1f;
    private float sideVelocity;
    private float tilt;
    private float tiltVelocity;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cam = targetCamera != null ? targetCamera : Camera.main;

        if (cam == null)
        {
            Debug.LogError("SantaController: no camera found. Tag your camera as MainCamera.", this);
            enabled = false;
            return;
        }

        targetX = transform.position.x;

        if (net == null)
        {
            Debug.LogWarning("SantaController: Net is not assigned. Santa will move but the net won't react.", this);
            return;
        }

        // Remember how the net was set up in the scene (assumed to start on the right).
        netBaseLocalPosition = net.localPosition;
        netBaseLocalPosition.x = Mathf.Abs(netBaseLocalPosition.x);
        netBaseScale = net.localScale;
        netBaseScale.x = Mathf.Abs(netBaseScale.x);
    }

    private void Update()
    {
        if (controlEnabled) ReadInput();
        MoveSanta();
        UpdateNet();
    }

    // Call this later from the pause menu / game over: SetControlEnabled(false)
    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        if (!value) dragging = false;
    }

    // ---------- Input ----------

    private void ReadInput()
    {
        // Pointer covers mouse, touch and pen with one code path.
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.isPressed)
        {
            dragging = false;
            return;
        }

        float pointerX = GetPointerWorldX(pointer);

        // A new press starts a new drag, so Santa never jumps when you touch the screen.
        if (!dragging || pointer.press.wasPressedThisFrame)
        {
            dragging = true;
            dragStartPointerX = pointerX;
            dragStartSantaX = targetX;
        }

        float desired = dragMode == DragMode.Relative
            ? dragStartSantaX + (pointerX - dragStartPointerX) * dragSensitivity
            : pointerX;

        float clamped = ClampToScreen(desired);

        // If Santa hit the edge, restart the drag from there so reversing responds instantly.
        if (dragMode == DragMode.Relative && !Mathf.Approximately(clamped, desired))
        {
            dragStartSantaX = clamped;
            dragStartPointerX = pointerX;
        }

        targetX = clamped;
    }

    private float GetPointerWorldX(Pointer pointer)
    {
        Vector2 screenPos = pointer.position.ReadValue();
        float depth = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth)).x;
    }

    private float ClampToScreen(float x)
    {
        float halfView = cam.orthographicSize * cam.aspect;
        float halfSanta = spriteRenderer.bounds.extents.x;
        float limit = Mathf.Max(0f, halfView - halfSanta - edgePadding);
        float camX = cam.transform.position.x;
        return Mathf.Clamp(x, camX - limit, camX + limit);
    }

    // ---------- Movement ----------

    private void MoveSanta()
    {
        // Re-clamp every frame so a resized browser window can't leave Santa off screen.
        targetX = ClampToScreen(targetX);

        Vector3 pos = transform.position;
        pos.x = Mathf.SmoothDamp(pos.x, targetX, ref velocityX, smoothTime, maxSpeed);
        transform.position = pos;
    }

    // ---------- Net ----------

    private void UpdateNet()
    {
        if (net == null) return;

        // Pick a side based on which way Santa is actually moving.
        if (velocityX > sideSwitchSpeed) targetSide = 1f;
        else if (velocityX < -sideSwitchSpeed) targetSide = -1f;

        side = Mathf.SmoothDamp(side, targetSide, ref sideVelocity, flipTime);
        if (Mathf.Abs(side - targetSide) < 0.001f) side = targetSide;

        // Slide to the other side and mirror the image.
        float artSign = netArtFacesRight ? 1f : -1f;
        net.localPosition = new Vector3(
            netBaseLocalPosition.x * side,
            netBaseLocalPosition.y,
            netBaseLocalPosition.z);
        net.localScale = new Vector3(
            netBaseScale.x * side * artSign,
            netBaseScale.y,
            netBaseScale.z);

        // Swing: the net leans back against the direction of travel, plus a tiny idle sway.
        float targetTilt = Mathf.Clamp(velocityX * tiltPerSpeed, -maxTilt, maxTilt);
        tilt = Mathf.SmoothDamp(tilt, targetTilt, ref tiltVelocity, tiltSmoothTime);
        float idle = Mathf.Sin(Time.time * idleSwingSpeed) * idleSwingAngle;
        net.localRotation = Quaternion.Euler(0f, 0f, tilt + idle);
    }
}