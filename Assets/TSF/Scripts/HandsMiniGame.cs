using DG.Tweening;
using UnityEngine;

namespace TSF
{
    public class HandsMiniGame : MonoBehaviour, IPortalMiniGame
    {
        [SerializeField] private Transform targetObject;
        [SerializeField, Min(0f)] private float appearDuration = 0.5f;
        [SerializeField] private Ease appearEase = Ease.OutBack;
        [SerializeField] private bool playOnEnable = true;

        private PortalAnimator _portalAnimator;
        private Tween _appearTween;
        private Vector3 _targetScale;
        private bool _hasTargetScale;
        private bool _appeared;

        private void Awake()
        {
            _portalAnimator = GetComponent<PortalAnimator>();
            CaptureTargetScale();
        }

        private void OnEnable()
        {
            _portalAnimator = GetComponent<PortalAnimator>();
            HideTargetObject();
            _appeared = false;
        }

        private void Update()
        {
            if (playOnEnable)
                TryPlayAppearAnimation();
        }

        private void OnDisable()
        {
            _appearTween?.Kill();
            _appearTween = null;
        }

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
        }

        public void OnHandExit()
        {
        }

        public void PlayAppearAnimation()
        {
            Transform target = targetObject;
            if (target == null)
                return;

            CaptureTargetScale();
            _appearTween?.Kill();
            _appeared = true;

            target.localScale = Vector3.zero;

            if (appearDuration <= 0f)
            {
                target.localScale = _targetScale;
                return;
            }

            _appearTween = target
                .DOScale(_targetScale, appearDuration)
                .SetEase(appearEase)
                .SetTarget(this);
        }

        private void TryPlayAppearAnimation()
        {
            if (_appeared)
                return;

            if (_portalAnimator != null && !_portalAnimator.IsFullyOpen)
                return;

            PlayAppearAnimation();
        }

        private void HideTargetObject()
        {
            Transform target = targetObject;
            if (target == null)
                return;

            CaptureTargetScale();
            _appearTween?.Kill();
            target.localScale = Vector3.zero;
        }

        private void CaptureTargetScale()
        {
            if (_hasTargetScale)
                return;

            Transform target = targetObject;
            if (target == null)
                return;

            _targetScale = target.localScale;
            _hasTargetScale = true;
        }
    }
}
