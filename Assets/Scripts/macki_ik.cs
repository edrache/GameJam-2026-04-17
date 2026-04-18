using UnityEngine;

public class TentacleBossController : MonoBehaviour
{
    public Transform player;
    public Transform[] targets;

    [Header("Idle")]
    public float radius = 2f;
    public float orbitSpeed = 2f;
    public float idleHeight = 1f;

    [Header("Attack")]
    public float attackInterval = 2.5f;
    public float attackStrength = 3f;
    public float attackSpeed = 12f;
    public float telegraphTime = 0.25f;

    private float timer;
    private int attackingIndex = -1;
    private float attackT;
    private bool attacking;

    void Update()
    {
        timer += Time.deltaTime;

        if (!attacking && timer >= attackInterval)
        {
            timer = 0f;
            attackingIndex = GetClosest();
            attacking = true;
            attackT = 0f;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 idle = GetIdlePos(i);

            if (attacking && i == attackingIndex)
            {
                UpdateAttack(i, idle);
            }
            else
            {
                // 🧠 miękki powrót do orbit
                targets[i].position = Vector3.Lerp(
                    targets[i].position,
                    idle,
                    Time.deltaTime * 5f
                );
            }
        }
    }

    Vector3 GetIdlePos(int i)
    {
        float angle = (float)i / targets.Length * Mathf.PI * 2f;
        angle += Time.time * orbitSpeed;

        return player.position + new Vector3(
            Mathf.Cos(angle) * radius,
            idleHeight + Mathf.Sin(Time.time * 2f + i) * 0.3f,
            Mathf.Sin(angle) * radius
        );
    }

    void UpdateAttack(int i, Vector3 idle)
    {
        attackT += Time.deltaTime;

        Vector3 dir = (player.position - idle).normalized;
        Vector3 overshoot = player.position + dir * attackStrength;

        // 🧠 TELEGRAPH (cofnięcie przed uderzeniem)
        Vector3 preAttack = Vector3.Lerp(idle, player.position - dir * 1.2f, 0.5f);

        Vector3 targetPos;

        if (attackT < telegraphTime)
        {
            // 🟡 cofnięcie (ostrzeżenie)
            targetPos = Vector3.Lerp(idle, preAttack, attackT / telegraphTime);
        }
        else
        {
            // 🔥 szybki whip attack
            float t = (attackT - telegraphTime) * attackSpeed;
            targetPos = Vector3.Lerp(preAttack, overshoot, t);
        }

        targets[i].position = targetPos;

        if (attackT > 1f)
        {
            attacking = false;
            attackingIndex = -1;
        }
    }

    int GetClosest()
    {
        int c = 0;
        float d = Mathf.Infinity;

        for (int i = 0; i < targets.Length; i++)
        {
            float dist = Vector3.Distance(targets[i].position, player.position);
            if (dist < d)
            {
                d = dist;
                c = i;
            }
        }
        return c;
    }
}