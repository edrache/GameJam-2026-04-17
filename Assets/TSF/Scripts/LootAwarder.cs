using UnityEngine;

namespace TSF
{
    public static class LootAwarder
    {
        public static void Award(GameObject lootPrefab, int fallbackPoints)
        {
            if (lootPrefab == null)
                return;

            LootScoreValue lootScoreValue = lootPrefab.GetComponentInChildren<LootScoreValue>();
            int points = lootScoreValue != null ? lootScoreValue.Points : fallbackPoints;
            string displayName = lootScoreValue != null ? lootScoreValue.DisplayName : lootPrefab.name;

            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
                scoreManager.AddScore(points, $"Loot: {displayName}", "Base loot value.");

            LootCollectionManager collectionManager = LootCollectionManager.Instance;
            if (collectionManager != null)
                collectionManager.RecordLoot(lootPrefab, lootScoreValue, points);
            else
                Debug.LogWarning($"[LootAwarder] Loot '{lootPrefab.name}' was awarded, but no LootCollectionManager was found.");

            LootAbilityManager abilityManager = LootAbilityManager.Instance;
            if (abilityManager != null && lootScoreValue != null)
                abilityManager.ApplyOnLootCollected(lootScoreValue);
        }
    }
}
