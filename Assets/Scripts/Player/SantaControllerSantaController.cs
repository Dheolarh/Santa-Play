using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class SantaController : MonoBehaviour
{
    public enum DragMode
    {
        Relative, // Santa moves by how far you drag. Best for touch.
        Absolute  // Santa goes to wherever your finger/mouse is.
    }

    public enum NetFlipMode
    {
        Swipe,          // Net only changes side when you flick in the other direction.
        FollowMovement  // Net changes side whenever Santa starts moving the other way.
    }

    [Header("References")]
    [Tooltip("Leave empty to use the Main Camera.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("The Net root object. Child of Santa, on his RIGHT side, rotation 0.")]
    [SerializeField] private Transform net;

    [Header("Movement")]
    [SerializeField] private DragMode dragMode = DragMode.Relative;
    [Tooltip("1 = Santa moves exactly as far as your finger. Higher = faster.")]
    [SerializeField, Min(0.1f)] private float dragSensitivity = 1f;
    [Tooltip("Lower = snappier, higher = floatier.")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.08f;
    [SerializeField, Min(1f)] private float maxSpeed = 40f;
    [Tooltip("Extra gap kept between Santa/net and the screen edges.")]
    [SerializeField] private float edgePadding = 0.1f;

    [Header("Net Side")]
    [SerializeField] private NetFlipMode netFlipMode = NetFlipMode.Swipe;
    [Tooltip("Tick if your net image has its hoop on the right (as in your design).")]
    [SerializeField] private bool netArtFacesRight = true;
    [Tooltip("How long the net takes to swing to the other side.")]
    [SerializeField, Min(0.01f)] private float flipTime = 0.12f;

    [Header("Swipe Flip (Swipe mode)")]
    [Tooltip("Finger speed (units/sec) needed to count as a swipe. Higher = harder to flip by accident.")]
    [SerializeField, Min(1f)] private float swipeSpeed = 16f;
    [Tooltip("Smooths the finger speed so one jittery frame can't trigger a flip.")]
    [SerializeField, Min(0.001f)] private float swipeSmoothing = 0.05f;
    [Tooltip("Minimum time between two flips.")]
    [SerializeField, Min(0f)] private float flipCooldown = 0.2f;
    [Tooltip("Prints your drag speeds to the Console so you can tune Swipe Speed. Untick when done.")]
    [SerializeField] private bool logSwipes = true;

    [Header("Follow Movement Flip (FollowMovement mode)")]
    [SerializeField, Min(0f)] private float sideSwitchSpeed = 0.6f;

    [Header("Net Swing")]
    [Tooltip("Degrees of tilt per unit/sec of Santa's speed.")]
    [SerializeField] private float tiltPerSpeed = 1.2f;
    [SerializeField] private float maxTilt = 25f;
    [SerializeField, Min(0.01f)] private float tiltSmoothTime = 0.1f;
    [Tooltip("Tiny constant sway so the net never looks frozen.")]
    [SerializeField] private float idleSwingAngle = 2f;
    [SerializeField] private float idleSwingSpeed = 2f;

    [Header("Net Catch Reaction")]
    [Tooltip("How fast the net bobs after a catch (bounces per second).")]
    [SerializeField, Min(0.1f)] private float kickFrequency = 3.5f;
    [Tooltip("Lower = bouncier, higher = settles faster.")]
    [SerializeField, Range(0.05f, 1.5f)] private float kickDamping = 0.35f;


    [Header("Shockwave Shove")]
    [Tooltip("How fast Santa wobbles back after being pushed.")]
    [SerializeField, Min(0.1f)] private float shoveFrequency = 2.5f;
    [Tooltip("Lower = more wobbly, higher = settles faster.")]
    [SerializeField, Range(0.05f, 1.5f)] private float shoveDamping = 0.35f;
    [Tooltip("Furthest Santa can be pushed from where your finger wants him.")]
    [SerializeField, Min(0f)] private float maxShove = 2f;

    private SpriteRenderer spriteRenderer;
    private Camera cam;

    // Movement state
    private float targetX;
    private float velocityX;
    private float smoothedX;
    private float shoveX;
    private float shoveVelocity;
    private bool controlEnabled = true;

    // Drag / swipe state
    private bool dragging;
    private float dragStartPointerX;
    private float dragStartSantaX;
    private float lastPointerX;
    private float pointerSpeed;
    private float peakPointerSpeed;
    private float lastFlipTime = -999f;

    // Net state
    private Vector3 netBaseLocalPosition;
    private Vector3 netBaseScale;
    private float side = 1f;        // animates between 1 (right) and -1 (left)
    private float targetSide = 1f;
    private float sideVelocity;
    private float tilt;
    private float tiltVelocity;
    private float kickAngle;
    private float kickVelocity;

    // How far Santa + net stick out from Santa's centre.
    // "Outer" = on the side the net is on, "Inner" = the opposite side.
    private float outerReach;
    private float innerReach;

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
        smoothedX = targetX;
        outerReach = innerReach = spriteRenderer.bounds.extents.x;

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

        CacheNetReach();
    }

    private void Update()
    {
        if (controlEnabled) ReadInput();
        MoveSanta();
        UpdateNet();
    }

    // Call this from pause / game over: SetControlEnabled(false)
    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        if (!value) EndDrag();
    }

    // Called by the NetCatcher when snow lands in the net. Makes the whole net bob.
    public void KickNet(float degreesPerSecond)
    {
        // Push the hoop downward: clockwise when the net is on the right, counter-clockwise on the left.
        kickVelocity += -targetSide * degreesPerSecond;
    }

    // ---------- Screen limits ----------

    // Measures how far Santa + the whole net reach sideways, so everything stays on screen.
    private void CacheNetReach()
    {
        SpriteRenderer[] renderers = net.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        float santaX = transform.position.x;
        outerReach = Mathf.Max(outerReach, b.max.x - santaX);
        innerReach = Mathf.Max(innerReach, santaX - b.min.x);
    }

    private float ClampToScreen(float x)
    {
        // t = 1 when the net is on the right, 0 when on the left (blends during the flip).
        float t = (side + 1f) * 0.5f;
        float leftReach = Mathf.Lerp(outerReach, innerReach, t);
        float rightReach = Mathf.Lerp(innerReach, outerReach, t);

        float halfView = cam.orthographicSize * cam.aspect;
        float camX = cam.transform.position.x;

        float minX = camX - halfView + leftReach + edgePadding;
        float maxX = camX + halfView - rightReach - edgePadding;

        // Very narrow screens: just stay in the middle.
        if (minX > maxX)
        {
            float mid = (minX + maxX) * 0.5f;
            minX = maxX = mid;
        }

        return Mathf.Clamp(x, minX, maxX);
    }

    // ---------- Input ----------

    private void ReadInput()
    {
        // Pointer covers mouse, touch and pen with one code path.
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.isPressed)
        {
            EndDrag();
            return;
        }

        float pointerX = GetPointerWorldX(pointer);

        // A new press starts a new drag, so Santa never jumps when you touch the screen.
        if (!dragging || pointer.press.wasPressedThisFrame)
        {
            dragging = true;
            dragStartPointerX = pointerX;
            dragStartSantaX = targetX;
            lastPointerX = pointerX;
            pointerSpeed = 0f;
            peakPointerSpeed = 0f;
        }
        else
        {
            UpdatePointerSpeed(pointerX);
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

    private void EndDrag()
    {
        if (dragging && logSwipes && netFlipMode == NetFlipMode.Swipe)
        {
            Debug.Log("Drag ended. Fastest finger speed: " + peakPointerSpeed.ToString("F1") +
                      " (a swipe needs " + swipeSpeed.ToString("F1") + ")");
        }

        dragging = false;
        pointerSpeed = 0f;
    }

    private float GetPointerWorldX(Pointer pointer)
    {
        Vector2 screenPos = pointer.position.ReadValue();
        float depth = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth)).x;
    }

    private void UpdatePointerSpeed(float pointerX)
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float rawSpeed = (pointerX - lastPointerX) / dt;
        lastPointerX = pointerX;

        // Smooth it so a single jittery frame can't look like a swipe.
        float blend = 1f - Mathf.Exp(-dt / swipeSmoothing);
        pointerSpeed = Mathf.Lerp(pointerSpeed, rawSpeed, blend);
        peakPointerSpeed = Mathf.Max(peakPointerSpeed, Mathf.Abs(pointerSpeed));

        if (netFlipMode == NetFlipMode.Swipe) TrySwipeFlip();
    }

    private void TrySwipeFlip()
    {
        if (Mathf.Abs(pointerSpeed) < swipeSpeed) return;
        if (Time.time - lastFlipTime < flipCooldown) return;

        float direction = pointerSpeed > 0f ? 1f : -1f;

        // Swiping the same way the net already faces does nothing.
        if (Mathf.Approximately(direction, targetSide)) return;

        targetSide = direction;
        lastFlipTime = Time.time;

        if (logSwipes)
        {
            Debug.Log("Swipe! Net -> " + (direction > 0f ? "right" : "left") +
                      " (speed " + Mathf.Abs(pointerSpeed).ToString("F1") + ")");
        }
    }

    // ---------- Movement ----------

        private void MoveSanta()
    {
        targetX = ClampToScreen(targetX);
        smoothedX = Mathf.SmoothDamp(smoothedX, targetX, ref velocityX, smoothTime, maxSpeed);

        // Shockwave shove: a damped spring that settles back to zero.
        float dt = Mathf.Min(Time.deltaTime, 0.033f);
        float omega = 2f * Mathf.PI * shoveFrequency;
        float accel = -omega * omega * shoveX - 2f * shoveDamping * omega * shoveVelocity;
        shoveVelocity += accel * dt;
        shoveX += shoveVelocity * dt;
        shoveX = Mathf.Clamp(shoveX, -maxShove, maxShove);

        Vector3 pos = transform.position;
        pos.x = ClampToScreen(smoothedX + shoveX); // still can't leave the screen
        transform.position = pos;
    }

    // Called by a Shockwave. Positive = push right, negative = push left.
    public void Shove(float unitsPerSecond)
    {
        shoveVelocity += unitsPerSecond;
    }

    // ---------- Net ----------

    private void UpdateNet()
    {
        if (net == null) return;

        if (netFlipMode == NetFlipMode.FollowMovement)
        {
            if (velocityX > sideSwitchSpeed) targetSide = 1f;
            else if (velocityX < -sideSwitchSpeed) targetSide = -1f;
        }

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

        // Catch bounce: a damped spring that settles back to zero.
        float dt = Mathf.Min(Time.deltaTime, 0.033f);
        float omega = 2f * Mathf.PI * kickFrequency;
        float accel = -omega * omega * kickAngle - 2f * kickDamping * omega * kickVelocity;
        kickVelocity += accel * dt;
        kickAngle += kickVelocity * dt;

        net.localRotation = Quaternion.Euler(0f, 0f, tilt + idle + kickAngle);
    }
}