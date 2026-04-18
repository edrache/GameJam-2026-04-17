using TSF;
using UnityEngine;
using UnityEngine.UI;

public class PortalProximitySlider : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] Transform player;
    [SerializeField] float minDistance = 1f;
    [SerializeField] float maxDistance = 10f;

    PortalAnimator[] _portals;

    void Start()
    {
        _portals = FindObjectsByType<PortalAnimator>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (slider == null)
            return;

        float nearest = NearestPortalDistance();
        slider.value = Mathf.InverseLerp(minDistance, maxDistance, nearest);
    }

    float NearestPortalDistance()
    {
        if (_portals == null || _portals.Length == 0)
            RefreshPortals();

        float min = float.MaxValue;
        bool shouldRefreshPortals = false;
        Vector3 pos = player != null ? player.position : transform.position;
        foreach (var portal in _portals)
        {
            if (portal == null)
            {
                shouldRefreshPortals = true;
                continue;
            }

            float d = Vector3.Distance(pos, portal.transform.position);
            if (d < min) min = d;
        }

        if (shouldRefreshPortals)
            RefreshPortals();

        return min == float.MaxValue ? maxDistance : min;
    }

    void RefreshPortals()
    {
        _portals = FindObjectsByType<PortalAnimator>(FindObjectsSortMode.None);
    }
}
