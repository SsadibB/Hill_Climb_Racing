using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HillClimbGame
{
    public static class AssetGenerator
    {
        private const string ArtPath = "Assets/HillClimbGame/Art";
        private const string AudioPath = "Assets/HillClimbGame/Audio";

        [MenuItem("HillClimbGame/0 - Generate Sprites and Audio")]
        public static void GenerateAllAssets()
        {
            if (!Directory.Exists(ArtPath)) Directory.CreateDirectory(ArtPath);
            if (!Directory.Exists(AudioPath)) Directory.CreateDirectory(AudioPath);

            GenerateSprites();
            GenerateAudioClips();

            AssetDatabase.Refresh();
            ConfigureImportedAssets();
            Debug.Log("[HillClimbGame] All procedural sprites and audio generated and imported successfully!");
        }

        #region SPRITE GENERATION

        private static void GenerateSprites()
        {
            SaveSprite("jeep_body.png", CreateJeepBodyTexture(256, 128));
            SaveSprite("jeep_wheel.png", CreateWheelTexture(128, 128));
            SaveSprite("coin.png", CreateCoinTexture(64, 64));
            SaveSprite("fuel_can.png", CreateFuelCanTexture(64, 96));
            SaveSprite("cloud.png", CreateCloudTexture(256, 128));
            SaveSprite("sun.png", CreateSunTexture(128, 128));
            SaveSprite("moon.png", CreateMoonTexture(128, 128));
            SaveSprite("mountain_far.png", CreateMountainFarTexture(512, 256));
            SaveSprite("mountain_mid.png", CreateMountainMidTexture(512, 256));
            SaveSprite("sky_gradient.png", CreateSkyGradientTexture(128, 512));
            SaveSprite("road_terrain.png", CreateGrassyHillTerrainTexture(512, 512));
            SaveSprite("grassy_hill_terrain.png", CreateGrassyHillTerrainTexture(512, 512));
        }

        private static void SaveSprite(string fileName, Texture2D tex)
        {
            string path = Path.Combine(ArtPath, fileName);
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static Texture2D CreateJeepBodyTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            Color jeepOrange = new Color(0.92f, 0.42f, 0.08f);
            Color darkOrange = new Color(0.75f, 0.30f, 0.05f);
            Color rollCageColor = new Color(0.2f, 0.2f, 0.22f);
            Color windowColor = new Color(0.4f, 0.75f, 0.95f, 0.85f);
            Color bumperColor = new Color(0.25f, 0.25f, 0.27f);
            Color headlightColor = new Color(1f, 0.95f, 0.5f);

            // 1. Lower body (chassis / hood / rear bed)
            for (int y = 20; y < 65; y++)
            {
                for (int x = 20; x < 235; x++)
                {
                    // Wheel cutouts at bottom
                    float distFrontWheel = Vector2.Distance(new Vector2(x, y), new Vector2(185, 20));
                    float distRearWheel = Vector2.Distance(new Vector2(x, y), new Vector2(65, 20));
                    if (distFrontWheel < 28 || distRearWheel < 28) continue;

                    // Front taper
                    if (x > 210 && y > 50) continue;

                    Color c = (y > 45) ? jeepOrange : darkOrange;
                    tex.SetPixel(x, y, c);
                }
            }

            // 2. Cabin / Roll cage & windshield
            // Windshield angle from x=140, y=65 up to x=115, y=105
            for (int y = 65; y < 105; y++)
            {
                float t = (y - 65f) / 40f;
                int minX = (int)Mathf.Lerp(30, 45, t);
                int maxX = (int)Mathf.Lerp(155, 120, t);

                for (int x = minX; x <= maxX; x++)
                {
                    // Window area
                    if (x > minX + 8 && x < maxX - 8 && y < 98)
                    {
                        tex.SetPixel(x, y, windowColor);
                    }
                    else
                    {
                        // Roll cage / frame
                        tex.SetPixel(x, y, rollCageColor);
                    }
                }
            }

            // 3. Front bumper and grill
            for (int y = 24; y < 45; y++)
            {
                for (int x = 230; x < 245; x++)
                    tex.SetPixel(x, y, bumperColor);
            }

            // 4. Rear spare mount / bumper
            for (int y = 24; y < 45; y++)
            {
                for (int x = 12; x < 24; x++)
                    tex.SetPixel(x, y, bumperColor);
            }

            // 5. Headlight
            for (int y = 42; y < 52; y++)
            {
                for (int x = 230; x < 236; x++)
                    tex.SetPixel(x, y, headlightColor);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateWheelTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float radius = w * 0.48f;
            float rimRadius = radius * 0.62f;
            float hubRadius = radius * 0.24f;

            Color tireColor = new Color(0.12f, 0.12f, 0.14f);
            Color treadColor = new Color(0.04f, 0.04f, 0.05f);
            Color rimOuterColor = new Color(0.85f, 0.85f, 0.88f);
            Color spokeBright = new Color(0.92f, 0.92f, 0.95f);
            Color cavityDark = new Color(0.15f, 0.15f, 0.18f);
            Color hubCenter = new Color(0.78f, 0.78f, 0.82f);
            Color lugNutColor = new Color(0.10f, 0.10f, 0.12f);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > radius)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    float angle = Mathf.Atan2(y - center.y, x - center.x);

                    // Treads on outer tire
                    if (d > radius - 7)
                    {
                        bool isTread = Mathf.Sin(angle * 14f) > 0f;
                        tex.SetPixel(x, y, isTread ? treadColor : tireColor);
                    }
                    else if (d > rimRadius)
                    {
                        tex.SetPixel(x, y, tireColor);
                    }
                    else if (d > rimRadius - 4)
                    {
                        // Outer rim lip
                        tex.SetPixel(x, y, rimOuterColor);
                    }
                    else if (d > hubRadius)
                    {
                        // 5 bold spokes with high contrast cavities
                        // spoke angle frequency 5
                        float spokeSin = Mathf.Sin(angle * 5f);
                        if (Mathf.Abs(spokeSin) < 0.42f)
                        {
                            tex.SetPixel(x, y, spokeBright);
                        }
                        else
                        {
                            tex.SetPixel(x, y, cavityDark);
                        }
                    }
                    else if (d > hubRadius - 4)
                    {
                        // Hub ring
                        tex.SetPixel(x, y, rimOuterColor);
                    }
                    else
                    {
                        // Hub center with 5 lug nuts
                        float lugDist = Vector2.Distance(new Vector2(x, y), center);
                        bool isLugNut = false;
                        if (lugDist > hubRadius * 0.4f && lugDist < hubRadius * 0.85f)
                        {
                            for (int lug = 0; lug < 5; lug++)
                            {
                                float la = lug * Mathf.PI * 2f / 5f;
                                Vector2 lugPos = center + new Vector2(Mathf.Cos(la), Mathf.Sin(la)) * (hubRadius * 0.62f);
                                if (Vector2.Distance(new Vector2(x, y), lugPos) < 3.2f)
                                {
                                    isLugNut = true;
                                    break;
                                }
                            }
                        }

                        if (isLugNut)
                            tex.SetPixel(x, y, lugNutColor);
                        else
                            tex.SetPixel(x, y, hubCenter);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCoinTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float radius = w * 0.46f;
            Color goldOuter = new Color(0.95f, 0.72f, 0.10f);
            Color goldInner = new Color(1f, 0.88f, 0.25f);
            Color goldShine = new Color(1f, 1f, 0.7f);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > radius)
                    {
                        tex.SetPixel(x, y, clear);
                    }
                    else if (d > radius - 3)
                    {
                        tex.SetPixel(x, y, goldOuter);
                    }
                    else if (d > radius * 0.65f)
                    {
                        tex.SetPixel(x, y, goldInner);
                    }
                    else
                    {
                        // Star / dollar emblem
                        bool star = (Mathf.Abs(x - center.x) < 3 && Mathf.Abs(y - center.y) < 14) ||
                                    (Mathf.Abs(y - center.y) < 3 && Mathf.Abs(x - center.x) < 14);
                        tex.SetPixel(x, y, star ? goldShine : goldOuter);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateFuelCanTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color redCan = new Color(0.85f, 0.15f, 0.12f);
            Color darkRed = new Color(0.65f, 0.08f, 0.08f);
            Color white = Color.white;
            Color capColor = new Color(0.85f, 0.85f, 0.2f);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            // Body
            for (int y = 6; y < 70; y++)
            {
                for (int x = 10; x < w - 10; x++)
                {
                    Color c = (x < 14 || x > w - 14 || y < 10) ? darkRed : redCan;
                    // White stripe with "F" in middle
                    if (y >= 34 && y <= 42) c = white;
                    tex.SetPixel(x, y, c);
                }
            }

            // Handle
            for (int y = 70; y < 86; y++)
            {
                for (int x = 16; x < w - 16; x++)
                {
                    if (y >= 74 && y <= 80 && x > 24 && x < w - 24) continue; // hole in handle
                    tex.SetPixel(x, y, darkRed);
                }
            }

            // Spout / Cap
            for (int y = 74; y < 90; y++)
            {
                for (int x = w - 22; x < w - 12; x++)
                    tex.SetPixel(x, y, capColor);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCloudTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            Vector2[] puffs = new Vector2[]
            {
                new Vector2(60, 45),
                new Vector2(105, 65),
                new Vector2(150, 70),
                new Vector2(195, 55),
                new Vector2(130, 40)
            };
            float[] radii = new float[] { 35, 48, 52, 42, 38 };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float maxAlpha = 0f;
                    Vector2 pt = new Vector2(x, y);
                    for (int i = 0; i < puffs.Length; i++)
                    {
                        float d = Vector2.Distance(pt, puffs[i]);
                        if (d < radii[i])
                        {
                            float a = 1f - Mathf.SmoothStep(radii[i] * 0.4f, radii[i], d);
                            if (a > maxAlpha) maxAlpha = a;
                        }
                    }
                    if (maxAlpha > 0.01f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, maxAlpha * 0.9f));
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSunTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float radius = w * 0.42f;
            Color sunCore = new Color(1f, 0.98f, 0.75f, 1f);
            Color sunEdge = new Color(1f, 0.65f, 0.15f, 0.9f);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > radius)
                    {
                        tex.SetPixel(x, y, clear);
                    }
                    else
                    {
                        float t = d / radius;
                        Color c = Color.Lerp(sunCore, sunEdge, t);
                        c.a = Mathf.Clamp01(1.1f - t);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMoonTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            Vector2 cutout = center + new Vector2(14, 10);
            float radius = w * 0.40f;
            Color moonColor = new Color(0.92f, 0.94f, 1f, 0.95f);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 pt = new Vector2(x, y);
                    float d1 = Vector2.Distance(pt, center);
                    float d2 = Vector2.Distance(pt, cutout);

                    if (d1 < radius && d2 > radius * 0.75f)
                        tex.SetPixel(x, y, moonColor);
                    else
                        tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMountainFarTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color mountainColor = new Color(0.35f, 0.28f, 0.55f, 0.85f);
            Color clear = new Color(0, 0, 0, 0);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                float height = 70f + 110f * Mathf.PerlinNoise(nx * 3.5f, 0.2f)
                                  + 40f * Mathf.Sin(nx * Mathf.PI * 4f);
                for (int y = 0; y < h; y++)
                {
                    if (y < height)
                        tex.SetPixel(x, y, mountainColor);
                    else
                        tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMountainMidTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color midHillColor = new Color(0.20f, 0.38f, 0.35f, 0.95f);
            Color clear = new Color(0, 0, 0, 0);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                float height = 40f + 85f * Mathf.PerlinNoise(nx * 5f, 1.8f)
                                  + 35f * Mathf.Sin(nx * Mathf.PI * 6f);
                for (int y = 0; y < h; y++)
                {
                    if (y < height)
                        tex.SetPixel(x, y, midHillColor);
                    else
                        tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSkyGradientTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            // Vivid cyan-blue sky: rich bright blue at top, light azure at horizon
            Color skyTop    = new Color(0.10f, 0.48f, 0.95f);  // Deep vibrant cyan-blue
            Color skyMid    = new Color(0.30f, 0.70f, 1.00f);  // Bright azure
            Color skyBottom = new Color(0.62f, 0.88f, 1.00f);  // Light pale cyan horizon

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color c = (t > 0.5f)
                    ? Color.Lerp(skyMid, skyTop, (t - 0.5f) * 2f)
                    : Color.Lerp(skyBottom, skyMid, t * 2f);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGrassyHillTerrainTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;

            // Visual style:
            // TOP ~12%  — Vivid green grass crest strip (bright lime-green)
            // NEXT ~8%  — Transitional dark-green / brown blend
            // BODY ~80% — Rich warm brown earth / soil with subtle circular pebble patterns
            Color grassCrest    = new Color(0.22f, 0.85f, 0.10f); // Vivid lime-green crest
            Color grassEdge     = new Color(0.38f, 0.70f, 0.12f); // Slightly darker grass edge
            Color soilTop       = new Color(0.52f, 0.33f, 0.14f); // Rich warm brown topsoil
            Color soilMid       = new Color(0.44f, 0.27f, 0.10f); // Medium brown soil
            Color soilDeep      = new Color(0.35f, 0.21f, 0.08f); // Deep dark brown
            Color pebbleDark    = new Color(0.28f, 0.18f, 0.07f); // Darker pebble outline
            Color pebbleLight   = new Color(0.60f, 0.40f, 0.18f); // Lighter pebble face

            // Pebble seed positions (repeating texture — distribute across 512x512)
            var rng = new System.Random(42);
            var pebbles = new System.Collections.Generic.List<UnityEngine.Vector3>(); // (x, y, radius)
            for (int p = 0; p < 55; p++)
            {
                float px = (float)(rng.NextDouble() * w);
                float py = (float)(rng.NextDouble() * (h * 0.78f)); // only in soil region
                float pr = (float)(rng.NextDouble() * 14f + 5f);    // radius 5..19 px
                pebbles.Add(new UnityEngine.Vector3(px, py, pr));
            }

            int grassTop  = (int)(h * 0.88f); // Grass crest starts here (top 12%)
            int grassEdgeY= (int)(h * 0.80f); // Grass/soil transition

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y >= grassTop)
                    {
                        // Vivid green grass crest — brighter at very top
                        float t = (float)(y - grassTop) / (h - grassTop);
                        tex.SetPixel(x, y, Color.Lerp(grassCrest, new Color(0.30f, 0.95f, 0.18f), t));
                    }
                    else if (y >= grassEdgeY)
                    {
                        // Transition strip grass -> soil
                        float t = (float)(y - grassEdgeY) / (grassTop - grassEdgeY);
                        tex.SetPixel(x, y, Color.Lerp(soilTop, grassEdge, t));
                    }
                    else
                    {
                        // Brown soil body — base color gradient (lighter near surface, darker deep)
                        float dt = (float)y / grassEdgeY;
                        Color baseCol = Color.Lerp(soilDeep, soilMid, dt);
                        // Add topsoil blend near surface
                        if (dt > 0.7f)
                            baseCol = Color.Lerp(baseCol, soilTop, (dt - 0.7f) / 0.3f);

                        // Overlay circular pebble patterns
                        bool inPebble  = false;
                        bool pebbleRim = false;
                        float minDist  = float.MaxValue;
                        for (int pi = 0; pi < pebbles.Count; pi++)
                        {
                            float dist2 = (x - pebbles[pi].x) * (x - pebbles[pi].x)
                                        + (y - pebbles[pi].y) * (y - pebbles[pi].y);
                            float r    = pebbles[pi].z;
                            if (dist2 < r * r)
                            {
                                float normDist = UnityEngine.Mathf.Sqrt(dist2) / r;
                                if (normDist > 0.75f)
                                    pebbleRim = true;
                                else
                                    inPebble = true;
                                if (normDist < minDist) minDist = normDist;
                            }
                        }

                        Color finalCol = baseCol;
                        if (pebbleRim)
                            finalCol = Color.Lerp(baseCol, pebbleDark, 0.45f);
                        if (inPebble)
                            finalCol = Color.Lerp(baseCol, pebbleLight, 0.30f);

                        tex.SetPixel(x, y, finalCol);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        #endregion

        #region AUDIO GENERATION

        private static void GenerateAudioClips()
        {
            SaveWav("click.wav", GenerateBeepWave(880f, 0.06f, 0.3f));
            SaveWav("coin.wav", GenerateCoinChimeWave());
            SaveWav("fuel.wav", GenerateFuelPickupWave());
            SaveWav("crash.wav", GenerateCrashNoiseWave());
            SaveWav("engine.wav", GenerateEngineLoopWave());
            SaveWav("rain.wav", GenerateRainAmbientWave());
            SaveWav("wind.wav", GenerateWindAmbientWave());
        }

        private static void SaveWav(string fileName, byte[] wavData)
        {
            string path = Path.Combine(AudioPath, fileName);
            File.WriteAllBytes(path, wavData);
        }

        private static byte[] GenerateCoinChimeWave()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = t < 0.12f ? 987.77f : 1318.51f; // B5 to E6
                float env = Mathf.Exp(-t * 10f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.45f;
            }

            return EncodeWav(samples, sampleRate);
        }

        private static byte[] GenerateFuelPickupWave()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(300f, 800f, t / duration);
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
            }

            return EncodeWav(samples, sampleRate);
        }

        private static byte[] GenerateBeepWave(float freq, float duration, float vol)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
            }

            return EncodeWav(samples, sampleRate);
        }

        private static byte[] GenerateCrashNoiseWave()
        {
            int sampleRate = 44100;
            float duration = 0.8f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];
            var rng = new System.Random(1234);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float rumble = Mathf.Sin(2f * Mathf.PI * 65f * t);
                float env = Mathf.Exp(-t * 5f);
                samples[i] = (noise * 0.6f + rumble * 0.4f) * env * 0.5f;
            }

            return EncodeWav(samples, sampleRate);
        }

        private static byte[] GenerateEngineLoopWave()
        {
            int sampleRate = 44100;
            float duration = 1.0f; // loopable
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float f1 = Mathf.Sin(2f * Mathf.PI * 55f * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.5f;
                float f3 = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.25f;
                samples[i] = (f1 + f2 + f3) * 0.35f;
            }

            return EncodeWav(samples, sampleRate);
        }

        private static byte[] GenerateRainAmbientWave()
        {
            int sampleRate = 44100;
            float duration = 2.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];
            var rng = new System.Random(4321);

            float last = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                // Pink noise approximation (simple low pass filter)
                last = (last * 0.85f) + (white * 0.15f);
                samples[i] = last * 0.4f;
            }

            return EncodeWav(samples, sampleRate);
        }

        private static byte[] GenerateWindAmbientWave()
        {
            int sampleRate = 44100;
            float duration = 2.5f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];
            var rng = new System.Random(8888);

            float low = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                low = (low * 0.96f) + (white * 0.04f);
                float swell = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.4f * t);
                samples[i] = low * swell * 0.45f;
            }

            return EncodeWav(samples, sampleRate);
        }

        private static byte[] EncodeWav(float[] samples, int sampleRate)
        {
            using (var mem = new MemoryStream())
            using (var writer = new BinaryWriter(mem))
            {
                short channels = 1;
                short bitsPerSample = 16;
                int byteRate = sampleRate * channels * (bitsPerSample / 8);
                short blockAlign = (short)(channels * (bitsPerSample / 8));
                int subChunk2Size = samples.Length * (bitsPerSample / 8);
                int chunkSize = 36 + subChunk2Size;

                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(chunkSize);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // SubChunk1Size (PCM = 16)
                writer.Write((short)1); // AudioFormat 1 = PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);

                // data chunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(subChunk2Size);

                for (int i = 0; i < samples.Length; i++)
                {
                    short val = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    writer.Write(val);
                }

                return mem.ToArray();
            }
        }

        #endregion

        private static void ConfigureImportedAssets()
        {
            string[] artFiles = Directory.GetFiles(ArtPath, "*.png");
            foreach (var file in artFiles)
            {
                string assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100f;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
