
using UnityEngine;

public class SilencePowerUp : MonoBehaviour
{
    [Header("PowerUp Settings")]
    public float silenceDuration = 10f;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (other.GetComponentInParent<vCrouchController>() == null)
            return;

        collected = true;

        Enemy.ActivateSilence(silenceDuration);

        Destroy(gameObject);
    }
}
