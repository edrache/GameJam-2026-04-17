using System.Collections;
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
            _score += amount;

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

        public void ResetScore()
        {
            _score = 0;
            SetDisplayedScore(_score);
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
    }
}
