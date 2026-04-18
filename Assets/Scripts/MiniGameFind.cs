using UnityEngine;
using UnityEngine.UI;

public class MiniGameFind : MonoBehaviour
{
    [Header("Sloty obrazków (9 Image w canvasie)")]
    [SerializeField] private Image[] slots;

    [Header("Pula sprite'ów do losowania")]
    [SerializeField] private Sprite[] spritePool;

    [Header("Kolory stanu")]
    [SerializeField] private Color colorNormal   = Color.white;
    [SerializeField] private Color colorFocused  = Color.yellow;
    [SerializeField] private Color colorSelected = Color.green;

    private int focusedIndex  = 0;
    private int selectedIndex = -1;

    void Start()
    {
        Randomize();
        ApplyColors();
    }

    void Update()
    {
        HandleInput();
    }

    // ── Nawigacja ────────────────────────────────────────────────

    private void HandleInput()
    {
        int next = focusedIndex;

        if (Input.GetKeyDown(KeyCode.RightArrow)) next = FindNeighbor(focusedIndex, Vector2.right);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  next = FindNeighbor(focusedIndex, Vector2.left);
        if (Input.GetKeyDown(KeyCode.DownArrow))  next = FindNeighbor(focusedIndex, Vector2.down);
        if (Input.GetKeyDown(KeyCode.UpArrow))    next = FindNeighbor(focusedIndex, Vector2.up);

        if (next != focusedIndex)
        {
            focusedIndex = next;
            ApplyColors();
        }

        if (Input.GetKeyDown(KeyCode.Space))
            Select(focusedIndex);
    }

    private int FindNeighbor(int from, Vector2 direction)
    {
        Vector2 origin = slots[from].rectTransform.position;
        int best = from;
        float bestScore = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i == from) continue;

            Vector2 toSlot = (Vector2)slots[i].rectTransform.position - origin;
            float dot = Vector2.Dot(toSlot.normalized, direction);

            if (dot <= 0.01f) continue;

            float score = toSlot.magnitude / dot;
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        return best;
    }

    // ── Zaznaczenie ──────────────────────────────────────────────

    private void Select(int index)
    {
        selectedIndex = (selectedIndex == index) ? -1 : index;
        ApplyColors();

        if (selectedIndex >= 0)
            OnSlotSelected(selectedIndex);
    }

    // Nadpisz lub podłącz event w tej metodzie
    private void OnSlotSelected(int index)
    {
        Debug.Log($"Zaznaczono slot {index}: {slots[index].sprite?.name}");
    }

    // ── Kolory ───────────────────────────────────────────────────

    private void ApplyColors()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == selectedIndex)
                slots[i].color = colorSelected;
            else if (i == focusedIndex)
                slots[i].color = colorFocused;
            else
                slots[i].color = colorNormal;
        }
    }

    // ── Losowanie ────────────────────────────────────────────────

    public void Randomize()
    {
        if (slots == null || slots.Length == 0 || spritePool == null || spritePool.Length < slots.Length)
        {
            Debug.LogWarning("MiniGameFind: pula musi mieć co najmniej tyle sprite'ów co slotów.");
            return;
        }

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

        focusedIndex  = 0;
        selectedIndex = -1;
    }
}