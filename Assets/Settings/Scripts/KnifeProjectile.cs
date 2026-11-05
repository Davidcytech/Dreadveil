
using UnityEngine;

public class KnifeProjectile : MonoBehaviour
{
    public float speed = 25f;
    public float maxDistance = 30f;

    private Vector3 direction;
    private Vector3 startPosition;
    private Transform owner;

    public void Launch(Vector3 shootDirection, Transform player)
    {
        direction = shootDirection.normalized;
        owner = player;
        startPosition = transform.position;

        transform.forward = direction;
    }

    private void Update()
    {
        float distance = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance))
        {
            if (owner != null &&
                (hit.transform == owner || hit.transform.IsChildOf(owner)))
            {
                transform.position += direction * distance;
                return;
            }

            StealthKillTarget target = hit.transform.GetComponentInParent<StealthKillTarget>();

            if (target != null)
            {
                target.KillWithKnife();
            }

            Destroy(gameObject);
            return;
        }

        transform.position += direction * distance;

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }
}
