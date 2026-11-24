using UnityEngine;

public class Collectabel : MonoBehaviour
{
    [Tooltip("Tem de ser único por cada colecionável (ex: Coletavel_Level2_1)")]
    public string id;

    private bool collected;

    private void Start()
    {
        if (!string.IsNullOrEmpty(id) && CheckpointData.IsDone(id))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        collected = true;
        player.AddCollecatbel();

        if (!string.IsNullOrEmpty(id))
            CheckpointData.MarkDone(id);

        Destroy(gameObject);
    }
}