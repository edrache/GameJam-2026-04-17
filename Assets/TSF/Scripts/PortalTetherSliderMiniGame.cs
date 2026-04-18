using UnityEngine;
using UnityEngine.UI;

namespace TSF
{
    public class PortalTetherSliderMiniGame : MonoBehaviour, IPortalMiniGame
    {
        [SerializeField] private Slider slider;
        [SerializeField] private float fillDuration = 2f;
        [SerializeField] private GameObject lootPrefab;
        [SerializeField, Min(0)] private int fallbackLootPoints = 10;
        [SerializeField, Min(0f)] private float removeDuration = 0.25f;
        [SerializeField, Min(0f)] private float minimumPortalDistance = 0.5f;
        [SerializeField] private Transform lookTarget;

        private ArmReachController _reach;
        private PlayerController _playerController;
        private FPSCameraController _cameraController;
        private Transform _activeLookTarget;
        private float _maxPortalDistance;
        private bool _active;
        private bool _completed;

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
            _reach = reach;
            _playerController = reach.GetComponentInParent<PlayerController>();
            _cameraController = _playerController != null ? _playerController.GetComponentInChildren<FPSCameraController>() : null;

            _maxPortalDistance = GetPlanarDistanceToPlayer();
            _activeLookTarget = lookTarget != null ? lookTarget : transform;
            _reach.LockMiniGameExit(this);
            ApplyPlayerLock();

            _active = true;
            _completed = false;
            slider.value = 0f;
            slider.gameObject.SetActive(true);
        }

        public void OnHandExit()
        {
            _reach?.UnlockMiniGameExit(this);
            ClearPlayerLock();
            _active = false;
            _reach = null;
            slider.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_active || _completed) return;

            slider.value += Time.deltaTime / fillDuration;

            if (slider.value >= 1f)
            {
                _completed = true;
                slider.gameObject.SetActive(false);
                AwardLootScore();
                _reach.UnlockMiniGameExit(this);
                ClearPlayerLock();
                _reach.TriggerLoot(lootPrefab);
                QueuePortalRemoval();
            }
        }

        private void LateUpdate()
        {
            if (_active && !_completed)
                ApplyPlayerLock();
        }

        private void OnDisable()
        {
            _reach?.UnlockMiniGameExit(this);
            ClearPlayerLock();
        }

        private float GetPlanarDistanceToPlayer()
        {
            Transform playerTransform = _playerController != null ? _playerController.transform : _reach != null ? _reach.transform : null;
            if (playerTransform == null)
                return minimumPortalDistance;

            Vector3 offset = playerTransform.position - transform.position;
            offset.y = 0f;
            return Mathf.Max(minimumPortalDistance, offset.magnitude);
        }

        private void ClearPlayerLock()
        {
            _playerController?.ClearDistanceConstraint(this);
            _cameraController?.ClearForcedLookAt(this);
            _playerController = null;
            _cameraController = null;
            _activeLookTarget = null;
        }

        private void ApplyPlayerLock()
        {
            _playerController?.SetDistanceConstraint(this, transform, minimumPortalDistance, _maxPortalDistance);
            _cameraController?.SetForcedLookAt(this, _activeLookTarget);
        }

        private void AwardLootScore()
        {
            if (lootPrefab == null)
                return;

            int points = fallbackLootPoints;
            LootScoreValue lootScoreValue = lootPrefab.GetComponentInChildren<LootScoreValue>();
            if (lootScoreValue != null)
                points = lootScoreValue.Points;

            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
                scoreManager.AddScore(points);
        }

        private void QueuePortalRemoval()
        {
            _active = false;
            _reach.RemovePortalWhenIdle(this, gameObject, removeDuration);
            _reach = null;
        }
    }
}
