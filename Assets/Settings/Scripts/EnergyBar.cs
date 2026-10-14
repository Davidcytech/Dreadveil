using UnityEngine;
using UnityEngine.UI;

public class EnergyBar : MonoBehaviour
{
    public Player player;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (player == null) return;

        slider.value = player.energy / player.maxEnergy;
    }
}