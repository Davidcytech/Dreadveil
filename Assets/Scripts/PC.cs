using UnityEngine;

public class PC : MonoBehaviour
{
    public GameObject player;
    public Door door;
    public Light ThingyLight;
    public float interactDistance = 2f;

    void Start()
    {
        ThingyLight.color = Color.red;
    }

    void Update()
    {
        float distance = Vector3.Distance(
            player.transform.position,
            transform.position
        );

        if (distance <= interactDistance && Input.GetKeyDown(KeyCode.F))
        {
            door.isLocked = false;
            ThingyLight.color = Color.green;
        }
    }
}