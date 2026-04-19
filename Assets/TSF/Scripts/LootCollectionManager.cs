using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSF
{
    public class LootCollectionManager : MonoBehaviour
    {
        private static LootCollectionManager _instance;

        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private Image iconTemplate;
        [SerializeField, Min(1f)] private float fallbackIconSize = 64f;
        [SerializeField] private bool hideTemplateOnAwake = true;
        [SerializeField] private bool clearIconsOnAwake = true;
        [SerializeField] private bool logCollectedLoot = true;

        [SerializeField] private List<CollectedLootInfo> collectedLoot = new List<CollectedLootInfo>();

        public static LootCollectionManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<LootCollectionManager>();
                return _instance;
            }
        }

        public IReadOnlyList<CollectedLootInfo> CollectedLoot => collectedLoot;
        public event Action<CollectedLootInfo> LootCollected;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            collectedLoot.Clear();
            EnsureIconContainer();

            if (hideTemplateOnAwake && iconTemplate != null)
                iconTemplate.gameObject.SetActive(false);

            if (clearIconsOnAwake)
                ClearIcons();
        }

        public CollectedLootInfo RecordLoot(GameObject lootPrefab, LootScoreValue lootScoreValue, int points)
        {
            string prefabName = lootPrefab != null ? lootPrefab.name : "Loot";
            string lootId = lootScoreValue != null ? lootScoreValue.LootId : prefabName;
            string displayName = lootScoreValue != null ? lootScoreValue.DisplayName : prefabName;
            Sprite icon = lootScoreValue != null ? lootScoreValue.Icon : null;

            CollectedLootInfo entry = new CollectedLootInfo(lootId, displayName, points, icon, lootScoreValue);
            collectedLoot.Add(entry);
            AddIcon(entry);
            if (logCollectedLoot)
                Debug.Log($"[LootCollectionManager] Recorded loot '{entry.DisplayName}' with id '{entry.LootId}' and icon '{(entry.Icon != null ? entry.Icon.name : "none")}'.", this);

            LootCollected?.Invoke(entry);
            return entry;
        }

        public void ResetCollection()
        {
            collectedLoot.Clear();
            ClearIcons();
        }

        private void AddIcon(CollectedLootInfo entry)
        {
            EnsureIconContainer();
            if (iconContainer == null)
                return;

            Image image = iconTemplate != null
                ? Instantiate(iconTemplate, iconContainer)
                : CreateIconImage(iconContainer);

            image.gameObject.name = $"Loot Icon - {entry.DisplayName}";
            image.gameObject.SetActive(true);
            image.sprite = entry.Icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.SetNativeSize();
            ConfigureIconLayout(image.rectTransform);

            if (entry.Icon == null)
                Debug.LogWarning($"[LootCollectionManager] Loot '{entry.DisplayName}' was recorded without an icon.", this);
        }

        private void ConfigureIconLayout(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(fallbackIconSize, fallbackIconSize);

            LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();

            layoutElement.minWidth = fallbackIconSize;
            layoutElement.minHeight = fallbackIconSize;
            layoutElement.preferredWidth = fallbackIconSize;
            layoutElement.preferredHeight = fallbackIconSize;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        private Image CreateIconImage(RectTransform parent)
        {
            GameObject iconObject = new GameObject("Loot Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return iconObject.GetComponent<Image>();
        }

        private void EnsureIconContainer()
        {
            if (iconContainer != null)
                return;

            GameObject containerObject = new GameObject("LootCollection", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform rectTransform = containerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(20f, -82f);
            rectTransform.sizeDelta = new Vector2(720f, 72f);

            HorizontalLayoutGroup layoutGroup = containerObject.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.spacing = 8f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            iconContainer = rectTransform;
        }

        private void ClearIcons()
        {
            if (iconContainer == null)
                return;

            for (int i = iconContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = iconContainer.GetChild(i);
                if (iconTemplate != null && child == iconTemplate.transform)
                    continue;

                Destroy(child.gameObject);
            }
        }
    }

    [Serializable]
    public class CollectedLootInfo
    {
        [SerializeField] private string lootId;
        [SerializeField] private string displayName;
        [SerializeField] private int points;
        [SerializeField] private Sprite icon;
        [SerializeField] private LootScoreValue sourceValue;

        public CollectedLootInfo(string lootId, string displayName, int points, Sprite icon, LootScoreValue sourceValue)
        {
            this.lootId = lootId;
            this.displayName = displayName;
            this.points = points;
            this.icon = icon;
            this.sourceValue = sourceValue;
        }

        public string LootId => lootId;
        public string DisplayName => displayName;
        public int Points => points;
        public Sprite Icon => icon;
        public LootScoreValue SourceValue => sourceValue;
    }
}
