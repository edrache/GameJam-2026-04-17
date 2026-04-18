using UnityEngine;
using UnityEngine.UI;

namespace TSF
{
    public class PortalSliderMiniGame : MonoBehaviour, IPortalMiniGame
    {
        [SerializeField] private Slider slider;
        [SerializeField] private float fillDuration = 2f;

        private ArmReachController _reach;
        private bool _active;
        private bool _completed;

        public void OnHandEnter(ArmReachController reach)
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
                _reach.TriggerLoot();
            }
        }
    }
}
