using UnityEngine;

public class Projectile : MonoBehaviour
{
    // deixar isto para caso a bala nao acerte o inimigo, ela dar despawn
    public float duration = 5f;

    private void Start()
    {
        Destroy(gameObject, duration);
    }

    private void OnCollisionEnter(Collision collision)
    {
       Destroy(gameObject);
    }
}
