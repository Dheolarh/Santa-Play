using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tiny object pool. Instead of Instantiate/Destroy every flake and popup (which causes
// garbage-collection hitches in WebGL), objects are switched off and reused.
//
//   Flake f = Pool.Get(prefab, position, Quaternion.identity);   // instead of Instantiate
//   Pool.Release(gameObject);                                    // instead of Destroy
//
// Objects that did not come from the pool are simply destroyed by Release, so it is always safe.
public static class Pool
{
    private static readonly Dictionary<GameObject, Stack<GameObject>> free =
        new Dictionary<GameObject, Stack<GameObject>>();

    // Which prefab each pooled instance came from.
    private static readonly Dictionary<GameObject, GameObject> prefabOf =
        new Dictionary<GameObject, GameObject>();

    private static Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Clear();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    // A scene reload (e.g. restart) destroys the pooled objects, so forget them.
    private static void OnSceneUnloaded(Scene scene)
    {
        Clear();
    }

    private static void Clear()
    {
        free.Clear();
        prefabOf.Clear();
        root = null;
    }

    // Creates the objects up front, so nothing is created while the player is playing.
    public static void Prewarm<T>(T prefab, int count) where T : Component
    {
        if (prefab == null) return;

        Stack<GameObject> stack = StackFor(prefab.gameObject);
        while (stack.Count < count)
            stack.Push(Create(prefab.gameObject));
    }

    // Takes an object from the pool (or makes a new one if the pool is empty) and switches it on.
    public static T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        if (prefab == null) return null;

        Stack<GameObject> stack = StackFor(prefab.gameObject);

        GameObject go = null;
        while (go == null && stack.Count > 0)
            go = stack.Pop(); // skips anything that was destroyed in the meantime

        if (go == null) go = Create(prefab.gameObject);

        // Position first, then activate, so OnEnable sees the right place.
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        return go.GetComponent<T>();
    }

    // Switches the object off and keeps it for reuse. Destroys it if it was not made by the pool.
    public static void Release(GameObject go)
    {
        if (go == null) return;

        if (!prefabOf.TryGetValue(go, out GameObject prefab))
        {
            Object.Destroy(go);
            return;
        }

        if (!go.activeSelf) return; // already released

        go.SetActive(false);
        StackFor(prefab).Push(go);
    }

    private static Stack<GameObject> StackFor(GameObject prefab)
    {
        if (!free.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            free[prefab] = stack;
        }
        return stack;
    }

    private static GameObject Create(GameObject prefab)
    {
        if (root == null) root = new GameObject("Pool").transform;

        GameObject go = Object.Instantiate(prefab, root);
        go.SetActive(false);
        prefabOf[go] = prefab;
        return go;
    }
}
