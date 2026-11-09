using UnityEngine;

public class Dynamite : MonoBehaviour
{
    public float distanciaInteracao = 3f; 
    private Transform player;

    void Start()
    {
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
                Destroy(gameObject); 
            }
        }
    }
}