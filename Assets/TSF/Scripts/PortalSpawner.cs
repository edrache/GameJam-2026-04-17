using System.Collections.Generic;
using UnityEngine;

namespace TSF
{
    public class PortalSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] portalPrefabs;
        [SerializeField] private BoxCollider[] spawnZones;
        [SerializeField] private int portalCount = 5;
        [SerializeField] private float overlapCheckRadius = 0.6f;
        [SerializeField] private LayerMask overlapCheckMask = Physics.DefaultRaycastLayers;
        [SerializeField] private int maxAttemptsPerPortal = 30;

        private readonly List<GameObject> _spawnedPortals = new();
        private readonly HashSet<Collider> _zoneColliders = new();

        void Start()
        {
            foreach (var zone in spawnZones)
                if (zone != null) _zoneColliders.Add(zone);

            SpawnAll();
        }

        public void SpawnAll()
        {
            DespawnAll();

            if (portalPrefabs == null || portalPrefabs.Length == 0) return;
            if (spawnZones == null || spawnZones.Length == 0) return;

            for (int i = 0; i < portalCount; i++)
            {
                TrySpawnOne();
            }
        }

        public void DespawnAll()
        {
            foreach (var portal in _spawnedPortals)
            {
                if (portal != null) Destroy(portal);
            }
            _spawnedPortals.Clear();
        }

        private void TrySpawnOne()
        {
            // Weight zones by volume so larger zones get proportionally more portals
            float totalVolume = 0f;
            foreach (var zone in spawnZones)
                totalVolume += ZoneVolume(zone);

            for (int attempt = 0; attempt < maxAttemptsPerPortal; attempt++)
            {
                Vector3 candidate = RandomPointInZones(totalVolume);

                if (IsFreeSpot(candidate))
                {
                    GameObject prefab = portalPrefabs[Random.Range(0, portalPrefabs.Length)];
                    GameObject portal = Instantiate(prefab, candidate, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    _spawnedPortals.Add(portal);
                    return;
                }
            }

            Debug.LogWarning($"[PortalSpawner] Could not find free spot after {maxAttemptsPerPortal} attempts.");
        }

        private bool IsFreeSpot(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, overlapCheckRadius,
                overlapCheckMask, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (!_zoneColliders.Contains(hit))
                    return false;
            }
            return true;
        }

        private Vector3 RandomPointInZones(float totalVolume)
        {
            float pick = Random.Range(0f, totalVolume);
            float cumulative = 0f;

            foreach (var zone in spawnZones)
            {
                cumulative += ZoneVolume(zone);
                if (pick <= cumulative)
                    return RandomPointInBox(zone);
            }

            return RandomPointInBox(spawnZones[spawnZones.Length - 1]);
        }

        private static Vector3 RandomPointInBox(BoxCollider box)
        {
            Vector3 localPoint = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            );

            return box.transform.TransformPoint(
                Vector3.Scale(localPoint, box.size) + box.center
            );
        }

        private static float ZoneVolume(BoxCollider box)
        {
            Vector3 s = box.size;
            Vector3 ls = box.transform.lossyScale;
            return Mathf.Abs(s.x * ls.x) * Mathf.Abs(s.y * ls.y) * Mathf.Abs(s.z * ls.z);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (spawnZones == null) return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            foreach (var zone in spawnZones)
            {
                if (zone == null) continue;
                Matrix4x4 prev = Gizmos.matrix;
                Gizmos.matrix = zone.transform.localToWorldMatrix;
                Gizmos.DrawCube(zone.center, zone.size);
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
                Gizmos.DrawWireCube(zone.center, zone.size);
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
                Gizmos.matrix = prev;
            }
        }
#endif
    }
}
