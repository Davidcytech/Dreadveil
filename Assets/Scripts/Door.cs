using UnityEngine;

public class Door : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 3f;
    public bool isLocked = false;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.rotation;

        openRotation = Quaternion.Euler(
            transform.eulerAngles + new Vector3(0, openAngle, 0)
        );
    }

    void Update()
    {
        if (isOpen)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                openRotation,
                Time.deltaTime * openSpeed
            );
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isLocked)
            {
                isOpen = true;
            }
        }
    }
}