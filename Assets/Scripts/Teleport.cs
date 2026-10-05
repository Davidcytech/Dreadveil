using UnityEngine;

public class Teleport : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float teleportDistance = 20f;
    [SerializeField] private float maxGroundDistance = 30f;
    [SerializeField] private LayerMask teleportMask = ~0;

    [Header("Indicador (holograma)")]
    [Tooltip("Opcional. Se vazio, é criado um cubo automaticamente.")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float spinSpeed = 45f;

    private Rigidbody rb;
    private Collider ownCollider;
    private bool isAiming;
    private bool hasTarget;
    private Vector3 currentTarget;

    private GameObject indicator;
    private Vector3 indicatorOffset; // do pivot ao centro do corpo

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ownCollider = GetComponent<Collider>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        CreateIndicator();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            isAiming = true;

        if (isAiming && Input.GetKey(KeyCode.E))
        {
            hasTarget = TryGetTarget(out currentTarget);

            if (hasTarget)
                ShowIndicator(currentTarget);
            else
                indicator.SetActive(false);
        }

        if (isAiming && Input.GetKeyUp(KeyCode.E))
        {
            isAiming = false;
            indicator.SetActive(false);

            if (hasTarget)
                ApplyPosition(currentTarget);
            else
                Debug.LogWarning("Teleport: no ground found below the aim point.");
        }
    }

    // ---------- Indicador ----------

    private void CreateIndicator()
    {
        Bounds b = ownCollider.bounds;
        indicatorOffset = b.center - transform.position;

        if (indicatorPrefab != null)
        {
            indicator = Instantiate(indicatorPrefab);
        }
        else
        {
            indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.transform.localScale = b.size;
        }

        indicator.name = "Teleport Indicator";

        // O indicador nunca deve ter colliders (senão bloqueia os raycasts)
        foreach (Collider c in indicator.GetComponentsInChildren<Collider>())
            Destroy(c);

        indicator.SetActive(false);
    }

    private void ShowIndicator(Vector3 pivotTarget)
    {
        indicator.SetActive(true);
        indicator.transform.position = pivotTarget + indicatorOffset;
        indicator.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnDestroy()
    {
        if (indicator != null) Destroy(indicator);
    }

    // ---------- Cálculo do destino ----------

    private bool TryGetTarget(out Vector3 target)
    {
        target = default;

        float feetOffset = transform.position.y - ownCollider.bounds.min.y;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint;

        if (RaycastIgnoreSelf(ray.origin, ray.direction, teleportDistance, out RaycastHit hit))
            aimPoint = hit.point + hit.normal * 0.4f;
        else
            aimPoint = ray.origin + ray.direction * teleportDistance;

        if (!RaycastIgnoreSelf(aimPoint + Vector3.up, Vector3.down, maxGroundDistance, out RaycastHit groundHit))
            return false;

        target = groundHit.point + Vector3.up * (feetOffset + 0.05f);
        return true;
    }

    // Raycast que ignora o próprio jogador e triggers, sem desligar o collider
    private bool RaycastIgnoreSelf(Vector3 origin, Vector3 dir, float dist, out RaycastHit result)
    {
        result = default;
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, teleportMask, QueryTriggerInteraction.Ignore);

        float closest = float.MaxValue;
        bool found = false;

        foreach (RaycastHit h in hits)
        {
            if (h.transform.root == transform.root) continue;

            if (h.distance < closest)
            {
                closest = h.distance;
                result = h;
                found = true;
            }
        }

        return found;
    }

    private void ApplyPosition(Vector3 position)
    {
        if (rb != null)
        {
            rb.position = position;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = position;
    }
}