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
    }

    public PlaneData[] planes;
}
