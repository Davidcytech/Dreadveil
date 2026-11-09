
using UnityEngine;
using UnityEngine.UI;

public class ObjectInteraction : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public Image interactionImage;

    [Header("Configurações")]
    public float interactionDistance = 3f;

    private bool imageOpen;

    private void Start()
    {
        interactionImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactionDistance && !imageOpen && Input.GetKeyDown(KeyCode.F))
        {
            interactionImage.gameObject.SetActive(true);
            imageOpen = true;
        }

        if (imageOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            interactionImage.gameObject.SetActive(false);
            imageOpen = false;
        }
    }
}
