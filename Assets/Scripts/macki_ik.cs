using UnityEngine;

public class TentacleTargetsController : MonoBehaviour
{
    public Transform player;
    public Transform[] targets;

    public float radius = 2f;
    public float height = 1f;
    public float speed = 2f;

    void Update()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / targets.Length;
            angle += Time.time * speed;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                height + Mathf.Sin(Time.time * 2f + i), // lekkie falowanie
                Mathf.Sin(angle) * radius
            );

            targets[i].position = player.position + offset;
        }
    }
}