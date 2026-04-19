using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using MoreMountains.Feedbacks;

namespace TSF
{
    public class ScoreManager : MonoBehaviour
    {
        private const string HighScoreKey = "HighScore";

        private static ScoreManager _instance;

        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private string scorePrefix = "Score: ";
        [SerializeField] private MMF_Player scoreAddedFeedbacks;
        [SerializeField, Min(0f)] private float scoreCountDuration = 0.4f;

        private int _score;
        private int _displayedScore;
        private Coroutine _scoreCountCoroutine;
        private readonly List<ScoreLogEntry> _scoreLog = new List<ScoreLogEntry>();

        public static ScoreManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<ScoreManager>();
                return _instance;
            }
        }

        public int Score => _score;
        public int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);
        public IReadOnlyList<ScoreLogEntry> ScoreLog => _scoreLog;

        // Returns true if current score is a new high score.
        public bool CheckAndSaveHighScore()
        {
            int current = _score;
            int best = HighScore;
            if (current <= best) return false;
            PlayerPrefs.SetInt(HighScoreKey, current);
            PlayerPrefs.Save();
            return true;
        }

        public void ClearHighScore()
        {
            PlayerPrefs.DeleteKey(HighScoreKey);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _displayedScore = _score;
            UpdateScoreText();
        }

        public void AddScore(int amount)
        {
            AddScore(amount, "Score changed", string.Empty);
        }

        public void AddScore(int amount, string source, string reason)
        {
            int previousScore = _score;
            _score += amount;
            RecordScoreChange(source, reason, amount, previousScore, _score);

            if (amount > 0)
            {
                StartScoreCount();
                scoreAddedFeedbacks?.PlayFeedbacks();
            }
            else
            {
                SetDisplayedScore(_score);
            }
        }

        public void MultiplyScore(float multiplier)
        {
            MultiplyScore(multiplier, "Score multiplier", string.Empty);
        }

        public void MultiplyScore(float multiplier, string source, string reason)
        {
            int previousScore = _score;
            int newScore = Mathf.RoundToInt(_score * multiplier);
            SetScore(newScore, source, reason);
            if (newScore == previousScore)
                RecordScoreChange(source, reason, 0, previousScore, newScore);
        }

        public void SetScore(int score)
        {
            SetScore(score, "Score set", string.Empty);
        }

        public void SetScore(int score, string source, string reason)
        {
            int previousScore = _score;
            _score = score;
            if (previousScore != _score)
                RecordScoreChange(source, reason, _score - previousScore, previousScore, _score);
            SetDisplayedScore(_score);
        }

        public void ResetScore()
        {
            _score = 0;
            _scoreLog.Clear();
            SetDisplayedScore(_score);
        }

        public string BuildScoreSummary()
        {
            StringBuilder builder = new StringBuilder();

            AppendCollectedLootSummary(builder);

            if (_scoreLog.Count == 0)
            {
                builder.AppendLine("No score changes recorded.");
                return builder.ToString();
            }

            builder.AppendLine("Score changes:");
            for (int i = 0; i < _scoreLog.Count; i++)
            {
                ScoreLogEntry entry = _scoreLog[i];
                string sign = entry.Delta > 0 ? "+" : string.Empty;
                builder.Append($"{i + 1}. {sign}{entry.Delta} ({entry.PreviousScore} -> {entry.NewScore})");

                if (!string.IsNullOrWhiteSpace(entry.Source))
                    builder.Append($" - {entry.Source}");

                if (!string.IsNullOrWhiteSpace(entry.Reason))
                    builder.Append($": {entry.Reason}");

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private void AppendCollectedLootSummary(StringBuilder builder)
        {
            LootCollectionManager collectionManager = LootCollectionManager.Instance;
            if (collectionManager == null || collectionManager.CollectedLoot.Count == 0)
            {
                builder.AppendLine("Collected loot: none");
                builder.AppendLine();
                return;
            }

            Dictionary<string, LootSummary> lootSummaries = new Dictionary<string, LootSummary>();
            IReadOnlyList<CollectedLootInfo> collectedLoot = collectionManager.CollectedLoot;
            for (int i = 0; i < collectedLoot.Count; i++)
            {
                CollectedLootInfo loot = collectedLoot[i];
                if (!lootSummaries.TryGetValue(loot.LootId, out LootSummary summary))
                    summary = new LootSummary(loot.DisplayName);

                summary.Count++;
                summary.Points += loot.Points;
                lootSummaries[loot.LootId] = summary;
            }

            builder.AppendLine("Collected loot:");
            foreach (KeyValuePair<string, LootSummary> pair in lootSummaries)
            {
                LootSummary summary = pair.Value;
                builder.AppendLine($"- {summary.DisplayName} x{summary.Count}: +{summary.Points}");
            }

            builder.AppendLine();
        }

        private void RecordScoreChange(string source, string reason, int delta, int previousScore, int newScore)
        {
            _scoreLog.Add(new ScoreLogEntry(
                Time.timeSinceLevelLoad,
                string.IsNullOrWhiteSpace(source) ? "Score changed" : source,
                reason,
                delta,
                previousScore,
                newScore));
        }

        private void UpdateScoreText()
        {
            if (scoreText != null)
                scoreText.text = $"{scorePrefix}{_displayedScore}";
        }

        private void StartScoreCount()
        {
            if (_scoreCountCoroutine != null)
                StopCoroutine(_scoreCountCoroutine);

            if (scoreCountDuration <= 0f)
            {
                SetDisplayedScore(_score);
                return;
            }

            _scoreCountCoroutine = StartCoroutine(CountScoreToTarget());
        }

        private IEnumerator CountScoreToTarget()
        {
            int startScore = _displayedScore;
            float elapsed = 0f;

            while (elapsed < scoreCountDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / scoreCountDuration);
                _displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, _score, progress));
                UpdateScoreText();
                yield return null;
            }

            _displayedScore = _score;
            UpdateScoreText();
            _scoreCountCoroutine = null;
        }

        private void SetDisplayedScore(int value)
        {
            if (_scoreCountCoroutine != null)
            {
                StopCoroutine(_scoreCountCoroutine);
                _scoreCountCoroutine = null;
            }

            _displayedScore = value;
            UpdateScoreText();
        }

        private struct LootSummary
        {
            public LootSummary(string displayName)
            {
                DisplayName = displayName;
                Count = 0;
                Points = 0;
            }

            public string DisplayName;
            public int Count;
            public int Points;
        }
    }

    [Serializable]
    public class ScoreLogEntry
    {
        [SerializeField] private float time;
        [SerializeField] private string source;
        [SerializeField] private string reason;
        [SerializeField] private int delta;
        [SerializeField] private int previousScore;
        [SerializeField] private int newScore;

        public ScoreLogEntry(float time, string source, string reason, int delta, int previousScore, int newScore)
        {
            this.time = time;
            this.source = source;
            this.reason = reason;
            this.delta = delta;
            this.previousScore = previousScore;
            this.newScore = newScore;
        }

        public float Time => time;
        public string Source => source;
        public string Reason => reason;
        public int Delta => delta;
        public int PreviousScore => previousScore;
        public int NewScore => newScore;
    }
}
