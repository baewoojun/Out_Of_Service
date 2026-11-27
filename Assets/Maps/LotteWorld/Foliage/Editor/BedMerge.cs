// BedMerge.cs - 새 화단(LotteWorld_BedsNew 아래)의 "풀·꽃"과 "경계석·흙"을 오브젝트 하나로 합친다
// 메뉴: Tools > LotteWorld > Look > 화단 하나로 합치기 (열린 씬)
// - 화단 교체(BedSwap) 뒤에 실행. 화단마다 자식이 둘(…_Plants, …_Plants_Pit)이라 씬에서 클릭하면 한쪽만 잡혀 따로 움직이던 것을 막는다
// - 합친 오브젝트 하나 = 메시 하나(재질별 서브메시) + 충돌체(경계석·흙 메시 그대로). 이름·순서·정적 표시·레이어는 그대로
// - 자리 기준은 경계석·흙이다. 한쪽만 옮겨 놓은 화단은 경계석·흙이 있는 곳에 꽃을 다시 맞춘다
// - 풀·꽃만 멀리서 안 그리던 LOD 는 없어진다 (화단 23개 기준 부담이 작다)
// - 합친 메시는 Foliage/Beds/Merged/ 에 저장. 씬 저장은 직접. Undo 됨
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BedMerge
{
    const string NewRoot = "LotteWorld_BedsNew";
    const string MeshDir = "Assets/Maps/LotteWorld/Foliage/Beds/Merged";

    [MenuItem("Tools/LotteWorld/Look/화단 하나로 합치기 (열린 씬)", false, 209)]
    static void MergeMenu() => Debug.Log(Merge());

    public static string Merge()
    {
        if (!AssetDatabase.IsValidFolder(MeshDir)) AssetDatabase.CreateFolder("Assets/Maps/LotteWorld/Foliage/Beds", "Merged");
        int merged = 0, skipped = 0, refit = 0;
        for (int si = 0; si < SceneManager.sceneCount; si++)
        {
            var scene = SceneManager.GetSceneAt(si); if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != NewRoot) continue;
                var beds = new List<Transform>(); foreach (Transform t in root.transform) beds.Add(t);
                foreach (var bed in beds)
                {
                    MeshFilter pit = null, plants = null;
                    foreach (var mf in bed.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.transform == bed) continue;
                        if (mf.name.EndsWith("_Pit")) pit = mf; else plants = mf;
                    }
                    if (pit == null || plants == null || pit.sharedMesh == null || plants.sharedMesh == null) { skipped++; continue; }   // 이미 합쳤거나 모양이 다름

                    // 꽃이 경계석 기준으로 있어야 할 자리 = 원본 모델에서의 상대 자리
                    Matrix4x4 rel = pit.transform.worldToLocalMatrix * plants.transform.localToWorldMatrix;
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(pit.sharedMesh));
                    if (asset != null)
                    {
                        Transform sp = null, sq = null;
                        foreach (var mf in asset.GetComponentsInChildren<MeshFilter>(true)) { if (mf.sharedMesh == pit.sharedMesh) sq = mf.transform; if (mf.sharedMesh == plants.sharedMesh) sp = mf.transform; }
                        if (sp != null && sq != null)
                        {
                            var srcRel = sq.worldToLocalMatrix * sp.localToWorldMatrix;
                            if (!Approximately(rel, srcRel)) refit++;
                            rel = srcRel;
                        }
                    }

                    var pr = pit.GetComponent<MeshRenderer>(); var lr = plants.GetComponent<MeshRenderer>();
                    var combine = new List<CombineInstance>(); var mats = new List<Material>();
                    for (int s = 0; s < pit.sharedMesh.subMeshCount; s++) { combine.Add(new CombineInstance { mesh = pit.sharedMesh, subMeshIndex = s, transform = Matrix4x4.identity }); mats.Add(s < pr.sharedMaterials.Length ? pr.sharedMaterials[s] : null); }
                    for (int s = 0; s < plants.sharedMesh.subMeshCount; s++) { combine.Add(new CombineInstance { mesh = plants.sharedMesh, subMeshIndex = s, transform = rel }); mats.Add(s < lr.sharedMaterials.Length ? lr.sharedMaterials[s] : null); }

                    string meshPath = MeshDir + "/" + bed.name.Replace("__New", "") + "_Merged.asset";
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath); bool isNew = mesh == null;
                    if (isNew) mesh = new Mesh();
                    mesh.Clear(); mesh.name = bed.name.Replace("__New", "") + "_Merged";
                    int verts = 0; foreach (var c in combine) verts += c.mesh.vertexCount;
                    mesh.indexFormat = verts > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;   // 큰 생울타리 화단은 6.5만 정점을 넘는다
                    mesh.CombineMeshes(combine.ToArray(), false, true);
                    mesh.RecalculateBounds();
                    if (isNew) AssetDatabase.CreateAsset(mesh, meshPath); else EditorUtility.SetDirty(mesh);

                    var go = new GameObject(bed.name);
                    Undo.RegisterCreatedObjectUndo(go, "화단 합치기");
                    go.transform.SetParent(root.transform, false);
                    go.transform.SetPositionAndRotation(pit.transform.position, pit.transform.rotation);
                    go.transform.localScale = pit.transform.lossyScale;      // 묶음 루트는 크기 1
                    go.layer = pit.gameObject.layer;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = mats.ToArray(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    go.AddComponent<MeshCollider>().sharedMesh = pit.sharedMesh;   // 밟히는 건 경계석·흙만
                    GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(pit.gameObject));
                    go.transform.SetSiblingIndex(bed.GetSiblingIndex());

                    Undo.DestroyObjectImmediate(bed.gameObject);
                    merged++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
        AssetDatabase.SaveAssets();
        return "[BedMerge] 화단 " + merged + "개를 하나로 합침" + (refit > 0 ? " (꽃과 경계석이 어긋나 있던 " + refit + "개는 경계석 자리에 다시 맞춤)" : "") + (skipped > 0 ? ", 건너뜀 " + skipped + "개(이미 합쳤거나 자식 구성이 다름)" : "") + ". 씬 저장은 직접 하세요.";
    }

    static bool Approximately(Matrix4x4 a, Matrix4x4 b)
    {
        for (int i = 0; i < 16; i++) if (Mathf.Abs(a[i] - b[i]) > 0.01f) return false;
        return true;
    }
}
#endif
