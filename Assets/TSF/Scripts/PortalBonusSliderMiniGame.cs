using System.Collections;
using MoreMountains.Feedbacks;
using Rewired;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TSF
{
    public class PortalBonusSliderMiniGame : MonoBehaviour, IPortalMiniGame
    {
        public enum BonusHitMode
        {
            CompleteSlider,
            AddScore
        }

        [System.Serializable]
        private class BonusZone
        {
            [SerializeField, Range(0f, 1f)] private float start = 0.25f;
            [SerializeField, Range(0f, 1f)] private float end = 0.35f;

            private bool _claimed;

            public bool Claimed => _claimed;
            public float Start => start;
            public float End => end;

            public bool Contains(float value)
            {
                float min = Mathf.Min(start, end);
                float max = Mathf.Max(start, end);
                return value >= min && value <= max;
            }

            public void Claim()
            {
                _claimed = true;
            }

            public void Reset()
            {
                _claimed = false;
            }
        }

        [Header("Slider")]
        [SerializeField] private Slider slider;
        [SerializeField, Min(0.01f)] private float fillDuration = 2f;

        [Header("Bonus")]
        [SerializeField] private BonusHitMode hitMode = BonusHitMode.CompleteSlider;
        [SerializeField] private string bonusAction = "Bonus";
        [SerializeField, Min(0)] private int bonusScore = 20;
        [SerializeField] private BonusZone[] bonusZones =
        {
            new BonusZone(),
            new BonusZone(),
            new BonusZone()
        };

        [Header("Bonus Markers")]
        [SerializeField] private RectTransform bonusMarkersRoot;
        [SerializeField] private RectTransform bonusMarkerPrefab;
        [SerializeField, Min(0f)] private float markerRadius = 80f;
        [SerializeField, Min(2)] private int markersPerZone = 5;
        [SerializeField] private float markerZeroAngle = -90f;
        [SerializeField] private float markerSweepDegrees = 360f;
        [SerializeField] private bool markersClockwise = true;
        [SerializeField] private bool invertMarkerY = true;
        [SerializeField] private bool hideMarkerTemplate = true;
        [SerializeField] private bool rebuildMarkersOnEnable = true;
        [SerializeField] private bool rebuildMarkersOnHandEnter = true;
        [SerializeField] private Color availableMarkerColor = new Color(1f, 0.82f, 0.12f, 1f);
        [SerializeField] private Color claimedMarkerColor = new Color(0.35f, 1f, 0.55f, 1f);
        [SerializeField] private Color debugZeroMarkerColor = new Color(1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color debugQuarterMarkerColor = new Color(1f, 0.82f, 0.12f, 1f);
        [SerializeField] private Color debugHalfMarkerColor = new Color(0.35f, 1f, 0.55f, 1f);
        [SerializeField] private Color debugThreeQuarterMarkerColor = new Color(0.1f, 0.7f, 1f, 1f);

        [Header("Loot")]
        [SerializeField] private GameObject lootPrefab;
        [SerializeField, Min(0)] private int fallbackLootPoints = 10;
        [SerializeField, Min(0f)] private float removeDuration = 0.25f;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player bonusHitFeedback;
        [SerializeField] private MMF_Player bonusMissFeedback;
        [SerializeField] private MMF_Player completionFeedback;

        private ArmReachController _reach;
        private Player _player;
        private bool _initialized;
        private bool _active;
        private bool _completed;
        private Coroutine _markerRebuildCoroutine;
        private Image[][] _zoneMarkerImages;
#if UNITY_EDITOR
        private bool _editorRebuildQueued;
#endif

        private void OnEnable()
        {
            if (rebuildMarkersOnEnable)
                RebuildBonusMarkers();
        }

        private void OnValidate()
        {
            markerRadius = Mathf.Max(0f, markerRadius);
            markersPerZone = Mathf.Max(2, markersPerZone);

#if UNITY_EDITOR
            QueueEditorMarkerRebuild();
#endif
        }

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
            _reach = reach;
            _active = true;
            _completed = false;
            ResetSliderRun();

            if (slider != null)
                slider.gameObject.SetActive(true);

            if (rebuildMarkersOnHandEnter)
                QueueRuntimeMarkerRebuild();
        }

        public void OnHandExit()
        {
            _active = false;
            _reach = null;

            if (slider != null)
                slider.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_active || _completed || slider == null)
                return;

            if (ReInput.isReady && !_initialized)
                Initialize();

            slider.value += Time.deltaTime / fillDuration;

            if (_player != null && !string.IsNullOrEmpty(bonusAction) && _player.GetButtonDown(bonusAction))
                HandleBonusInput();

            if (slider.value >= 1f)
                Complete();
        }

        private void Initialize()
        {
            _player = ReInput.players.GetPlayer(0);
            _initialized = true;
        }

        private void HandleBonusInput()
        {
            BonusZone zone = GetActiveBonusZone();
            if (zone == null)
            {
                ResetSliderRun();
                bonusMissFeedback?.PlayFeedbacks();
                return;
            }

            zone.Claim();
            RefreshBonusMarkers();
            bonusHitFeedback?.PlayFeedbacks();

            switch (hitMode)
            {
                case BonusHitMode.CompleteSlider:
                    ScoreManager.Instance?.AddScore(0, "Portal slider bonus", "Hit the bonus zone and completed the slider instantly.");
                    slider.value = 1f;
                    Complete();
                    break;
                case BonusHitMode.AddScore:
                    ScoreManager.Instance?.AddScore(bonusScore, "Portal slider bonus", "Hit the active bonus zone.");
                    break;
            }
        }

        private BonusZone GetActiveBonusZone()
        {
            if (bonusZones == null)
                return null;

            for (int i = 0; i < bonusZones.Length; i++)
            {
                BonusZone zone = bonusZones[i];
                if (zone != null && !zone.Claimed && zone.Contains(slider.value))
                    return zone;
            }

            return null;
        }

        private void ResetSliderRun()
        {
            if (slider != null)
                slider.value = 0f;

            if (bonusZones == null)
                return;

            for (int i = 0; i < bonusZones.Length; i++)
                bonusZones[i]?.Reset();

            RefreshBonusMarkers();
        }

        private void Complete()
        {
            if (_completed)
                return;

            _completed = true;
            _active = false;

            if (slider != null)
                slider.gameObject.SetActive(false);

            completionFeedback?.PlayFeedbacks();
            LootAwarder.Award(lootPrefab, fallbackLootPoints);
            _reach?.TriggerLoot(lootPrefab);
            QueuePortalRemoval();
        }

        private void QueuePortalRemoval()
        {
            if (_reach == null)
                return;

            ArmReachController reach = _reach;
            _reach = null;
            reach.RemovePortalWhenIdle(this, gameObject, removeDuration);
        }

        [ContextMenu("Bonus Markers/Rebuild Bonus Markers")]
        private void RebuildBonusMarkers()
        {
            RectTransform root = GetMarkersRoot();
            if (root == null || bonusMarkerPrefab == null || bonusZones == null)
                return;

            ClearGeneratedMarkers(root);
            HideMarkerTemplateIfNeeded(root);

            _zoneMarkerImages = new Image[bonusZones.Length][];
            for (int zoneIndex = 0; zoneIndex < bonusZones.Length; zoneIndex++)
            {
                BonusZone zone = bonusZones[zoneIndex];
                if (zone == null)
                    continue;

                int markerCount = Mathf.Max(2, markersPerZone);
                _zoneMarkerImages[zoneIndex] = new Image[markerCount];

                for (int markerIndex = 0; markerIndex < markerCount; markerIndex++)
                {
                    float progress = markerCount == 1 ? 0.5f : markerIndex / (markerCount - 1f);
                    float value = Mathf.Lerp(zone.Start, zone.End, progress);
                    RectTransform marker = Instantiate(bonusMarkerPrefab, root, false);
                    marker.name = $"_GeneratedBonusMarker_{zoneIndex}_{markerIndex}";
                    marker.gameObject.SetActive(true);
                    PlaceMarker(marker, value);

                    Image markerImage = marker.GetComponent<Image>();
                    if (markerImage != null)
                    {
                        markerImage.color = availableMarkerColor;
                        _zoneMarkerImages[zoneIndex][markerIndex] = markerImage;
                    }
                }
            }
        }

        [ContextMenu("Bonus Markers/Debug Cardinal Points")]
        private void DebugCardinalMarkers()
        {
            RectTransform root = PrepareMarkerRoot();
            if (root == null)
                return;

            CreateMarker(root, "Debug_0_Top", 0f, debugZeroMarkerColor);
            CreateMarker(root, "Debug_025", 0.25f, debugQuarterMarkerColor);
            CreateMarker(root, "Debug_050", 0.5f, debugHalfMarkerColor);
            CreateMarker(root, "Debug_075", 0.75f, debugThreeQuarterMarkerColor);
        }

        [ContextMenu("Bonus Markers/Debug Bonus Zone Edges")]
        private void DebugBonusZoneEdges()
        {
            RectTransform root = PrepareMarkerRoot();
            if (root == null || bonusZones == null)
                return;

            for (int zoneIndex = 0; zoneIndex < bonusZones.Length; zoneIndex++)
            {
                BonusZone zone = bonusZones[zoneIndex];
                if (zone == null)
                    continue;

                CreateMarker(root, $"Debug_Zone_{zoneIndex}_Start", zone.Start, debugZeroMarkerColor);
                CreateMarker(root, $"Debug_Zone_{zoneIndex}_End", zone.End, debugHalfMarkerColor);
            }
        }

        [ContextMenu("Bonus Markers/Debug Current Slider Value")]
        private void DebugCurrentSliderValue()
        {
            RectTransform root = PrepareMarkerRoot();
            if (root == null || slider == null)
                return;

            CreateMarker(root, "Debug_Current_Slider_Value", slider.value, debugThreeQuarterMarkerColor);
        }

        [ContextMenu("Bonus Markers/Clear Generated Markers")]
        private void ClearBonusMarkersFromMenu()
        {
            RectTransform root = GetMarkersRoot();
            if (root == null)
                return;

            ClearGeneratedMarkers(root);
            _zoneMarkerImages = null;
        }

        private RectTransform GetMarkersRoot()
        {
            if (bonusMarkersRoot != null)
                return bonusMarkersRoot;

            return slider != null ? slider.transform as RectTransform : null;
        }

        private RectTransform PrepareMarkerRoot()
        {
            RectTransform root = GetMarkersRoot();
            if (root == null || bonusMarkerPrefab == null)
                return null;

            ClearGeneratedMarkers(root);
            HideMarkerTemplateIfNeeded(root);
            _zoneMarkerImages = null;
            return root;
        }

        private void ClearGeneratedMarkers(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith("_GeneratedBonusMarker_"))
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private RectTransform CreateMarker(RectTransform root, string markerName, float value, Color color)
        {
            RectTransform marker = Instantiate(bonusMarkerPrefab, root, false);
            marker.name = $"_GeneratedBonusMarker_{markerName}_{value:0.###}";
            marker.gameObject.SetActive(true);
            PlaceMarker(marker, value);

            Image markerImage = marker.GetComponent<Image>();
            if (markerImage != null)
                markerImage.color = color;

            return marker;
        }

        private void QueueRuntimeMarkerRebuild()
        {
            if (!Application.isPlaying)
            {
                RebuildBonusMarkers();
                return;
            }

            if (_markerRebuildCoroutine != null)
                StopCoroutine(_markerRebuildCoroutine);

            _markerRebuildCoroutine = StartCoroutine(RebuildMarkersAfterLayout());
        }

        private IEnumerator RebuildMarkersAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            RebuildBonusMarkers();
            RefreshBonusMarkers();
            _markerRebuildCoroutine = null;
        }

        private void PlaceMarker(RectTransform marker, float value)
        {
            float angleDirection = markersClockwise == invertMarkerY ? 1f : -1f;
            float angle = markerZeroAngle + value * markerSweepDegrees * angleDirection;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            if (invertMarkerY)
                direction.y *= -1f;

            marker.anchorMin = new Vector2(0.5f, 0.5f);
            marker.anchorMax = new Vector2(0.5f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = direction * markerRadius;
            marker.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
        }

        private void HideMarkerTemplateIfNeeded(RectTransform root)
        {
            if (!hideMarkerTemplate || bonusMarkerPrefab == null || bonusMarkerPrefab.parent != root)
                return;

            bonusMarkerPrefab.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void QueueEditorMarkerRebuild()
        {
            if (Application.isPlaying || !isActiveAndEnabled || !rebuildMarkersOnEnable || _editorRebuildQueued)
                return;

            _editorRebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null)
                    return;

                _editorRebuildQueued = false;
                if (!Application.isPlaying && isActiveAndEnabled && rebuildMarkersOnEnable)
                    RebuildBonusMarkers();
            };
        }
#endif

        private void RefreshBonusMarkers()
        {
            if (_zoneMarkerImages == null || bonusZones == null)
                return;

            int zoneCount = Mathf.Min(_zoneMarkerImages.Length, bonusZones.Length);
            for (int zoneIndex = 0; zoneIndex < zoneCount; zoneIndex++)
            {
                Image[] markers = _zoneMarkerImages[zoneIndex];
                if (markers == null)
                    continue;

                Color color = bonusZones[zoneIndex] != null && bonusZones[zoneIndex].Claimed
                    ? claimedMarkerColor
                    : availableMarkerColor;

                for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
                {
                    if (markers[markerIndex] != null)
                        markers[markerIndex].color = color;
                }
            }
        }
    }
}
