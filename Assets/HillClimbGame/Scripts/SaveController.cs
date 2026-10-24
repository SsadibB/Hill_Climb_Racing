using UnityEngine;

namespace HillClimbGame
{
    public class SaveController : MonoBehaviour
    {
        private const string KEY_BEST_DISTANCE = "HCG_BestDistance";
        private const string KEY_TOTAL_COINS   = "HCG_TotalCoins";

        public static SaveController Instance { get; private set; }

        public float BestDistance  { get; private set; }
        public int   TotalCoins    { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        public void Load()
        {
            BestDistance = PlayerPrefs.GetFloat(KEY_BEST_DISTANCE, 0f);
            TotalCoins   = PlayerPrefs.GetInt(KEY_TOTAL_COINS,     0);
        }

        public bool TryUpdateBestDistance(float distance)
        {
            if (distance > BestDistance)
            {
                BestDistance = distance;
                PlayerPrefs.SetFloat(KEY_BEST_DISTANCE, BestDistance);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public void AddCoins(int amount)
        {
            TotalCoins += amount;
            PlayerPrefs.SetInt(KEY_TOTAL_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        public void ResetAll()
        {
            BestDistance = 0f;
            TotalCoins   = 0;
            PlayerPrefs.DeleteKey(KEY_BEST_DISTANCE);
            PlayerPrefs.DeleteKey(KEY_TOTAL_COINS);
            PlayerPrefs.Save();
        }
    }
}
