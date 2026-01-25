using System.Collections.Generic;
using UnityEngine;

public class AirportMapGenerator : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Runway (single)")]
    public GameObject runwayPrefab;
    public float runwayY = 0f;

    [Header("Airport Buildings")]
    public GameObject[] terminalPrefabs;
    public GameObject[] hangarPrefabs;
    public GameObject[] atcPrefabs;
    public GameObject[] storagePrefabs;

    [Header("Props / Vehicles / Planes")]
    public GameObject[] propPrefabs;
    public GameObject[] vehiclePrefabs;
    public GameObject[] parkedPlanePrefabs;

    [Header("Streaming (SMALL)")]
    public float chunkLength = 200f;
    public int chunksAhead = 2;
    public int chunksBehind = 1;

    [Header("Airport footprint (only build near origin)")]
    public int airportChunksRadius = 1;   // build buildings only in [-1..+1] chunks around chunk 0
    public float safeHalfWidth = 26f;

    [Header("Side placement")]
    public float leftBandX = -70f;
    public float rightBandX = 70f;

    [Header("No-overlap (optional)")]
    public bool useOverlapCheck = false;  // OFF by default so stuff actually spawns
    public LayerMask overlapMask;         // include layer "Generated" if you use it
    public float padding = 2.0f;
    public int maxTries = 8;

    [Header("Seed")]
    public bool useRandomSeed = true;
    public int seed = 1234;

    System.Random rng;
    readonly HashSet<int> spawned = new();
    readonly Dictionary<int, List<GameObject>> chunkObjects = new();

    void Start()
    {
        if (!player) { Debug.LogError("Assign player."); enabled = false; return; }
        rng = new System.Random(useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : seed);
        Stream(true);
    }

    void Update() => Stream(false);

    void Stream(bool force)
    {
        int playerChunk = Mathf.FloorToInt(player.position.z / chunkLength);
        int start = playerChunk - chunksBehind;
        int end = playerChunk + chunksAhead;

        for (int c = start; c <= end; c++)
            if (force || !spawned.Contains(c))
                SpawnChunk(c);

        var remove = new List<int>();
        foreach (var c in spawned)
            if (c < start - 1 || c > end + 1)
                remove.Add(c);

        for (int i = 0; i < remove.Count; i++)
            DespawnChunk(remove[i]);
    }

    void SpawnChunk(int chunkIndex)
    {
        spawned.Add(chunkIndex);

        float zStart = chunkIndex * chunkLength;
        float zCenter = zStart + chunkLength * 0.5f;

        var list = new List<GameObject>();
        chunkObjects[chunkIndex] = list;

        // ✅ Only one runway total (chunk 0)
        if (runwayPrefab && chunkIndex == 0)
        {
            var lane = Instantiate(runwayPrefab, new Vector3(0f, runwayY, 0f), Quaternion.identity, transform);
            lane.name = "Runway_SINGLE";
            list.Add(lane);
        }

        // ✅ Only build the airport near the origin, not forever
        if (Mathf.Abs(chunkIndex) > airportChunksRadius)
            return;

        // Small authored layout: terminal + hangars + storage + a little clutter
        SpawnTerminalApron(list, zStart);
        SpawnHangarZone(list, zStart);
        SpawnCargoZone(list, zStart);

        // Optional: runway edge line only near the airport
        SpawnRunwayEdgeLine(list, zStart);
    }

    void SpawnTerminalApron(List<GameObject> list, float zStart)
    {
        float zMid = zStart + chunkLength * 0.5f;

        PlaceBig(list, Pick(terminalPrefabs), new Vector3(rightBandX, runwayY, zMid), Quaternion.Euler(0, -90, 0));

        SpawnGrid(list, parkedPlanePrefabs, new Vector3(rightBandX - 25f, runwayY, zStart + 30f),
            cols: 2, rows: 2, spacingX: 18f, spacingZ: 40f, yaw: 90f);

        SpawnGrid(list, vehiclePrefabs, new Vector3(rightBandX - 10f, runwayY, zStart + 20f),
            cols: 2, rows: 3, spacingX: 10f, spacingZ: 18f, yaw: 90f);

        SpawnScatter(list, propPrefabs, centerX: rightBandX + 15f, zStart: zStart, count: 10, xRange: 18f, zRange: chunkLength);
    }

    void SpawnHangarZone(List<GameObject> list, float zStart)
    {
        float baseZ = zStart + 25f;

        SpawnRow(list, hangarPrefabs, new Vector3(leftBandX, runwayY, baseZ), count: 2, spacingZ: 75f, yaw: 90f);

        if (atcPrefabs != null && atcPrefabs.Length > 0 && Next01() < 0.5f)
            PlaceBig(list, Pick(atcPrefabs), new Vector3(leftBandX + 25f, runwayY, zStart + chunkLength * 0.6f), Quaternion.identity);

        SpawnScatter(list, propPrefabs, centerX: leftBandX + 25f, zStart: zStart, count: 12, xRange: 30f, zRange: chunkLength);
    }

    void SpawnCargoZone(List<GameObject> list, float zStart)
    {
        if (storagePrefabs == null || storagePrefabs.Length == 0) return;

        PlaceBig(list, Pick(storagePrefabs), new Vector3(leftBandX - 25f, runwayY, zStart + chunkLength * 0.5f), Quaternion.Euler(0, 90, 0));

        SpawnGrid(list, propPrefabs, new Vector3(leftBandX - 10f, runwayY, zStart + 35f),
            cols: 3, rows: 3, spacingX: 10f, spacingZ: 16f, yaw: 0f);
    }

    void SpawnRunwayEdgeLine(List<GameObject> list, float zStart)
    {
        if (propPrefabs == null || propPrefabs.Length == 0) return;

        int points = 6;
        float step = chunkLength / points;

        for (int i = 0; i <= points; i++)
        {
            float z = zStart + i * step;
            TryPlace(list, Pick(propPrefabs), new Vector3(-(safeHalfWidth + 6f), runwayY, z), Quaternion.identity, small: true);
            TryPlace(list, Pick(propPrefabs), new Vector3((safeHalfWidth + 6f), runwayY, z), Quaternion.identity, small: true);
        }
    }

    // -------- helpers --------

    void SpawnRow(List<GameObject> list, GameObject[] pool, Vector3 start, int count, float spacingZ, float yaw)
    {
        if (pool == null || pool.Length == 0) return;
        for (int i = 0; i < count; i++)
            PlaceBig(list, Pick(pool), start + new Vector3(0f, 0f, i * spacingZ), Quaternion.Euler(0, yaw, 0));
    }

    void SpawnGrid(List<GameObject> list, GameObject[] pool, Vector3 origin, int cols, int rows, float spacingX, float spacingZ, float yaw)
    {
        if (pool == null || pool.Length == 0) return;

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var p = Pick(pool);
                Vector3 pos = origin + new Vector3(c * spacingX, 0f, r * spacingZ);

                if (Mathf.Abs(pos.x) < safeHalfWidth + 6f) continue;

                TryPlace(list, p, pos, Quaternion.Euler(0, yaw, 0), small: true);
            }
    }

    void SpawnScatter(List<GameObject> list, GameObject[] pool, float centerX, float zStart, int count, float xRange, float zRange)
    {
        if (pool == null || pool.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            float x = centerX + NextFloat(-xRange, xRange);
            float z = zStart + NextFloat(0f, zRange);

            if (Mathf.Abs(x) < safeHalfWidth + 10f) continue;

            TryPlace(list, Pick(pool), new Vector3(x, runwayY, z), Quaternion.Euler(0, NextFloat(0f, 360f), 0f), small: true);
        }
    }

    void PlaceBig(List<GameObject> list, GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!prefab) return;

        for (int t = 0; t < maxTries; t++)
        {
            Vector3 p = pos + new Vector3(NextFloat(-4f, 4f), 0f, NextFloat(-8f, 8f));
            if (Mathf.Abs(p.x) < safeHalfWidth + 12f) continue;

            if (TryPlace(list, prefab, p, rot, small: false)) return;
        }

        // If overlap check is on and it keeps failing, just force-spawn once:
        if (useOverlapCheck == false)
        {
            var go = Instantiate(prefab, pos, rot, transform);
            SetGeneratedLayer(go);
            list.Add(go);
        }
    }

    bool TryPlace(List<GameObject> list, GameObject prefab, Vector3 pos, Quaternion rot, bool small)
    {
        if (!prefab) return false;

        // ✅ Most important: if overlap check is disabled, always place
        if (!useOverlapCheck)
        {
            var go = Instantiate(prefab, pos, rot, transform);
            SetGeneratedLayer(go);
            list.Add(go);
            return true;
        }

        // Simple overlap check (sphere) instead of unreliable prefab bounds
        float radius = small ? 3.0f : 8.0f;
        bool blocked = Physics.CheckSphere(pos, radius + padding, overlapMask, QueryTriggerInteraction.Ignore);
        if (blocked) return false;

        var inst = Instantiate(prefab, pos, rot, transform);
        SetGeneratedLayer(inst);
        list.Add(inst);
        return true;
    }

    void SetGeneratedLayer(GameObject go)
    {
        int layer = LayerMask.NameToLayer("Generated");
        if (layer < 0) return;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    void DespawnChunk(int chunkIndex)
    {
        if (!chunkObjects.TryGetValue(chunkIndex, out var list)) return;
        for (int i = 0; i < list.Count; i++)
            if (list[i]) Destroy(list[i]);
        chunkObjects.Remove(chunkIndex);
        spawned.Remove(chunkIndex);
    }

    GameObject Pick(GameObject[] arr) => (arr != null && arr.Length > 0) ? arr[NextInt(0, arr.Length)] : null;
    float Next01() => (float)rng.NextDouble();
    int NextInt(int min, int maxEx) => rng.Next(min, maxEx);
    float NextFloat(float min, float max) => min + (max - min) * Next01();
}
