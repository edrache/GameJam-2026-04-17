using UnityEngine;
using Rewired;

namespace TSF
{
    public class ArmReachController : MonoBehaviour
    {
        private static readonly int IsReachingId = Animator.StringToHash("IsReaching");
        private static readonly int LootId = Animator.StringToHash("Loot");
        private const string ArmPortalStateName = "Arm portal";

        [SerializeField] private Animator armAnimator;
        [SerializeField] private PortalAnimator currentPortal;

        private Player _player;
        private bool _initialized;
        private bool _handInPortal;
        private IPortalMiniGame _activeMiniGame;

        void Update()
        {
            if (armAnimator == null) return;
            if (!ReInput.isReady) return;
            if (!_initialized) Initialize();

            armAnimator.SetBool(IsReachingId, _player.GetButton("Reach"));

            bool inPortal = armAnimator.GetCurrentAnimatorStateInfo(0).IsName(ArmPortalStateName);
            if (inPortal && !_handInPortal)
                OnHandEnterPortal();
            else if (!inPortal && _handInPortal)
                OnHandExitPortal();
        }

        void OnDisable()
        {
            if (_handInPortal)
                OnHandExitPortal();
        }

        private void Initialize()
        {
            _player = ReInput.players.GetPlayer(0);
            _initialized = true;
        }

        private void OnHandEnterPortal()
        {
            _handInPortal = true;
            if (currentPortal == null) return;
            _activeMiniGame = currentPortal.GetComponent<IPortalMiniGame>();
            _activeMiniGame?.OnHandEnter(this);
        }

        private void OnHandExitPortal()
        {
            _handInPortal = false;
            _activeMiniGame?.OnHandExit();
            _activeMiniGame = null;
        }

        public void TriggerLoot()
        {
            armAnimator.SetTrigger(LootId);
        }
    }
}
