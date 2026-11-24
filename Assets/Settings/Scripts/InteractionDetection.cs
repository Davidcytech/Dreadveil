using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    [SerializeField] private LayerMask interactableLayer;       // "Interativo"
    [SerializeField] private float searchRadius = 6f;           // tem de ser >= à maior distância de interação
    [SerializeField] private bool hideWhenCursorVisible = false; // esconde durante minigames/menus

    [Header("Textos")]
    [SerializeField] private string pcText = "<color=#FFD54F>F</color>  hackear";
    [SerializeField] private string minigameText = "<color=#FFD54F>F</color>  usar terminal";
    [SerializeField] private string dynamiteText = "<color=#FFD54F>F</color>  apanhar dinamite";
    [SerializeField] private string hostageText = "<color=#FFD54F>F</color>  libertar refém";
    [SerializeField] private string wallText = "<color=#FFD54F>F</color>  plantar dinamite";

    private bool showing;

    private Player playerScript;

    private void Awake()
    {
        playerScript = GetComponent<Player>();
    }

    private void Update()
    {
        if (PromptManager.Instance == null)
            return;

        // não interfere com o prompt dos abates
        object owner = PromptManager.Instance.CurrentOwner;
        if (owner != null && owner != (object)this)
        {
            showing = false;
            return;
        }

        if (hideWhenCursorVisible && Cursor.visible)
        {
            Hide();
            return;
        }

        string text = FindBestPrompt();

        if (text != null)
        {
            PromptManager.Instance.Show(this, text);
            showing = true;
        }
        else
        {
            Hide();
        }
    }

    private string FindBestPrompt()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, searchRadius, interactableLayer, QueryTriggerInteraction.Collide);

        string bestText = null;
        float bestDist = Mathf.Infinity;

        foreach (Collider col in hits)
        {
            if (!col.enabled || col.transform.IsChildOf(transform))
                continue;

            if (!Evaluate(col, out float dist, out string text))
                continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestText = text;
            }
        }

        return bestText;
    }

    // Devolve true se o objeto é interativo AGORA e o player está dentro do alcance dele
    private bool Evaluate(Collider col, out float dist, out string text)
    {
        dist = 0f;
        text = null;

        PC pc = col.GetComponentInParent<PC>();
        if (pc != null)
        {
            if (pc.AlreadyHacked) return false;
            dist = Vector3.Distance(transform.position, pc.transform.position);
            text = pcText;
            return dist <= pc.interactDistance;
        }

        MinigamePC2 mg = col.GetComponentInParent<MinigamePC2>();
        if (mg != null)
        {
            if (mg.IsCompleted) return false;
            dist = Vector3.Distance(transform.position, mg.transform.position);
            text = minigameText;
            return dist <= mg.interactDistance;
        }

        Dynamite dyn = col.GetComponentInParent<Dynamite>();
        if (dyn != null)
        {
            dist = Vector3.Distance(transform.position, dyn.transform.position);
            text = dynamiteText;
            return dist <= dyn.distanciaInteracao;
        }

                Hostage h = col.GetComponentInParent<Hostage>();
        if (h != null)
        {
            dist = Vector3.Distance(transform.position, h.transform.position);
            text = hostageText;
            return dist <= h.interactRadius;
        }

        Destroy wall = col.GetComponentInParent<Destroy>();
        if (wall != null)
        {
            if (wall.jaExplodiu) return false;
            if (playerScript == null || !playerScript.hasdynamite) return false;

            dist = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
            text = wallText;
            return dist <= wall.distanciaInteracao;
        }

        return false;
    }

    private void Hide()
    {
        if (!showing) return;
        PromptManager.Instance.Hide(this);
        showing = false;
    }

    private void OnDisable()
    {
        if (PromptManager.Instance != null)
            PromptManager.Instance.Hide(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}