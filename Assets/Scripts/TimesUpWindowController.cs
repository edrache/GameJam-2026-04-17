using MoreMountains.Feedbacks;
using Rewired;
using TMPro;
using TSF;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimesUpWindowController : MonoBehaviour
{
    [Header("Feedbacks")]
    [SerializeField] private MMF_Player showFeedback;

    [Header("UI References")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private string finalScorePrefix = "Score: ";
    [SerializeField] private string highScorePrefix = "Best: ";
    [SerializeField] private string newHighScoreLabel = "New Best!";

    [Header("Input")]
    [SerializeField] private string restartAction = "Restart";

    private bool _active;
    private Rewired.Player _player;

    private void Awake()
    {
        if (ReInput.isReady)
            _player = ReInput.players.GetPlayer(0);
    }

    private void Update()
    {
        if (!_active) return;
        if (_player == null && ReInput.isReady)
            _player = ReInput.players.GetPlayer(0);
        if (_player != null && _player.GetButtonDown(restartAction))
            Restart();
    }

    public void Show(int score, bool isNewHighScore)
    {
        gameObject.SetActive(true);
        _active = true;

        if (finalScoreText != null)
            finalScoreText.text = $"{finalScorePrefix}{score}";

        if (highScoreText != null)
        {
            if (isNewHighScore)
                highScoreText.text = newHighScoreLabel;
            else
            {
                int best = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : score;
                highScoreText.text = $"{highScorePrefix}{best}";
            }
        }

        if (showFeedback != null)
        {
            showFeedback.PlayerTimescaleMode = TimescaleModes.Unscaled;
            showFeedback.PlayFeedbacks();
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
