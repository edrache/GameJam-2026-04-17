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

        private Tween _appearTween;
        private Vector3 _targetScale;
        private bool _hasTargetScale;

        private Transform Target => targetObject != null ? targetObject : transform;

        private void Awake()
        {
            CaptureTargetScale();
        }

        private void OnEnable()
        {
            if (playOnEnable)
                PlayAppearAnimation();
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
            Transform target = Target;
            if (target == null)
                return;

            CaptureTargetScale();
            _appearTween?.Kill();

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

        private void CaptureTargetScale()
        {
            if (_hasTargetScale)
                return;

            Transform target = Target;
            if (target == null)
                return;

            _targetScale = target.localScale;
            _hasTargetScale = true;
        }
    }
}
