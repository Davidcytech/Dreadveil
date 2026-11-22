using TMPro;
using UnityEngine;

public class PromptManager : MonoBehaviour
{
    public static PromptManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI promptText;

    private object currentOwner;

    private void Awake()
    {
        Instance = this;
        Hide(null);
    }

    // 'owner' identifica quem pediu o prompt, para só esse o poder esconder
    public void Show(object owner, string text)
    {
        currentOwner = owner;
        promptText.text = text;
        promptText.gameObject.SetActive(true);
    }

    public void Hide(object owner)
    {
        if (owner != null && owner != currentOwner)
            return;

        currentOwner = null;
        promptText.gameObject.SetActive(false);
    }
}