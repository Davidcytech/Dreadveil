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

    // guarda o estado original de cada renderer para o restaurar depois
    private class SavedRenderer
    {
        public Material[] materials;
        public Mesh mesh;       // mesh original
        public Mesh copy;       // mesh com submeshes duplicadas
    }

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            ActivateRadar();
    }

    public void ActivateRadar()
    {
        if (player == null || highlightMaterial == null)
            return;

        if (active || Time.time < nextUseTime)
            return;

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
        Collider[] hits = Physics.OverlapSphere(transform.position, radarRadius, enemyLayer);

        var saved = new Dictionary<Renderer, SavedRenderer>();

        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            Transform root = enemy != null ? enemy.transform : col.transform;

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (saved.ContainsKey(r)) continue;

                SavedRenderer s = ApplyHighlight(r);
                if (s != null)
                    saved[r] = s;
            }
        }

        yield return new WaitForSeconds(highlightDuration);

        foreach (var pair in saved)
            Restore(pair.Key, pair.Value);

        active = false;
    }

    private SavedRenderer ApplyHighlight(Renderer r)
    {
        Mesh source = GetMesh(r);
        if (source == null) return null;

        if (!source.isReadable)
        {
            Debug.LogWarning("Radar: a mesh '" + source.name + "' não tem Read/Write ligado (FBX > Model > Read/Write).", r);
            return null;
        }

        Material[] original = r.sharedMaterials;
        int n = source.subMeshCount;

        // cópia da mesh com cada submesh duplicada
        Mesh copy = Instantiate(source);
        copy.subMeshCount = n * 2;
        for (int i = 0; i < n; i++)
            copy.SetSubMesh(n + i, source.GetSubMesh(i), UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);

        // materiais originais + um highlight por cada submesh
        Material[] withHighlight = new Material[n * 2];
        for (int i = 0; i < n; i++)
        {
            withHighlight[i] = i < original.Length ? original[i] : original[original.Length - 1];
            withHighlight[n + i] = highlightMaterial;
        }

        SetMesh(r, copy);
        r.sharedMaterials = withHighlight;

        return new SavedRenderer { materials = original, mesh = source, copy = copy };
    }

    private void Restore(Renderer r, SavedRenderer s)
    {
        if (r != null)
        {
            SetMesh(r, s.mesh);
            r.sharedMaterials = s.materials;
        }

        if (s.copy != null)
            Destroy(s.copy);
    }

    private Mesh GetMesh(Renderer r)
    {
        if (r is SkinnedMeshRenderer smr) return smr.sharedMesh;

        if (r is MeshRenderer)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            return mf != null ? mf.sharedMesh : null;
        }

        return null;
    }

    private void SetMesh(Renderer r, Mesh mesh)
    {
        if (r is SkinnedMeshRenderer smr)
            smr.sharedMesh = mesh;
        else
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = mesh;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Cyan();
        Gizmos.DrawWireSphere(transform.position, radarRadius);
    }

    private Color Cyan() { return Color.cyan; }
}