#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AirportMapGenerator))]
public class AutoFillAirportPrefabs : Editor
{
    // Helper: add names WITHOUT ".prefab"
    static readonly string[] RunwayNames =
    {
        "Runway"
    };

    static readonly string[] TerminalNames =
    {
        "Terminal", "Terminal-2", "SM_Jetway"
    };

    static readonly string[] HangarNames =
    {
        "Hangar", "Hangar_Large", "Hangar_Middle", "Hangar_Small"
    };

    static readonly string[] ATCNames =
    {
        "ATC Building", "SM_ATC_Building"
    };

    static readonly string[] StorageNames =
    {
        "Industrial Building Modules",
        "Storage", "Storage0", "Storage 2", "Storage 3", "Storage 4", "Storage 5", "Storage 6"
    };

    static readonly string[] PropNames =
    {
        "LampIndustrial","lamppost","fence","bench",
        "barrelBlue","barrelGray","barrelRed","barrelYellow",
        "stackOfBarrels","stackOfBoxes",
        "containerYellow","concreteBlocks",
        "hydrant2",
        "rock07","rocks",
        "woodGarbage","woodbox","woodplank",
        "barelTrash","barelTrashGarbage",
        "Localizer","Poles"
    };

    static readonly string[] VehicleNames =
    {
        "SM_Apron_Bus","SM_Pushback_Tug",
        "SM_baggage_tractor","SM_baggage_tractor_cart",
        "SM_boarding_stairs"
    };

    static readonly string[] ParkedPlaneNames =
    {
        "SM_boeing737","SM_boeing737_Gray","SM_boeing737_Red","SM_boeing737_Yellow","SM_boeing737_BlueTail",
        "SM_Boeing777","SM_Boeing777_Red","SM_Boeing777_RedTail",
        "SM_Learjet45","SM_Learjet45_Gray",
        "SM_Helicopter","SM_Helicopter_Color2","SM_Helicopter_Color3"
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Fill Prefabs (Search Project)"))
        {
            var gen = (AirportMapGenerator)target;

            gen.runwayPrefab = FindFirstPrefab(RunwayNames);

            gen.terminalPrefabs = FindPrefabs(TerminalNames);
            gen.hangarPrefabs = FindPrefabs(HangarNames);
            gen.atcPrefabs = FindPrefabs(ATCNames);
            gen.storagePrefabs = FindPrefabs(StorageNames);

            gen.propPrefabs = FindPrefabs(PropNames);
            gen.vehiclePrefabs = FindPrefabs(VehicleNames);
            gen.parkedPlanePrefabs = FindPrefabs(ParkedPlaneNames);

            EditorUtility.SetDirty(gen);

            Debug.Log(
                "Auto-fill complete.\n" +
                $"Runway: {(gen.runwayPrefab ? gen.runwayPrefab.name : "NONE")}\n" +
                $"Terminal: {gen.terminalPrefabs?.Length ?? 0}, Hangars: {gen.hangarPrefabs?.Length ?? 0}, ATC: {gen.atcPrefabs?.Length ?? 0}, Storage: {gen.storagePrefabs?.Length ?? 0}\n" +
                $"Props: {gen.propPrefabs?.Length ?? 0}, Vehicles: {gen.vehiclePrefabs?.Length ?? 0}, Parked Planes: {gen.parkedPlanePrefabs?.Length ?? 0}"
            );
        }
    }

    static GameObject FindFirstPrefab(string[] names)
    {
        var arr = FindPrefabs(names);
        return arr.Length > 0 ? arr[0] : null;
    }

    static GameObject[] FindPrefabs(string[] names)
    {
        var results = new List<GameObject>();

        foreach (var name in names.Distinct())
        {
            // Find prefabs with that name
            string[] guids = AssetDatabase.FindAssets($"\"{name}\" t:prefab");

            if (guids == null || guids.Length == 0)
                continue; // stay quiet (no spam). You'll just get fewer items.

            // Prefer exact filename match if multiple results
            string bestPath = ChooseBestPath(guids, name);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bestPath);

            if (prefab) results.Add(prefab);
        }

        return results.ToArray();
    }

    static string ChooseBestPath(string[] guids, string targetName)
    {
        string best = null;

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string file = System.IO.Path.GetFileNameWithoutExtension(path);

            if (file == targetName) return path; // exact match wins
            if (best == null) best = path;
        }

        return best;
    }
}
#endif
