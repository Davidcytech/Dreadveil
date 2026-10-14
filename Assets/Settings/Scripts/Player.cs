
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public float health = 100f;
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float energyPerRegeneration = 25f;
    public float regenerationInterval = 5f;
    private bool dead;

    private float regenerationTimer;

    private void Update()
    {
        if (energy >= maxEnergy)
        {
            energy = maxEnergy;
            regenerationTimer = 0f;
            return;
        }

        regenerationTimer += Time.deltaTime;

        if (regenerationTimer >= regenerationInterval)
        {
            energy = Mathf.Min(maxEnergy, energy + energyPerRegeneration);
            regenerationTimer = 0f;
        }
    }

    public bool TryUseEnergy(float amount)
    {
        if (energy < amount)
            return false;

        energy -= amount;
        return true;
    }

    public void TakeDamage(float damage)
    {
        if (dead) return;
        health -= damage;
        

        if (health <= 0f)
        {
            health = 0f;
            dead = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
          //  Destroy(gameObject);
        }
    }
    //redução de energia 
    private bool energyInhibited;
    private Coroutine inhibitionCoroutine;

    public void InhibitEnergy(float duration)
    {
        if (inhibitionCoroutine != null)
            StopCoroutine(inhibitionCoroutine);

        inhibitionCoroutine = StartCoroutine(EnergyInhibition(duration));
    }

    private IEnumerator EnergyInhibition(float duration)
    {
        float savedEnergy = energy;

        energyInhibited = true;
        energy = 0f;

        yield return new WaitForSeconds(duration);

        energy = savedEnergy;
        energyInhibited = false;
        inhibitionCoroutine = null;
    }
        private void OnGUI()
    {
        if (!dead) return;

        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 64;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.red;

        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Capturado", style);
    }
}
