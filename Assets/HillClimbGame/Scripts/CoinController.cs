using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace HillClimbGame
{
    public class CoinController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private Text coinCountText;

        [Header("Coin Pickups (pre-created)")]
        [SerializeField] private List<CoinPickup> coins = new List<CoinPickup>();

        public int RunCoins { get; private set; }

        public event System.Action<int> OnCoinCollected;

        void Awake()
        {
            AutoWire();
            RunCoins = 0;
            UpdateHUD();
        }

        public void AutoWire()
        {
            var canvas = GameObject.Find("MainCanvas")?.transform;
            if (canvas != null && coinCountText == null)
            {
                var texts = canvas.GetComponentsInChildren<Text>(true);
                foreach (var txt in texts)
                {
                    if (txt.name == "CoinCounter") { coinCountText = txt; break; }
                }
            }

            var foundCoins = FindObjectsByType<CoinPickup>(FindObjectsInactive.Include);
            coins = new List<CoinPickup>(foundCoins);
        }

        public void RegisterCoin(CoinPickup coin)
        {
            if (coin != null && !coins.Contains(coin))
                coins.Add(coin);
        }

        public void CollectCoin(CoinPickup coin)
        {
            if (coin == null || coin.IsCollected) return;
            coin.Collect();
            RunCoins++;
            UpdateHUD();
            OnCoinCollected?.Invoke(RunCoins);

            // Save milestone every 5 coins
            if (RunCoins % 5 == 0)
                SaveController.Instance?.AddCoins(5);
        }

        void UpdateHUD()
        {
            if (coinCountText != null) coinCountText.text = RunCoins.ToString();
        }

        public void ResetCoins()
        {
            RunCoins = 0;
            UpdateHUD();
        }
    }
}
