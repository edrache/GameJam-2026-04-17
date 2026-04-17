using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ClockTimer : MonoBehaviour
{
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float totalTime = 30f;
    [SerializeField] private float rightStart = 17f;
    [SerializeField] private float rightEnd = -190f;
    [SerializeField] private float warningTime = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private MMF_Player warningFeedback;

    private bool warningTriggered;

    public UnityEvent onTimeUp;

    private float timeRemaining;
    private bool running;

    public float TimeRemaining => timeRemaining;
    public float NormalizedTime => timeRemaining / totalTime;

    void Start()
    {
        StartTimer();
    }

    void Update()
    {
        if (!running) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            running = false;
            SetRight(rightEnd);
            UpdateText();
            UpdateColor();
            onTimeUp?.Invoke();
            return;
        }

        SetRight(Mathf.Lerp(rightEnd, rightStart, timeRemaining / totalTime));
        UpdateText();
        UpdateColor();
    }

    public void StartTimer()
    {
        timeRemaining = totalTime;
        running = true;
        warningTriggered = false;
        SetRight(rightStart);
        UpdateText();
        UpdateColor();
    }

    public void StopTimer() => running = false;

    public void ResumeTimer() => running = true;

    private void UpdateText()
    {
        if (timerText == null) return;
        int total = Mathf.CeilToInt(timeRemaining);
        int minutes = total / 60;
        int seconds = total % 60;
        timerText.text = $"{minutes}:{seconds:D2}";
    }

    private void UpdateColor()
    {
        if (fillImage == null) return;

        bool isWarning = timeRemaining <= warningTime;
        fillImage.color = isWarning ? warningColor : normalColor;

        if (isWarning && !warningTriggered)
        {
            warningTriggered = true;
            if (warningFeedback == null)
                Debug.LogError("[ClockTimer] warningFeedback is not assigned!", this);
            else
                warningFeedback.PlayFeedbacks();
        }
    }

    // RectTransform "Right" = -offsetMax.x
    private void SetRight(float right)
    {
        Vector2 max = fillRect.offsetMax;
        max.x = -right;
        fillRect.offsetMax = max;
    }
}