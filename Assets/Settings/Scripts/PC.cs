using UnityEngine;

public class PC : MonoBehaviour
{
    public GameObject player;
    public Door door;
    public Light ThingyLight;
    public Material material;
    public float interactDistance = 2f;

    private Color originalBaseColor;
    private Color originalEmissionColor;
    private Color originalLightColor;

    private bool hasBaseColor;
    private bool hasEmissionColor;

    void Start()
    {
        if (material != null)
        {
            hasBaseColor = material.HasProperty("_BaseColor");

            if (hasBaseColor)
                originalBaseColor = material.GetColor("_BaseColor");

            hasEmissionColor = material.HasProperty("_EmissionColor");

            if (hasEmissionColor)
            {
                originalEmissionColor = material.GetColor("_EmissionColor");
                material.EnableKeyword("_EMISSION");
            }
        }

        if (ThingyLight != null)
            originalLightColor = ThingyLight.color;
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(
            player.transform.position,
            transform.position
        );

        if (distance <= interactDistance && Input.GetKeyDown(KeyCode.F))
        {
            if (door != null)
                door.isLocked = false;

            if (ThingyLight != null)
                ThingyLight.color = Color.green;

            if (material != null)
            {
                if (hasBaseColor)
                    material.SetColor("_BaseColor", Color.green);

                if (hasEmissionColor)
                    material.SetColor("_EmissionColor", Color.green);
            }
        }
    }

    void OnApplicationQuit()
    {
        RestoreOriginalColors();
    }

    void RestoreOriginalColors()
    {
        if (material != null)
        {
            if (hasBaseColor)
                material.SetColor("_BaseColor", originalBaseColor);

            if (hasEmissionColor)
                material.SetColor("_EmissionColor", originalEmissionColor);
        }

        if (ThingyLight != null)
            ThingyLight.color = originalLightColor;
    }
}