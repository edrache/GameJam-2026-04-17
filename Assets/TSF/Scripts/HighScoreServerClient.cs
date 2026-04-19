using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TSF
{
    public class HighScoreServerClient : MonoBehaviour
    {
        private const string LocalBestScoreKey = "HighScore";

        [SerializeField] private string bestScoreUrl = "https://your-domain.com/highscore/best.php";
        [SerializeField] private string saveScoreUrl = "https://your-domain.com/highscore/save.php";
        [SerializeField] private bool logResponses = true;

        public void TrySubmitScore(int score)
        {
            int localBest = PlayerPrefs.GetInt(LocalBestScoreKey, 0);
            if (score <= localBest)
                return;

            PlayerPrefs.SetInt(LocalBestScoreKey, score);
            PlayerPrefs.Save();

            StartCoroutine(SendScore(score));
        }

        public void TrySubmitCurrentScore()
        {
            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager == null)
            {
                Debug.LogWarning("Cannot submit high score because no ScoreManager exists in the scene.");
                return;
            }

            TrySubmitScore(scoreManager.Score);
        }

        public IEnumerator GetServerBestScore(System.Action<int> onLoaded)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(bestScoreUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Failed to load server high score: {request.error}");
                    yield break;
                }

                if (int.TryParse(request.downloadHandler.text.Trim(), out int bestScore))
                    onLoaded?.Invoke(bestScore);
            }
        }

        private IEnumerator SendScore(int score)
        {
            string separator = saveScoreUrl.Contains("?") ? "&" : "?";
            string url = $"{saveScoreUrl}{separator}score={score}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Failed to submit high score: {request.error}");
                    yield break;
                }

                if (logResponses)
                    Debug.Log($"Server high score: {request.downloadHandler.text.Trim()}");
            }
        }
    }
}
