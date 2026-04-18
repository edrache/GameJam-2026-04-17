using DG.Tweening;
using UnityEngine;

namespace TSF
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public class PortalAnimator : MonoBehaviour
    {
        private static readonly int TwirlOffsetId = Shader.PropertyToID("_TwirlOffset");
        private static readonly int OpenAmountId = Shader.PropertyToID("_OpenAmount");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int PermanentRevealId = Shader.PropertyToID("_PortalPermanentReveal");
        private static readonly int FlashlightWorldPosId = Shader.PropertyToID("_FlashlightWorldPos");
        private static readonly int FlashlightWorldDirId = Shader.PropertyToID("_FlashlightWorldDir");
        private static readonly int FlashlightCosHalfAngleId = Shader.PropertyToID("_FlashlightCosHalfAngle");
        private static readonly int FlashlightRangeId = Shader.PropertyToID("_FlashlightRange");

        [Header("Motion")]
        [SerializeField] private float rotationSpeed = 1.5f;

        [Header("State")]
        [SerializeField, Range(0f, 1f)] private float initialOpenAmount = 0f;
        [SerializeField] private float initialGlowIntensity = 2f;

        [Header("Flashlight Opening")]
        [SerializeField, Min(0f)] private float requiredExposureTime = 2f;
        [SerializeField] private bool decayExposureWhenUnlit;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Tween _openTween;
        private float _twirlOffset;
        private float _openAmount;
        private float _glowIntensity;
        private float _exposureTime;
        private bool _permanentlyOpen;
        private bool _closing;

        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = value;
        }

        public bool IsFullyOpen => Application.isPlaying && !_closing && _permanentlyOpen && _openAmount >= 1f;

        private void Awake()
        {
            InitializeRenderer();
            InitializeState();
        }

        private void OnEnable()
        {
            InitializeRenderer();
            InitializeState();
        }

        private void OnValidate()
        {
            initialOpenAmount = Mathf.Clamp01(initialOpenAmount);
            requiredExposureTime = Mathf.Max(0f, requiredExposureTime);

            InitializeRenderer();

            if (!Application.isPlaying)
            {
                _glowIntensity = initialGlowIntensity;
                ApplyFloat(OpenAmountId, 1f);
                ApplyFloat(GlowIntensityId, _glowIntensity);
            }
        }

        private void InitializeRenderer()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propertyBlock ??= new MaterialPropertyBlock();
        }

        private void InitializeState()
        {
            if (!Application.isPlaying)
            {
                _openAmount = 1f;
                _glowIntensity = initialGlowIntensity;
                _closing = false;
                ApplyFloat(OpenAmountId, _openAmount);
                ApplyFloat(GlowIntensityId, _glowIntensity);
                ApplyFloat(PermanentRevealId, 0f);
                return;
            }

            _openAmount = initialOpenAmount;
            _glowIntensity = initialGlowIntensity;
            _exposureTime = Mathf.Clamp01(initialOpenAmount) * requiredExposureTime;
            _permanentlyOpen = initialOpenAmount >= 1f;
            _closing = false;

            ApplyFloat(OpenAmountId, _openAmount);
            ApplyFloat(GlowIntensityId, _glowIntensity);
            ApplyFloat(PermanentRevealId, _permanentlyOpen ? 1f : 0f);
        }

        private void Update()
        {
            _twirlOffset += rotationSpeed * Time.deltaTime;
            ApplyFloat(TwirlOffsetId, _twirlOffset);

            if (!Application.isPlaying || _permanentlyOpen || _closing)
                return;

            float flashlightExposure = GetFlashlightExposureAmount();
            if (flashlightExposure > 0.001f)
            {
                AddExposure(Time.deltaTime * flashlightExposure);
            }
            else if (decayExposureWhenUnlit)
            {
                RemoveExposure(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            _openTween?.Kill();
        }

        public void Open(float duration = 1f)
        {
            _closing = false;
            _permanentlyOpen = true;
            _exposureTime = requiredExposureTime;
            ApplyFloat(PermanentRevealId, 1f);
            TweenOpenAmount(1f, duration);
        }

        public void Close(float duration = 0.5f)
        {
            _closing = true;
            _permanentlyOpen = false;
            _exposureTime = 0f;
            ApplyFloat(PermanentRevealId, duration > 0f ? 1f : 0f);
            TweenOpenAmount(0f, duration, () =>
            {
                _closing = false;
                ApplyFloat(PermanentRevealId, 0f);
            });
        }

        public void SetIntensity(float value)
        {
            _glowIntensity = Mathf.Max(0f, value);
            ApplyFloat(GlowIntensityId, _glowIntensity);
        }

        private void TweenOpenAmount(float target, float duration, TweenCallback onComplete = null)
        {
            _openTween?.Kill();

            if (duration <= 0f)
            {
                _openAmount = target;
                ApplyFloat(OpenAmountId, _openAmount);
                onComplete?.Invoke();
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

            if (onComplete != null)
                _openTween.OnComplete(onComplete);
        }

        private void AddExposure(float deltaTime)
        {
            if (requiredExposureTime <= 0f)
            {
                _exposureTime = 0f;
                _openAmount = 1f;
                _permanentlyOpen = true;
                ApplyFloat(OpenAmountId, _openAmount);
                ApplyFloat(PermanentRevealId, 1f);
                return;
            }

            _exposureTime = Mathf.Min(requiredExposureTime, _exposureTime + deltaTime);
            _openAmount = Mathf.Clamp01(_exposureTime / requiredExposureTime);
            ApplyFloat(OpenAmountId, _openAmount);

            if (_openAmount >= 1f)
            {
                _permanentlyOpen = true;
                ApplyFloat(PermanentRevealId, 1f);
            }
        }

        private void RemoveExposure(float deltaTime)
        {
            if (requiredExposureTime <= 0f || _exposureTime <= 0f)
                return;

            _exposureTime = Mathf.Max(0f, _exposureTime - deltaTime);
            _openAmount = Mathf.Clamp01(_exposureTime / requiredExposureTime);
            ApplyFloat(OpenAmountId, _openAmount);
        }

        private float GetFlashlightExposureAmount()
        {
            float cosHalfAngle = Shader.GetGlobalFloat(FlashlightCosHalfAngleId);
            if (cosHalfAngle < 0f)
                return 0f;

            Vector3 flashlightPosition = Shader.GetGlobalVector(FlashlightWorldPosId);
            Vector3 flashlightDirection = ((Vector3)Shader.GetGlobalVector(FlashlightWorldDirId)).normalized;
            float flashlightRange = Shader.GetGlobalFloat(FlashlightRangeId);

            Bounds bounds = _renderer.bounds;
            float portalRadius = bounds.extents.magnitude;
            Vector3 flashlightToPortal = bounds.center - flashlightPosition;
            float distanceToPortal = flashlightToPortal.magnitude;
            float closestPortalDistance = Mathf.Max(0f, distanceToPortal - portalRadius);

            if (closestPortalDistance > flashlightRange)
                return 0f;

            Vector3 directionToPortal = flashlightToPortal / Mathf.Max(distanceToPortal, 0.0001f);
            float angleToPortal = Mathf.Acos(Mathf.Clamp(Vector3.Dot(directionToPortal, flashlightDirection), -1f, 1f));
            float flashlightHalfAngle = Mathf.Acos(Mathf.Clamp(cosHalfAngle, -1f, 1f));
            float portalAngularRadius = Mathf.Atan2(portalRadius, Mathf.Max(distanceToPortal, 0.0001f));
            float angularMargin = flashlightHalfAngle + portalAngularRadius - angleToPortal;
            float angularSoftness = Mathf.Max(0.05f, portalAngularRadius * 0.5f);
            float angularExposure = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-angularSoftness, angularSoftness, angularMargin));

            float rangeSoftness = Mathf.Max(0.25f, portalRadius);
            float rangeExposure = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, rangeSoftness, flashlightRange - closestPortalDistance));

            return angularExposure * rangeExposure;
        }

        private void ApplyFloat(int propertyId, float value)
        {
            if (_renderer == null || _propertyBlock == null)
                return;

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(propertyId, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
