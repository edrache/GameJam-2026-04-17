using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TSF;
using MoreMountains.Feedbacks;

public class MiniGameFind : MonoBehaviour, IPortalMiniGame
{
    [Header("Sloty obrazków (9 Image w canvasie)")]
    [SerializeField] private Image[] slots;

    [Header("Pula sprite'ów do losowania")]
    [SerializeField] private Sprite[] spritePool;

    [Header("Okno mini-gry")]
    [SerializeField] private Canvas miniGameCanvas;

    [Header("Ikony błędów")]
    [SerializeField] private Transform failIconsContainer;

    [Header("Nagroda")]
    [SerializeField] private GameObject lootPrefab;
    [SerializeField, Min(0)] private int fallbackLootPoints = 10;
    [SerializeField, Min(0f)] private float removeDuration = 0.25f;

    [Header("Blokada gracza")]
    [SerializeField, Min(0f)] private float minimumPortalDistance = 0.5f;
    [SerializeField] private Transform lookTarget;

    [Header("Czas wyświetlenia SelectionWrong (sekundy)")]
    [SerializeField] private float wrongDisplayTime = 0.8f;

    [Header("Feedbacki dźwiękowe")]
    [SerializeField] private MMF_Player completionFeedback;

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

    private ArmReachController _reach;
    private PlayerController _playerController;
    private FPSCameraController _cameraController;
    private float _maxPortalDistance;
    private bool _active;
    private bool _completed;

    private int focusedIndex = 0;
    private int wrongCount   = 0;
    private readonly HashSet<int> selectedIndices = new();

    // ── IPortalMiniGame ──────────────────────────────────────────

    public void OnHandEnter(ArmReachController reach, PortalSide side)
    {
        Debug.Log("[MiniGameFind] OnHandEnter called");
        _reach             = reach;
        _playerController  = reach.GetComponentInParent<PlayerController>();
        _cameraController  = _playerController != null ? _playerController.GetComponentInChildren<FPSCameraController>() : null;
        _maxPortalDistance = GetPlanarDistanceToPlayer();
        _active            = true;
        _completed         = false;
        _reach.LockMiniGameExit(this);
        ApplyPlayerLock();

        ResetState();
        SetWindowVisible(true);
        Randomize();
        ApplyVisuals();
        DebugLogPositions();
    }

    public void OnHandExit()
    {
        _reach?.UnlockMiniGameExit(this);
        ClearPlayerLock();
        _active = false;
        _reach  = null;
        SetWindowVisible(false);
    }

    void LateUpdate()
    {
        if (_active && !_completed)
            ApplyPlayerLock();
    }

    void OnDisable()
    {
        _reach?.UnlockMiniGameExit(this);
        ClearPlayerLock();
    }

    private float GetPlanarDistanceToPlayer()
    {
        Transform playerTransform = _playerController != null ? _playerController.transform
                                  : _reach != null            ? _reach.transform
                                  : null;
        if (playerTransform == null) return minimumPortalDistance;

        Vector3 offset = playerTransform.position - transform.position;
        offset.y = 0f;
        return Mathf.Max(minimumPortalDistance, offset.magnitude);
    }

    private void ApplyPlayerLock()
    {
        _playerController?.SetDistanceConstraint(this, transform, minimumPortalDistance, _maxPortalDistance);
        _cameraController?.SetForcedLookAt(this, lookTarget != null ? lookTarget : transform);
    }

    private void ClearPlayerLock()
    {
        _playerController?.ClearDistanceConstraint(this);
        _cameraController?.ClearForcedLookAt(this);
        _playerController = null;
        _cameraController = null;
    }

    // ── Inicjalizacja ────────────────────────────────────────────

    void Start()
    {
        CacheOverlays();
        SetWindowVisible(false);
    }

    private void SetWindowVisible(bool visible)
    {
        if (miniGameCanvas != null)
            miniGameCanvas.enabled = visible;
    }

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

    private void ResetState()
    {
        focusedIndex = 0;
        wrongCount   = 0;
        selectedIndices.Clear();

        if (failIcons != null)
        {
            foreach (Image icon in failIcons)
            {
                if (icon == null) continue;
                Color c = icon.color;
                c.a = 0.3f;
                icon.color = c;
            }
        }
    }

    // ── Input ────────────────────────────────────────────────────

    void Update()
    {
        if (!_active || _completed) return;
        HandleInput();
    }

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

    private bool IsCorrectSlot(int index) =>
        slots[index].sprite != null && CorrectSpriteNames.Contains(slots[index].sprite.name);

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

                if (selectedIndices.Count == MaxSelected)
                    OnSuccess();
            }
        }
        else
        {
            StartCoroutine(ShowWrong(index));
        }
    }

    // ── Sukces ───────────────────────────────────────────────────

    private void OnSuccess()
    {
        _completed = true;
        SetWindowVisible(false);
        completionFeedback?.PlayFeedbacks();
        LootAwarder.Award(lootPrefab, fallbackLootPoints);
        _reach.UnlockMiniGameExit(this);
        ClearPlayerLock();
        _reach.TriggerLoot(lootPrefab);
        QueuePortalRemoval();
    }

    private void QueuePortalRemoval()
    {
        _active = false;
        _reach.RemovePortalWhenIdle(this, transform.root.gameObject, removeDuration);
        _reach = null;
    }

    // ── Porażka ──────────────────────────────────────────────────

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

        if (wrongCount >= 2 && _reach != null)
        {
            SetWindowVisible(false);
            _reach.UnlockMiniGameExit(this);
            ClearPlayerLock();
            QueuePortalRemoval();
        }
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

    private void Randomize()
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
    }

    // ── Debug ────────────────────────────────────────────────────

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DebugLogPositions()
    {
        for (int i = 0; i < slots.Length; i++)
            Debug.Log($"[MiniGameFind] Slot {i} '{slots[i].name}': position={slots[i].rectTransform.position}");
    }
}
