using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace TSF
{
    public class GameStartFeedbacks : MonoBehaviour
    {
        [SerializeField] private MMF_Player startFeedbacks;
        [SerializeField, Min(0f)] private float startDelay = 0f;
        [SerializeField] private bool playOnlyOnce = true;

        private bool _hasPlayed;
        private Coroutine _playCoroutine;

        private void Start()
        {
            Play();
        }

        public void Play()
        {
            if (playOnlyOnce && _hasPlayed)
                return;

            if (_playCoroutine != null)
                StopCoroutine(_playCoroutine);

            _playCoroutine = StartCoroutine(PlayAfterDelay());
        }

        private IEnumerator PlayAfterDelay()
        {
            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);

            _hasPlayed = true;
            startFeedbacks?.PlayFeedbacks();
            _playCoroutine = null;
        }
    }
}
