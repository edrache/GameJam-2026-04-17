using System.Collections;
using UnityEngine;
using Rewired;

namespace TSF
{
    public class ArmReachController : MonoBehaviour
    {
        private static readonly int IsReachingId = Animator.StringToHash("IsReaching");
        private static readonly int LootId = Animator.StringToHash("Loot");
        private const string ArmPortalStateName = "Arm portal";
        private const string ArmLootStateName = "Arm loot";

        [SerializeField] private Animator armAnimator;
        [SerializeField] private float reachDistance = 2f;
        [SerializeField] private Transform lootPoint;
        [SerializeField] private string idleStateName = "Idle";

        private Player _player;
        private bool _initialized;
        private bool _handInPortal;
        private bool _suppressPortalEnterUntilExit;
        private IPortalMiniGame _activeMiniGame;
        private PortalAnimator _activePortal;

        void Update()
        {
            if (armAnimator == null) return;
            if (!ReInput.isReady) return;
            if (!_initialized) Initialize();

            bool reaching = _player.GetButton("Reach");

            if (_handInPortal && !IsPortalInReach())
                reaching = false;

            armAnimator.SetBool(IsReachingId, reaching);

            bool inPortal = armAnimator.GetCurrentAnimatorStateInfo(0).IsName(ArmPortalStateName);
            if (inPortal && !_handInPortal && !_suppressPortalEnterUntilExit)
                OnHandEnterPortal();
            else if (!inPortal)
            {
                _suppressPortalEnterUntilExit = false;
                if (_handInPortal)
                    OnHandExitPortal();
            }
        }

        void LateUpdate()
        {
            if (armAnimator == null)
                return;

            if (armAnimator.GetCurrentAnimatorStateInfo(0).IsName(ArmPortalStateName))
                armAnimator.SetBool(IsReachingId, true);
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

        private bool IsPortalInReach()
        {
            if (_activeMiniGame == null)
                return FindPortalInReach() != null;

            Transform portalTransform = _activePortal != null ? _activePortal.transform : (_activeMiniGame as MonoBehaviour)?.transform;
            return portalTransform != null && Vector3.Distance(transform.position, portalTransform.position) <= reachDistance;
        }

        private PortalAnimator FindPortalInReach()
        {
            PortalAnimator[] portals = FindObjectsByType<PortalAnimator>(FindObjectsSortMode.None);
            PortalAnimator nearest = null;
            float nearestDist = reachDistance;
            foreach (PortalAnimator portal in portals)
            {
                float dist = Vector3.Distance(transform.position, portal.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = portal;
                }
            }
            return nearest;
        }

        private void OnHandEnterPortal()
        {
            _handInPortal = true;
            PortalAnimator portal = FindPortalInReach();
            if (portal == null) return;

            PortalSide side = GetPortalSide(portal);
            _activePortal = portal;
            _activeMiniGame = GetPortalMiniGame(portal);
            _activeMiniGame?.OnHandEnter(this, side);
        }

        private void OnHandExitPortal()
        {
            _handInPortal = false;
            _activeMiniGame?.OnHandExit();
            _activeMiniGame = null;
            _activePortal = null;
        }

        public void TriggerLoot()
        {
            armAnimator.SetTrigger(LootId);
        }

        public void TriggerLoot(GameObject lootPrefab)
        {
            armAnimator.SetTrigger(LootId);
            if (lootPrefab != null && lootPoint != null)
                StartCoroutine(SpawnAndDestroyLoot(lootPrefab));
        }

        public void ReleaseMiniGame(IPortalMiniGame miniGame)
        {
            PortalSideMiniGameRouter router = _activeMiniGame as PortalSideMiniGameRouter;
            if (_activeMiniGame != miniGame && (router == null || !router.IsActiveMiniGame(miniGame)))
                return;

            router?.ClearActiveMiniGame(miniGame);
            _handInPortal = false;
            _suppressPortalEnterUntilExit = true;
            _activeMiniGame = null;
            _activePortal = null;
        }

        public void RemovePortalWhenIdle(IPortalMiniGame miniGame, GameObject portal, float duration)
        {
            ReleaseMiniGame(miniGame);
            if (portal == null)
                return;

            foreach (Collider portalCollider in portal.GetComponents<Collider>())
                portalCollider.enabled = false;

            StartCoroutine(RemovePortalWhenIdle(portal, duration));
        }

        private IEnumerator SpawnAndDestroyLoot(GameObject lootPrefab)
        {
            yield return new WaitUntil(() => armAnimator.GetCurrentAnimatorStateInfo(0).IsName(ArmLootStateName));
            GameObject instance = Instantiate(lootPrefab, lootPoint);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            yield return new WaitUntil(() => !armAnimator.GetCurrentAnimatorStateInfo(0).IsName(ArmLootStateName));
            Destroy(instance);
        }

        private IEnumerator RemovePortalWhenIdle(GameObject portal, float duration)
        {
            yield return new WaitUntil(() => portal == null || IsAnimatorInIdleState());
            if (portal == null)
                yield break;

            PortalAnimator portalAnimator = portal.GetComponent<PortalAnimator>();
            if (portalAnimator != null)
                portalAnimator.Close(duration);

            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            if (portal != null)
                Destroy(portal);
        }

        private bool IsAnimatorInIdleState()
        {
            if (armAnimator == null)
                return true;

            AnimatorStateInfo stateInfo = armAnimator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(idleStateName);
        }

        private IPortalMiniGame GetPortalMiniGame(PortalAnimator portal)
        {
            PortalSideMiniGameRouter router = portal.GetComponent<PortalSideMiniGameRouter>();
            if (router != null)
                return router;

            return portal.GetComponent<IPortalMiniGame>();
        }

        private PortalSide GetPortalSide(PortalAnimator portal)
        {
            Vector3 portalToReach = transform.position - portal.transform.position;
            float facingDot = Vector3.Dot(portal.transform.forward, portalToReach);
            return facingDot >= 0f ? PortalSide.Front : PortalSide.Back;
        }
    }
}
