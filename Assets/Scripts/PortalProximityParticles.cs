using UnityEngine;

public class PortalProximityParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (particles != null) particles.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (particles != null) particles.Stop();
    }
}
