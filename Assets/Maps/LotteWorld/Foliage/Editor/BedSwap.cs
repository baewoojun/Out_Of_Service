// BedSwap.cs - 화단(Deco_Bed_*, Deco_RoundBed)을 새 화단 모델로 바꾼다 (경계석 낱돌 + 흙 + 잎·꽃 카드)
// 메뉴: Tools > LotteWorld > Look > 화단 교체 (열린 씬) / 화단 교체 되돌리기 (열린 씬)
// - 옛 화단은 지우지 않고 끈다. 새 화단은 "LotteWorld_BedsNew" 아래. 재질은 Foliage/Materials/NT_*.mat 재사용. 씬 저장은 직접
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BedSwap
{
    const string Fbx = "Assets/Maps/LotteWorld/Foliage/Beds/LotteWorld_DecoBeds.fbx";
    const string MatDir = "Assets/Maps/LotteWorld/Foliage/Materials/";
    const string NewRoot = "LotteWorld_BedsNew";
    static readonly Regex OldName = new Regex(@"^(Deco_Bed_\d+_(flower|hedge|wild)|Deco_RoundBed)$");

    static bool UnderNew(Transform t) { for (var p = t; p != null; p = p.parent) if (p.name == NewRoot) return true; return false; }

    static GameObject Prepare()
    {
        var imp = AssetImporter.GetAtPath(Fbx) as ModelImporter;
        if (imp == null) return null;
        bool dirty = false;
        if (!imp.bakeAxisConversion) { imp.bakeAxisConversion = true; dirty = true; }
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var have = imp.GetExternalObjectMap();
        var names = new HashSet<string>();
        foreach (var r in asset.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) if (m != null) names.Add(m.name);
        foreach (var n in names)
        {
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), n);
            if (have.ContainsKey(id)) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + n + ".mat");
            if (m != null) { imp.AddRemap(id, m); dirty = true; }
        }
        if (dirty) { imp.SaveAndReimport(); asset = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx); }
        return asset;
    }

    [MenuItem("Tools/LotteWorld/Look/화단 교체 (열린 씬)", false, 207)]
    static void Swap()
    {
        var asset = Prepare();
        if (asset == null) { Debug.LogError("[BedSwap] " + Fbx + " 없음"); return; }
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int made = 0, miss = 0; var missing = new List<string>();
        foreach (var old in all)
        {
            if (old == null || !OldName.IsMatch(old.name) || UnderNew(old) || !old.gameObject.activeSelf) continue;
            if (old.GetComponent<MeshRenderer>() == null && old.GetComponentInChildren<MeshRenderer>() == null) continue;
            var src = asset.transform.Find(old.name);
            if (src == null) { miss++; missing.Add(old.name); continue; }
            var scene = old.gameObject.scene;
            GameObject holder = null;
            foreach (var g in scene.GetRootGameObjects()) if (g.name == NewRoot) holder = g;
            if (holder == null) { holder = new GameObject(NewRoot); EditorSceneManager.MoveGameObjectToScene(holder, scene); Undo.RegisterCreatedObjectUndo(holder, "BedSwap"); }

            var inst = Object.Instantiate(src.gameObject);
            Undo.RegisterCreatedObjectUndo(inst, "BedSwap");
            inst.name = old.name + "__New";
            // 에셋 기준 자세 차이만큼 보정해서 옛 화단 자리에
            var oldSrc = PrefabUtility.GetCorrespondingObjectFromOriginalSource(old.gameObject);
            Quaternion oldBase = oldSrc != null ? oldSrc.transform.rotation : Quaternion.identity;
            float oldBaseScale = oldSrc != null ? oldSrc.transform.lossyScale.x : 1f;
            inst.transform.SetParent(holder.transform, false);
            inst.transform.rotation = old.rotation * Quaternion.Inverse(oldBase) * src.rotation;
            inst.transform.localScale = src.lossyScale * (oldBaseScale != 0 ? old.lossyScale.x / oldBaseScale : 1f);
            inst.transform.position = old.position;

            var flags = GameObjectUtility.GetStaticEditorFlags(old.gameObject);
            var plants = new List<Renderer>();
            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                r.gameObject.layer = old.gameObject.layer;
                if (r.name.EndsWith("_Pit"))
                {
                    var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
                }
                else { plants.Add(r); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            }
            if (plants.Count > 0)
            {   // 풀·꽃만 멀리서 안 그림 (경계석·흙은 항상 보임)
                var lg = inst.AddComponent<LODGroup>();
                lg.SetLODs(new[] { new LOD(0.025f, plants.ToArray()) });
                lg.RecalculateBounds();
            }
            Undo.RecordObject(old.gameObject, "BedSwap"); old.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            made++;
        }
        Debug.Log("[BedSwap] 화단 " + made + "개 교체" + (miss > 0 ? ", 새 모델에 없는 이름 " + miss + "개 (" + string.Join(",", missing) + ")" : "") + ". 씬 저장은 직접 하세요.");
    }

    [MenuItem("Tools/LotteWorld/Look/화단 교체 되돌리기 (열린 씬)", false, 208)]
    static void Revert()
    {
        int on = 0, del = 0;
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var holders = new List<GameObject>();
        foreach (var t in all)
        {
            if (t == null) continue;
            if (t.name == NewRoot && t.parent == null) { holders.Add(t.gameObject); continue; }
            if (OldName.IsMatch(t.name) && !UnderNew(t) && !t.gameObject.activeSelf)
            { Undo.RecordObject(t.gameObject, "BedSwap"); t.gameObject.SetActive(true); EditorSceneManager.MarkSceneDirty(t.gameObject.scene); on++; }
        }
        foreach (var h in holders) { var sc = h.scene; Undo.DestroyObjectImmediate(h); EditorSceneManager.MarkSceneDirty(sc); del++; }
        Debug.Log("[BedSwap] 되돌림: 옛 화단 " + on + "개 다시 켬, 새 묶음 " + del + "개 지움. 씬 저장은 직접 하세요.");
    }
}
#endif
