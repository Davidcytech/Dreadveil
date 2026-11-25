using UnityEngine;

public class powerupenergia : MonoBehaviour
{
    public float duration = 60f;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null){
            return;
        }
        player.EnergyPowerup(duration);
        Destroy(gameObject);
    }
    
}
