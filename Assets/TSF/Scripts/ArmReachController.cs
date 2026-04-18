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

        private Player _player;
        private bool _initialized;
        private bool _handInPortal;
        private IPortalMiniGame _activeMiniGame;

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

        private bool IsPortalInReach()
        {
            if (_activeMiniGame == null)
                return FindPortalInReach() != null;
            var portal = (_activeMiniGame as MonoBehaviour)?.transform;
            return portal != null && Vector3.Distance(transform.position, portal.position) <= reachDistance;
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
            _activeMiniGame = portal.GetComponent<IPortalMiniGame>();
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

        public void TriggerLoot(GameObject lootPrefab)
        {
            armAnimator.SetTrigger(LootId);
            if (lootPrefab != null && lootPoint != null)
                StartCoroutine(SpawnAndDestroyLoot(lootPrefab));
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
    }
}
