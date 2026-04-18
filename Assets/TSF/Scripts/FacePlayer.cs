using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private bool lockXAxis;
    [SerializeField] private bool lockYAxis;
    [SerializeField] private bool lockZAxis;

    private void Start()
    {
        if (player == null)
        {
            var cam = Camera.main;
            if (cam != null) player = cam.transform;
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        var direction = player.position - transform.position;

        if (lockXAxis) direction.x = 0f;
        if (lockYAxis) direction.y = 0f;
        if (lockZAxis) direction.z = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        var targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
        transform.rotation = targetRotation;
    }
}
