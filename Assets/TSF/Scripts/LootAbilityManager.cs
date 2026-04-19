using System.Collections.Generic;
using UnityEngine;

namespace TSF
{
    public class LootAbilityManager : MonoBehaviour
    {
        private static LootAbilityManager _instance;

        private readonly Dictionary<string, int> _awardedImmediateSetCounts = new Dictionary<string, int>();

        public static LootAbilityManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<LootAbilityManager>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _awardedImmediateSetCounts.Clear();
        }

        public void ApplyOnLootCollected(LootScoreValue lootScoreValue)
        {
            IReadOnlyList<LootAbilityConfig> abilities = lootScoreValue.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                LootAbilityConfig ability = abilities[i];
                switch (ability.Type)
                {
                    case LootAbilityType.AddTimeImmediately:
                        AddTime(ability.Seconds);
                        break;

                    case LootAbilityType.MultiplyScoreImmediately:
                        MultiplyScore(ability.Multiplier, lootScoreValue.DisplayName);
                        break;
                }
            }

            ApplyImmediateSetBonuses();
        }

        public int ApplyEndGameBonuses(int currentScore)
        {
            int score = currentScore;
            ScoreManager scoreManager = ScoreManager.Instance;
            LootCollectionManager collectionManager = LootCollectionManager.Instance;
            if (collectionManager == null)
                return score;

            HashSet<string> appliedFinalSetBonuses = new HashSet<string>();
            IReadOnlyList<CollectedLootInfo> collectedLoot = collectionManager.CollectedLoot;

            for (int lootIndex = 0; lootIndex < collectedLoot.Count; lootIndex++)
            {
                CollectedLootInfo entry = collectedLoot[lootIndex];
                LootScoreValue lootScoreValue = entry.SourceValue;
                if (lootScoreValue == null)
                    continue;

                IReadOnlyList<LootAbilityConfig> abilities = lootScoreValue.Abilities;
                for (int abilityIndex = 0; abilityIndex < abilities.Count; abilityIndex++)
                {
                    LootAbilityConfig ability = abilities[abilityIndex];
                    if (ability.Type != LootAbilityType.AddScoreAtEndForEachCompletedSet)
                        continue;

                    string setBonusKey = GetAbilityKey(entry.LootId, abilityIndex, ability);
                    if (!appliedFinalSetBonuses.Add(setBonusKey))
                        continue;

                    int completedSets = Mathf.Max(0, CountCompletedSets(ability.RequiredLootIds));
                    int bonusScore = completedSets * ability.Score;
                    if (bonusScore <= 0)
                        continue;

                    string reason = $"End-game set bonus: {completedSets} completed set(s) of {FormatLootRequirements(ability.RequiredLootIds)}.";
                    if (scoreManager != null)
                    {
                        scoreManager.AddScore(bonusScore, $"Ability: {entry.DisplayName}", reason);
                        score = scoreManager.Score;
                    }
                    else
                    {
                        score += bonusScore;
                    }
                }
            }

            HashSet<string> appliedFinalMultipliers = new HashSet<string>();
            for (int lootIndex = 0; lootIndex < collectedLoot.Count; lootIndex++)
            {
                CollectedLootInfo entry = collectedLoot[lootIndex];
                LootScoreValue lootScoreValue = entry.SourceValue;
                if (lootScoreValue == null)
                    continue;

                IReadOnlyList<LootAbilityConfig> abilities = lootScoreValue.Abilities;
                for (int abilityIndex = 0; abilityIndex < abilities.Count; abilityIndex++)
                {
                    LootAbilityConfig ability = abilities[abilityIndex];
                    if (ability.Type != LootAbilityType.FinalMultiplierIfOnlyThisLootType)
                        continue;

                    string multiplierKey = GetAbilityKey(entry.LootId, abilityIndex, ability);
                    if (appliedFinalMultipliers.Add(multiplierKey) && HasOnlyLootType(entry.LootId))
                    {
                        string reason = $"End-game multiplier because only {entry.DisplayName} loot was collected.";
                        if (scoreManager != null)
                        {
                            scoreManager.MultiplyScore(ability.Multiplier, $"Ability: {entry.DisplayName}", reason);
                            score = scoreManager.Score;
                        }
                        else
                        {
                            score = Mathf.RoundToInt(score * ability.Multiplier);
                        }
                    }
                }
            }

            return score;
        }

        private void AddTime(float seconds)
        {
            if (seconds <= 0f)
                return;

            ClockTimer clockTimer = FindFirstObjectByType<ClockTimer>();
            if (clockTimer != null)
                clockTimer.AddTime(seconds);
        }

        private void MultiplyScore(float multiplier, string lootDisplayName)
        {
            if (multiplier <= 0f || ScoreManager.Instance == null)
                return;

            ScoreManager.Instance.MultiplyScore(
                multiplier,
                $"Ability: {lootDisplayName}",
                $"Immediate multiplier x{multiplier:0.##}.");
        }

        private void ApplyImmediateSetBonuses()
        {
            LootCollectionManager collectionManager = LootCollectionManager.Instance;
            if (collectionManager == null)
                return;

            IReadOnlyList<CollectedLootInfo> collectedLoot = collectionManager.CollectedLoot;
            for (int lootIndex = 0; lootIndex < collectedLoot.Count; lootIndex++)
            {
                CollectedLootInfo entry = collectedLoot[lootIndex];
                LootScoreValue lootScoreValue = entry.SourceValue;
                if (lootScoreValue == null)
                    continue;

                IReadOnlyList<LootAbilityConfig> abilities = lootScoreValue.Abilities;
                for (int abilityIndex = 0; abilityIndex < abilities.Count; abilityIndex++)
                {
                    LootAbilityConfig ability = abilities[abilityIndex];
                    if (ability.Type == LootAbilityType.AddScoreImmediatelyForEachCompletedSet)
                        ApplyImmediateSetBonus(entry.LootId, entry.DisplayName, abilityIndex, ability);
                }
            }
        }

        private void ApplyImmediateSetBonus(string ownerLootId, string ownerDisplayName, int abilityIndex, LootAbilityConfig ability)
        {
            int completedSets = CountCompletedSets(ability.RequiredLootIds);
            if (completedSets <= 0)
                return;

            string key = GetAbilityKey(ownerLootId, abilityIndex, ability);
            _awardedImmediateSetCounts.TryGetValue(key, out int alreadyAwarded);
            int newSets = completedSets - alreadyAwarded;
            if (newSets <= 0)
                return;

            _awardedImmediateSetCounts[key] = completedSets;
            ScoreManager.Instance?.AddScore(
                newSets * ability.Score,
                $"Ability: {ownerDisplayName}",
                $"Immediate set bonus: {newSets} new completed set(s) of {FormatLootRequirements(ability.RequiredLootIds)}.");
        }

        private int CountCompletedSets(IReadOnlyList<string> requiredLootIds)
        {
            if (requiredLootIds == null || requiredLootIds.Count == 0)
                return 0;

            Dictionary<string, int> availableCounts = CountLootById();
            Dictionary<string, int> requiredCounts = new Dictionary<string, int>();
            for (int i = 0; i < requiredLootIds.Count; i++)
            {
                string lootId = requiredLootIds[i];
                if (string.IsNullOrWhiteSpace(lootId))
                    continue;

                requiredCounts.TryGetValue(lootId, out int count);
                requiredCounts[lootId] = count + 1;
            }

            if (requiredCounts.Count == 0)
                return 0;

            int completedSets = int.MaxValue;
            foreach (KeyValuePair<string, int> requirement in requiredCounts)
            {
                availableCounts.TryGetValue(requirement.Key, out int available);
                completedSets = Mathf.Min(completedSets, available / requirement.Value);
            }

            return completedSets == int.MaxValue ? 0 : completedSets;
        }

        private bool HasOnlyLootType(string lootId)
        {
            LootCollectionManager collectionManager = LootCollectionManager.Instance;
            if (collectionManager == null || collectionManager.CollectedLoot.Count == 0)
                return false;

            IReadOnlyList<CollectedLootInfo> collectedLoot = collectionManager.CollectedLoot;
            for (int i = 0; i < collectedLoot.Count; i++)
            {
                if (collectedLoot[i].LootId != lootId)
                    return false;
            }

            return true;
        }

        private Dictionary<string, int> CountLootById()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            LootCollectionManager collectionManager = LootCollectionManager.Instance;
            if (collectionManager == null)
                return counts;

            IReadOnlyList<CollectedLootInfo> collectedLoot = collectionManager.CollectedLoot;
            for (int i = 0; i < collectedLoot.Count; i++)
            {
                string lootId = collectedLoot[i].LootId;
                counts.TryGetValue(lootId, out int count);
                counts[lootId] = count + 1;
            }

            return counts;
        }

        private string GetAbilityKey(string ownerLootId, int abilityIndex, LootAbilityConfig ability)
        {
            if (!string.IsNullOrWhiteSpace(ability.AbilityId))
                return ability.AbilityId;

            return $"{ownerLootId}:{ability.Type}:{abilityIndex}";
        }

        private string FormatLootRequirements(IReadOnlyList<string> requiredLootIds)
        {
            if (requiredLootIds == null || requiredLootIds.Count == 0)
                return "any loot";

            return string.Join(", ", requiredLootIds);
        }
    }
}
