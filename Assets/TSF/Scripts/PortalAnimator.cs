using DG.Tweening;
using UnityEngine;

namespace TSF
{
    [RequireComponent(typeof(MeshRenderer))]
    public class PortalAnimator : MonoBehaviour
    {
        private static readonly int TwirlOffsetId = Shader.PropertyToID("_TwirlOffset");
        private static readonly int OpenAmountId = Shader.PropertyToID("_OpenAmount");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

        [Header("Motion")]
        [SerializeField] private float rotationSpeed = 1.5f;

        [Header("State")]
        [SerializeField, Range(0f, 1f)] private float initialOpenAmount = 1f;
        [SerializeField] private float initialGlowIntensity = 2f;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Tween _openTween;
        private float _twirlOffset;
        private float _openAmount;
        private float _glowIntensity;

        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = value;
        }

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _openAmount = initialOpenAmount;
            _glowIntensity = initialGlowIntensity;

            ApplyFloat(OpenAmountId, _openAmount);
            ApplyFloat(GlowIntensityId, _glowIntensity);
        }

        private void Update()
        {
            _twirlOffset += rotationSpeed * Time.deltaTime;
            ApplyFloat(TwirlOffsetId, _twirlOffset);
        }

        private void OnDestroy()
        {
            _openTween?.Kill();
        }

        public void Open(float duration = 1f)
        {
            TweenOpenAmount(1f, duration);
        }

        public void Close(float duration = 0.5f)
        {
            TweenOpenAmount(0f, duration);
        }

        public void SetIntensity(float value)
        {
            _glowIntensity = Mathf.Max(0f, value);
            ApplyFloat(GlowIntensityId, _glowIntensity);
        }

        private void TweenOpenAmount(float target, float duration)
        {
            _openTween?.Kill();

            if (duration <= 0f)
            {
                _openAmount = target;
                ApplyFloat(OpenAmountId, _openAmount);
                return;
            }

            _openTween = DOTween
                .To(() => _openAmount, value =>
                {
                    _openAmount = value;
                    ApplyFloat(OpenAmountId, _openAmount);
                }, target, duration)
                .SetEase(Ease.InOutSine)
                .SetTarget(this);
        }

        private void ApplyFloat(int propertyId, float value)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(propertyId, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
