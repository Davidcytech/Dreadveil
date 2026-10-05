using UnityEngine;

public class StealthKillTarget : MonoBehaviour
{
    [Header("Stealth Kill")]
    [SerializeField] private Transform takedownPoint;

    public Transform TakedownPoint
    {
        get { return takedownPoint; }
    }

    public GameObject TargetObject
    {
        get { return gameObject; }
    }
}