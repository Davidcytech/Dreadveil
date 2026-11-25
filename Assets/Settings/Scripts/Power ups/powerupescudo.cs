using UnityEngine;

public class powerupescudo : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        
        if (player == null)
        {
            return;
        }

        // Ativa o escudo no jogador
        player.ShieldPowerup();
        
        // Destrói o objeto do power-up na cena
        Destroy(gameObject);
    }
}