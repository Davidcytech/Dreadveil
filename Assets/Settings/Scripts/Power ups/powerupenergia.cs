using UnityEngine;

public class EnergyPowerUp : MonoBehaviour
{
    public string id = "Energia_Level2";    // NOVO: único

    public float duration = 60f;

    // NOVO
    private void Start()
    {
        if (CheckpointData.IsDone(id))
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        player.EnergyPowerup(duration);
        CheckpointData.MarkDone(id);        // NOVO
        Destroy(gameObject);
    }
}