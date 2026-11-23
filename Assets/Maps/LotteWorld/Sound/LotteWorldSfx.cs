// LotteWorldSfx.cs - 롯데월드 맵 효과음을 씬에 자동으로 붙임 (런타임). 씬 파일은 건드리지 않음.
//
// 플레이를 시작하거나 씬이 열리면 아래를 찾아 소리를 붙임:
//   - 깜빡이는 전등 (LotteWorld.Props.LightFlicker)  → '지이잉' 험 + 꺼지고 켜질 때 '탁/지직'   (FlickerSound)
//   - 물 표면 (셰이더 LotteWorld/Water 를 쓰는 메시) → 카메라(플레이어)가 들어가면 첨벙, 걸으면 촤악  (WaterSurface, PlayerWaterSound)
//   - 물 튀는 곳 (LW_WaterFX 아래 Splash_*)         → 물 흐르는 소리 반복
//   - 놀이기구 (RideMotor)                           → 켜질 때 덜컥 + 회전목마 오르골 / 회전바구니 모터 / 바이킹·풍차 삐걱
//   - 후룸 (FlumePath)                              → 체인 모터 소리 + 배가 내리막 끝에서 물에 닿을 때 첨벙
//   - 공원 스피커 (놀이기구가 있는 씬에 하나)      → 놀이기구가 켜지면 망가진 테마송, 그 뒤 간격 두고 반복   (ParkTheme)
// 놀이기구 소리는 RideDirector.ActivateAll() 로 켜질 때부터 나고, 속도(Power)에 맞춰 음 높이·크기가 바뀜 (RideMotor 가 함).
//
// 소리 파일: Sound/Resources/LotteWorldSfx/*.wav  (Tools/GenerateLotteWorldSfx.py 로 다시 만들 수 있음,
//            park_theme_creepy.wav 는 Tools/MakeCreepyTheme.py 로 Sound/Source 의 원곡에서 만듦)
// 끄기: LotteWorldSfx.Enabled = false (씬 열기 전에), 또는 이미 붙은 건 해당 컴포넌트를 끄면 됨.
// 이미 AudioSource 를 손으로 넣어 둔 놀이기구(loopAudio 가 채워진 것)는 건드리지 않음.
using System.Collections;
using System.Collections.Generic;
using LotteWorld.Props;
using LotteWorld.Rides;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Sound
{
    public static class LotteWorldSfx
    {
        public static bool Enabled = true;
        const string Folder = "LotteWorldSfx/";
        const string WaterShader = "LotteWorld/Water";
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); hooked = false; WaterSurface.All.Clear(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!hooked) { SceneManager.sceneLoaded += (s, m) => SetupScene(s); hooked = true; }
            for (int i = 0; i < SceneManager.sceneCount; i++) SetupScene(SceneManager.GetSceneAt(i));
        }

        public static AudioClip Clip(string name)
        {
            if (cache.TryGetValue(name, out var c) && c) return c;
            c = Resources.Load<AudioClip>(Folder + name);
            if (!c) Debug.LogWarning("[Sfx] 소리 파일 없음: Resources/" + Folder + name);
            cache[name] = c;
            return c;
        }

        public static AudioClip Pick(params string[] names) { return Clip(names[Random.Range(0, names.Length)]); }

        /// <summary>go 에 3D AudioSource 를 붙임. minDist 안에서는 최대 크기, maxDist 밖에서는 안 들림.</summary>
        public static AudioSource Source(GameObject go, AudioClip clip, bool loop, float volume, float minDist, float maxDist, int priority = 128)
        {
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.loop = loop; a.volume = volume; a.playOnAwake = false;
            a.spatialBlend = 1f; a.dopplerLevel = 0f; a.priority = priority;
            a.rolloffMode = AudioRolloffMode.Linear; a.minDistance = minDist; a.maxDistance = maxDist;
            if (loop && clip) a.time = Random.Range(0f, clip.length * 0.95f);   // 여러 개가 같은 박자로 안 울리게
            return a;
        }

        /// <summary>한 번 재생하고 사라지는 3D 소리</summary>
        public static void OneShot(AudioClip clip, Vector3 pos, float volume, float minDist, float maxDist, float pitch = 1f)
        {
            if (!clip) return;
            var go = new GameObject("~Sfx_" + clip.name);
            go.transform.position = pos;
            var a = Source(go, clip, false, volume, minDist, maxDist);
            a.pitch = pitch;
            a.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        static void SetupScene(Scene scene)
        {
            if (!Enabled || !scene.isLoaded) return;
            int lights = 0, waters = 0, flows = 0, rides = 0, flumes = 0;
            var rideDone = new HashSet<Transform>();
            var flowSpots = new List<Vector3>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var f in root.GetComponentsInChildren<LightFlicker>(true))
                    if (!f.GetComponent<FlickerSound>()) { f.gameObject.AddComponent<FlickerSound>(); lights++; }

                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                    if (!r.GetComponent<WaterSurface>() && UsesWater(r)) { r.gameObject.AddComponent<WaterSurface>(); waters++; }

                if (root.name == "LW_WaterFX")
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (!t.name.StartsWith("Splash_")) continue;
                        bool near = false;
                        foreach (var p in flowSpots) if ((p - t.position).sqrMagnitude < 36f) { near = true; break; }
                        if (near) continue;
                        flowSpots.Add(t.position);
                        var a = Source(t.gameObject, Clip("water_flow_loop"), true, 0.55f, 2f, 26f, 160);
                        a.Play(); flows++;
                    }

                foreach (var m in root.GetComponentsInChildren<RideMotor>(true))
                    if (SetupRide(m, rideDone)) rides++;

                foreach (var fp in root.GetComponentsInChildren<FlumePath>(true))
                {
                    if (fp.GetComponent<FlumeSplash>()) continue;
                    if (!fp.loopAudio)
                    {
                        fp.loopAudio = Source(fp.gameObject, Clip("ride_motor_loop"), true, 0.5f, 4f, 35f);
                        fp.loopAudio.pitch = 0.8f;
                    }
                    fp.gameObject.AddComponent<FlumeSplash>();
                    flumes++;
                }
            }

            if (waters > 0)
            {
                var cam = Camera.main;
                if (cam && !cam.GetComponent<PlayerWaterSound>()) cam.gameObject.AddComponent<PlayerWaterSound>();
            }
            if (rides > 0 && Object.FindAnyObjectByType<ParkTheme>() == null)
            {
                var pt = new GameObject("~ParkTheme");
                SceneManager.MoveGameObjectToScene(pt, scene);
                pt.AddComponent<ParkTheme>();
            }
            if (lights + waters + flows + rides + flumes > 0)
                Debug.Log(string.Format("[Sfx] {0}: 전등 {1}, 물 표면 {2}, 물소리 {3}, 놀이기구 {4}, 후룸 {5}", scene.name, lights, waters, flows, rides, flumes));
        }

        static bool UsesWater(Renderer r)
        {
            foreach (var m in r.sharedMaterials) if (m && m.shader && m.shader.name == WaterShader) return true;
            return false;
        }

        static bool SetupRide(RideMotor m, HashSet<Transform> done)
        {
            if (m.loopAudio) return false;                               // 손으로 넣어 둔 소리는 그대로
            var rr = m.GetComponentInParent<RideRoot>();
            Transform root = rr ? rr.transform : m.transform.root;
            string path = root.name + "/" + m.name;
            for (var t = m.transform; t && t != root; t = t.parent) path += "/" + t.name;

            string loop; float vol, minD, maxD;
            if (path.Contains("Carousel")) { loop = "carousel_music_loop"; vol = 0.8f; minD = 4f; maxD = 45f; }
            else if (path.Contains("PirateShip")) { loop = "ride_creak_loop"; vol = 0.9f; minD = 4f; maxD = 40f; }
            else if (path.Contains("Castle")) { loop = "ride_creak_loop"; vol = 0.6f; minD = 3f; maxD = 30f; }
            else { loop = "ride_motor_loop"; vol = 0.7f; minD = 3f; maxD = 30f; }

            if (!done.Add(root)) return false;                           // 한 놀이기구에 하나만 (회전바구니 Sub1~3 은 소리 없음)
            m.loopAudio = Source(m.gameObject, Clip(loop), true, vol, minD, maxD);
            if (!m.startAudio) m.startAudio = Source(m.gameObject, Clip("ride_start_clunk"), false, 1f, 4f, 50f);
            return true;
        }
    }
}
