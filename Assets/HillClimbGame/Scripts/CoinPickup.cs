using UnityEngine;
using DG.Tweening;

namespace HillClimbGame
{
    /// <summary>Pre-created coin pickup. Spins in place. Triggers once. Resets between runs.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class CoinPickup : MonoBehaviour
    {
        [SerializeField] private float spinSpeed = 180f;   // degrees/sec

        private bool           _collected;
        private CoinController _coinController;
        private Collider2D     _collider;
        private SpriteRenderer _renderer;

        public bool IsCollected => _collected;

        void Awake()
        {
            _coinController = Object.FindAnyObjectByType<CoinController>();
            _collider = GetComponent<Collider2D>();
            if (_collider != null) _collider.isTrigger = true;
            _renderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (!_collected)
                transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || !gameObject.activeSelf) return;

            bool isPlayer = other.CompareTag("Player") ||
                            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) ||
                            other.GetComponentInParent<VehicleController>() != null;

            if (isPlayer)
            {
                if (_coinController == null)
                    _coinController = Object.FindAnyObjectByType<CoinController>();
                _coinController?.CollectCoin(this);
            }
        }

        public void Collect()
        {
            _collected = true;
            AudioController.Instance?.PlayCoinPickup();
            gameObject.SetActive(false);
        }

        public void ResetCoin(Vector3 newPosition)
        {
            _collected = false;
            transform.position = newPosition;
            transform.rotation = Quaternion.identity;

            if (_collider == null) _collider = GetComponent<Collider2D>();
            if (_collider != null)
            {
                _collider.enabled = true;
                _collider.isTrigger = true;
            }

            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _renderer.enabled = true;

            gameObject.SetActive(true);
        }

        public void ResetCoin()
        {
            _collected = false;
            transform.rotation = Quaternion.identity;

            if (_collider == null) _collider = GetComponent<Collider2D>();
            if (_collider != null)
            {
                _collider.enabled = true;
                _collider.isTrigger = true;
            }

            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _renderer.enabled = true;

            gameObject.SetActive(true);
        }
    }
}
