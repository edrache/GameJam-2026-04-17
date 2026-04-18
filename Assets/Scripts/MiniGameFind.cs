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
        if (slots == null || slots.Length == 0 || spritePool == null || spritePool.Length < slots.Length)
        {
            Debug.LogWarning("MiniGameFind: pula musi mieć co najmniej tyle sprite'ów co slotów.");
            return;
        }

        // Fisher-Yates shuffle na kopii puli
        Sprite[] shuffled = (Sprite[])spritePool.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].sprite = shuffled[i];
            slots[i].enabled = true;
        }
    }
}