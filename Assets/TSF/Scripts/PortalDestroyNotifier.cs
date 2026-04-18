using UnityEngine;

namespace TSF
{
    public class PortalDestroyNotifier : MonoBehaviour
    {
        private PortalSpawner _spawner;

        public void Init(PortalSpawner spawner)
        {
            _spawner = spawner;
        }

        void OnDestroy()
        {
            if (_spawner != null)
                _spawner.OnPortalDestroyed(gameObject);
        }
    }
}
