using UnityEngine;
using System.Collections;

public class BigDoorManager : MonoBehaviour
{
    public static BigDoorManager Instance { get; private set; }

    [System.Serializable]
    public class DoorFragment
    {
        public string nome = "Fragmento";
        public Transform fragmento;

        [Tooltip("Minigames completos necessários para abrir.")]
        public int minigamesNecessarios = 1;

        [Tooltip("Quanto o fragmento se desloca ao abrir (espaço local).")]
        public Vector3 deslocamentoAberto = new Vector3(0f, 0f, -0.35f);

        public float duracao = 1f;

        [HideInInspector] public Vector3 posicaoFechada;
        [HideInInspector] public bool aberto;
    }

    [Header("Fragmentos (cima = 1, meio = 2, baixo = 3)")]
    public DoorFragment[] fragmentos;

    public int MinigamesCompletos { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (DoorFragment f in fragmentos)
        {
            if (f.fragmento != null)
                f.posicaoFechada = f.fragmento.localPosition;
        }
    }

    // Chamado pelo Player sempre que a contagem de minigames muda.
    public void AtualizarPortas(int total)
    {
        MinigamesCompletos = total;
        Debug.Log("BigDoorManager recebeu: " + MinigamesCompletos + " minigames completos");

        VerificarPorta();
    }

    void VerificarPorta()
    {
        foreach (DoorFragment f in fragmentos)
        {
            if (!f.aberto && f.fragmento != null && MinigamesCompletos >= f.minigamesNecessarios)
            {
                f.aberto = true;
                StartCoroutine(AbrirFragmento(f));
            }
        }
    }

    IEnumerator AbrirFragmento(DoorFragment f)
    {
        Vector3 inicio = f.posicaoFechada;
        Vector3 fim = inicio + f.deslocamentoAberto;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, f.duracao);
            f.fragmento.localPosition = Vector3.Lerp(inicio, fim, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        f.fragmento.localPosition = fim;
    }
}