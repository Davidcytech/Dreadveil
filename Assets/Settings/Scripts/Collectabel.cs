using UnityEngine;

public class Collectabel : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null){
            return;
        }

        player.AddCollecatbel();
        Destroy(gameObject);
    }
    
}
