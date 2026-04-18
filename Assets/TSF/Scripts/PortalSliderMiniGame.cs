using UnityEngine;
using UnityEngine.UI;

namespace TSF
{
    public class PortalSliderMiniGame : MonoBehaviour, IPortalMiniGame
    {
        [SerializeField] private Slider slider;
        [SerializeField] private float fillDuration = 2f;
        [SerializeField] private GameObject lootPrefab;
        [SerializeField, Min(0)] private int fallbackLootPoints = 10;
        [SerializeField, Min(0f)] private float removeDuration = 0.25f;

        private ArmReachController _reach;
        private bool _active;
        private bool _completed;

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
            _reach = reach;
            _active = true;
            _completed = false;
            slider.value = 0f;
            slider.gameObject.SetActive(true);
        }

        public void OnHandExit()
        {
            _active = false;
            _reach = null;
            slider.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_active || _completed) return;

            slider.value += Time.deltaTime / fillDuration;

            if (slider.value >= 1f)
            {
                _completed = true;
                slider.gameObject.SetActive(false);
                AwardLootScore();
                _reach.TriggerLoot(lootPrefab);
                QueuePortalRemoval();
            }
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
