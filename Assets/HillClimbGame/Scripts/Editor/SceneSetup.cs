using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HillClimbGame
{
    public static class SceneSetup
    {
        private static GameObject GetOrCreate(string name, Transform parent = null)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
            }
            if (parent != null && go.transform.parent != parent)
            {
                go.transform.SetParent(parent, true);
            }
            return go;
        }

        [MenuItem("HillClimbGame/9 - Run All Setup Steps")]
        public static void RunAll()
        {
            // 1. Generate Sprites and Audio assets
            AssetGenerator.GenerateAllAssets();

            // 2. Setup Hierarchy & Roots
            var envRoot     = GetOrCreate("Environment").transform;
            var vehicleRoot = GetOrCreate("Vehicle").transform;
            var worldRoot   = GetOrCreate("WorldObjects").transform;
            var weatherRoot = GetOrCreate("WeatherSystem").transform;
            var audioRoot   = GetOrCreate("AudioSystem").transform;
            var systemsRoot = GetOrCreate("GameSystems").transform;
            var uiRoot      = GetOrCreate("UI").transform;

            // 3. Setup Camera
            SetupCamera();

            // 4. Setup Lighting
            SetupLight2D();

            // 5. Setup Audio Sources
            SetupAudioSources(audioRoot);

            // 6. Setup Visual Environment (Parallax mountains, clouds, sun/moon, sky)
            SetupEnvironmentVisuals(envRoot);

            // 7. Setup Terrain Segments
            SetupTerrainSegments(envRoot);

            // 8. Setup Vehicle & Physics
            SetupVehicle(vehicleRoot);

            // 9. Setup Pickups (Coins & Fuel Cans)
            SetupPickups(worldRoot);

            // 10. Setup Weather Systems & Particles
            SetupWeatherParticles(weatherRoot);

            // 11. Build UI Canvas & EventSystem
            UIBuilder.BuildUI();

            // 12. Attach & Wire All Controller Scripts
            AttachAndWireControllers(systemsRoot);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[HillClimbGame] ===============================================");
            Debug.Log("[HillClimbGame] === ALL SETUP STEPS COMPLETED SUCCESSFULLY! ===");
            Debug.Log("[HillClimbGame] ===============================================");
        }

        private static void SetupCamera()
        {
            var camGo = GetOrCreate("Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            if (cam == null) cam = camGo.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 6.5f;
            cam.backgroundColor  = new Color(0.35f, 0.65f, 0.95f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.nearClipPlane    = 0.1f;
            cam.farClipPlane     = 100f;
            camGo.transform.position = new Vector3(-2f, 1.8f, -10f);

            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();

            EditorUtility.SetDirty(camGo);
        }

        private static void SetupLight2D()
        {
            var lightGo = GetOrCreate("GlobalLight2D");
            var light2D = lightGo.GetComponent<Light2D>();
            if (light2D == null) light2D = lightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.intensity = 1.25f;
            light2D.color     = new Color(1f, 0.97f, 0.92f);
            EditorUtility.SetDirty(lightGo);
        }

        private static void SetupAudioSources(Transform parent)
        {
            SetupAudioSource(GetOrCreate("AmbientAudioSource", parent), true, 0.6f);
            SetupAudioSource(GetOrCreate("WeatherAudioSource", parent), true, 0.7f);
            SetupAudioSource(GetOrCreate("MusicAudioSource",   parent), true, 0.45f);
            SetupAudioSource(GetOrCreate("UIAudioSource",      parent), false, 0.85f);
        }

        private static void SetupAudioSource(GameObject go, bool loop, float volume)
        {
            var src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            src.loop         = loop;
            src.volume       = volume;
            src.playOnAwake  = false;
            src.spatialBlend = 0f;
            EditorUtility.SetDirty(go);
        }

        private static void SetupEnvironmentVisuals(Transform envRoot)
        {
            // Sky Background
            var skyGo = GetOrCreate("SkyBackground", envRoot);
            skyGo.transform.position = new Vector3(0, 5, 20f);
            skyGo.transform.localScale = new Vector3(80, 50, 1);
            var skySr = skyGo.GetComponent<SpriteRenderer>();
            if (skySr == null) skySr = skyGo.AddComponent<SpriteRenderer>();
            skySr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/sky_gradient.png");
            skySr.sortingOrder = -100;

            // Sun / Moon
            var sunGo = GetOrCreate("SunOrMoon", envRoot);
            sunGo.transform.position = new Vector3(10, 7.5f, 18f);
            sunGo.transform.localScale = new Vector3(2.5f, 2.5f, 1);
            var sunSr = sunGo.GetComponent<SpriteRenderer>();
            if (sunSr == null) sunSr = sunGo.AddComponent<SpriteRenderer>();
            sunSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/sun.png");
            sunSr.sortingOrder = -90;

            // Far Mountains
            var farGo = GetOrCreate("FarMountains", envRoot);
            farGo.transform.position = new Vector3(0, -1.0f, 10f);
            farGo.transform.localScale = new Vector3(50, 14, 1);
            var farSr = farGo.GetComponent<SpriteRenderer>();
            if (farSr == null) farSr = farGo.AddComponent<SpriteRenderer>();
            farSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/mountain_far.png");
            farSr.sortingOrder = -80;

            // Mid Mountains
            var midGo = GetOrCreate("MiddleMountains", envRoot);
            midGo.transform.position = new Vector3(0, -2.0f, 5f);
            midGo.transform.localScale = new Vector3(50, 10, 1);
            var midSr = midGo.GetComponent<SpriteRenderer>();
            if (midSr == null) midSr = midGo.AddComponent<SpriteRenderer>();
            midSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/mountain_mid.png");
            midSr.sortingOrder = -70;

            // Clouds
            SetupCloud(GetOrCreate("Cloud01", envRoot), new Vector3(-8, 6.5f, 15f), new Vector3(3, 1.5f, 1));
            SetupCloud(GetOrCreate("Cloud02", envRoot), new Vector3(4, 7.8f, 15f), new Vector3(3.5f, 1.8f, 1));
            SetupCloud(GetOrCreate("Cloud03", envRoot), new Vector3(15, 6.0f, 15f), new Vector3(2.5f, 1.3f, 1));

            // Day / Night Overlay
            var overlayGo = GetOrCreate("DayNightOverlay", envRoot);
            overlayGo.transform.position = new Vector3(0, 5, 1f);
            overlayGo.transform.localScale = new Vector3(80, 50, 1);
            var overlaySr = overlayGo.GetComponent<SpriteRenderer>();
            if (overlaySr == null) overlaySr = overlayGo.AddComponent<SpriteRenderer>();
            overlaySr.sprite = CreateSolidWhiteSprite();
            overlaySr.color = new Color(0, 0, 0, 0);
            overlaySr.sortingOrder = 10;
        }

        private static void SetupCloud(GameObject go, Vector3 pos, Vector3 scale)
        {
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/cloud.png");
            sr.sortingOrder = -60;
            EditorUtility.SetDirty(go);
        }

        private static void SetupTerrainSegments(Transform envRoot)
        {
            var groundTerrain = GetOrCreate("GroundTerrain", envRoot).transform;

            var roadTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HillClimbGame/Art/grassy_hill_terrain.png") ??
                          AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HillClimbGame/Art/road_terrain.png");
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/HillClimbGame/Materials/TerrainMat.mat");
            if (mat == null)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, "Assets/HillClimbGame/Materials/TerrainMat.mat");
            }
            if (roadTex != null) mat.mainTexture = roadTex;
            mat.color = Color.white;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // Create PhysicsMaterial2D for high tire and road grip
            var roadPhysMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/HillClimbGame/Materials/RoadPhysMat.physicsMaterial2D");
            if (roadPhysMat == null)
            {
                roadPhysMat = new PhysicsMaterial2D("RoadPhysMat");
                roadPhysMat.friction = 1.6f;
                roadPhysMat.bounciness = 0f;
                AssetDatabase.CreateAsset(roadPhysMat, "Assets/HillClimbGame/Materials/RoadPhysMat.physicsMaterial2D");
                AssetDatabase.SaveAssets();
            }

            for (int i = 1; i <= 5; i++)
            {
                string segName = "TerrainSeg0" + i;
                var go = GetOrCreate(segName, groundTerrain);

                if (go.GetComponent<TerrainSegment>() == null) go.AddComponent<TerrainSegment>();
                if (go.GetComponent<MeshFilter>() == null) go.AddComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();
                if (mr == null) mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.sortingOrder = 3;

                var pc = go.GetComponent<PolygonCollider2D>();
                if (pc == null) pc = go.AddComponent<PolygonCollider2D>();
                pc.sharedMaterial = roadPhysMat;

                EditorUtility.SetDirty(go);
            }
        }

        private static void SetupVehicle(Transform vehicleRoot)
        {
            var bodyGo = GetOrCreate("VehicleBody", vehicleRoot);
            bodyGo.tag = "Player";
            bodyGo.transform.position = new Vector3(-3f, 0.7f, 0f);

            var bodyRb = bodyGo.GetComponent<Rigidbody2D>();
            if (bodyRb == null) bodyRb = bodyGo.AddComponent<Rigidbody2D>();
            bodyRb.mass = 1.5f;
            bodyRb.gravityScale = 2.2f;
            bodyRb.linearDamping = 0.3f;
            bodyRb.angularDamping = 1.2f;
            bodyRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var bodyCol = bodyGo.GetComponent<BoxCollider2D>();
            if (bodyCol == null) bodyCol = bodyGo.AddComponent<BoxCollider2D>();
            bodyCol.size = new Vector2(2.1f, 0.75f);
            bodyCol.offset = new Vector2(0f, 0.25f);

            var bodySr = bodyGo.GetComponent<SpriteRenderer>();
            if (bodySr == null) bodySr = bodyGo.AddComponent<SpriteRenderer>();
            bodySr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/jeep_body.png");
            bodySr.sortingOrder = 5;
            bodyGo.transform.localScale = Vector3.one;

            // Front Wheel
            var frontGo = GetOrCreate("FrontWheel", vehicleRoot);
            frontGo.tag = "Player";
            SetupWheelObject(frontGo, new Vector3(-3f + 0.65f, 0.7f - 0.44f, 0f));

            // Rear Wheel
            var rearGo = GetOrCreate("RearWheel", vehicleRoot);
            rearGo.tag = "Player";
            SetupWheelObject(rearGo, new Vector3(-3f - 0.65f, 0.7f - 0.44f, 0f));

            // Ignore collision between vehicle chassis and wheels
            var frontCol = frontGo.GetComponent<Collider2D>();
            var rearCol  = rearGo.GetComponent<Collider2D>();
            if (frontCol != null) Physics2D.IgnoreCollision(bodyCol, frontCol, true);
            if (rearCol  != null) Physics2D.IgnoreCollision(bodyCol, rearCol, true);

            // Setup Wheel Joints
            var allJoints = bodyGo.GetComponents<WheelJoint2D>();
            WheelJoint2D frontJoint = allJoints.Length > 0 ? allJoints[0] : bodyGo.AddComponent<WheelJoint2D>();
            WheelJoint2D rearJoint  = allJoints.Length > 1 ? allJoints[1] : bodyGo.AddComponent<WheelJoint2D>();

            ConfigureWheelJoint(frontJoint, frontGo.GetComponent<Rigidbody2D>(), new Vector2(0.65f, -0.44f));
            ConfigureWheelJoint(rearJoint,  rearGo.GetComponent<Rigidbody2D>(),  new Vector2(-0.65f, -0.44f));

            // Ensure VehicleController script is on VehicleBody
            var vc = bodyGo.GetComponent<VehicleController>();
            if (vc == null) vc = bodyGo.AddComponent<VehicleController>();
            vc.AutoWire();

            EditorUtility.SetDirty(bodyGo);
        }

        private static void SetupWheelObject(GameObject go, Vector3 pos)
        {
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.46f, 0.46f, 1f);

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 0.25f;
            rb.gravityScale = 2.2f;

            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.64f;
            var roadPhysMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/HillClimbGame/Materials/RoadPhysMat.physicsMaterial2D");
            if (roadPhysMat != null) col.sharedMaterial = roadPhysMat;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/jeep_wheel.png");
            sr.sortingOrder = 6;

            EditorUtility.SetDirty(go);
        }

        private static void ConfigureWheelJoint(WheelJoint2D joint, Rigidbody2D wheelRb, Vector2 anchor)
        {
            joint.connectedBody = wheelRb;
            joint.anchor = anchor;
            joint.useMotor = false;
            var susp = joint.suspension;
            susp.frequency = 5.5f;
            susp.dampingRatio = 0.75f;
            joint.suspension = susp;
        }

        private static void SetupPickups(Transform worldRoot)
        {
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/coin.png");
            float[] coinX = { 15f, 32f, 52f, 75f, 98f };

            for (int i = 0; i < coinX.Length; i++)
            {
                var go = GetOrCreate("Coin0" + (i + 1), worldRoot);
                go.transform.position = new Vector3(coinX[i], 1.5f, 0f);
                go.transform.localScale = Vector3.one;

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = coinSprite;
                sr.sortingOrder = 7;

                var col = go.GetComponent<CircleCollider2D>();
                if (col == null) col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.32f;

                if (go.GetComponent<CoinPickup>() == null) go.AddComponent<CoinPickup>();
                EditorUtility.SetDirty(go);
            }

            var fuelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/fuel_can.png");
            float[] fuelX = { 42f, 115f };

            for (int i = 0; i < fuelX.Length; i++)
            {
                var go = GetOrCreate("FuelCan0" + (i + 1), worldRoot);
                go.transform.position = new Vector3(fuelX[i], 1.5f, 0f);
                go.transform.localScale = Vector3.one;

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = fuelSprite;
                sr.sortingOrder = 7;

                var col = go.GetComponent<BoxCollider2D>();
                if (col == null) col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.6f, 0.8f);

                if (go.GetComponent<FuelCanPickup>() == null) go.AddComponent<FuelCanPickup>();
                EditorUtility.SetDirty(go);
            }
        }

        private static void SetupWeatherParticles(Transform weatherRoot)
        {
            SetupParticleSystem(GetOrCreate("RainLight", weatherRoot), new Color(0.7f, 0.8f, 1f, 0.7f), 250);
            SetupParticleSystem(GetOrCreate("RainHeavy", weatherRoot), new Color(0.5f, 0.65f, 0.95f, 0.9f), 650);
            SetupParticleSystem(GetOrCreate("Snow", weatherRoot), new Color(1f, 1f, 1f, 0.85f), 300);

            var fogGo = GetOrCreate("FogOverlay", weatherRoot);
            fogGo.transform.position = new Vector3(0, 5, 0);
            fogGo.transform.localScale = new Vector3(80, 50, 1);
            var fogSr = fogGo.GetComponent<SpriteRenderer>();
            if (fogSr == null) fogSr = fogGo.AddComponent<SpriteRenderer>();
            fogSr.sprite = CreateSolidWhiteSprite();
            fogSr.color = new Color(0.7f, 0.75f, 0.85f, 0f);
            fogSr.sortingOrder = 8;

            var flashGo = GetOrCreate("LightningFlash", weatherRoot);
            flashGo.transform.position = new Vector3(0, 5, 0);
            flashGo.transform.localScale = new Vector3(80, 50, 1);
            var flashSr = flashGo.GetComponent<SpriteRenderer>();
            if (flashSr == null) flashSr = flashGo.AddComponent<SpriteRenderer>();
            flashSr.sprite = CreateSolidWhiteSprite();
            flashSr.color = new Color(1f, 1f, 1f, 0f);
            flashSr.sortingOrder = 9;
        }

        private static void SetupParticleSystem(GameObject go, Color color, int maxParticles)
        {
            go.transform.position = new Vector3(0, 8, 0);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles = maxParticles;
            main.startLifetime = 2.2f;
            main.startSpeed = 12f;
            main.startSize = 0.06f;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.startColor = color;

            var emission = ps.emission;
            emission.rateOverTime = maxParticles * 0.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(35f, 0.1f, 1f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-2f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);

            ps.Stop();

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.sortingOrder = 7;
            EditorUtility.SetDirty(go);
        }

        private static void AttachAndWireControllers(Transform systemsRoot)
        {
            // Core Controllers
            var saveCtrl = AttachScript<SaveController>(GetOrCreate("SaveController", systemsRoot));
            var terrainCtrl = AttachScript<TerrainController>(GetOrCreate("TerrainController", systemsRoot));
            var fuelCtrl = AttachScript<FuelController>(GetOrCreate("FuelController", systemsRoot));
            var coinCtrl = AttachScript<CoinController>(GetOrCreate("CoinController", systemsRoot));
            var weatherCtrl = AttachScript<WeatherController>(GetOrCreate("WeatherController", systemsRoot));
            var dayNightCtrl = AttachScript<DayNightController>(GetOrCreate("DayNightController", systemsRoot));
            var audioCtrl = AttachScript<AudioController>(GetOrCreate("AudioController", systemsRoot));

            var gcGo = GetOrCreate("GameController", systemsRoot);
            var uiCtrl = AttachScript<UIController>(gcGo);
            var gameCtrl = AttachScript<GameController>(gcGo);

            // Wire audio clips
            WireAudioClips(audioCtrl);

            // Auto-wire each component
            terrainCtrl?.AutoWire();
            fuelCtrl?.AutoWire();
            coinCtrl?.AutoWire();
            weatherCtrl?.AutoWire();
            dayNightCtrl?.AutoWire();
            audioCtrl?.AutoWire();
            uiCtrl?.AutoWire();
            gameCtrl?.AutoWire();

            // Set moon & sun sprites on DayNightController
            var serializedDN = new SerializedObject(dayNightCtrl);
            var sunProp = serializedDN.FindProperty("sunSprite");
            var moonProp = serializedDN.FindProperty("moonSprite");
            if (sunProp != null) sunProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/sun.png");
            if (moonProp != null) moonProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HillClimbGame/Art/moon.png");
            serializedDN.ApplyModifiedProperties();

            // Clean up obsolete root controller objects if any duplicate existed
            CleanOldRootDuplicate("VehicleController");
        }

        private static void WireAudioClips(AudioController audioCtrl)
        {
            if (audioCtrl == null) return;
            var so = new SerializedObject(audioCtrl);

            SetClipProperty(so, "uiClickClip", "Assets/HillClimbGame/Audio/click.wav");
            SetClipProperty(so, "dayAmbient", "Assets/HillClimbGame/Audio/wind.wav");
            SetClipProperty(so, "rainLightClip", "Assets/HillClimbGame/Audio/rain.wav");
            SetClipProperty(so, "rainHeavyClip", "Assets/HillClimbGame/Audio/rain.wav");
            SetClipProperty(so, "thunderClip", "Assets/HillClimbGame/Audio/crash.wav");
            SetClipProperty(so, "backgroundMusic", "Assets/HillClimbGame/Audio/engine.wav");

            so.ApplyModifiedProperties();
        }

        private static void SetClipProperty(SerializedObject so, string propName, string assetPath)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null) prop.objectReferenceValue = clip;
            }
        }

        private static void CleanOldRootDuplicate(string name)
        {
            var old = GameObject.Find(name);
            if (old != null && old.transform.parent == null && old.transform.childCount == 0)
            {
                // Only destroy if it has no children and isn't the actual vehicle body
                if (old.name != "VehicleBody") GameObject.DestroyImmediate(old);
            }
        }

        private static T AttachScript<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            EditorUtility.SetDirty(go);
            return comp;
        }

        private static Sprite _whiteSprite;
        private static Sprite CreateSolidWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }
    }
}
