using UnityEngine;

namespace TSF
{
    public class PortalClippingPlaneController : MonoBehaviour
    {
        private static readonly int ClipPlanePositionId = Shader.PropertyToID("_ClipPlanePosition");
        private static readonly int ClipPlaneNormalId = Shader.PropertyToID("_ClipPlaneNormal");
        private static readonly int ClipSideId = Shader.PropertyToID("_ClipSide");
        private static readonly int ClipEnabledId = Shader.PropertyToID("_ClipEnabled");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int GradientTopColorId = Shader.PropertyToID("_GradientTopColor");
        private static readonly int GradientBottomColorId = Shader.PropertyToID("_GradientBottomColor");
        private static readonly int GradientOffsetId = Shader.PropertyToID("_GradientOffset");
        private static readonly int GradientScaleId = Shader.PropertyToID("_GradientScale");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineId = Shader.PropertyToID("_Outline");

        [SerializeField] private Transform clippedRoot;
        [SerializeField] private Transform portalPlane;
        [SerializeField] private Shader clippedShader;
        [SerializeField] private Color clippedColor = Color.white;
        [SerializeField] private Color gradientTopColor = new Color(1f, 0.9f, 0.75f, 1f);
        [SerializeField] private Color gradientBottomColor = new Color(0.85f, 0.45f, 0.35f, 1f);
        [SerializeField] private float gradientOffset;
        [SerializeField, Min(0.001f)] private float gradientScale = 1f;
        [SerializeField] private Color outlineColor = new Color(0.08f, 0.025f, 0.03f, 1f);
        [SerializeField, Min(0f)] private float outlineWidth = 1.5f;
        [SerializeField] private bool autoDetectClipSide = true;
        [SerializeField] private bool invertClipSide;
        [SerializeField] private bool includeInactiveRenderers = true;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            ApplyClippingPlane();
        }

        private void LateUpdate()
        {
            ApplyClippingPlane();
        }

        private void OnValidate()
        {
            if (portalPlane == null)
                portalPlane = transform;

            if (clippedShader == null)
                clippedShader = Shader.Find("TSF/PortalClippedBase1");
        }

        private void Initialize()
        {
            if (portalPlane == null)
                portalPlane = transform;

            if (clippedShader == null)
                clippedShader = Shader.Find("TSF/PortalClippedBase1");

            _propertyBlock ??= new MaterialPropertyBlock();
            RefreshRenderers();
            ApplyClippedShader();
        }

        private void RefreshRenderers()
        {
            if (clippedRoot == null)
            {
                _renderers = System.Array.Empty<Renderer>();
                return;
            }

            _renderers = clippedRoot.GetComponentsInChildren<Renderer>(includeInactiveRenderers);
        }

        private void ApplyClippedShader()
        {
            if (clippedShader == null || _renderers == null)
                return;

            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer == null)
                    continue;

                Material[] materials = targetRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                        materials[i].shader = clippedShader;
                }
            }
        }

        private void ApplyClippingPlane()
        {
            if (portalPlane == null || _renderers == null)
                return;

            Vector3 portalPosition = portalPlane.position;
            Vector3 portalForward = portalPlane.forward;
            Vector4 planePosition = new Vector4(portalPosition.x, portalPosition.y, portalPosition.z, 0f);
            Vector4 planeNormal = new Vector4(portalForward.x, portalForward.y, portalForward.z, 0f);
            float clipSide = GetClipSide();

            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer == null)
                    continue;

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetVector(ClipPlanePositionId, planePosition);
                _propertyBlock.SetVector(ClipPlaneNormalId, planeNormal);
                _propertyBlock.SetFloat(ClipSideId, clipSide);
                _propertyBlock.SetFloat(ClipEnabledId, 1f);
                _propertyBlock.SetColor(ColorId, clippedColor);
                _propertyBlock.SetColor(GradientTopColorId, gradientTopColor);
                _propertyBlock.SetColor(GradientBottomColorId, gradientBottomColor);
                _propertyBlock.SetFloat(GradientOffsetId, gradientOffset);
                _propertyBlock.SetFloat(GradientScaleId, Mathf.Max(0.001f, gradientScale));
                _propertyBlock.SetColor(OutlineColorId, outlineColor);
                _propertyBlock.SetFloat(OutlineId, outlineWidth);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private float GetClipSide()
        {
            float side = 1f;
            if (autoDetectClipSide && clippedRoot != null && portalPlane != null)
            {
                float detectedSide = Vector3.Dot(clippedRoot.position - portalPlane.position, portalPlane.forward);
                if (Mathf.Abs(detectedSide) > 0.001f)
                    side = Mathf.Sign(detectedSide);
            }

            return invertClipSide ? -side : side;
        }
    }
}
