// LotteWorldDayLook.cs - 롯데월드 맵 "낮" 화면을 한 톤으로 묶는 도구 (후처리 + 실내 반사)
// 메뉴: Tools > LotteWorld > Look > 낮 조명·후처리 붙이기 / 실내 반사 다시 굽기 / 낮 조명·후처리 떼기
// - 열려 있는 맵 씬(Map3_LotteWorld, Map3_MagicIsland)마다 루트 오브젝트 "LW_DayLook" 를 만든다
//     LW_DayVolume        : 전역 후처리 볼륨 (톤매핑·블룸·색 보정). 프로필은 Look/LW_DayProfile.asset 한 장을 같이 쓴다
//     LW_DayProbe_Indoor  : 실내 씬에만. 건물 안을 찍은 반사 큐브맵(Look/Probes/)을 금속·광택 재질이 비추게 한다
// - 밤에는 LW_DayLook 을 통째로 끈다. 낮·밤 전환 도구(LotteWorldNight)가 SetForTod 를 부른다
// - 화면 값은 LW_DayProfile.asset 을 인스펙터에서 직접 고치면 된다. [붙이기]를 다시 눌러도 이미 있는 프로필 값은 덮어쓰지 않는다
// - 씬 저장은 직접. Undo 됨
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LotteWorld.Look
{
    public static class LotteWorldDayLook
    {
        public const string RootName = "LW_DayLook";
        const string VolumeName = "LW_DayVolume";
        const string ProbeName = "LW_DayProbe_Indoor";
        const string LookDir = "Assets/Maps/LotteWorld/Look";
        const string ProbeDir = LookDir + "/Probes";
        const string ProfilePath = LookDir + "/LW_DayProfile.asset";
        const string ProbeTexPath = ProbeDir + "/LW_DayProbe_Indoor.exr";
        const string IndoorScene = "Map3_LotteWorld";
        const string OutdoorScene = "Map3_MagicIsland";
        const string ShellName = "LotteWorld_Shell";
        const int ProbeResolution = 256;

        [MenuItem("Tools/LotteWorld/Look/낮 조명·후처리 붙이기", false, 20)]
        static void ApplyMenu() => Debug.Log(Apply());

        [MenuItem("Tools/LotteWorld/Look/실내 반사 다시 굽기", false, 21)]
        static void RebakeMenu() => Debug.Log(RebakeIndoorProbe());

        [MenuItem("Tools/LotteWorld/Look/낮 조명·후처리 떼기", false, 22)]
        static void RemoveMenu() => Debug.Log(Remove());

        // ------------------------------------------------------------ 붙이기

        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "[LW DayLook] Play 모드에서는 쓸 수 없습니다.";
            if (IsNight()) return "[LW DayLook] 지금은 밤 상태입니다. 낮으로 전환한 뒤 다시 실행하세요 (반사를 낮 조명으로 구워야 합니다).";

            var scenes = MapScenes();
            if (scenes.Count == 0) return "[LW DayLook] 맵 씬(" + IndoorScene + " / " + OutdoorScene + ")이 열려 있지 않습니다.";

            var profile = LoadOrCreateProfile();
            var log = new System.Text.StringBuilder("[LW DayLook] ");
            foreach (var scene in scenes)
            {
                var root = FindRoot(scene);
                if (root == null)
                {
                    root = new GameObject(RootName);
                    SceneManager.MoveGameObjectToScene(root, scene);
                    Undo.RegisterCreatedObjectUndo(root, "LW DayLook");
                }
                root.SetActive(true);

                var volT = root.transform.Find(VolumeName);
                var volGo = volT != null ? volT.gameObject : NewChild(root, VolumeName);
                var vol = volGo.GetComponent<Volume>();
                if (vol == null) vol = Undo.AddComponent<Volume>(volGo);
                vol.isGlobal = true;
                vol.priority = 0f;      // 밤 볼륨(100)보다 낮게
                vol.weight = 1f;
                vol.sharedProfile = profile;
                EditorUtility.SetDirty(vol);

                int cams = EnablePostOnCameras(scene);
                log.Append(scene.name).Append(": 볼륨 붙임, 카메라 ").Append(cams).Append("대 후처리 켬");

                if (scene.name == IndoorScene) log.Append(", ").Append(SetupIndoorProbe(scene, root));
                log.Append(" / ");
                EditorSceneManager.MarkSceneDirty(scene);
            }
            AssetDatabase.SaveAssets();
            log.Append("씬 저장은 직접 하세요.");
            return log.ToString();
        }

        public static string RebakeIndoorProbe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "[LW DayLook] Play 모드에서는 쓸 수 없습니다.";
            if (IsNight()) return "[LW DayLook] 지금은 밤 상태입니다. 낮으로 전환한 뒤 다시 구우세요.";
            foreach (var scene in MapScenes())
            {
                if (scene.name != IndoorScene) continue;
                var root = FindRoot(scene);
                var t = root != null ? root.transform.Find(ProbeName) : null;
                var probe = t != null ? t.GetComponent<ReflectionProbe>() : null;
                if (probe == null) return "[LW DayLook] 실내 반사 프로브가 없습니다. 먼저 [낮 조명·후처리 붙이기]를 실행하세요.";
                return "[LW DayLook] " + Bake(probe);
            }
            return "[LW DayLook] " + IndoorScene + " 씬이 열려 있지 않습니다.";
        }

        public static string Remove()
        {
            int n = 0;
            foreach (var scene in MapScenes())
            {
                var root = FindRoot(scene);
                if (root == null) continue;
                Undo.DestroyObjectImmediate(root);
                EditorSceneManager.MarkSceneDirty(scene);
                n++;
            }
            return "[LW DayLook] 씬 " + n + "곳에서 뗌 (프로필·큐브맵 에셋은 남겨 둠). 씬 저장은 직접 하세요.";
        }

        /// <summary>낮·밤 전환 도구가 부른다. 밤이면 LW_DayLook 을 끄고 낮이면 켠다.</summary>
        public static void SetForTod(bool isNight, HashSet<Scene> dirtyScenes)
        {
            foreach (var scene in MapScenes())
            {
                var root = FindRoot(scene);
                if (root == null || root.activeSelf == !isNight) continue;
                Undo.RecordObject(root, "LW DayLook 낮·밤");
                root.SetActive(!isNight);
                EditorUtility.SetDirty(root);
                if (dirtyScenes != null) dirtyScenes.Add(scene);
            }
        }

        // ------------------------------------------------------------ 후처리 프로필

        static VolumeProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;   // 손으로 고친 값을 지키려고 이미 있으면 건드리지 않는다

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            // 놀이공원 원색을 살리려고 Neutral (밤은 ACES)
            var tone = Add<Tonemapping>(profile);
            tone.mode.overrideState = true; tone.mode.value = TonemappingMode.Neutral;

            // 전구·등 같은 발광 재질만 번지게 (임계값 1 = 일반 표면은 안 번짐)
            var bloom = Add<Bloom>(profile);
            bloom.threshold.overrideState = true; bloom.threshold.value = 1f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.25f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.65f;

            var color = Add<ColorAdjustments>(profile);
            color.postExposure.overrideState = true; color.postExposure.value = 0f;
            color.contrast.overrideState = true; color.contrast.value = 8f;
            color.saturation.overrideState = true; color.saturation.value = 5f;

            var vig = Add<Vignette>(profile);
            vig.intensity.overrideState = true; vig.intensity.value = 0.18f;
            vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(false);
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        static int EnablePostOnCameras(Scene scene)
        {
            int n = 0;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var cam in go.GetComponentsInChildren<Camera>(true))
                {
                    var uad = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (uad == null) uad = Undo.AddComponent<UniversalAdditionalCameraData>(cam.gameObject);
                    if (uad.renderType != CameraRenderType.Base) continue;
                    n++;
                    if (uad.renderPostProcessing) continue;
                    Undo.RecordObject(uad, "LW DayLook 카메라 후처리");
                    uad.renderPostProcessing = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(uad);
                    EditorUtility.SetDirty(uad);
                }
            return n;
        }

        // ------------------------------------------------------------ 실내 반사 프로브

        static string SetupIndoorProbe(Scene scene, GameObject root)
        {
            GameObject shell = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == ShellName) { shell = go; break; }
            if (shell == null) return "반사 프로브 건너뜀(" + ShellName + " 없음)";

            Bounds all = default, floor = default; bool hasAll = false, hasFloor = false;
            foreach (var r in shell.GetComponentsInChildren<Renderer>(false))
            {
                if (!hasAll) { all = r.bounds; hasAll = true; } else all.Encapsulate(r.bounds);
                if (r.name.StartsWith("Floor_"))
                {
                    if (!hasFloor) { floor = r.bounds; hasFloor = true; } else floor.Encapsulate(r.bounds);
                }
            }
            if (!hasAll) return "반사 프로브 건너뜀(껍데기에 켜진 렌더러 없음)";

            var t = root.transform.Find(ProbeName);
            var go2 = t != null ? t.gameObject : NewChild(root, ProbeName);
            var probe = go2.GetComponent<ReflectionProbe>();
            if (probe == null) probe = Undo.AddComponent<ReflectionProbe>(go2);

            // 찍는 자리: 건물 한가운데, 1층 바닥 위 4 m. 놀이기구 속이면 옆으로 옮겨 가며 빈 곳을 찾는다
            float y = (hasFloor ? floor.max.y : all.min.y) + 4f;
            var pos = new Vector3(all.center.x, y, all.center.z);
            if (t == null) pos = FindClearSpot(pos, Mathf.Min(all.extents.x, all.extents.z) * 0.5f);
            else pos = go2.transform.position;   // 손으로 옮긴 자리는 지킨다
            go2.transform.SetPositionAndRotation(pos, Quaternion.identity);

            probe.mode = ReflectionProbeMode.Custom;
            probe.renderDynamicObjects = true;   // 정적 표시가 없는 놀이기구도 같이 찍는다
            probe.resolution = ProbeResolution;
            probe.hdr = true;
            probe.boxProjection = true;
            probe.size = all.size;
            probe.center = all.center - pos;     // 상자는 건물 전체, 찍는 자리만 가운데 아래쪽
            probe.blendDistance = 0f;
            probe.importance = 1;
            probe.intensity = 1f;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = Mathf.Max(all.size.x, all.size.z) * 1.5f;
            probe.shadowDistance = 0f;
            EditorUtility.SetDirty(probe);

            return Bake(probe);
        }

        static Vector3 FindClearSpot(Vector3 start, float maxRadius)
        {
            if (!Physics.CheckSphere(start, 1.5f)) return start;
            for (float r = 4f; r <= maxRadius; r += 4f)
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f;
                    var p = start + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    if (!Physics.CheckSphere(p, 1.5f)) return p;
                }
            return start;
        }

        static string Bake(ReflectionProbe probe)
        {
            if (!AssetDatabase.IsValidFolder(ProbeDir)) AssetDatabase.CreateFolder(LookDir, "Probes");
            if (!Lightmapping.BakeReflectionProbe(probe, ProbeTexPath))
                return "반사 큐브맵 굽기 실패 (" + ProbeTexPath + ")";
            AssetDatabase.ImportAsset(ProbeTexPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(ProbeTexPath);
            if (tex == null) return "반사 큐브맵을 불러오지 못함 (" + ProbeTexPath + ")";
            Undo.RecordObject(probe, "LW DayLook 반사 굽기");
            probe.customBakedTexture = tex;
            EditorUtility.SetDirty(probe);
            EditorSceneManager.MarkSceneDirty(probe.gameObject.scene);
            var p = probe.transform.position;
            return string.Format("실내 반사 구움 (자리 {0:0.0}, {1:0.0}, {2:0.0} / 상자 {3:0} x {4:0} x {5:0} m / {6}px)",
                p.x, p.y, p.z, probe.size.x, probe.size.y, probe.size.z, ProbeResolution);
        }

        // ------------------------------------------------------------ 공통

        static bool IsNight()
        {
            var tod = LotteWorld.Night.LotteWorldNight.GetTodState();
            return tod != null && tod.isNight;
        }

        static List<Scene> MapScenes()
        {
            var list = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.IsValid() && s.isLoaded && (s.name == IndoorScene || s.name == OutdoorScene)) list.Add(s);
            }
            return list;
        }

        static GameObject FindRoot(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == RootName) return go;
            return null;
        }

        static GameObject NewChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "LW DayLook");
            return go;
        }
    }
}
#endif
