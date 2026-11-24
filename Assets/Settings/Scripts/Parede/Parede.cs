using System.Collections;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    public string id = "Parede_Level2";        // NOVO: único em cada parede
    public Checkpoint checkpointAoExplodir;

    public GameObject objetoDestruidoPrefab;
    public GameObject dinamitePrefab;

    public float tempoParaExplodir = 3f;

    public Vector3 offsetDinamite = Vector3.zero;

    public float tempoParaLimpar = 5f;

    public bool jaExplodiu = false;
    public float distanciaInteracao = 3f;

    // NOVO: se já explodiu antes do último checkpoint, a parede não existe
    private void Start()
    {
        if (CheckpointData.IsDone(id))
            Object.Destroy(gameObject);
    }

    // Chamado pelo Player: coloca a dinamite e explode passado o tempo.
    public void ExplodirParede()
    {
        if (jaExplodiu)
            return;

        jaExplodiu = true;

        CheckpointData.MarkDone(id);           // NOVO: regista que a parede foi explodida

        StartCoroutine(ExplodirComDelay());
    }

    private IEnumerator ExplodirComDelay()
    {
        GameObject dinamite = null;

        if (dinamitePrefab != null)
        {
            // Centro real da parede (usa o collider, não o pivô).
            Vector3 centro = transform.position;

            Collider col = GetComponent<Collider>();

            if (col != null)
                centro = col.bounds.center;
                dinamite = Instantiate(dinamitePrefab,centro + offsetDinamite,transform.rotation);
        }

        yield return new WaitForSeconds(tempoParaExplodir);

        if (dinamite != null)
            Destroy(dinamite);

        if (objetoDestruidoPrefab != null)
        {
            GameObject partida = Instantiate(objetoDestruidoPrefab,transform.position,transform.rotation);

            // Apaga os pedaços passado o tempo.
            if (tempoParaLimpar > 0f)
                Object.Destroy(partida, tempoParaLimpar);
        }

        // Repõe as portas na posição inicial.
        if (BigDoorManager.Instance != null)
        {
            BigDoorManager.Instance.ResetarPortas();
        }

        if (checkpointAoExplodir != null)
        {
            Player p = FindFirstObjectByType<Player>();

            if (p != null)
                checkpointAoExplodir.Activate(p);
        }
        // Destrói a parede original.
        Destroy(gameObject);
    }
}