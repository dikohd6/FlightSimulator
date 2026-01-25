using System.Collections.Generic;
using UnityEngine;

public class AirportTerrainBuilder : MonoBehaviour
{
    [Header("Where is the airport centered? (usually 0,0)")]
    public Vector2 airportCenterXZ = new Vector2(0, 0);

    [Header("How big should the FLAT airport area be?")]
    public Vector2 airportHalfSizeXZ = new Vector2(260, 260);
    public float airportWorldY = 0f;
    public float blendEdge = 70f; // smooth transition outside airport

    [Header("Terrain Size")]
    public int terrainSizeX = 1400;
    public int terrainSizeZ = 1400;
    public int terrainHeight = 140;
    [Header("Post-Fix Settings")]
    public string generatedLayerName = "Generated";
    public float buildingYOffset = 0.02f;   // small lift so it doesn't z-fight
    public float runwayClearExtra = 14f;    // extra runway corridor clear width
    public float runwaySink = 0.15f;        // push terrain slightly BELOW runway
    public float airportPadExtra = 60f;     // expand flat pad to cover terminal/hangars

    [Header("Terrain Resolution")]
    public int heightmapResolution = 513;   // 2^n + 1
    public int alphamapResolution = 512;
    public int detailResolution = 1024;

    [Header("Hills / Noise")]
    public bool generateHills = true;
    public float noiseScale = 0.0045f;
    [Range(0f, 0.4f)] public float noiseHeight = 0.14f; // normalized (0..1)
    public int noiseOctaves = 3;
    public float lacunarity = 2f;
    public float persistence = 0.5f;

    [Header("Terrain Layers (optional)")]
    public TerrainLayer grassLayer;
    public TerrainLayer dirtLayer;
    public TerrainLayer asphaltLayer; // optional: paints a strip inside airport

    [Header("Runway Corridor Width (for asphalt strip & clearing)")]
    public float safeHalfWidth = 26f;

    [Header("Grass Details (optional)")]
    public Texture2D grassDetailTexture;
    [Range(0, 1)] public float grassCoverage = 0.55f;
    [Range(0, 16)] public int grassDensity = 10;
    public float grassClearPadding = 12f;
    [Header("Runway No-Grass Zone")]
    public float runwayLength = 800f;          // set this close to your runway prefab length
    public float runwayHalfWidth = 35f;        // slightly wider than runway
    public float runwayYaw = 0f;               // if your runway is rotated
    public float runwayClearDetailsPadding = 10f;

    [Header("Trees (optional)")]
    public GameObject[] treePrefabs;
    [Range(0, 1)] public float treeCoverage = 0.18f;
    public int maxTrees = 2500;
    public float minTreeSpacing = 7f;
    [Header("Anti-Overlap Planes")]
    public string planeNameContains = "boeing";     // or "SM_" or "Learjet" etc.
    public float pushRadius = 14f;                 // size of plane bubble
    public int pushIterations = 6;                 // more = stronger separation
    public float pushStrength = 0.75f;             // 0.5-1.2 range

    [Header("Seed")]
    public bool useRandomSeed = true;
    public int seed = 1234;

    [Header("Build Options")]
    public bool buildOnStart = true;
    public bool destroyOldGeneratedTerrain = true;

    System.Random rng;
    TerrainData td;
    Vector3 terrainOrigin; // world position of terrain lower-left corner

    void Start()
    {
        if (buildOnStart)
            Build();
    }

    [ContextMenu("Build Terrain Now")]
    public void Build()
    {
        rng = new System.Random(useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : seed);

        if (destroyOldGeneratedTerrain)
        {
            var old = GameObject.Find("GeneratedTerrain");
            if (old) DestroyImmediate(old);
        }

        td = new TerrainData
        {
            heightmapResolution = heightmapResolution,
            alphamapResolution = alphamapResolution,
            size = new Vector3(terrainSizeX, terrainHeight, terrainSizeZ)
        };
        td.SetDetailResolution(detailResolution, 16);

        GameObject terrainGO = Terrain.CreateTerrainGameObject(td);
        terrainGO.name = "GeneratedTerrain";

        // Center the terrain around airportCenterXZ
        terrainOrigin = new Vector3(
            airportCenterXZ.x - terrainSizeX * 0.5f,
            0f,
            airportCenterXZ.y - terrainSizeZ * 0.5f
        );
        terrainGO.transform.position = terrainOrigin;

        var terrain = terrainGO.GetComponent<Terrain>();
        terrain.drawInstanced = true;

        // Layers
        var layers = new List<TerrainLayer>();
        if (grassLayer) layers.Add(grassLayer);
        if (dirtLayer) layers.Add(dirtLayer);
        if (asphaltLayer) layers.Add(asphaltLayer);
        if (layers.Count > 0) td.terrainLayers = layers.ToArray();

        // Heights
        td.SetHeights(0, 0, GenerateHeights());

        // Textures
        if (td.terrainLayers != null && td.terrainLayers.Length > 0)
            PaintAlphamaps();

        // Grass details
        if (grassDetailTexture)
            PaintGrassDetails();

        // Trees
        if (treePrefabs != null && treePrefabs.Length > 0)
            SpawnTrees();

        // Nice defaults
        terrain.detailObjectDistance = 140f;
        terrain.detailObjectDensity = 1f;
        terrain.treeDistance = 1000f;
        terrain.treeBillboardDistance = 140f;

        StartCoroutine(PostFixAfterAirportSpawns());
        System.Collections.IEnumerator PostFixAfterAirportSpawns()
        {
            yield return null; // wait 1 frame for AirportMapGenerator

            var terrain = GameObject.Find("GeneratedTerrain")?.GetComponent<Terrain>();
            if (!terrain) yield break;

            ForceFlattenAirportAndRunway(terrain);
            SnapAirportObjectsToTerrain(terrain);
            ClearGrassOnRunway(terrain);
            SeparateOverlappingPlanes();


        }

    }
    
    
    
    void SeparateOverlappingPlanes()
    {
        // Find likely planes by name (fast + simple)
        var all = GameObject.FindObjectsOfType<Transform>(true);
        var planes = new List<Transform>();

        foreach (var t in all)
        {
            string n = t.name.ToLower();
            if (n.Contains(planeNameContains.ToLower()))
                planes.Add(t);
        }

        if (planes.Count < 2) return;

        for (int it = 0; it < pushIterations; it++)
        {
            for (int i = 0; i < planes.Count; i++)
                for (int j = i + 1; j < planes.Count; j++)
                {
                    var a = planes[i];
                    var b = planes[j];
                    if (!a || !b) continue;

                    Vector3 pa = a.position;
                    Vector3 pb = b.position;

                    Vector2 da = new Vector2(pa.x, pa.z);
                    Vector2 db = new Vector2(pb.x, pb.z);

                    Vector2 diff = db - da;
                    float dist = diff.magnitude;

                    float minDist = pushRadius * 2f;

                    if (dist < 0.001f)
                    {
                        // same position: random small nudge
                        diff = new Vector2((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f).normalized;
                        dist = 0.01f;
                    }

                    if (dist < minDist)
                    {
                        Vector2 dir = diff / dist;
                        float overlap = (minDist - dist);

                        Vector2 push = dir * (overlap * 0.5f * pushStrength);

                        pa.x -= push.x; pa.z -= push.y;
                        pb.x += push.x; pb.z += push.y;

                        a.position = pa;
                        b.position = pb;
                    }
                }
        }
    }

    void ClearGrassOnRunway(Terrain terrain)
    {
        var td = terrain.terrainData;
        int dRes = td.detailResolution;
        int layers = td.detailPrototypes != null ? td.detailPrototypes.Length : 0;
        if (layers == 0) return;

        // Runway rectangle in world space
        Vector3 center = new Vector3(airportCenterXZ.x, 0f, airportCenterXZ.y);
        Quaternion rot = Quaternion.Euler(0f, runwayYaw, 0f);

        float halfW = runwayHalfWidth + runwayClearDetailsPadding;
        float halfL = runwayLength * 0.5f + runwayClearDetailsPadding;

        for (int layer = 0; layer < layers; layer++)
        {
            int[,] dl = td.GetDetailLayer(0, 0, dRes, dRes, layer);

            for (int z = 0; z < dRes; z++)
                for (int x = 0; x < dRes; x++)
                {
                    float nx = (float)x / (dRes - 1);
                    float nz = (float)z / (dRes - 1);

                    float wx = terrain.transform.position.x + nx * td.size.x;
                    float wz = terrain.transform.position.z + nz * td.size.z;

                    // convert world to runway-local
                    Vector3 local = Quaternion.Inverse(rot) * (new Vector3(wx, 0f, wz) - center);

                    bool inRunwayRect =
                        Mathf.Abs(local.x) < halfW &&
                        Mathf.Abs(local.z) < halfL;

                    if (inRunwayRect)
                        dl[z, x] = 0;
                }

            td.SetDetailLayer(0, 0, layer, dl);
        }
    }

    void ForceFlattenAirportAndRunway(Terrain terrain)
    {
        var td = terrain.terrainData;
        int res = td.heightmapResolution;

        float[,] h = td.GetHeights(0, 0, res, res);

        float flat01 = Mathf.Clamp01(airportWorldY / td.size.y);
        float sink01 = Mathf.Clamp01((airportWorldY - 0.25f) / td.size.y); // push terrain slightly BELOW runway

        float padHalfX = airportHalfSizeXZ.x + 100f; // big enough to cover terminals/hangars
        float padHalfZ = airportHalfSizeXZ.y + 100f;

        float runwayHalfX = safeHalfWidth + 20f;

        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1);
                float nz = (float)z / (res - 1);

                float wx = terrain.transform.position.x + nx * td.size.x;
                float wz = terrain.transform.position.z + nz * td.size.z;

                bool inPad =
                    Mathf.Abs(wx - airportCenterXZ.x) < padHalfX &&
                    Mathf.Abs(wz - airportCenterXZ.y) < padHalfZ;

                bool inRunway =
                    Mathf.Abs(wx - airportCenterXZ.x) < runwayHalfX;

                if (inPad)
                    h[z, x] = flat01;

                if (inPad && inRunway)
                    h[z, x] = sink01;
            }

        td.SetHeights(0, 0, h);
    }
    void SnapAirportObjectsToTerrain(Terrain terrain)
    {
        int genLayer = LayerMask.NameToLayer("Generated");
        if (genLayer < 0)
        {
            Debug.LogWarning("Create a layer named 'Generated' and assign it in AirportMapGenerator.");
            return;
        }

        var all = GameObject.FindObjectsOfType<Transform>(true);

        foreach (var tr in all)
        {
            if (tr.gameObject.layer != genLayer) continue;

            // Don't double-adjust children
            if (tr.parent != null && tr.parent.gameObject.layer == genLayer)
                continue;

            Vector3 p = tr.position;
            float y = terrain.SampleHeight(new Vector3(p.x, 0f, p.z)) + terrain.transform.position.y;

            // Keep runway exactly at airportWorldY
            if (tr.name.ToLower().Contains("runway"))
                p.y = airportWorldY;
            else
                p.y = y + 0.03f; // small lift to avoid z-fighting

            tr.position = p;
        }
    }

    float[,] GenerateHeights()
    {
        int res = td.heightmapResolution;
        float[,] h = new float[res, res];

        float padH = Mathf.Clamp01(airportWorldY / terrainHeight);

        // noise offsets
        float offX = NextFloat(-100000, 100000);
        float offZ = NextFloat(-100000, 100000);

        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1);
                float nz = (float)z / (res - 1);

                float wx = terrainOrigin.x + nx * td.size.x;
                float wz = terrainOrigin.z + nz * td.size.z;

                float baseH = 0f;
                if (generateHills)
                {
                    float n = FBM((wx + offX) * noiseScale, (wz + offZ) * noiseScale,
                                  noiseOctaves, lacunarity, persistence);
                    baseH = n * noiseHeight;
                }

                // Blend to flat pad inside airport
                float tPad = AirportBlend01(wx, wz);
                h[z, x] = Mathf.Lerp(baseH, padH, tPad);
            }

        return h;
    }

    void PaintAlphamaps()
    {
        int aRes = td.alphamapResolution;
        int layerCount = td.terrainLayers.Length;
        float[,,] map = new float[aRes, aRes, layerCount];

        int grassIdx = IndexOfLayer(grassLayer);
        int dirtIdx = IndexOfLayer(dirtLayer);
        int asphaltIdx = IndexOfLayer(asphaltLayer);

        for (int z = 0; z < aRes; z++)
            for (int x = 0; x < aRes; x++)
            {
                float nx = (float)x / (aRes - 1);
                float nz = (float)z / (aRes - 1);

                float wx = terrainOrigin.x + nx * td.size.x;
                float wz = terrainOrigin.z + nz * td.size.z;

                float tPad = AirportBlend01(wx, wz);

                float g = (grassIdx >= 0) ? 1f : 0f;
                float d = 0f;
                float a = 0f;

                // Dirt inside airport pad
                if (tPad > 0f && dirtIdx >= 0)
                {
                    d = Mathf.Clamp01(tPad * 0.85f);
                    g *= (1f - d);
                }

                // Asphalt strip down the middle (runway corridor)
                float dx = Mathf.Abs(wx - airportCenterXZ.x);
                if (tPad > 0f && asphaltIdx >= 0 && dx < safeHalfWidth * 0.65f)
                {
                    a = Mathf.Clamp01(tPad);
                    g *= (1f - a);
                    d *= (1f - a);
                }

                float sum = g + d + a;
                if (sum < 0.0001f) sum = 1f;

                if (grassIdx >= 0) map[z, x, grassIdx] = g / sum;
                if (dirtIdx >= 0) map[z, x, dirtIdx] = d / sum;
                if (asphaltIdx >= 0) map[z, x, asphaltIdx] = a / sum;
            }

        td.SetAlphamaps(0, 0, map);
    }

    void PaintGrassDetails()
    {
        td.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                prototypeTexture = grassDetailTexture,
                renderMode = DetailRenderMode.GrassBillboard,
                healthyColor = Color.white,
                dryColor = Color.white,
                minWidth = 0.8f,
                maxWidth = 1.7f,
                minHeight = 0.8f,
                maxHeight = 2.0f,
                noiseSpread = 0.12f
            }
        };

        int dRes = td.detailResolution;
        int[,] layer = new int[dRes, dRes];

        for (int z = 0; z < dRes; z++)
            for (int x = 0; x < dRes; x++)
            {
                float nx = (float)x / (dRes - 1);
                float nz = (float)z / (dRes - 1);

                float wx = terrainOrigin.x + nx * td.size.x;
                float wz = terrainOrigin.z + nz * td.size.z;

                // clear grass in/near airport pad
                if (IsInAirportWithPadding(wx, wz, grassClearPadding))
                {
                    layer[z, x] = 0;
                    continue;
                }

                layer[z, x] = (Next01() < grassCoverage) ? grassDensity : 0;
            }

        td.SetDetailLayer(0, 0, 0, layer);
    }

    void SpawnTrees()
    {
        var protos = new List<TreePrototype>();
        foreach (var p in treePrefabs)
            if (p) protos.Add(new TreePrototype { prefab = p });
        if (protos.Count == 0) return;

        td.treePrototypes = protos.ToArray();

        var trees = new List<TreeInstance>();
        var placed = new List<Vector2>();

        for (int i = 0; i < maxTrees; i++)
        {
            if (Next01() > treeCoverage) continue;

            float nx = Next01();
            float nz = Next01();

            float wx = terrainOrigin.x + nx * td.size.x;
            float wz = terrainOrigin.z + nz * td.size.z;

            // clear trees in airport pad + runway corridor
            if (IsInAirportWithPadding(wx, wz, 10f)) continue;
            if (Mathf.Abs(wx - airportCenterXZ.x) < safeHalfWidth + 18f) continue;

            // spacing
            var p2 = new Vector2(wx, wz);
            bool tooClose = false;
            for (int k = 0; k < placed.Count; k++)
            {
                if (Vector2.Distance(placed[k], p2) < minTreeSpacing) { tooClose = true; break; }
            }
            if (tooClose) continue;

            placed.Add(p2);

            trees.Add(new TreeInstance
            {
                prototypeIndex = NextInt(0, protos.Count),
                position = new Vector3(nx, 0f, nz),
                widthScale = Mathf.Lerp(0.85f, 1.25f, Next01()),
                heightScale = Mathf.Lerp(0.85f, 1.35f, Next01()),
                color = Color.white,
                lightmapColor = Color.white
            });
        }

        td.treeInstances = trees.ToArray();
    }

    // 0 outside, 1 inside, smooth edge
    float AirportBlend01(float worldX, float worldZ)
    {
        float dx = Mathf.Abs(worldX - airportCenterXZ.x);
        float dz = Mathf.Abs(worldZ - airportCenterXZ.y);

        float ax = airportHalfSizeXZ.x;
        float az = airportHalfSizeXZ.y;

        float insideX = Mathf.InverseLerp(ax + blendEdge, ax, dx);
        float insideZ = Mathf.InverseLerp(az + blendEdge, az, dz);

        return Mathf.Clamp01(Mathf.Min(insideX, insideZ));
    }

    bool IsInAirportWithPadding(float worldX, float worldZ, float pad)
    {
        float dx = Mathf.Abs(worldX - airportCenterXZ.x);
        float dz = Mathf.Abs(worldZ - airportCenterXZ.y);
        return dx < (airportHalfSizeXZ.x + pad) && dz < (airportHalfSizeXZ.y + pad);
    }

    int IndexOfLayer(TerrainLayer layer)
    {
        if (!layer || td.terrainLayers == null) return -1;
        for (int i = 0; i < td.terrainLayers.Length; i++)
            if (td.terrainLayers[i] == layer) return i;
        return -1;
    }

    static float FBM(float x, float z, int oct, float lac, float pers)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int i = 0; i < oct; i++)
        {
            sum += Mathf.PerlinNoise(x * freq, z * freq) * amp;
            norm += amp;
            amp *= pers;
            freq *= lac;
        }
        return (norm > 0f) ? (sum / norm) : 0f;
    }

    float Next01() => (float)rng.NextDouble();
    int NextInt(int min, int maxEx) => rng.Next(min, maxEx);
    float NextFloat(float min, float max) => min + (max - min) * Next01();
}
