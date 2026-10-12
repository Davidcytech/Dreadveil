using UnityEngine;

public class Projectile : MonoBehaviour
{
    // deixar isto para caso a bala nao acerte o inimigo, ela dar despawn
    public float duration = 5f;
    public float damage = 100f;

    private void Start()
    {
        Destroy(gameObject, duration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponentInParent<Player>();

        if (player != null)
        {
            player.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
