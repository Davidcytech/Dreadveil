
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public float health = 100f;
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float energyPerRegeneration = 25f;
    public float regenerationInterval = 5f;

    private float regenerationTimer;

    private bool dead;
    public GameObject gameOverPanel;


    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }


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
            if (gameOverPanel != null)
            {
              gameOverPanel.SetActive(true);
            }
            Debug.Log("oof");
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
}
