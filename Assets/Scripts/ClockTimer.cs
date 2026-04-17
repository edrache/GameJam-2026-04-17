using UnityEngine;
using UnityEngine.Events;

public class ClockTimer : MonoBehaviour
{
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private float totalTime = 30f;
    [SerializeField] private float rightStart = 17f;
    [SerializeField] private float rightEnd = -190f;

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
            onTimeUp?.Invoke();
            return;
        }

        SetRight(Mathf.Lerp(rightEnd, rightStart, timeRemaining / totalTime));
    }

    public void StartTimer()
    {
        timeRemaining = totalTime;
        running = true;
        SetRight(rightStart);
    }

    public void StopTimer() => running = false;

    public void ResumeTimer() => running = true;

    // RectTransform "Right" = -offsetMax.x
    private void SetRight(float right)
    {
        Vector2 max = fillRect.offsetMax;
        max.x = -right;
        fillRect.offsetMax = max;
    }
}