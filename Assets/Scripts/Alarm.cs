using UnityEngine;

public class Alarm : MonoBehaviour
{
    public AudioSource audioSource;
    public float hearingRadius = 30f;  
    public bool oneShot = true;        

    private bool triggered;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot) return;

        
        if (other.GetComponentInParent<vCrouchController>() == null) return;

        triggered = true;

        if (audioSource != null) audioSource.Play();

        
        Collider[] hits = Physics.OverlapSphere(transform.position, hearingRadius);
        foreach (Collider c in hits)
        {
            Enemy enemy = c.GetComponentInParent<Enemy>();
            if (enemy != null) enemy.HearSound(transform.position);
        }
        triggered = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}