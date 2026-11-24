using UnityEngine;

public class powerupescudo : MonoBehaviour
{
    public string id = "Escudo_Level2";     // NOVO: único em cada power-up

    // NOVO: se já foi apanhado antes do último checkpoint, desaparece
    private void Start()
    {
        if (CheckpointData.IsDone(id))
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        
        if (player == null)
        {
            return;
        }

        // Ativa o escudo no jogador
        player.ShieldPowerup();

        CheckpointData.MarkDone(id);        // NOVO: regista que foi apanhado
        
        // Destrói o objeto do power-up na cena
        Destroy(gameObject);
    }
}