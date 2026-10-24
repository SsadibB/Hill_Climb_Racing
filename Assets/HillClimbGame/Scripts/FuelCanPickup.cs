using UnityEngine;

namespace HillClimbGame
{
    /// <summary>Pre-created fuel can pickup. Detected by trigger collision with the vehicle.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class FuelCanPickup : MonoBehaviour
    {
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed     = 2f;

        private Vector3        _basePos;
        private bool           _hasBasePos;
        private FuelController _fuelController;

        void Awake()
        {
            _fuelController = Object.FindAnyObjectByType<FuelController>();
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            _basePos = transform.position;
            _hasBasePos = true;
        }

        public void SetBasePosition(Vector3 pos)
        {
            _basePos = pos;
            _hasBasePos = true;
            transform.position = pos;
        }

        void Update()
        {
            if (!_hasBasePos) _basePos = transform.position;
            float y = _basePos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = new Vector3(_basePos.x, y, _basePos.z);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!gameObject.activeSelf) return;

            bool isPlayer = other.CompareTag("Player") ||
                            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) ||
                            other.GetComponentInParent<VehicleController>() != null;

            if (isPlayer)
            {
                _fuelController?.CollectFuelCan(this);
            }
        }

        public bool IsCollected => !gameObject.activeSelf;

        public void ResetPickup(Vector3 newPos)
        {
            SetBasePosition(newPos);
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
            gameObject.SetActive(true);
        }

        public void ResetPickup()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
            gameObject.SetActive(true);
            if (_hasBasePos)
                transform.position = _basePos;
        }
    }
}
