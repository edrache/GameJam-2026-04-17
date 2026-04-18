using UnityEngine;

namespace TSF
{
    public class LootScoreValue : MonoBehaviour
    {
        [SerializeField, Min(0)] private int points = 10;

        public int Points => points;
    }
}
