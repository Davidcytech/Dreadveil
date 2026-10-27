
using UnityEngine;

public class Door : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 3f;
    public bool isLocked = false;

    
    public AudioSource doorSound;
    public float soundRange = 12f;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.rotation;

        openRotation = Quaternion.Euler(
            transform.eulerAngles + new Vector3(0, openAngle, 0)
        );

        if (doorSound == null)
            doorSound = GetComponent<AudioSource>();
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
            if (!isLocked && !isOpen)
            {
                isOpen = true;

                if (doorSound != null)
                    doorSound.Play();

                NotifyEnemies();
            }
        }
    }

    private void NotifyEnemies()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance <= soundRange)
                enemy.HearSound(transform.position);
        }
    }
}
