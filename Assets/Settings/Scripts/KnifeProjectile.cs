using System;
using System.Collections.Generic;
using UnityEngine;

public class KnifeProjectile : MonoBehaviour
{
    public float speed = 25f;
    public float maxDistance = 30f;

    [Header("Aspeto")]
    [Tooltip("Roda o modelo se a seringa aparecer de lado (ex.: 90, 0, 0 ou 0, 90, 0)")]
    public Vector3 modelRotationOffset = Vector3.zero;

    [Header("Debug")]
    public bool debugLog = false;

    [Header("Barulho (igual ao ataque brutal)")]
    public bool makesNoise = true;
    public float soundHearingRadius = 15f;

    private Vector3 direction;
    private Vector3 startPosition;
    private Transform owner;

    // Spawna a faca e põe-na a andar. Funciona mesmo que o prefab não tenha este script.
    public static KnifeProjectile Spawn(GameObject prefab, Vector3 position, Vector3 direction, Transform owner)
    {
        GameObject knife = Instantiate(prefab, position, Quaternion.LookRotation(direction));
        knife.SetActive(true); // o molde pode estar desligado

        // Garante que só existe UM KnifeProjectile, na raiz da faca.
        KnifeProjectile projectile = knife.GetComponent<KnifeProjectile>();

        foreach (KnifeProjectile extra in knife.GetComponentsInChildren<KnifeProjectile>())
        {
            if (extra != projectile && extra.gameObject != knife)
                Destroy(extra);
        }

        if (projectile == null)
            projectile = knife.AddComponent<KnifeProjectile>();

        projectile.Launch(direction, owner);
        return projectile;
    }

    private void Awake()
    {
        // Proteção: este script NÃO pode estar no Player (senão o player anda e desaparece).
        if (GetComponent<Player>() != null)
        {
            Debug.LogError("KnifeProjectile está no Player! Remove-o do Player: o Spawn adiciona-o à faca sozinho.");
            enabled = false;
            Destroy(this);
            return;
        }

        direction = transform.forward;
        startPosition = transform.position;

        // A faca anda por código, a física não pode interferir.
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    public void Launch(Vector3 shootDirection, Transform player)
    {
        direction = shootDirection.sqrMagnitude > 0.0001f
            ? shootDirection.normalized
            : transform.forward;

        owner = player;
        startPosition = transform.position;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(modelRotationOffset);

        if (debugLog)
            Debug.Log(name + " lançada | dir = " + direction + " | speed = " + speed);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            direction,
            step,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider.transform;

            // Ignora a própria faca e o player.
            if (t == transform || t.IsChildOf(transform))
                continue;

            if (owner != null && (t == owner || t.IsChildOf(owner)))
                continue;

            StealthKillTarget target = t.GetComponentInParent<StealthKillTarget>();

            // Atravessa inimigos já abatidos.
            if (target != null && !target.CanBeKilled)
                continue;

            if (debugLog)
                Debug.Log(name + " bateu em: " + hit.collider.name);

            if (target != null)
            {
                target.KillWithKnife(direction);

                if (makesNoise)
                    AlertNearbyEnemies(target);
            }

            Destroy(gameObject);
            return;
        }

        // Anda para a frente.
        transform.position += direction * step;

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            if (debugLog)
                Debug.Log(name + " destruída: distância máxima");

            Destroy(gameObject);
        }
    }

    private void AlertNearbyEnemies(StealthKillTarget victim)
    {
        Collider[] hits = Physics.OverlapSphere(victim.transform.position, soundHearingRadius);

        HashSet<Enemy> alerted = new HashSet<Enemy>();

        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();

            if (enemy == null || !enemy.enabled)
                continue;

            if (enemy.gameObject == victim.gameObject)
                continue;

            if (!alerted.Add(enemy))
                continue;

            enemy.HearSound(victim.transform.position);
        }
    }
}