using UnityEngine;

public class Player : MonoBehaviour
{
    public float health = 100f;
    public float energy = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;

        if (health <= 0)
        {
            health = 0;
            Debug.Log("Player morreu");
        }
    }
}