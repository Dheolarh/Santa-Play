using UnityEngine;

public class FlakeSpawner : MonoBehaviour
{
    [System.Serializable]
    public class FlakeOption
    {
        public Flake prefab;
        [Min(0f)] public float weight = 1f;
    }

    [Header("What to spawn")]
    [SerializeField] private FlakeOption[] options;

    [Header("Timing")]
    [SerializeField] private float spawnInterval = 0.6f;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Area")]
    [Tooltip("Keeps flakes away from the left and right screen edges.")]
    [SerializeField] private float sideMargin = 0.6f;
    [Tooltip("How far above/below the screen flakes appear/disappear.")]
    [SerializeField] private float offscreenOffset = 1f;

    private Camera cam;
    private float timer;
    private bool spawning;

    private void Awake()
    {
        cam = Camera.main;
        spawning = spawnOnStart;

        if (cam == null)
            Debug.LogError("FlakeSpawner: no camera tagged MainCamera found.", this);
        if (options == null || options.Length == 0)
            Debug.LogWarning("FlakeSpawner: the Options list is empty.", this);
    }

    private void Update()
    {
        if (!spawning || cam == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnOne();
        }
    }

    // We'll call these from the game timer later.
    public void StartSpawning() { spawning = true; }
    public void StopSpawning() { spawning = false; }

    private void SpawnOne()
    {
        Flake prefab = PickPrefab();
        if (prefab == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 center = cam.transform.position;

        float x = Random.Range(center.x - halfWidth + sideMargin, center.x + halfWidth - sideMargin);
        float spawnY = center.y + halfHeight + offscreenOffset;
        float killY = center.y - halfHeight - offscreenOffset;

        Flake flake = Instantiate(prefab, new Vector3(x, spawnY, 0f), Quaternion.identity);
        flake.Init(killY);
    }

    private Flake PickPrefab()
    {
        if (options == null) return null;

        float total = 0f;
        foreach (FlakeOption o in options)
            if (o.prefab != null) total += o.weight;

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (FlakeOption o in options)
        {
            if (o.prefab == null) continue;
            roll -= o.weight;
            if (roll <= 0f) return o.prefab;
        }
        return null;
    }
}