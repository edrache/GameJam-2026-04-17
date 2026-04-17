using MoreMountains.Feedbacks;
using UnityEngine;

public class TimesUpWindowController : MonoBehaviour
{
    [SerializeField] private MMF_Player showFeedback;

    public void Show()
    {
        gameObject.SetActive(true);
        showFeedback?.PlayFeedbacks();
    }
}
