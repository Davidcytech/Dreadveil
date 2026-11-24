using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Transform respawnPoint;      
    private bool used;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null) Activate(player);
    }

    public void Activate(Player player)
    {
        if (used) return;
        used = true;

        Transform t = respawnPoint != null ? respawnPoint : transform;
        player.SaveCheckpoint(t.position, t.rotation);
    }
}