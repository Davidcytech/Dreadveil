using UnityEngine;
using UnityEngine.SceneManagement;

public class Hostage : MonoBehaviour
{
    public float interactRadius = 2.5f;
    public KeyCode interactKey = KeyCode.F;

    private bool won;
    

    private void Update()
    {
        if (won) return;

        if (Input.GetKeyDown(interactKey) && PlayerNearBy())
        {
            won = true;
            SceneManager.LoadScene("Level 2");
        }
    }

    private bool PlayerNearBy()

    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius);

        foreach (Collider col in hits)
        {
            if (col.GetComponentInParent<Player>() != null)
                return true;
        }

        return false;
    }

}