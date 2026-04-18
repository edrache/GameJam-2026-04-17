using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameFind : MonoBehaviour
{
    [Header("Sloty obrazków (9 Image w canvasie)")]
    [SerializeField] private Image[] slots;

    [Header("Pula sprite'ów do losowania")]
    [SerializeField] private Sprite[] spritePool;

    [Header("Czas wyświetlenia SelectionWrong (sekundy)")]
    [SerializeField] private float wrongDisplayTime = 0.8f;

    [Header("Kontener ikon błędów")]
    [SerializeField] private Transform failIconsContainer;
    [SerializeField] private GameObject miniGameWindow;

    private static readonly HashSet<string> CorrectSpriteNames = new()
    {
        "cover_calico",
        "cover_flamecraft",
        "cover_wingspan"
    };

    private Image[] outlineImages;
    private Image[] correctImages;
    private Image[] wrongImages;
    private Image[] failIcons;

    private const int MaxSelected = 3;

    private int focusedIndex  = 0;
    private int wrongCount    = 0;
    private readonly HashSet<int> selectedIndices = new();

    void Start()
    {
        CacheOverlays();
        Randomize();
        ApplyVisuals();
        DebugLogPositions();
    }

    void Update()
    {
        HandleInput();
    }

    // ── Inicjalizacja overlayów ──────────────────────────────────

    private void CacheOverlays()
    {
        outlineImages = new Image[slots.Length];
        correctImages = new Image[slots.Length];
        wrongImages   = new Image[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            Transform t = slots[i].transform;
            outlineImages[i] = FindChild(t, "SelectionOutline");
            correctImages[i] = FindChild(t, "SelectionCorrect");
            wrongImages[i]   = FindChild(t, "SelectionWrong");
        }

        if (failIconsContainer != null)
        {
            failIcons = new Image[failIconsContainer.childCount];
            for (int i = 0; i < failIcons.Length; i++)
                failIcons[i] = failIconsContainer.GetChild(i).GetComponent<Image>();
        }
    }

    private Image FindChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            Debug.LogWarning($"[MiniGameFind] Brak dziecka '{childName}' w '{parent.name}'.");
            return null;
        }
        return child.GetComponent<Image>();
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
            ApplyVisuals();
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

    private bool IsCorrectSlot(int index)
    {
        string spriteName = slots[index].sprite?.name;
        return spriteName != null && CorrectSpriteNames.Contains(spriteName);
    }

    private void Select(int index)
    {
        if (selectedIndices.Contains(index))
        {
            selectedIndices.Remove(index);
            ApplyVisuals();
            return;
        }

        if (IsCorrectSlot(index))
        {
            if (selectedIndices.Count < MaxSelected)
            {
                selectedIndices.Add(index);
                ApplyVisuals();
                Debug.Log($"Poprawny slot {index}: {slots[index].sprite?.name}");
            }
        }
        else
        {
            StartCoroutine(ShowWrong(index));
        }
    }

    private IEnumerator ShowWrong(int index)
    {
        SetActive(wrongImages[index], true);

        if (failIcons != null && wrongCount < failIcons.Length)
        {
            Image icon = failIcons[wrongCount];
            Color c = icon.color;
            c.a = 1f;
            icon.color = c;
        }

        wrongCount++;

        yield return new WaitForSeconds(wrongDisplayTime);
        SetActive(wrongImages[index], false);

        if (wrongCount >= 2)
            EndGame();
    }

    private void EndGame()
    {
        GameObject window = miniGameWindow != null ? miniGameWindow : gameObject;
        window.SetActive(false);
    }

    // ── Wizualizacja ─────────────────────────────────────────────

    private void ApplyVisuals()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool isFocused  = i == focusedIndex;
            bool isSelected = selectedIndices.Contains(i);

            SetActive(outlineImages[i], isFocused && !isSelected);
            SetActive(correctImages[i], isSelected);
        }
    }

    private void SetActive(Image img, bool active)
    {
        if (img != null) img.gameObject.SetActive(active);
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

        focusedIndex = 0;
        wrongCount   = 0;
        selectedIndices.Clear();
    }

    // ── Debug ────────────────────────────────────────────────────

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DebugLogPositions()
    {
        for (int i = 0; i < slots.Length; i++)
            Debug.Log($"[MiniGameFind] Slot {i} '{slots[i].name}': position={slots[i].rectTransform.position}");
    }
}
