using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TSF
{
    public class HandsMiniGame : MonoBehaviour, IPortalMiniGame
    {
        [Serializable]
        private struct FloatRange
        {
            [SerializeField] private float min;
            [SerializeField] private float max;

            public FloatRange(float min, float max)
            {
                this.min = min;
                this.max = max;
            }

            public float RandomValue => UnityEngine.Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
        }

        [Serializable]
        private struct Vector3Range
        {
            [SerializeField] private Vector3 min;
            [SerializeField] private Vector3 max;

            public Vector3 RandomValue => new Vector3(
                UnityEngine.Random.Range(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x)),
                UnityEngine.Random.Range(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y)),
                UnityEngine.Random.Range(Mathf.Min(min.z, max.z), Mathf.Max(min.z, max.z)));
        }

        [Serializable]
        private class AnimatedObject
        {
            [SerializeField] private Transform targetObject;
            [SerializeField] private FloatRange appearDurationRange = new FloatRange(0.25f, 0.75f);
            [SerializeField] private FloatRange appearDelayRange = new FloatRange(0f, 0.25f);
            [SerializeField] private FloatRange scaleMultiplierRange = new FloatRange(0.9f, 1.1f);
            [SerializeField] private Vector3Range localPositionOffsetRange;
            [SerializeField] private Animator[] randomSpeedAnimators;
            [SerializeField] private FloatRange animationSpeedRange = new FloatRange(0.85f, 1.15f);
            [SerializeField] private Animator[] randomOffsetAnimators;
            [SerializeField] private FloatRange normalizedAnimationOffsetRange = new FloatRange(0f, 1f);

            private Tween _appearTween;
            private Vector3 _targetScale;
            private Vector3 _targetLocalPosition;
            private bool _hasTargetValues;

            public void Hide()
            {
                if (targetObject == null)
                    return;

                CaptureTargetValues();
                _appearTween?.Kill();
                _appearTween = null;
                targetObject.localScale = Vector3.zero;
                targetObject.localPosition = _targetLocalPosition;
            }

            public void Play(Ease ease, MonoBehaviour tweenOwner)
            {
                if (targetObject == null)
                    return;

                CaptureTargetValues();
                _appearTween?.Kill();

                targetObject.localScale = Vector3.zero;
                targetObject.localPosition = _targetLocalPosition + localPositionOffsetRange.RandomValue;

                ApplyRandomAnimatorValues();

                float duration = Mathf.Max(0f, appearDurationRange.RandomValue);
                float delay = Mathf.Max(0f, appearDelayRange.RandomValue);
                Vector3 randomTargetScale = _targetScale * Mathf.Max(0f, scaleMultiplierRange.RandomValue);

                if (duration <= 0f && delay <= 0f)
                {
                    targetObject.localScale = randomTargetScale;
                    return;
                }

                _appearTween = targetObject
                    .DOScale(randomTargetScale, duration)
                    .SetDelay(delay)
                    .SetEase(ease)
                    .SetTarget(tweenOwner);
            }

            public void KillTween()
            {
                _appearTween?.Kill();
                _appearTween = null;
            }

            private void CaptureTargetValues()
            {
                if (_hasTargetValues || targetObject == null)
                    return;

                _targetScale = targetObject.localScale;
                _targetLocalPosition = targetObject.localPosition;
                _hasTargetValues = true;
            }

            private void ApplyRandomAnimatorValues()
            {
                ApplyRandomAnimationSpeeds();
                ApplyRandomAnimationOffsets();
            }

            private void ApplyRandomAnimationSpeeds()
            {
                if (randomSpeedAnimators == null)
                    return;

                foreach (Animator animator in randomSpeedAnimators)
                {
                    if (animator != null)
                        animator.speed = Mathf.Max(0f, animationSpeedRange.RandomValue);
                }
            }

            private void ApplyRandomAnimationOffsets()
            {
                if (randomOffsetAnimators == null)
                    return;

                foreach (Animator animator in randomOffsetAnimators)
                {
                    if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                        continue;

                    int layer = 0;
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
                    if (stateInfo.fullPathHash == 0)
                        continue;

                    animator.Play(stateInfo.fullPathHash, layer, normalizedAnimationOffsetRange.RandomValue);
                    animator.Update(0f);
                }
            }
        }

        [SerializeField] private Transform targetObject;
        [SerializeField, Min(0f)] private float appearDuration = 0.5f;
        [SerializeField] private Ease appearEase = Ease.OutBack;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private List<AnimatedObject> animatedObjects = new List<AnimatedObject>();

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

            if (animatedObjects == null)
                return;

            foreach (AnimatedObject animatedObject in animatedObjects)
                animatedObject?.KillTween();
        }

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
        }

        public void OnHandExit()
        {
        }

        public void PlayAppearAnimation()
        {
            if (!HasAnimatedObjects())
            {
                PlayLegacyAppearAnimation();
                return;
            }

            _appeared = true;

            foreach (AnimatedObject animatedObject in animatedObjects)
                animatedObject?.Play(appearEase, this);
        }

        private void PlayLegacyAppearAnimation()
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
            if (HasAnimatedObjects())
            {
                foreach (AnimatedObject animatedObject in animatedObjects)
                    animatedObject?.Hide();

                return;
            }

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

        private bool HasAnimatedObjects()
        {
            return animatedObjects != null && animatedObjects.Count > 0;
        }
    }
}
