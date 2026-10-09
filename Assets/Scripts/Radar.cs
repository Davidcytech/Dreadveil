using UnityEngine;
using System.Collections;

public class Radar : MonoBehaviour
{
    public float radarRange = 20f;
    public float energyCost = 25f;
    public float highlight = 3f;

    public LayerMask enemy;
    public Material highlightMaterial;

    private void Start()
    {

    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.R))
      {
        ActivateRadar();
      }
    }

    public void ActivateRadar()
    {
        Player player = GetComponent<Player>();

        if(player == null){
            return;
        }

        if(player.energy < energyCost)
        {
            //Teste para já, depois metemos a barra :0
            Debug.Log("Energia insuficiente");
            return;
        }

        player.energy -= energyCost;

        Collider[] enemies = Physics.OverlapSphere(transform.position,radarRange,enemy);

        foreach (Collider enemy in enemies)
        {
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();

            foreach(Renderer renderer in renderers)
            {
                Material originalMaterial = renderer.material;
                renderer.material = highlightMaterial;
                StartCoroutine(RemoveHighlight(renderer,originalMaterial,highlight));
            }
        }


    }

    
    private System.Collections.IEnumerator RemoveHighlight(Renderer renderer,Material originalMaterial,float duration)
    {
      yield return new WaitForSeconds(duration);

        if (renderer != null)
        {
            renderer.material = originalMaterial;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radarRange);
    }

}
