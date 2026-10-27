using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Bomb bombPrefab;
    [Tooltip("Leave empty to use the Main Camera.")]
    [SerializeField] private Camera targetCamera;

    [Header("Timing")]
    [Tooltip("Seconds before the first bomb can appear.")]
    [SerializeField, Min(0f)] private float startDelay = 5f;
    [SerializeField, Min(0.1f)] private float minInterval = 4f;
    [SerializeField, Min(0.1f)] private float maxInterval = 8f;

    [Header("Limits")]
    [Tooltip("No new bomb spawns while this many are already falling.")]
    [SerializeField, Min(1)] private int maxBombsOnScreen = 2;

    [Header("Position")]
    [Tooltip("Keeps bombs away from the left/right screen edges.")]
    [SerializeField, Min(0f)] private float sidePadding = 0.5f;
    [Tooltip("How far above the top of the screen bombs appear.")]
    [SerializeField] private float spawnAboveScreen = 1f;
    [Tooltip("How far below the bottom of the screen bombs are removed.")]
    [SerializeField] private float killBelowScreen = 1.5f;

    private Camera cam;
    private float timer;
    private bool spawningEnabled = true;

    private void Awake()
    {
        cam = targetCamera != null ? targetCamera : Camera.main;

        if (cam == null)
        {
            Debug.LogError("BombSpawner: no camera found. Tag your camera as MainCamera.", this);
            enabled = false;
            return;
        }
        if (bombPrefab == null)
            Debug.LogError("BombSpawner: Bomb Prefab is not assigned.", this);

        timer = startDelay;
    }

    private void OnValidate()
    {
        if (maxInterval < minInterval) maxInterval = minInterval;
    }

    // Call with false on pause / game over, true to resume.
    public void SetSpawningEnabled(bool value)
    {
        spawningEnabled = value;
    }

    private void Update()
    {
        if (!spawningEnabled || bombPrefab == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (Bomb.Active.Count < maxBombsOnScreen) Spawn();
        timer = Random.Range(minInterval, maxInterval);
    }

    private void Spawn()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        float x = Random.Range(c.x - halfW + sidePadding, c.x + halfW - sidePadding);
        float y = c.y + halfH + spawnAboveScreen;

        Bomb bomb = Instantiate(bombPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        bomb.Init(c.y - halfH - killBelowScreen);
    }
}