using UnityEngine;

namespace TSF
{
    public class PortalSideMiniGameRouter : MonoBehaviour, IPortalMiniGame
    {
        [SerializeField] private MonoBehaviour frontMiniGame;
        [SerializeField] private MonoBehaviour backMiniGame;
        [SerializeField] private bool fallbackToOtherSide = true;

        private IPortalMiniGame _activeMiniGame;

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
            _activeMiniGame = GetMiniGame(side);
            _activeMiniGame?.OnHandEnter(reach, side);
        }

        public void OnHandExit()
        {
            _activeMiniGame?.OnHandExit();
            _activeMiniGame = null;
        }

        public bool IsActiveMiniGame(IPortalMiniGame miniGame)
        {
            return miniGame != null && (object.ReferenceEquals(_activeMiniGame, miniGame) || object.ReferenceEquals(miniGame, this));
        }

        public void ClearActiveMiniGame(IPortalMiniGame miniGame)
        {
            if (IsActiveMiniGame(miniGame))
                _activeMiniGame = null;
        }

        private IPortalMiniGame GetMiniGame(PortalSide side)
        {
            IPortalMiniGame primary = GetMiniGameReference(side == PortalSide.Front ? frontMiniGame : backMiniGame);
            if (primary != null || !fallbackToOtherSide)
                return primary;

            return GetMiniGameReference(side == PortalSide.Front ? backMiniGame : frontMiniGame);
        }

        private IPortalMiniGame GetMiniGameReference(MonoBehaviour miniGame)
        {
            if (miniGame == null || miniGame == this)
                return null;

            return miniGame as IPortalMiniGame;
        }

        private void OnValidate()
        {
            ValidateMiniGameReference(frontMiniGame, nameof(frontMiniGame));
            ValidateMiniGameReference(backMiniGame, nameof(backMiniGame));
        }

        private void ValidateMiniGameReference(MonoBehaviour miniGame, string fieldName)
        {
            if (miniGame == null || miniGame is IPortalMiniGame)
                return;

            Debug.LogWarning($"[{nameof(PortalSideMiniGameRouter)}] {fieldName} must implement {nameof(IPortalMiniGame)}.", this);
        }
    }
}
