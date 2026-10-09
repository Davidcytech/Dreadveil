using UnityEngine;

public class Alarm : MonoBehaviour
{
    public AudioSource audioSource;
    public float hearingRadius = 30f;   // até onde o som chega aos inimigos
    public bool oneShot = true;         // só dispara uma vez

    private bool triggered;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot) return;

        // só reage ao player (usa o teu script de crouch para o identificar)
        if (other.GetComponentInParent<vCrouchController>() == null) return;

        triggered = true;

        if (audioSource != null) audioSource.Play();

        // avisa todos os inimigos dentro do raio
        Collider[] hits = Physics.OverlapSphere(transform.position, hearingRadius);
        foreach (Collider c in hits)
        {
            Enemy enemy = c.GetComponentInParent<Enemy>();
            if (enemy != null) enemy.HearSound(transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}