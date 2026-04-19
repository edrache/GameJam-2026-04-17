using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace TSF
{
    public class HighScoreServerClient : MonoBehaviour
    {
        private const string LocalBestScoreKey = "HighScore";

        [Header("Server")]
        [SerializeField] private string bestScoreUrl = "https://edrache.cytr.us/getyourloot/highscore/best.php";
        [SerializeField] private string saveScoreUrl = "https://edrache.cytr.us/getyourloot/highscore/save.php";
        [SerializeField, Min(1)] private int requestTimeoutSeconds = 5;

        [Header("UI")]
        [SerializeField] private TMP_Text serverBestScoreText;
        [SerializeField] private string serverBestScorePrefix = "Best: ";
        [SerializeField] private string loadingLabel = "Best: ...";
        [SerializeField] private string errorLabel = "Best: ?";
        [SerializeField] private bool loadOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool logResponses = true;

        public int ServerBestScore { get; private set; }
        public bool HasLoadedServerBestScore { get; private set; }

        private void Start()
        {
            if (!loadOnStart)
                return;

            StartCoroutine(LoadServerBestScore());
        }

        public void TrySubmitScore(int score)
        {
            int localBest = PlayerPrefs.GetInt(LocalBestScoreKey, 0);
            if (score <= localBest)
                return;

            PlayerPrefs.SetInt(LocalBestScoreKey, score);
            PlayerPrefs.Save();

            StartCoroutine(SendScore(score));
        }

        public void TrySubmitScoreAfterServerCheck(int score, System.Action<bool, int> onFinished)
        {
            StartCoroutine(TrySubmitScoreAfterServerCheckRoutine(score, onFinished));
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

        public IEnumerator LoadServerBestScore(System.Action<int> onLoaded = null, System.Action onFailed = null)
        {
            if (serverBestScoreText != null)
                serverBestScoreText.text = loadingLabel;

            using (UnityWebRequest request = UnityWebRequest.Get(bestScoreUrl))
            {
                request.timeout = requestTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Failed to load server high score: {request.error}");
                    if (serverBestScoreText != null)
                        serverBestScoreText.text = errorLabel;
                    onFailed?.Invoke();
                    yield break;
                }

                if (int.TryParse(request.downloadHandler.text.Trim(), out int bestScore))
                {
                    ApplyServerBestScore(bestScore);
                    onLoaded?.Invoke(bestScore);
                }
                else
                {
                    Debug.LogWarning($"Failed to parse server high score: {request.downloadHandler.text}");
                    if (serverBestScoreText != null)
                        serverBestScoreText.text = errorLabel;
                    onFailed?.Invoke();
                }
            }
        }

        public IEnumerator GetServerBestScore(System.Action<int> onLoaded)
        {
            yield return LoadServerBestScore(onLoaded);
        }

        private IEnumerator TrySubmitScoreAfterServerCheckRoutine(int score, System.Action<bool, int> onFinished)
        {
            bool loaded = false;
            int serverBest = ServerBestScore;

            yield return LoadServerBestScore(
                bestScore =>
                {
                    loaded = true;
                    serverBest = bestScore;
                },
                () => loaded = false);

            if (!loaded)
            {
                onFinished?.Invoke(false, serverBest);
                yield break;
            }

            if (score <= serverBest)
            {
                onFinished?.Invoke(false, serverBest);
                yield break;
            }

            bool saved = false;
            yield return SendScore(score, savedBestScore =>
            {
                saved = true;
                bool isNewHighScore = savedBestScore == score;
                ApplyServerBestScore(savedBestScore);
                SaveLocalBestScore(savedBestScore);
                onFinished?.Invoke(isNewHighScore, savedBestScore);
            });

            if (!saved)
                onFinished?.Invoke(false, serverBest);
        }

        private IEnumerator SendScore(int score)
        {
            yield return SendScore(score, null);
        }

        private IEnumerator SendScore(int score, System.Action<int> onSaved)
        {
            string separator = saveScoreUrl.Contains("?") ? "&" : "?";
            string url = $"{saveScoreUrl}{separator}score={score}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = requestTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Failed to submit high score: {request.error}");
                    yield break;
                }

                if (logResponses)
                    Debug.Log($"Server high score: {request.downloadHandler.text.Trim()}");

                if (int.TryParse(request.downloadHandler.text.Trim(), out int savedBestScore))
                    onSaved?.Invoke(savedBestScore);
                else
                    Debug.LogWarning($"Failed to parse saved server high score: {request.downloadHandler.text}");
            }
        }

        private void ApplyServerBestScore(int score)
        {
            ServerBestScore = Mathf.Max(0, score);
            HasLoadedServerBestScore = true;
            SaveLocalBestScore(ServerBestScore);

            if (serverBestScoreText != null)
                serverBestScoreText.text = $"{serverBestScorePrefix}{ServerBestScore}";
        }

        private static void SaveLocalBestScore(int score)
        {
            int localBest = PlayerPrefs.GetInt(LocalBestScoreKey, 0);
            if (score <= localBest)
                return;

            PlayerPrefs.SetInt(LocalBestScoreKey, score);
            PlayerPrefs.Save();
        }
    }
}
