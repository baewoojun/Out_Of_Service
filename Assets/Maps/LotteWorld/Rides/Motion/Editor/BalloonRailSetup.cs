// BalloonRailSetup.cs - 메뉴 Tools > LotteWorld > Balloon Rail (열기구 레일)
//  1 설정하기 : 레일 메시 BalloonRail_Track.fbx 를 월드 원점에 놓고(Track), BalloonRail_<색>.fbx 6종을 레일 중심선에
//               같은 간격으로 count개(색 차례로) 걸고, BalloonRail 컴포넌트 붙임. 다시 누르면 다시 만듦(개수 유지)
//  2 지우기   : 'BalloonRail' 루트 삭제
//  3 지금 출발 (플레이 중) : 열쇠 없이 바로 움직이기 테스트
// 11/20 v3: 옛 생성기(Balloon Rail (Ceiling)) 대체. Unity 6.6 에서 막힌 FindObjectsByType 대신 Resources.FindObjectsOfTypeAll + 씬 필터.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LotteWorld.Rides.EditorTools
{
    public static class BalloonRailSetup
    {
        const string Root = "BalloonRail";
        static readonly string[] Colors = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple" };
        const int DefaultCount = 24;

        static GameObject LoadModel(string name)
        {
            var guid = AssetDatabase.FindAssets(name + " t:Model").FirstOrDefault(g => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == name);
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        [MenuItem("Tools/LotteWorld/Balloon Rail (열기구 레일)/1 설정하기", false, 1)]
        static void Setup()
        {
            var models = new List<GameObject>();
            foreach (var c in Colors) { var go = LoadModel("BalloonRail_" + c); if (go) models.Add(go); }
            var track = LoadModel("BalloonRail_Track");
            if (models.Count == 0)
            {
                EditorUtility.DisplayDialog("Balloon Rail", "BalloonRail_<색>.fbx 를 못 찾았어요.\nRides/Balloon 폴더가 있는지 확인하고 Setup Ride Materials 를 먼저 실행하세요.", "확인");
                return;
            }

            var rootGo = GameObject.Find(Root);
            int count = DefaultCount;
            if (rootGo == null) { rootGo = new GameObject(Root); Undo.RegisterCreatedObjectUndo(rootGo, "Balloon Rail"); }
            else
            {
                var old = rootGo.GetComponent<BalloonRail>();
                if (old && old.cars.Count > 0) count = old.cars.Count;
                for (int i = rootGo.transform.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(rootGo.transform.GetChild(i).gameObject);
            }
            rootGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); rootGo.transform.localScale = Vector3.one;
            var rail = rootGo.GetComponent<BalloonRail>();
            if (rail == null) rail = Undo.AddComponent<BalloonRail>(rootGo);
            Undo.RecordObject(rail, "Balloon Rail");
            rail.cars.Clear(); rail.BuildPath();
            float L = rail.Length;

            // 레일 메시 (월드 좌표로 구움 -> 원점에 그대로)
            string trackMsg = "레일 메시 없음 (BalloonRail_Track.fbx)";
            if (track)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(track, rootGo.transform);
                t.name = "Track"; t.transform.localPosition = Vector3.zero; t.transform.localRotation = track.transform.localRotation;
                Undo.RegisterCreatedObjectUndo(t, "Balloon Rail");
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    trackMsg = string.Format("레일 메시 크기 {0:0} x {1:0.0} x {2:0} m, 높이 {3:0.0}~{4:0.0} m (52~58 이어야 함)", b.size.x, b.size.y, b.size.z, b.min.y, b.max.y);
                }
            }

            // 프리팹의 진행 방향 축: 대차(Static 그룹)의 긴 쪽 = 블렌더 +Y. Unity 에서 X 로 들어왔으면 90° 돌려 +Z 로 맞춤
            Quaternion fix = Quaternion.identity; float height = 0f;
            {
                var probe = (GameObject)PrefabUtility.InstantiatePrefab(models[0]);
                probe.transform.SetPositionAndRotation(Vector3.zero, models[0].transform.rotation);
                var stat = FindDeep(probe.transform, "Static");
                var rs = (stat ? stat : probe.transform).GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    if (b.size.x > b.size.z * 1.3f) fix = Quaternion.Euler(0f, 90f, 0f);
                }
                var all = probe.GetComponentsInChildren<Renderer>();
                if (all.Length > 0) { var b = all[0].bounds; foreach (var r in all) b.Encapsulate(r.bounds); height = b.size.y; }
                Object.DestroyImmediate(probe);
            }

            for (int i = 0; i < count; i++)
            {
                float d = L * i / count;
                var car = new GameObject(string.Format("Car_{0:00}", i + 1));
                Undo.RegisterCreatedObjectUndo(car, "Balloon Rail");
                car.transform.SetParent(rootGo.transform, false);
                car.transform.SetPositionAndRotation(rootGo.transform.TransformPoint(rail.PointAt(d)), rail.FrameAt(d));
                var m = models[i % models.Count];
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(m, car.transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = fix * m.transform.localRotation;     // 프리팹 회전 값은 유지
                foreach (var t in inst.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);   // 움직이니까 Static 끔
                car.isStatic = false;
                rail.cars.Add(car.transform);
            }
            EditorUtility.SetDirty(rail);
            EditorSceneManager.MarkSceneDirty(rootGo.scene);
            Selection.activeGameObject = rootGo;
            Debug.Log(string.Format("[BalloonRail] 설정 완료: 열기구 {0}개 (색 {1}종), 레일 {2:0} m (793 이어야 함), 간격 {3:0.0} m, 열기구 높이 {4:0.0} m (13.0 이어야 함){5}, 진행축 보정 {6}\n{7}",
                count, models.Count, L, L / count, height, Mathf.Abs(height - 13.03f) > 0.5f ? "  ← 크기 다름! FBX 스케일 확인" : "", fix == Quaternion.identity ? "없음" : "Y 90°", trackMsg));
            var col = GameObject.Find("LW_Shell_Column (9)");
            if (col && (new Vector2(col.transform.position.x, col.transform.position.z) - new Vector2(-82.12f, 83.51f)).magnitude < 1f)
                Debug.LogWarning("[BalloonRail] 기둥 LW_Shell_Column (9) 가 아직 (-82.1, 83.5) 에 있음 → (-76, 72) 로 옮겨야 열기구가 안 부딪힘");
        }

        [MenuItem("Tools/LotteWorld/Balloon Rail (열기구 레일)/2 지우기", false, 2)]
        static void Remove()
        {
            var go = GameObject.Find(Root);
            if (go) { Undo.DestroyObjectImmediate(go); Debug.Log("[BalloonRail] 지움"); }
        }

        [MenuItem("Tools/LotteWorld/Balloon Rail (열기구 레일)/3 지금 출발 (플레이 중)", false, 3)]
        static void StartNow()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[BalloonRail] 플레이 중에만"); return; }
            int n = 0;
            foreach (var r in Resources.FindObjectsOfTypeAll<BalloonRail>())
                if (r.gameObject.scene.IsValid() && r.gameObject.scene.isLoaded) { r.Activate(); n++; }
            Debug.Log("[BalloonRail] 지금 출발: " + n + "개");
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r) return r; }
            return null;
        }
    }
}
