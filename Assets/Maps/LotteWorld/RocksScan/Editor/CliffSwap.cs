// CliffSwap.cs - 상자 모양 바위벽(A15 뒤 절벽, 북문 폭포 바위)을 후룸풍 바위벽 모델로 바꾼다
// 메뉴: Tools > LotteWorld > Look > 바위벽 교체 (열린 씬) / 바위벽 교체 되돌리기 (열린 씬)
// - 옛 바위 오브젝트는 지우지 않고 끈다. 새 모델은 "LotteWorld_CliffNew" 아래. 씬 저장은 직접
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CliffSwap
{
    const string Dir = "Assets/Maps/LotteWorld/RocksScan/Cliff/";
    const string NewRoot = "LotteWorld_CliffNew";

    struct Job { public string model, rootHint; public string[] old; public float mPerUv; }
    static readonly Job[] Jobs =
    {
        new Job { model = "Cliff_A15", rootHint = "A15", mPerUv = 291.2f, old = new[] { "Static_LW_DarkRock", "Static_LW_RockLight" } },
        new Job { model = "Cliff_NorthGate", rootHint = "NorthGate", mPerUv = 115.57f, old = new[] { "Waterfall_LW_Rock", "Waterfall_LW_RockDark", "S_new1_LW_Rock", "S_new1_LW_RockDark" } },
    };

    const float DetailTileM = 9f;   // 잔무늬 한 장이 덮는 길이(m)

    static void AsNormal(string p)
    {
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
    }

    // 바탕(구운 큰 색 얼룩 + 틈 그늘) + 구운 노멀 + 반복 잔무늬(디테일 맵)
    static Material Mat(Job job)
    {
        string mp = Dir + "LW_" + job.model + ".mat";
        AsNormal(Dir + job.model + "_Nrm.png"); AsNormal(Dir + "LW_CliffDetail_Nrm.png");
        var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, mp); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + job.model + "_BaseColor.png"));
        m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + job.model + "_Nrm.png"));
        m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_BumpScale", 1f);
        float k = job.mPerUv / DetailTileM;
        m.SetTexture("_DetailAlbedoMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "LW_CliffDetail_Albedo.png"));
        m.SetTexture("_DetailNormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "LW_CliffDetail_Nrm.png"));
        m.SetTextureScale("_DetailAlbedoMap", new Vector2(k, k));
        m.SetTextureScale("_DetailNormalMap", new Vector2(k, k));
        m.SetFloat("_DetailAlbedoMapScale", 1f);
        m.SetFloat("_DetailNormalMapScale", 0.7f);
        m.EnableKeyword("_DETAIL_MULX2"); m.EnableKeyword("_DETAIL_SCALED");
        m.SetFloat("_Smoothness", 0.08f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform FindModelRoot(Transform t, string hint)
    {
        var near = PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
        if (near != null && near.name.Contains(hint)) return near.transform;
        Transform best = null;
        for (var p = t; p != null; p = p.parent) if (p.name.Contains(hint)) best = p;
        return best;
    }

    [MenuItem("Tools/LotteWorld/Look/바위벽 교체 (열린 씬)", false, 205)]
    static void Swap()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var log = new System.Text.StringBuilder("[CliffSwap] ");
        foreach (var job in Jobs)
        {
            var mat = Mat(job);
            string path = Dir + job.model + ".fbx";
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { log.Append(job.model + ": FBX 없음 / "); continue; }
            if (!imp.bakeAxisConversion) { imp.bakeAxisConversion = true; imp.SaveAndReimport(); }
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var olds = new List<Transform>();
            var names = new HashSet<string>(job.old);
            foreach (var t in all)
                if (names.Contains(t.name) && FindModelRoot(t, job.rootHint) != null && FindModelRoot(t, NewRoot) == null) olds.Add(t);
            if (olds.Count == 0) { log.Append(job.model + ": 옛 바위를 못 찾음 / "); continue; }

            // 옛 모델 루트별로 하나씩
            var byRoot = new Dictionary<Transform, List<Transform>>();
            foreach (var o in olds)
            {
                var r = FindModelRoot(o, job.rootHint);
                if (!byRoot.TryGetValue(r, out var l)) byRoot[r] = l = new List<Transform>();
                l.Add(o);
            }
            int made = 0, off = 0;
            foreach (var kv in byRoot)
            {
                var oldRoot = kv.Key;
                var scene = oldRoot.gameObject.scene;
                GameObject holder = null;
                foreach (var g in scene.GetRootGameObjects()) if (g.name == NewRoot) holder = g;
                if (holder == null) { holder = new GameObject(NewRoot); EditorSceneManager.MoveGameObjectToScene(holder, scene); Undo.RegisterCreatedObjectUndo(holder, "CliffSwap"); }
                string instName = job.model + "__" + oldRoot.name;
                if (holder.transform.Find(instName) != null) continue;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                Undo.RegisterCreatedObjectUndo(inst, "CliffSwap");
                inst.name = instName;
                // 옛 모델 루트의 "에셋 기준" 자세와 견주어 새 모델을 같은 자리에
                var oldAsset = PrefabUtility.GetCorrespondingObjectFromSource(oldRoot.gameObject);
                Quaternion oldBase = oldAsset != null ? oldAsset.transform.rotation : Quaternion.identity;
                Vector3 oldBaseScale = oldAsset != null ? oldAsset.transform.localScale : Vector3.one;
                Quaternion rel = oldRoot.rotation * Quaternion.Inverse(oldBase);
                Vector3 s = oldRoot.lossyScale; float k = oldBaseScale.x != 0 ? s.x / oldBaseScale.x : 1f;
                inst.transform.SetParent(holder.transform, true);
                inst.transform.rotation = rel * asset.transform.rotation;
                inst.transform.localScale = asset.transform.localScale * k;
                inst.transform.position = oldRoot.position;

                var flags = GameObjectUtility.GetStaticEditorFlags(kv.Value[0].gameObject);
                Renderer r0 = null, r1 = null;
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.sharedMaterial = mat;
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                    r.gameObject.layer = kv.Value[0].gameObject.layer;
                    if (r.name.Contains("_LOD0")) r0 = r; else if (r.name.Contains("_LOD1")) r1 = r;
                }
                var lg = inst.GetComponent<LODGroup>(); if (lg == null) lg = inst.AddComponent<LODGroup>();
                if (r0 != null && r1 != null)
                {
                    lg.SetLODs(new[] { new LOD(0.55f, new[] { r0 }), new LOD(0.0f, new[] { r1 }) });
                    lg.RecalculateBounds();
                    var mc = r1.gameObject.GetComponent<MeshCollider>(); if (mc == null) mc = r1.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = r1.GetComponent<MeshFilter>().sharedMesh;
                }
                foreach (var o in kv.Value) if (o.gameObject.activeSelf) { Undo.RecordObject(o.gameObject, "CliffSwap"); o.gameObject.SetActive(false); off++; }
                EditorSceneManager.MarkSceneDirty(scene);
                made++;
            }
            log.Append(job.model + ": 새 모델 " + made + "개, 옛 바위 " + off + "개 끔 / ");
        }
        AssetDatabase.SaveAssets();
        Debug.Log(log.Append("씬 저장은 직접 하세요."));
    }

    [MenuItem("Tools/LotteWorld/Look/바위벽 교체 되돌리기 (열린 씬)", false, 206)]
    static void Revert()
    {
        int on = 0, del = 0;
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var job in Jobs)
        {
            var names = new HashSet<string>(job.old);
            foreach (var t in all)
                if (t != null && names.Contains(t.name) && FindModelRoot(t, job.rootHint) != null && !t.gameObject.activeSelf)
                { Undo.RecordObject(t.gameObject, "CliffSwap"); t.gameObject.SetActive(true); EditorSceneManager.MarkSceneDirty(t.gameObject.scene); on++; }
        }
        foreach (var t in all)
            if (t != null && t.name == NewRoot && t.parent == null) { var sc = t.gameObject.scene; Undo.DestroyObjectImmediate(t.gameObject); EditorSceneManager.MarkSceneDirty(sc); del++; }
        Debug.Log("[CliffSwap] 되돌림: 옛 바위 " + on + "개 다시 켬, 새 묶음 " + del + "개 지움. 씬 저장은 직접 하세요.");
    }
}
#endif
