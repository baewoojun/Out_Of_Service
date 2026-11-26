// RockSwap.cs - 일반 바위(Deco_Rock_* 프리팹)를 후룸라이드와 같은 스캔 바위로 바꿈
// 메뉴: Tools > LotteWorld > Look > 바위 교체 (열린 씬) / 바위 교체 되돌리기 (열린 씬)
// 새 바위 FBX(RocksScan/Rock_Scan_A~F)는 후룸라이드 재질(Static_boulder_*)을 그대로 씀 → 텍스처 추가 없음
// 옛 바위는 지우지 않고 끔. 새 바위는 씬 루트 "LotteWorld_RocksNew" 아래. 씬 저장은 하지 않음
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Look
{
    public static class RockSwap
    {
        const string Root = "Assets/Maps/LotteWorld/RocksScan";
        const string FlumeMats = "Assets/Maps/LotteWorld/Rides/Flume";
        const string NewRoot = "LotteWorld_RocksNew";
        static readonly string[] Names = { "Rock_Scan_A", "Rock_Scan_B", "Rock_Scan_C", "Rock_Scan_D", "Rock_Scan_E", "Rock_Scan_F" };
        static readonly string[] MatOf = { "Static_boulder_01", "Static_boulder_03", "Static_boulder_01.001", "Static_boulder_04", "Static_boulder_01", "Static_boulder_04" };
        static readonly Regex Suffix = new Regex(@"\s*\(\d+\)$");

        static bool Setup()
        {
            if (!AssetDatabase.IsValidFolder(Root)) { Debug.LogError("[Rocks] 폴더 없음: " + Root); return false; }
            if (!AssetDatabase.IsValidFolder(Root + "/Prefabs")) AssetDatabase.CreateFolder(Root, "Prefabs");
            var mats = new Dictionary<string, Material>();
            foreach (string g in AssetDatabase.FindAssets("Static_boulder t:Material", new[] { FlumeMats }))
            { var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)); if (m != null) mats[m.name] = m; }
            int made = 0;
            for (int i = 0; i < Names.Length; i++)
            {
                string p = Root + "/" + Names[i] + ".fbx";
                var mi = AssetImporter.GetAtPath(p) as ModelImporter; if (mi == null) { Debug.LogError("[Rocks] FBX 없음: " + p); continue; }
                if (!mi.bakeAxisConversion || mi.importAnimation) { mi.bakeAxisConversion = true; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.SaveAndReimport(); }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(p); if (model == null) continue;
                Material mat; if (!mats.TryGetValue(MatOf[i], out mat)) { Debug.LogError("[Rocks] 후룸 재질 못 찾음: " + MatOf[i]); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = Names[i];
                try
                {
                    foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
                    Transform l0 = go.transform.Find(Names[i] + "_LOD0"), l1 = go.transform.Find(Names[i] + "_LOD1");
                    if (l0 != null && l1 != null)
                    {
                        var lg = go.GetComponent<LODGroup>(); if (lg == null) lg = go.AddComponent<LODGroup>();
                        lg.fadeMode = LODFadeMode.CrossFade; lg.animateCrossFading = true;
                        lg.SetLODs(new[] { new LOD(0.15f, l0.GetComponentsInChildren<Renderer>()), new LOD(0.01f, l1.GetComponentsInChildren<Renderer>()) });
                        lg.RecalculateBounds();
                        MeshFilter mf = l1.GetComponentInChildren<MeshFilter>();
                        if (mf != null && mf.GetComponent<MeshCollider>() == null) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
                    }
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + Names[i] + ".prefab"); made++;
                }
                finally { Object.DestroyImmediate(go); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Rocks] 프리팹 " + made + "개 만듦 (후룸 재질 " + mats.Count + "개 찾음)");
            return made == Names.Length;
        }

        static bool IsOldRock(GameObject go)
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) return false;
            GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            string a = src != null ? src.name : "", b = Suffix.Replace(go.name, "");
            return a.StartsWith("Deco_Rock_") || b.StartsWith("Deco_Rock_");
        }

        [MenuItem("Tools/LotteWorld/Look/바위 교체 (열린 씬)")]
        public static void Swap()
        {
            if (!Setup()) { Debug.LogError("[Rocks] 설정 실패 → 교체 안 함"); return; }
            var prefabs = new GameObject[Names.Length]; var sizes = new float[Names.Length];
            for (int i = 0; i < Names.Length; i++) { prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + Names[i] + ".prefab"); sizes[i] = -1f; }
            int total = 0;
            for (int si = 0; si < SceneManager.sceneCount; si++)
            {
                Scene sc = SceneManager.GetSceneAt(si); if (!sc.isLoaded) continue;
                var olds = new List<GameObject>();
                foreach (GameObject r in sc.GetRootGameObjects())
                {
                    if (r.name == NewRoot) continue;
                    foreach (Transform t in r.GetComponentsInChildren<Transform>(false)) if (IsOldRock(t.gameObject)) olds.Add(t.gameObject);
                }
                if (olds.Count == 0) continue;
                Transform parent = null;
                foreach (GameObject r in sc.GetRootGameObjects()) if (r.name == NewRoot) parent = r.transform;
                if (parent == null) { var g = new GameObject(NewRoot); SceneManager.MoveGameObjectToScene(g, sc); Undo.RegisterCreatedObjectUndo(g, "Rocks root"); parent = g.transform; }
                foreach (GameObject old in olds)
                {
                    if (!old.activeInHierarchy) continue;
                    Bounds ob; if (!RendBounds(old.GetComponentsInChildren<Renderer>(false), out ob)) continue;
                    Vector3 pos = new Vector3(ob.center.x, ob.min.y, ob.center.z);
                    int hsh = Mathf.Abs(Mathf.RoundToInt(pos.x * 7.3f) * 73856093 ^ Mathf.RoundToInt(pos.z * 5.1f) * 19349663);
                    int k = hsh % Names.Length; if (prefabs[k] == null) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[k], sc);
                    inst.transform.SetParent(parent, false); inst.transform.position = pos;
                    Quaternion baseRot = prefabs[k].transform.rotation; Vector3 baseScale = prefabs[k].transform.localScale;      // FBX 루트 회전·스케일 유지
                    inst.transform.rotation = baseRot; inst.transform.localScale = baseScale;
                    if (sizes[k] < 0f) { Bounds nb; sizes[k] = RendBounds(inst.GetComponentsInChildren<Renderer>(true), out nb) ? nb.size.x + nb.size.y + nb.size.z : 6f; }
                    float s = Mathf.Clamp((ob.size.x + ob.size.y + ob.size.z) / sizes[k], 0.15f, 6f);
                    inst.transform.localScale = baseScale * s;
                    inst.transform.rotation = Quaternion.Euler(0f, hsh % 360, 0f) * baseRot;
                    // 바닥 맞춤: 돌린 뒤 실제 바닥이 옛 바위 바닥에 오게
                    Bounds fb; if (RendBounds(inst.GetComponentsInChildren<Renderer>(true), out fb)) inst.transform.position += new Vector3(ob.center.x - fb.center.x, ob.min.y - fb.min.y, ob.center.z - fb.center.z);
                    LODGroup lg = inst.GetComponent<LODGroup>(); if (lg != null) lg.RecalculateBounds();
                    Undo.RegisterCreatedObjectUndo(inst, "Rock swap");
                    Undo.RecordObject(old, "Rock swap"); old.SetActive(false); total++;
                }
                EditorSceneManager.MarkSceneDirty(sc);
            }
            Debug.Log("[Rocks] 바위 교체 " + total + "개 (씬 저장은 직접)");
        }

        [MenuItem("Tools/LotteWorld/Look/바위 교체 되돌리기 (열린 씬)")]
        public static void Revert()
        {
            int back = 0, removed = 0;
            for (int si = 0; si < SceneManager.sceneCount; si++)
            {
                Scene sc = SceneManager.GetSceneAt(si); if (!sc.isLoaded) continue;
                foreach (GameObject r in sc.GetRootGameObjects())
                {
                    if (r.name == NewRoot) { removed += r.transform.childCount; Undo.DestroyObjectImmediate(r); continue; }
                    foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                        if (!t.gameObject.activeSelf && IsOldRock(t.gameObject)) { Undo.RecordObject(t.gameObject, "Rock revert"); t.gameObject.SetActive(true); back++; }
                }
                EditorSceneManager.MarkSceneDirty(sc);
            }
            Debug.Log("[Rocks] 되돌림: 옛 바위 " + back + "개 다시 켬, 새 바위 " + removed + "개 지움");
        }

        static bool RendBounds(Renderer[] rs, out Bounds b)
        {
            b = new Bounds(); bool any = false;
            foreach (Renderer r in rs) { if (r == null || !r.enabled) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return any && b.size.y > 0.02f;
        }
    }
}
#endif
