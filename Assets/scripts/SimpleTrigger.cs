using UnityEngine;
using UnityEngine.UI;

public class SimpleTrigger : MonoBehaviour
{
    public GameObject objectToShow;

    private void Start()
    {
        objectToShow.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (objectToShow != null)
            {
                objectToShow.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (objectToShow != null)
            {
                objectToShow.SetActive(false);
            }
        }
    }
}