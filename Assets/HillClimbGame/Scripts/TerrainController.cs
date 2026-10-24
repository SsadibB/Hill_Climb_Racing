using System.Collections.Generic;
using UnityEngine;

namespace HillClimbGame
{
    public enum DifficultyTier
    {
        Easy,       // 0 - 150m:  Gentle rolling hills
        Medium,     // 150 - 400m: Steep climbs, deep valleys
        Hard,       // 400 - 800m: Sharp dips, uneven ridges
        Expert      // 800m+:     Narrow peaks, camel humps, technical drops
    }

    /// <summary>
    /// Procedural hill-climb terrain with progressive difficulty.
    /// Manages a fixed pool of pre-created terrain segments and collectibles.
    /// Coins are spawned in large groups, curved arcs, and valley trails.
    /// Fuel cans have progressively increasing spawn intervals.
    /// </summary>
    public class TerrainController : MonoBehaviour
    {
        [Header("Segment References (pre-created in Editor)")]
        [SerializeField] private List<TerrainSegment> segments = new List<TerrainSegment>();

        [Header("Terrain Dimensions")]
        [SerializeField] private float segmentWidth     = 24f;
        [SerializeField] private int   pointsPerSegment = 40;
        [SerializeField] private float groundDepth      = 14f;
        [SerializeField] private float baseHeight       = 0f;

        [Header("Procedural Random Seeds")]
        [SerializeField] private float seedMacro        = 137.2f;
        [SerializeField] private float seedHills        = 541.8f;
        [SerializeField] private float seedDetail       = 892.4f;
        [SerializeField] private bool  randomizeOnStart = true;

        [Header("Progressive Difficulty Thresholds (Meters)")]
        [SerializeField] private float mediumDistance   = 150f;
        [SerializeField] private float hardDistance     = 400f;
        [SerializeField] private float expertDistance   = 800f;

        [Header("Terrain Amplitudes (Inspector-tunable)")]
        [SerializeField] private float easyAmplitude   = 2.5f;
        [SerializeField] private float mediumAmplitude = 5.5f;
        [SerializeField] private float hardAmplitude   = 8.0f;
        [SerializeField] private float expertAmplitude = 11.0f;

        [Header("Coin Settings")]
        [SerializeField] private List<CoinPickup> coins = new List<CoinPickup>();
        [SerializeField] private float coinHeightOffset = 0.8f;
        [SerializeField] private int   minGroupSize     = 3;
        [SerializeField] private int   maxGroupSize     = 6;
        [SerializeField] private float coinSpacing      = 1.2f;
        [SerializeField] private float groupInterval    = 10f;

        [Header("Fuel Can Settings")]
        [SerializeField] private List<FuelCanPickup> fuelCans = new List<FuelCanPickup>();
        [SerializeField] private float fuelCanHeightOffset   = 0.9f;
        [SerializeField] private float fuelCanStartInterval  = 60f;
        [SerializeField] private float fuelCanBaseInterval   = 80f;
        [SerializeField] private float fuelCanDifficultyMult = 1.4f;

        [Header("Parallax Layers")]
        [SerializeField] private Transform farMountains;
        [SerializeField] private Transform midMountains;
        [SerializeField] private float     farParallaxFactor = 0.12f;
        [SerializeField] private float     midParallaxFactor = 0.32f;

        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float cameraFollowSpeed = 6f;
        [SerializeField] private Vector3 cameraOffset    = new Vector3(2.5f, 2.2f, -10f);

        [Header("Vehicle")]
        [SerializeField] private Transform vehicleTransform;
        [SerializeField] private Vector3 vehicleStartPos = new Vector3(-3f, 0.7f, 0f);

        public DifficultyTier CurrentTier { get; private set; } = DifficultyTier.Easy;
        public float Distance => _distance;

        private float _worldX;
        private float _distance;
        private float _nextCoinGroupX;
        private float _nextFuelX;
        private int   _arcPatternIdx;
        private float _farMountainStartX;
        private float _midMountainStartX;

        void Awake()
        {
            if (randomizeOnStart)
                RandomizeSeeds();

            AutoWire();
            _worldX = vehicleStartPos.x - segmentWidth;

            if (farMountains != null) _farMountainStartX = farMountains.position.x;
            if (midMountains != null) _midMountainStartX = midMountains.position.x;
        }

        public void RandomizeSeeds()
        {
            seedMacro  = Random.Range(50f, 9999f);
            seedHills  = Random.Range(50f, 9999f);
            seedDetail = Random.Range(50f, 9999f);
        }

        public void AutoWire()
        {
            if (segments == null || segments.Count == 0)
            {
                var found = FindObjectsByType<TerrainSegment>(FindObjectsInactive.Exclude);
                System.Array.Sort(found, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
                segments = new List<TerrainSegment>(found);
            }

            if (cameraTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) cameraTransform = cam.transform;
            }

            if (vehicleTransform == null)
            {
                var vb = GameObject.Find("VehicleBody");
                if (vb != null) vehicleTransform = vb.transform;
            }

            if (farMountains == null)
            {
                var fm = GameObject.Find("FarMountains");
                if (fm != null) farMountains = fm.transform;
            }

            if (midMountains == null)
            {
                var mm = GameObject.Find("MiddleMountains");
                if (mm != null) midMountains = mm.transform;
            }

            // Always rescan — picks up any newly created pool objects
            var foundCoins = FindObjectsByType<CoinPickup>(FindObjectsInactive.Include);
            System.Array.Sort(foundCoins, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            coins = new List<CoinPickup>(foundCoins);

            var foundFuel = FindObjectsByType<FuelCanPickup>(FindObjectsInactive.Include);
            System.Array.Sort(foundFuel, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            fuelCans = new List<FuelCanPickup>(foundFuel);
        }

        void Start()
        {
            GenerateAllSegments();
            PositionInitialCollectibles();
        }

        void Update()
        {
            if (vehicleTransform == null) return;

            float vx = vehicleTransform.position.x;
            RecycleSegments(vx);

            float startX = vehicleStartPos.x;
            if (vx > startX)
                _distance = (vx - startX);

            CurrentTier = GetTierAt(_distance);
        }

        void LateUpdate()
        {
            if (vehicleTransform == null) return;

            float vx = vehicleTransform.position.x;

            if (cameraTransform != null)
            {
                float targetY = Mathf.Max(vehicleTransform.position.y + cameraOffset.y, 1.8f);
                Vector3 target = new Vector3(vx + cameraOffset.x, targetY, cameraOffset.z);
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, cameraFollowSpeed * Time.deltaTime);
            }

            if (farMountains != null)
                farMountains.position = new Vector3(_farMountainStartX + vx * farParallaxFactor, farMountains.position.y, farMountains.position.z);
            if (midMountains != null)
                midMountains.position = new Vector3(_midMountainStartX + vx * midParallaxFactor, midMountains.position.y, midMountains.position.z);
        }

        public DifficultyTier GetTierAt(float dist)
        {
            if (dist < mediumDistance) return DifficultyTier.Easy;
            if (dist < hardDistance)   return DifficultyTier.Medium;
            if (dist < expertDistance) return DifficultyTier.Hard;
            return DifficultyTier.Expert;
        }

        // -----------------------------------------------------------------
        // DRAMATIC HEIGHT FUNCTION
        // Each tier uses asymmetric sinusoids + Perlin for natural, varied hills.
        // Easy ~2.5m, Medium ~5.5m, Hard ~8m, Expert ~11m amplitude.
        // -----------------------------------------------------------------
        public float GetHeightAt(float wx)
        {
            float spawnFlatEnd  = vehicleStartPos.x + 3.5f;
            if (wx <= spawnFlatEnd)
                return baseHeight;

            float entranceBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((wx - spawnFlatEnd) / 10f));
            float dist          = wx - spawnFlatEnd;

            // Slow macro undulation (non-repetitive landscape feel)
            float macroNoise = (Mathf.PerlinNoise(wx * 0.006f + seedMacro * 0.001f, 0.5f) - 0.5f) * 3.5f;

            // Easy: gentle rolling hills ~16-22 deg
            float eH = Mathf.Sin(wx * 0.055f + seedHills)           * easyAmplitude * 0.70f
                     + Mathf.Sin(wx * 0.120f + seedDetail)           * easyAmplitude * 0.22f
                     + Mathf.Sin(wx * 0.260f + seedDetail * 1.6f)    * easyAmplitude * 0.08f;

            // Medium: steep asymmetric hills ~28-36 deg
            float mPhase = wx * 0.075f + seedHills * 0.88f;
            float mSin   = Mathf.Sin(mPhase);
            float mH = (mSin + 0.4f * Mathf.Sin(mPhase * 1.7f))    * mediumAmplitude * 0.58f
                     + Mathf.Sin(wx * 0.130f + seedDetail * 1.1f)    * mediumAmplitude * 0.28f
                     + Mathf.Cos(wx * 0.200f + seedDetail * 0.7f)    * mediumAmplitude * 0.14f;

            // Hard: sharp peaks, punchy valleys ~36-44 deg
            float hPhase = wx * 0.092f + seedHills * 1.22f;
            float hSin   = Mathf.Sin(hPhase);
            float hPow   = Mathf.Sign(hSin) * Mathf.Pow(Mathf.Abs(hSin), 0.65f);
            float hH = hPow                                          * hardAmplitude * 0.55f
                     + Mathf.Sin(wx * 0.160f + seedDetail * 1.4f)    * hardAmplitude * 0.25f
                     + Mathf.Cos(wx * 0.290f + seedDetail * 0.5f)    * hardAmplitude * 0.12f
                     + Mathf.Sin(wx * 0.048f + seedMacro  * 0.5f)    * hardAmplitude * 0.08f;

            // Expert: narrow peaks, camel humps, rapid reversals ~42-52 deg
            float xPhase = wx * 0.112f + seedHills * 1.55f;
            float xSin   = Mathf.Sin(xPhase);
            float xPow   = Mathf.Sign(xSin) * Mathf.Pow(Mathf.Abs(xSin), 1.35f);
            float xH = xPow                                          * expertAmplitude * 0.50f
                     + Mathf.Sin(wx * 0.220f + seedDetail * 2.2f)    * expertAmplitude * 0.22f
                     + Mathf.Cos(xPhase * 0.48f + 2.1f)              * expertAmplitude * 0.16f
                     + Mathf.Sin(wx * 0.380f + seedDetail * 1.8f)    * expertAmplitude * 0.07f
                     + Mathf.Sin(wx * 0.052f + seedMacro  * 0.8f)    * expertAmplitude * 0.05f;

            float h;
            if (dist < mediumDistance)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - (mediumDistance - 40f)) / 40f));
                h = Mathf.Lerp(eH, mH, t);
            }
            else if (dist < hardDistance)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - (hardDistance - 50f)) / 50f));
                h = Mathf.Lerp(mH, hH, t);
            }
            else if (dist < expertDistance)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - (expertDistance - 60f)) / 60f));
                h = Mathf.Lerp(hH, xH, t);
            }
            else
            {
                h = xH;
            }

            return baseHeight + (h + macroNoise) * entranceBlend;
        }

        // -----------------------------------------------------------------
        // SEGMENT MESH BUILD
        // -----------------------------------------------------------------
        void GenerateAllSegments()
        {
            _worldX = vehicleStartPos.x - segmentWidth;
            foreach (var seg in segments)
            {
                BuildSegment(seg, _worldX);
                _worldX += segmentWidth;
            }
        }

        void BuildSegment(TerrainSegment seg, float startX)
        {
            if (seg == null) return;
            int pts    = pointsPerSegment;
            float step = segmentWidth / (pts - 1);

            float[] heights = new float[pts];
            for (int i = 0; i < pts; i++)
                heights[i] = GetHeightAt(startX + i * step);

            var verts = new Vector3[pts * 2];
            var uvs   = new Vector2[pts * 2];
            for (int i = 0; i < pts; i++)
            {
                float lx = i * step;
                float wx = startX + lx;
                verts[i]       = new Vector3(lx, heights[i], 0f);
                verts[i + pts] = new Vector3(lx, heights[i] - groundDepth, 0f);
                uvs[i]         = new Vector2(wx * 0.06f, 1f);
                uvs[i + pts]   = new Vector2(wx * 0.06f, 0f);
            }

            var tris = new int[(pts - 1) * 6];
            int ti = 0;
            for (int i = 0; i < pts - 1; i++)
            {
                tris[ti++] = i;
                tris[ti++] = i + pts;
                tris[ti++] = i + 1;
                tris[ti++] = i + 1;
                tris[ti++] = i + pts;
                tris[ti++] = i + pts + 1;
            }

            seg.SetMesh(verts, tris, uvs);

            var path = new Vector2[pts + 2];
            for (int i = 0; i < pts; i++)
                path[i] = new Vector2(verts[i].x, verts[i].y);
            path[pts]     = new Vector2(verts[pts - 1].x, verts[pts - 1].y - groundDepth);
            path[pts + 1] = new Vector2(verts[0].x,       verts[0].y       - groundDepth);
            seg.SetCollider(path);

            seg.transform.position = new Vector3(startX, 0f, 0f);
        }

        // -----------------------------------------------------------------
        // RECYCLING
        // -----------------------------------------------------------------
        void RecycleSegments(float vehicleX)
        {
            float cullX = vehicleX - segmentWidth * 1.5f;

            foreach (var seg in segments)
            {
                if (seg.transform.position.x + segmentWidth < cullX)
                {
                    BuildSegment(seg, _worldX);
                    RecycleCollectiblesIntoSegment(_worldX);
                    _worldX += segmentWidth;
                }
            }
        }

        // -----------------------------------------------------------------
        // COLLECTIBLE PLACEMENT
        // Coins: large groups in 5 arc patterns.
        // Fuel: progressively rarer per difficulty tier.
        // -----------------------------------------------------------------
        // -----------------------------------------------------------------
        // OBJECT POOLING & COLLECTIBLE PLACEMENT
        // Coins: pooled, active ones preserved, normal-aligned and reachable
        // Fuel: pooled, placed along reachable path per difficulty tier
        // -----------------------------------------------------------------
        CoinPickup GetAvailableCoin()
        {
            // 1. Look for inactive / collected coin
            for (int i = 0; i < coins.Count; i++)
            {
                var c = coins[i];
                if (c != null && (!c.gameObject.activeSelf || c.IsCollected))
                    return c;
            }

            // 2. Look for coin far behind the vehicle (passed and missed by player)
            float cullBehindX = vehicleTransform != null ? vehicleTransform.position.x - 22f : -9999f;
            for (int i = 0; i < coins.Count; i++)
            {
                var c = coins[i];
                if (c != null && c.transform.position.x < cullBehindX)
                    return c;
            }

            // 3. Dynamically grow pool if needed (instantiate from prototype)
            if (coins.Count > 0 && coins[0] != null)
            {
                var newCoin = Instantiate(coins[0], coins[0].transform.parent);
                newCoin.name = $"Coin_{coins.Count + 1:D2}";
                coins.Add(newCoin);
                var cc = Object.FindAnyObjectByType<CoinController>();
                cc?.RegisterCoin(newCoin);
                return newCoin;
            }

            return null;
        }

        FuelCanPickup GetAvailableFuelCan()
        {
            // 1. Look for inactive fuel can
            for (int i = 0; i < fuelCans.Count; i++)
            {
                var fc = fuelCans[i];
                if (fc != null && (!fc.gameObject.activeSelf || fc.IsCollected))
                    return fc;
            }

            // 2. Look for fuel can behind the vehicle
            float cullBehindX = vehicleTransform != null ? vehicleTransform.position.x - 30f : -9999f;
            for (int i = 0; i < fuelCans.Count; i++)
            {
                var fc = fuelCans[i];
                if (fc != null && fc.transform.position.x < cullBehindX)
                    return fc;
            }

            // 3. Dynamically grow pool if needed
            if (fuelCans.Count > 0 && fuelCans[0] != null)
            {
                var newFc = Instantiate(fuelCans[0], fuelCans[0].transform.parent);
                newFc.name = $"FuelCan_{fuelCans.Count + 1:D2}";
                fuelCans.Add(newFc);
                return newFc;
            }

            return null;
        }

        void PositionInitialCollectibles()
        {
            // Deactivate existing pickups to ensure fresh initial state
            foreach (var c in coins)
            {
                if (c != null) c.gameObject.SetActive(false);
            }
            foreach (var fc in fuelCans)
            {
                if (fc != null) fc.gameObject.SetActive(false);
            }

            _nextCoinGroupX = vehicleStartPos.x + 12f;
            _nextFuelX      = vehicleStartPos.x + fuelCanStartInterval;
            _arcPatternIdx  = 0;

            // Fill all initial segments with coin groups
            while (_nextCoinGroupX < _worldX)
            {
                PlaceCoinGroup(_nextCoinGroupX);
                _nextCoinGroupX += groupInterval;
            }

            // Place fuel cans across initial segments
            while (_nextFuelX < _worldX)
            {
                PlaceFuelCan(_nextFuelX);
                _nextFuelX += FuelCanInterval();
            }
        }

        void RecycleCollectiblesIntoSegment(float segStartX)
        {
            float segEndX = segStartX + segmentWidth;

            // Keep placing coin groups into the new segment until it is filled
            while (_nextCoinGroupX < segEndX)
            {
                PlaceCoinGroup(_nextCoinGroupX);
                _nextCoinGroupX += groupInterval;
            }

            // Place fuel can if it falls inside this segment
            if (_nextFuelX < segEndX)
            {
                PlaceFuelCan(_nextFuelX);
                _nextFuelX += FuelCanInterval();
            }
        }

        float FuelCanInterval()
        {
            int tierIdx = (int)GetTierAt(_distance);
            return fuelCanBaseInterval * Mathf.Pow(fuelCanDifficultyMult, tierIdx);
        }

        void PlaceCoinGroup(float centerX)
        {
            int count   = Random.Range(minGroupSize, maxGroupSize + 1);
            int pattern = _arcPatternIdx % 4;
            _arcPatternIdx++;

            for (int i = 0; i < count; i++)
            {
                var coin = GetAvailableCoin();
                if (coin == null) continue;

                Vector3 pos = GetCoinPositionInGroup(centerX, i, count, pattern);
                pos = ValidateCoinPosition(pos);
                coin.ResetCoin(pos);
            }
        }

        Vector3 GetCoinPositionInGroup(float centerX, int idx, int total, int pattern)
        {
            float half    = (total - 1) * 0.5f;
            float offsetX = (idx - half) * coinSpacing;
            float x       = centerX + offsetX;

            float terrainY = GetHeightAt(x);

            // Compute local surface normal
            float sampleStep = 0.35f;
            float dy = GetHeightAt(x + sampleStep) - GetHeightAt(x - sampleStep);
            Vector2 tangent = new Vector2(2f * sampleStep, dy).normalized;
            Vector2 normal  = new Vector2(-tangent.y, tangent.x);
            if (normal.y < 0f) normal = -normal;

            float baseOffset = coinHeightOffset;
            float extraOffset = 0f;

            switch (pattern)
            {
                case 1:
                    // Gentle crest arc: peaks slightly higher in center
                    float tArc = (float)idx / Mathf.Max(1, total - 1);
                    extraOffset = 0.55f * Mathf.Sin(tArc * Mathf.PI);
                    break;
                case 2:
                    // Valley follow trail: stays snug to terrain
                    extraOffset = 0f;
                    break;
                case 3:
                    // Smooth slope ramp: gentle gradient
                    float tRamp = (float)idx / Mathf.Max(1, total - 1);
                    extraOffset = 0.35f * tRamp;
                    break;
                default:
                    // Flat parallel trail
                    extraOffset = 0f;
                    break;
            }

            Vector2 groundPos = new Vector2(x, terrainY);
            Vector2 coinPos2D = groundPos + normal * (baseOffset + extraOffset);

            return new Vector3(coinPos2D.x, coinPos2D.y, 0f);
        }

        Vector3 ValidateCoinPosition(Vector3 pos)
        {
            float terrainY = GetHeightAt(pos.x);
            float minClearance   = 0.65f; // Ensures coin stays clearly above grass (never inside ground)
            float maxReachHeight = 1.65f; // Ensures coin is realistically reachable by normal driving or modest crest hop

            float heightAboveGround = pos.y - terrainY;
            if (heightAboveGround < minClearance)
            {
                pos.y = terrainY + minClearance;
            }
            else if (heightAboveGround > maxReachHeight)
            {
                pos.y = terrainY + maxReachHeight;
            }

            return pos;
        }

        void PlaceFuelCan(float x)
        {
            var fc = GetAvailableFuelCan();
            if (fc == null) return;

            float sampleStep = 0.35f;
            float dy = GetHeightAt(x + sampleStep) - GetHeightAt(x - sampleStep);
            Vector2 tangent = new Vector2(2f * sampleStep, dy).normalized;
            Vector2 normal  = new Vector2(-tangent.y, tangent.x);
            if (normal.y < 0f) normal = -normal;

            float terrainY = GetHeightAt(x);
            Vector2 pos2D  = new Vector2(x, terrainY) + normal * fuelCanHeightOffset;
            Vector3 finalPos = new Vector3(pos2D.x, pos2D.y, 0f);

            // Validate fuel can position
            float minH = 0.70f;
            float maxH = 1.50f;
            float currentH = finalPos.y - terrainY;
            if (currentH < minH) finalPos.y = terrainY + minH;
            else if (currentH > maxH) finalPos.y = terrainY + maxH;

            fc.ResetPickup(finalPos);
        }

        // -----------------------------------------------------------------
        // RESET
        // -----------------------------------------------------------------
        public void ResetTerrain()
        {
            _distance       = 0f;
            _arcPatternIdx  = 0;
            _worldX         = vehicleStartPos.x - segmentWidth;
            CurrentTier     = DifficultyTier.Easy;

            if (randomizeOnStart)
                RandomizeSeeds();

            GenerateAllSegments();
            PositionInitialCollectibles();
        }
    }
}
