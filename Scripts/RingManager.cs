using TMPro;
using UnityEngine;

public class RingManager : MonoBehaviour
{
    public PlaneHUD hud;
    public Transform ringsParent;   // Parent object that holds all the rings
    private Ring[] rings;
    public GameObject waypoint;
    private int currentRingIndex = 0;
    private bool rings_Collected=false;
    private Vector3 startPos;

    void Start()
    {
        // Get all Ring components under the parent, in hierarchy order
        rings = ringsParent.GetComponentsInChildren<Ring>();
        HighlightCurrentRing();
        startPos=waypoint.transform.position;
    }
    private void Update()
    {
        if (rings_Collected)
        {
            waypoint.SetActive(true);
            hud.LandingText.SetActive(true);

        }
        float yOffset = Mathf.Sin(Time.time * 3) * 0.5f;
        waypoint.transform.position = startPos + new Vector3(0, yOffset, 0);
    }
    public void EnterRing(Ring enteredRing)
    {
        if (rings[currentRingIndex] == enteredRing)

        {

            enteredRing.MarkEntered();
            hud.AddScore(10);
            currentRingIndex++;

            if (currentRingIndex < rings.Length)
            {
                HighlightCurrentRing();
            }
            else
            {
                rings_Collected = true;
            }
        }
        else
        {
            Debug.Log("❌ Wrong ring, try again!");
        }
    }

    private void HighlightCurrentRing()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            rings[i].SetHighlight(i == currentRingIndex);
        }
    }
    public bool GetRingsCollected()
    {
        return rings_Collected;
    }
}
