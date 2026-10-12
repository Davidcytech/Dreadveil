using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    public Player player;
    public Material highlightMaterial; // tem de ser o shader super bacano e especial que eu criei
    public LayerMask enemyLayer;
    public float radarRadius = 25f;
    public float highlightDuration = 4f;
    public float cooldown = 8f;
    public float energyCost = 20f;
    private float nextUseTime;
    private bool active;

    private void Awake()
    {
       if (player == null) player = GetComponent<Player>();
    }

    private void Update()
    {
         if (Input.GetKeyDown(KeyCode.Q))
        {
            ActivateRadar();
        }
    }

    public void ActivateRadar()
    {
        if (player == null || highlightMaterial == null)
        {
            return;
        }
        if(active || Time.time < nextUseTime)
        {
            return;
        }

         if (player.energy < energyCost)
        {
            Debug.Log("Energia insuficiente para o radar");
            return;
        }

        player.energy -= energyCost;
        nextUseTime = Time.time + cooldown;
        StartCoroutine(RadarFunction());
    }

    private IEnumerator RadarFunction()
    {
        active = true;
        Collider[] hits = Physics.OverlapSphere(transform.position, radarRadius,enemyLayer);

        // aqui sao guardados os materiais originais para depois voltarem a ser pintados
        var originals = new Dictionary<Renderer, Material[]>();

        foreach ( Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            Transform root = enemy != null ? enemy.transform : col.transform;

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (originals.ContainsKey(r)) continue; // o contain é para evitar repetições
                Material[] original = r.sharedMaterials;
                originals[r] = original;

                // Copia os materiais e acrescenta o do radar no fim, assim se ele estiver atras de um objeto tem highlight e se nao estiver fica normal
                Material[] withHighlight = new Material[original.Length + 1];
                original.CopyTo(withHighlight, 0);
                withHighlight[original.Length] = highlightMaterial;
                r.sharedMaterials = withHighlight;
            }
        }
         yield return new WaitForSeconds(highlightDuration);

        // restaura os materiais depois do radar
        foreach (var pair in originals)
        {
            if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        }

        active = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radarRadius);
    }

    }








