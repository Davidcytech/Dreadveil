using UnityEngine;

public class Hostage : MonoBehaviour
{
    public float interactRadius = 2.5f;
    public KeyCode interactKey = KeyCode.F;

    private bool won;

    private void Update()
    {
        if (won) return;

        if (Input.GetKeyDown(interactKey) && PlayerIsNear())
        {
            won = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private bool PlayerIsNear()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius);

        foreach (Collider col in hits)
        {
            if (col.GetComponentInParent<Player>() != null)
                return true;
        }

        return false;
    }

    // Ecrã de vitória desenhado por código (sem Canvas)
    private void OnGUI()
    {
        if (!won) return;

        // fundo escuro por cima do ecrã todo
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        // texto grande e centrado
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 64;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.green;

        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "VITÓRIA!", style);
    }

    // só tu vês isto, no Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}