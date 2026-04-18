using UnityEngine;
using UnityEngine.UI;

public class MiniGameFind : MonoBehaviour
{
    [Header("Sloty obrazków (9 Image w canvasie)")]
    [SerializeField] private Image[] slots;

    [Header("Pula sprite'ów do losowania")]
    [SerializeField] private Sprite[] spritePool;

    void Start()
    {
        Randomize();
    }

    public void Randomize()
    {
        if (slots == null || slots.Length == 0 || spritePool == null || spritePool.Length == 0)
        {
            Debug.LogWarning("MiniGameFind: przypisz sloty i pulę sprite'ów w Inspectorze.");
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].sprite = spritePool[Random.Range(0, spritePool.Length)];
            slots[i].enabled = slots[i].sprite != null;
        }
    }
}
