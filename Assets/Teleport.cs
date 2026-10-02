using UnityEngine;

public class Teleport : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float teleportDistance = 20f;
    [SerializeField] private float maxGroundDistance = 10f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        // Ray começa no centro da câmara
        Ray cameraRay = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        // Encontra o ponto onde a crosshair está a apontar
        if (Physics.Raycast(cameraRay, out RaycastHit hit, teleportDistance))
        {
            // A partir desse ponto, procura o chão para baixo
            Vector3 groundCheckStart = hit.point + Vector3.up * 0.5f;

            if (Physics.Raycast(
                groundCheckStart,
                Vector3.down,
                out RaycastHit groundHit,
                maxGroundDistance))
            {
                Vector3 teleportPosition = groundHit.point;

                // Coloca o jogador no chão
                transform.position = teleportPosition;
            }
        }
    }
}