using UnityEngine;
using TMPro;

namespace TSF
{
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager _instance;

        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private string scorePrefix = "Score: ";

        private int _score;

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

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            UpdateScoreText();
        }

        public void AddScore(int amount)
        {
            _score += amount;
            UpdateScoreText();
        }

        public void ResetScore()
        {
            _score = 0;
            UpdateScoreText();
        }

        private void UpdateScoreText()
        {
            if (scoreText != null)
                scoreText.text = $"{scorePrefix}{_score}";
        }
    }
}
