using UnityEngine;

public class SilencePowerUp : MonoBehaviour
{
    public string id = "Silencio_Level2"; // Único por cada power-up na cena

    [Header("PowerUp Settings")]
    public float silenceDuration = 10f;

    private bool collected;

    private void Start()
    {
        if (CheckpointData.IsDone(id))
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (other.GetComponentInParent<vCrouchController>() == null)
            return;

        collected = true;

        Enemy.ActivateSilence(silenceDuration);
        CheckpointData.MarkDone(id);
        Destroy(gameObject);
    }
}