using System.Collections;
using UnityEngine;

public class MinaProximidade : MonoBehaviour
{
    public float triggerRadius = 3f;     
    public float explosionRadius = 4f;   
    public float fuseTime = 0.5f;      
    public float damage = 10000f;

    private bool triggered;

    private void Update()
    {
        if (triggered)
        { 
            return;
        }
        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius);

        foreach (Collider col in hits)
        {
            if (col.GetComponentInParent<Player>() != null)
            {
                StartCoroutine(Explosion());
                return;
            }
        }
    }

    private IEnumerator Explosion()
    {
        triggered = true;
        yield return new WaitForSeconds(fuseTime); // espera um pouco antes de explodir
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
       // verifica as colisoes e aplica o dano
        foreach (Collider col in hits)
        {
            Player p = col.GetComponentInParent<Player>();
            if (p != null)
            {
                p.TakeDamage(damage);
            }
        }
        Destroy(gameObject);
    }

    // so teste
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}