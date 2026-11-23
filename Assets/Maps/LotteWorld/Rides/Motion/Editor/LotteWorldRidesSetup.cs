// LotteWorldRidesSetup.cs - 실내 놀이기구에 동작 붙이기 (매직아일랜드 제외)
// 메뉴: Tools > LotteWorld > Rides (놀이기구 작동) > ...
//   1 설정하기   : 회전목마·회전바구니·바이킹·성 풍차에 RideMotor, 후룸에 FlumePath, 씬에 RideDirector.
//                  움직이는 부품은 Static 체크를 끔(정적 배칭되면 안 움직임). Undo 됨. 씬 저장은 직접.
//   2 지우기     : 위에서 붙인 것 전부 떼기 (Static 은 다시 켬)
//   3 지금 켜기  : 플레이 중에만. 열쇠 다 모은 것처럼 켬 (= 플레이 중 F8)
//   4 되돌리기   : 플레이 중에만. 멈추고 처음 자세로 (= Shift+F8)
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Rides.EditorTools
{
    public static class LotteWorldRidesSetup
    {
        struct Part
        {
            public string ride, group; public RideMotor.Mode mode; public RideMotor.AxisMode axis;
            public float deg, amp, period, spinUp, stutter;
            public Part(string ride, string group, RideMotor.Mode mode, RideMotor.AxisMode axis, float deg, float amp, float period, float spinUp, float stutter)
            { this.ride = ride; this.group = group; this.mode = mode; this.axis = axis; this.deg = deg; this.amp = amp; this.period = period; this.spinUp = spinUp; this.stutter = stutter; }
        }

        // 축: 플레이 시작 때 실제 모습으로 정함 (FBX 루트가 X 90° 누워 들어와 로컬 축을 짐작하면 틀림 — 11/20 회전목마가 세로로 돎)
        //     회전목마·바구니 = 세상 위쪽, 바이킹 = 수평 방향 중 짧은 쪽(배 폭), 풍차 = 날개가 제일 얇은 방향
        static readonly Part[] Parts =
        {
            new Part("Carousel_A13",   "Rotor", RideMotor.Mode.Spin,     RideMotor.AxisMode.WorldUp,          18f,  0f, 0f, 10f, 0.7f),
            new Part("Basket_A09",     "Rotor", RideMotor.Mode.Spin,     RideMotor.AxisMode.WorldUp,          28f,  0f, 0f,  8f, 0.6f),
            new Part("Basket_A09",     "Sub1",  RideMotor.Mode.Spin,     RideMotor.AxisMode.WorldUp,         -70f,  0f, 0f,  8f, 0.6f),
            new Part("Basket_A09",     "Sub2",  RideMotor.Mode.Spin,     RideMotor.AxisMode.WorldUp,         -70f,  0f, 0f,  8f, 0.6f),
            new Part("Basket_A09",     "Sub3",  RideMotor.Mode.Spin,     RideMotor.AxisMode.WorldUp,         -70f,  0f, 0f,  8f, 0.6f),
            new Part("PirateShip_A07", "Swing", RideMotor.Mode.Pendulum, RideMotor.AxisMode.NarrowHorizontal,  0f, 42f, 8f, 24f, 0.3f),
            new Part("Castle_A12",     "Sail",  RideMotor.Mode.Spin,     RideMotor.AxisMode.Thinnest,         22f,  0f, 0f,  6f, 0.5f),
        };
        const string FlumeRide = "Flume_A08";
        const string DirectorName = "LotteWorld_RideDirector";

        [MenuItem("Tools/LotteWorld/Rides (놀이기구 작동)/1 설정하기")]
        static void Setup()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Rides] 플레이를 멈추고 실행해 줘"); return; }
            Undo.SetCurrentGroupName("놀이기구 작동 설정");
            int grp = Undo.GetCurrentGroup();
            var log = new List<string>(); var missing = new List<string>(); int unstatic = 0;
            Scene dirScene = SceneManager.GetActiveScene();

            var rides = new HashSet<string>();
            foreach (var p in Parts) rides.Add(p.ride);
            rides.Add(FlumeRide);
            var roots = new Dictionary<string, GameObject>();
            foreach (var name in rides)
            {
                var go = FindInScenes(name);
                if (go == null) { missing.Add(name); continue; }
                roots[name] = go;
                var rr = go.GetComponent<RideRoot>() ?? Undo.AddComponent<RideRoot>(go);
                rr.rideName = name;
                dirScene = go.scene;
            }

            foreach (var p in Parts)
            {
                GameObject root;
                if (!roots.TryGetValue(p.ride, out root)) continue;
                var t = FindDeep(root.transform, p.group);
                if (t == null) { missing.Add(p.ride + "/" + p.group); continue; }
                var m = t.GetComponent<RideMotor>() ?? Undo.AddComponent<RideMotor>(t.gameObject);
                Undo.RecordObject(m, "RideMotor");
                m.mode = p.mode; m.axisMode = p.axis; m.degPerSec = p.deg; m.amplitude = p.amp; m.period = p.period;
                m.spinUpTime = p.spinUp; m.stutter = p.stutter; m.startDelay = 0f;
                EditorUtility.SetDirty(m);
                unstatic += ClearStatic(t);
                log.Add(string.Format("{0}/{1} {2}", p.ride, p.group, p.mode == RideMotor.Mode.Spin ? p.deg + "°/s" : "±" + p.amp + "°"));
            }

            GameObject flume;
            if (roots.TryGetValue(FlumeRide, out flume))
            {
                var f = flume.GetComponent<FlumePath>() ?? Undo.AddComponent<FlumePath>(flume);
                f.boats.Clear();
                for (int i = 1; i <= 9; i++)
                {
                    var b = FindDeep(flume.transform, "Boat" + i);
                    if (b == null) continue;
                    f.boats.Add(b); unstatic += ClearStatic(b);
                }
                EditorUtility.SetDirty(f);
                log.Add(string.Format("{0} 배 {1}척", FlumeRide, f.boats.Count));
                if (f.boats.Count == 0) missing.Add(FlumeRide + "/Boat1~5");
            }

            var dir = FindAll<RideDirector>().FirstOrDefault();
            if (dir == null)
            {
                var go = new GameObject(DirectorName);
                Undo.RegisterCreatedObjectUndo(go, "RideDirector");
                if (dirScene.IsValid()) SceneManager.MoveGameObjectToScene(go, dirScene);
                dir = go.AddComponent<RideDirector>();
            }

            Undo.CollapseUndoOperations(grp);
            foreach (var s in TouchedScenes(roots.Values, dir.gameObject)) EditorSceneManager.MarkSceneDirty(s);
            Debug.Log("[Rides] 설정 완료: " + string.Join(" / ", log.ToArray()) + string.Format(" | Static 끈 오브젝트 {0}개", unstatic)
                + (missing.Count > 0 ? " | 못 찾음: " + string.Join(", ", missing.ToArray()) : "")
                + "\n플레이 → F8 로 켜 보기 (Shift+F8 되돌리기). 씬 저장은 직접.");
        }

        [MenuItem("Tools/LotteWorld/Rides (놀이기구 작동)/2 지우기")]
        static void Remove()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Rides] 플레이를 멈추고 실행해 줘"); return; }
            Undo.SetCurrentGroupName("놀이기구 작동 지우기");
            int grp = Undo.GetCurrentGroup(); int n = 0;
            foreach (var m in FindAll<RideMotor>())
            { RestoreStatic(m.transform); Undo.DestroyObjectImmediate(m); n++; }
            foreach (var f in FindAll<FlumePath>())
            { foreach (var b in f.boats) if (b) RestoreStatic(b); Undo.DestroyObjectImmediate(f); n++; }
            foreach (var r in FindAll<RideRoot>()) { Undo.DestroyObjectImmediate(r); n++; }
            foreach (var d in FindAll<RideDirector>()) { Undo.DestroyObjectImmediate(d.gameObject); n++; }
            Undo.CollapseUndoOperations(grp);
            for (int i = 0; i < SceneManager.sceneCount; i++) EditorSceneManager.MarkSceneDirty(SceneManager.GetSceneAt(i));
            Debug.Log("[Rides] 지움: " + n + "개 (움직이던 부품 Static 다시 켬)");
        }

        [MenuItem("Tools/LotteWorld/Rides (놀이기구 작동)/3 지금 켜기 (플레이 중)")]
        static void PlayNow() { RideDirector.ActivateAll(); }
        [MenuItem("Tools/LotteWorld/Rides (놀이기구 작동)/3 지금 켜기 (플레이 중)", true)]
        static bool PlayNowValid() { return EditorApplication.isPlaying; }

        [MenuItem("Tools/LotteWorld/Rides (놀이기구 작동)/4 되돌리기 (플레이 중)")]
        static void ResetNow() { RideDirector.ResetAll(); }
        [MenuItem("Tools/LotteWorld/Rides (놀이기구 작동)/4 되돌리기 (플레이 중)", true)]
        static bool ResetNowValid() { return EditorApplication.isPlaying; }

        // ---------------------------------------------------------------- helpers
        // 열린 씬 안의 컴포넌트 (꺼진 것 포함, 에셋·프리팹 원본 제외). Unity 버전 바뀌어도 안 깨지는 API만 씀
        static List<T> FindAll<T>() where T : Component
        {
            var r = new List<T>();
            foreach (var c in Resources.FindObjectsOfTypeAll<T>())
                if (c != null && c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded && !EditorUtility.IsPersistent(c)) r.Add(c);
            return r;
        }

        static GameObject FindInScenes(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (var r in sc.GetRootGameObjects())
                {
                    var t = FindDeep(r.transform, name);
                    if (t != null) return t.gameObject;
                }
            }
            return null;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
            return null;
        }

        // 움직이는 부품과 그 아래 전부 Static 끔 (배칭·라이트맵 Static 이면 안 움직이거나 그림자가 남음)
        static int ClearStatic(Transform t)
        {
            int n = 0;
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetStaticEditorFlags(c.gameObject) == 0) continue;
                Undo.RecordObject(c.gameObject, "Static 끄기");
                GameObjectUtility.SetStaticEditorFlags(c.gameObject, 0);
                n++;
            }
            return n;
        }

        static void RestoreStatic(Transform t)
        {
            var all = (StaticEditorFlags)95;   // 씬에 원래 있던 값 (GI·Occluder·Batching·Navigation·Occludee·OffMeshLink)
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(c.gameObject, "Static 켜기");
                GameObjectUtility.SetStaticEditorFlags(c.gameObject, all);
            }
        }

        static IEnumerable<Scene> TouchedScenes(IEnumerable<GameObject> gos, GameObject extra)
        {
            var set = new HashSet<Scene>();
            foreach (var g in gos) set.Add(g.scene);
            if (extra != null) set.Add(extra.scene);
            return set;
        }
    }
}
#endif
