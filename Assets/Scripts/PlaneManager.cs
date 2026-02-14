using UnityEngine;
using UnityEngine.Events;

public class PlaneManager : MonoBehaviour
{
    [System.Serializable]
    public class PlaneData
    {
        public GameObject plane;
        public int speed;
        public int acceleration;
        public int deceleration;
        public float rotation;
        public float levelSpeed;

        [Header("Camera Settings")]
        public Vector3 shoulderOffset = new Vector3(-0.06f, 2.47f, -20f); // NEW
    }

    public PlaneData[] planes;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}