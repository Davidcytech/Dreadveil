using UnityEngine;

public class Dynamite : MonoBehaviour
{
    public string id = "Dynamite_Level2";     // NOVO: único em cada dinamite
    public float distanciaInteracao = 3f; 
    private Transform player;

    void Start()
    {
        // NOVO: se já foi apanhada antes do último checkpoint, desaparece
        if (CheckpointData.IsDone(id))
        {
            Destroy(gameObject);
            return;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distancia = Vector3.Distance(transform.position, player.position);
        if (distancia <= distanciaInteracao && Input.GetKeyDown(KeyCode.F))
        {
            Player scriptPlayer = player.GetComponentInParent<Player>();
            if (scriptPlayer != null)
            {
                scriptPlayer.ApanharDinamite();
                CheckpointData.MarkDone(id);   // NOVO: regista que foi apanhada
                Destroy(gameObject); 
            }
        }
    }
}