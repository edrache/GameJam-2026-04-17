using System;
using System.Collections.Generic;
using UnityEngine;

namespace TSF
{
    public class LootScoreValue : MonoBehaviour
    {
        [SerializeField, Min(0)] private int points = 10;
        [SerializeField] private string lootId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private List<LootAbilityConfig> abilities = new List<LootAbilityConfig>();

        public int Points => points;
        public string LootId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(lootId))
                    return lootId;

                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                return gameObject.name;
            }
        }
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? LootId : displayName;
        public Sprite Icon => icon;
        public IReadOnlyList<LootAbilityConfig> Abilities => abilities;
    }

    public enum LootAbilityType
    {
        AddTimeImmediately,
        MultiplyScoreImmediately,
        FinalMultiplierIfOnlyThisLootType,
        AddScoreImmediatelyForEachCompletedSet,
        AddScoreAtEndForEachCompletedSet
    }

    [Serializable]
    public class LootAbilityConfig
    {
        [SerializeField] private string abilityId;
        [SerializeField] private LootAbilityType type;
        [SerializeField, Min(0f)] private float seconds = 20f;
        [SerializeField, Min(0f)] private float multiplier = 2f;
        [SerializeField] private int score = 50;
        [SerializeField] private List<string> requiredLootIds = new List<string>();

        public string AbilityId => abilityId;
        public LootAbilityType Type => type;
        public float Seconds => seconds;
        public float Multiplier => multiplier;
        public int Score => score;
        public IReadOnlyList<string> RequiredLootIds => requiredLootIds;
    }
}
