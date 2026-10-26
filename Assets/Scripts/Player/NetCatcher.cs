using System.Collections.Generic;
using UnityEngine;

public class NetCatcher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Centre of the hoop opening. Must be a direct child of this Net object.")]
    [SerializeField] private Transform hoopCenter;
    [Tooltip("Bottom tip of the bag at rest. Must be a direct child of this Net object.")]
    [SerializeField] private Transform bagTip;
    [Tooltip("Optional. The bag that sags and bounces.")]
    [SerializeField] private NetBag bag;
    [Tooltip("Optional. Makes the whole net bob when snow lands.")]
    [SerializeField] private SantaController santa;
    [Tooltip("The +10 / Gold +30 text prefab.")]
    [SerializeField] private ScorePopup popupPrefab;

    [Header("Catch Zone (green outline in the Scene view)")]
    [Tooltip("Half-width and half-height of the opening, in the HoopCenter's own units.")]
    [SerializeField] private Vector2 hoopRadius = new Vector2(1f, 0.4f);
    [Tooltip("Makes catching more forgiving. 1 = exact, 1.2 = 20% bigger.")]
    [SerializeField, Min(0.5f)] private float catchForgiveness = 1.15f;

    [Header("Capacity & Melting")]
    [Tooltip("How many flakes fit before the snow melts.")]
    [SerializeField, Min(1)] private int capacity = 5;
    [Tooltip("How long the full net holds its snow before it starts melting.")]
    [SerializeField, Min(0f)] private float fullHoldTime = 0.35f;
    [SerializeField, Min(0.05f)] private float meltDuration = 0.6f;
    [Tooltip("Colour the snow turns while melting.")]
    [SerializeField] private Color meltTint = new Color(0.65f, 0.85f, 1f, 1f);
    [Tooltip("Off = the net can't catch anything until the melt has finished. On = it can catch while melting.")]
    [SerializeField] private bool catchWhileMelting = false;

    [Header("Net Feel")]
    [Tooltip("How hard the bag bounces on each catch.")]
    [SerializeField, Min(0f)] private float bagJiggle = 3f;
    [Tooltip("How hard the whole net bobs on each catch (degrees per second).")]
    [SerializeField, Min(0f)] private float netKick = 100f;

    [Header("Snow Inside The Net")]
    [Tooltip("0 = at the rim, 1 = at the tip. First flake sits here (deepest).")]
    [SerializeField, Range(0.05f, 1f)] private float slotDepthMax = 0.8f;
    [Tooltip("The last flake (when full) sits here (highest).")]
    [SerializeField, Range(0.05f, 1f)] private float slotDepthMin = 0.35f;
    [Tooltip("How far flakes spread sideways inside the bag.")]
    [SerializeField, Min(0f)] private float slotSpread = 0.15f;
    [Tooltip("Size of a caught flake compared to a falling one.")]
    [SerializeField, Range(0.2f, 1f)] private float caughtScale = 0.7f;
    [SerializeField] private int caughtSortingOrder = 3;

    [Header("Popup")]
    [Tooltip("Where the +10 appears, relative to the hoop centre.")]
    [SerializeField] private Vector3 popupOffset = new Vector3(0f, 0.5f, 0f);
    [Tooltip("Random sideways offset so quick catches don't stack exactly.")]
    [SerializeField, Min(0f)] private float popupJitterX = 0.3f;

    [Header("Score (view only while playing)")]
    [SerializeField] private int totalScore;

    public int TotalScore => totalScore;

    // (new total, points just added). The score HUD will listen to this later.
    public event System.Action<int, int> ScoreChanged;

    private readonly List<Flake> held = new List<Flake>();
    private readonly List<Flake> meltingFlakes = new List<Flake>();
    private bool melting;
    private float meltTimer;
    private float fullTimer;
    private bool catchingEnabled = true;

    private void Awake()
    {
        if (hoopCenter == null)
            Debug.LogError("NetCatcher: Hoop Center is not assigned.", this);
        if (popupPrefab == null)
            Debug.LogWarning("NetCatcher: Popup Prefab is not assigned, so no +10 text will show.", this);
    }

    // Call with false on pause / game over, true to resume.
    public void SetCatchingEnabled(bool value)
    {
        catchingEnabled = value;
    }

    public void ResetScore()
    {
        totalScore = 0;
        ScoreChanged?.Invoke(totalScore, 0);
    }

    private void Update()
    {
        if (hoopCenter == null) return;

        // Full -> hold briefly -> melt everything.
        if (!melting && held.Count >= capacity)
        {
            fullTimer += Time.deltaTime;
            if (fullTimer >= fullHoldTime) StartMelt();
        }

        if (melting)
        {
            meltTimer += Time.deltaTime;
            if (meltTimer >= meltDuration)
            {
                meltingFlakes.Clear(); // the flakes destroy themselves when finished
                melting = false;
            }
        }

        bool canCatch = catchingEnabled
                        && held.Count < capacity
                        && (catchWhileMelting || !melting);
        if (canCatch) ScanForFlakes();

        UpdateBagLoad();
    }

    // Runs after every Update so flakes use the net's final position this frame.
    private void LateUpdate()
    {
        if (hoopCenter == null) return;
        PlaceFlakes(held);
        PlaceFlakes(meltingFlakes);
    }

    // ---------- Catching ----------

    private void ScanForFlakes()
    {
        List<Flake> all = Flake.Active;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            Flake flake = all[i];
            if (flake == null || flake.State != Flake.FlakeState.Falling) continue;
            if (!IsInsideHoop(flake.transform.position)) continue;

            CatchFlake(flake);
            if (held.Count >= capacity) break;
        }
    }

    private bool IsInsideHoop(Vector3 worldPosition)
    {
        // Converting into the hoop's own space handles the flip, tilt and mirroring for free.
        Vector3 local = hoopCenter.InverseTransformPoint(worldPosition);
        float nx = local.x / (hoopRadius.x * catchForgiveness);
        float ny = local.y / (hoopRadius.y * catchForgiveness);
        return nx * nx + ny * ny <= 1f;
    }

    private void CatchFlake(Flake flake)
    {
        flake.BeginCatch(caughtScale, caughtSortingOrder, Random.Range(-1f, 1f));
        held.Add(flake);
        fullTimer = 0f;

        // Score
        int points = flake.Points;
        totalScore += points;
        ScoreChanged?.Invoke(totalScore, points);

        // +10 / Gold +30 text
        SpawnPopup(flake);

        // Net feel
        float impact = flake.CatchImpact;
        if (bag != null) bag.Jiggle(bagJiggle * impact);
        if (santa != null) santa.KickNet(netKick * impact);
    }

    private void SpawnPopup(Flake flake)
    {
        if (popupPrefab == null) return;

        Vector3 pos = hoopCenter.position + popupOffset;
        pos.x += Random.Range(-popupJitterX, popupJitterX);
        pos.z = 0f;

        ScorePopup popup = Instantiate(popupPrefab, pos, Quaternion.identity);
        popup.Play(flake.PopupText, flake.PopupColor);
    }

    // ---------- Melting ----------

    private void StartMelt()
    {
        melting = true;
        meltTimer = 0f;
        fullTimer = 0f;

        for (int i = 0; i < held.Count; i++)
        {
            Flake flake = held[i];
            if (flake == null) continue;
            flake.StartMelt(meltDuration, meltTint);
            meltingFlakes.Add(flake);
        }
        held.Clear();
    }

    private void UpdateBagLoad()
    {
        if (bag == null) return;

        // Melting snow gets lighter, so the bag rises back gradually.
        float melted = melting ? Mathf.Clamp01(meltTimer / meltDuration) : 0f;
        float count = held.Count + meltingFlakes.Count * (1f - melted);
        bag.SetLoad(count / capacity);
    }

    // ---------- Snow inside the bag ----------

    private void PlaceFlakes(List<Flake> list)
    {
        if (list.Count == 0) return;

        Vector3 anchor = hoopCenter.localPosition;
        Vector3 restDown = bagTip != null ? bagTip.localPosition - anchor : new Vector3(0f, -1f, 0f);

        Vector2 s = bag != null ? bag.CurrentScale : Vector2.one;
        Quaternion rot = Quaternion.Euler(0f, 0f, bag != null ? bag.CurrentAngle : 0f);

        // Where the bottom of the bag is right now (it moves as the bag sags and sways).
        Vector3 down = rot * new Vector3(restDown.x * s.x, restDown.y * s.y, 0f);
        Vector3 sideways = rot * Vector3.right;

        for (int i = 0; i < list.Count; i++)
        {
            Flake flake = list[i];
            if (flake == null) continue;

            float k = capacity > 1 ? Mathf.Clamp01(i / (float)(capacity - 1)) : 0f;
            float depth = Mathf.Lerp(slotDepthMax, slotDepthMin, k);
            float width = 1f - depth * 0.7f; // the bag is narrower near the tip

            Vector3 local = anchor + down * depth + sideways * (flake.SlotJitter * slotSpread * width);
            flake.transform.position = transform.TransformPoint(local);
        }
    }

    // ---------- Scene view helper ----------

    private void OnDrawGizmosSelected()
    {
        if (hoopCenter == null) return;

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = hoopCenter.localToWorldMatrix;
        Gizmos.color = Color.green;

        const int segments = 40;
        float rx = hoopRadius.x * catchForgiveness;
        float ry = hoopRadius.y * catchForgiveness;
        Vector3 prev = new Vector3(rx, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 p = new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        Gizmos.matrix = previous;

        if (bagTip != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(hoopCenter.position, bagTip.position);
        }
    }
}