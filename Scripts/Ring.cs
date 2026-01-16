using UnityEngine;
public class Ring : MonoBehaviour
{
    private RingManager manager;
    private Renderer rend;

    private void Start()
    {
        manager = FindObjectOfType<RingManager>(); // Find manager in scene
        rend = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Make sure your plane has the tag "Player"
        {
            manager.EnterRing(this);
        }
    }

    public void MarkEntered()
    {
        // Hide the ring when entered
        gameObject.SetActive(false);
    }

    public void SetHighlight(bool isActive)
    {
        if (rend != null)
        {
            rend.material.color = isActive ? Color.green : Color.white;
        }
    }

   
}
