
using System.Collections;
using UnityEngine;

public class BossAttacks : MonoBehaviour
{
    private Boss boss;
    private Transform player;
    private Player playerScript;
    
    // bomba de espihnos
    public float spikeRange = 12f;
    public int spikeCount = 6;
    public float spikeInterval = 0.6f;
    public float spikeDelay = 1f;
    public float spikeRadius = 1f;
    public float spikeDamage = 2000f;

    //slam attack
    public float slamTriggerRange = 4.5f;
    public float slamRadius = 3.5f;
    public float slamWindup = 1.2f;
    public float slamDamage = 200f;
    public float jumpDodgeHeight = 0.8f;

    private void Awake()
    {
        boss = GetComponent<Boss>();

        GameObject p = GameObject.Find("vBasicController_character");

        if (p != null)
        {
            player = p.transform;
            playerScript = p.GetComponent<Player>();
        }
    }

    public IEnumerator SpikeAttack()
    {
        for (int i = 0; i < spikeCount; i++)
        {
            if (boss.IsDead || player == null){
                yield break;
            }

            FaceTarget(player.position);

            StartCoroutine(SpikeRoutine(player.position));

            yield return new WaitForSeconds(spikeInterval);
        }

        yield return new WaitForSeconds(spikeDelay);
    }

    private IEnumerator SpikeRoutine(Vector3 pos)
    {
        GameObject warning = CreateDisc(pos,spikeRadius,new Color(1f, 0.2f, 0.2f));
        yield return new WaitForSeconds(spikeDelay);

        if (warning != null){
            Destroy(warning);
        }    

        if (boss.IsDead || player == null){
            yield break;
        }
        GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        Destroy(spike.GetComponent<Collider>());

        spike.transform.position = pos + Vector3.up * 0.75f;

        spike.transform.localScale = new Vector3(spikeRadius * 0.6f, 0.75f,spikeRadius * 0.6f);

        SetColor(spike, new Color(0.3f, 0.3f, 0.3f));

        if (playerScript != null && FlatDistance(player.position, pos) <= spikeRadius)
        {
            playerScript.TakeDamage(spikeDamage);
        }

        yield return new WaitForSeconds(0.4f);

        if (spike != null){
            Destroy(spike);
        }
    }

    public IEnumerator SlamAttack()
    {
        GameObject warning = CreateDisc(transform.position,slamRadius,new Color(1f, 0.5f, 0f));
        float t = 0f;

        while (t < slamWindup)
        {
            if (boss.IsDead)
            {
                if (warning != null){
                    Destroy(warning);
                }
                yield break;
            }

            t += Time.deltaTime;

            float s = Mathf.Lerp(0.2f,1f,t / slamWindup) * slamRadius * 2f;

            if (warning != null)
            {
                warning.transform.localScale =new Vector3(s, 0.02f, s);
            }

            yield return null;
        }

        if (warning != null)
            Destroy(warning);

        if (player == null)
            yield break;

        bool inRadius = FlatDistance(player.position, transform.position) <= slamRadius;

        bool jumped =jumpDodgeHeight > 0f &&player.position.y - transform.position.y> jumpDodgeHeight;

        if (playerScript != null && inRadius && !jumped)
            playerScript.TakeDamage(slamDamage);

        yield return new WaitForSeconds(0.3f);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private GameObject CreateDisc(Vector3 pos,float radius, Color color)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        Destroy(disc.GetComponent<Collider>());

        disc.transform.position = pos + Vector3.up * 0.05f;

        disc.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        SetColor(disc, color);

        return disc;
    }

    private void SetColor(GameObject go, Color color)
    {
        Renderer rend = go.GetComponent<Renderer>();

        if (rend != null)
            rend.material.color = color;
    }
}
