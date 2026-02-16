using UnityEngine;

public class PlaneManager : MonoBehaviour
{
    [System.Serializable]
    public class PlaneData
    {
        public GameObject planePrefab;   // prefab in Project, NOT scene object
        public int speed;
        public int acceleration;
        public int deceleration;
        public float rotation;
        public float levelSpeed;

        [Header("Camera Settings")]
        public Vector3 shoulderOffset = new Vector3(-0.06f, 2.47f, -20f);
    }

    public PlaneData[] planes;

    public static PlaneManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
