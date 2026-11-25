#if UNITY_EDITOR
// RidesParkSetup.cs - 3묶음 놀이기구 고퀄(The Park 단계, 11/24 다시 구움) 재질 연결
// 대상: Assets/Maps/LotteWorld/Rides/{Carousel, PirateShip, Basket, Flume}/  (FBX 이름·그룹·피벗은 11/17 것과 같음)
// 메뉴 Tools > LotteWorld > Look > 놀이기구 고퀄 1. 재질 연결
//   - Textures/<재질>_BaseColor / _Normal / _MS 를 URP Lit 재질에 연결 (다른 놀이기구·매직아일랜드는 안 건드림)
//   - 이름이 On 으로 끝나는 재질(BulbOn, LanternOn)은 발광
//   - 끝나면 콘솔에 놀이기구마다 재질 수·크기·삼각형 수
// 그다음: Tools > LotteWorld > Rides (놀이기구 작동) > 1 설정하기 를 한 번 다시 (움직이는 부품 Static 끄기)
// 메뉴 놀이기구 고퀄 2. 탑승 붙이기 : 씬의 4개 기구 루트에 RideSeats(E 로 타고 내리기) + 고정 메시(Static_/Base_)에 MeshCollider(없을 때만).
//   해적선 동쪽 탑승 데크·계단도 Static_StoneWall 안에 들어 있어서 콜라이더가 있어야 올라갈 수 있음. Undo 됨, 씬 저장은 직접.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LotteWorld.Rides;

namespace LotteWorldLook
{
    public static class RidesParkSetup
    {
        const string Root = "Assets/Maps/LotteWorld/Rides";
        static readonly string[] Rides = { "Carousel/Carousel_A13", "PirateShip/PirateShip_A07", "Basket/Basket_A09", "Flume/Flume_A08" };

        [MenuItem("Tools/LotteWorld/Look/놀이기구 고퀄 1. 재질 연결")]
        public static void Setup()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var r in Rides)
            {
                string dir = Root + "/" + Path.GetDirectoryName(r).Replace('\\', '/'), fbx = Root + "/" + r + ".fbx";
                if (!File.Exists(fbx)) { Debug.LogWarning("[RidesPark] FBX 없음: " + fbx); continue; }
                string tex = dir + "/Textures", matDir = dir + "/Materials";
                if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "Materials");
                {
                    if (Directory.Exists(tex))
                        foreach (var p in Directory.GetFiles(tex, "*.png"))
                        {
                            string path = p.Replace('\\', '/');
                            var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
                            bool dirty = false;
                            if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                            if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                            if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
                            if (dirty) ti.SaveAndReimport();
                        }
                }

                var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
                mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.SaveAndReimport();
                var names = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
                int withTex = 0;
                foreach (var n in names)
                {
                    string mp = $"{matDir}/{n}.mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
                    mat.shader = shader; mat.SetColor("_BaseColor", Color.white);
                    var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_BaseColor.png");
                    var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_Normal.png");
                    var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_MS.png");
                    mat.SetTexture("_BaseMap", bc);
                    if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
                    else { mat.SetTexture("_BumpMap", null); mat.DisableKeyword("_NORMALMAP"); }
                    // 금색·놋쇠는 매끈함을 낮춤 (11/24: 해적선 금색 방패가 하늘을 비춰 하얗게 보임)
                    if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", n.Contains("Gold") || n.Contains("Brass") ? 0.5f : 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                    if (bc) withTex++;
                    if (n.EndsWith("On"))
                    {
                        mat.SetColor("_BaseColor", new Color(1f, .86f, .58f));
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", new Color(1f, .8f, .45f) * 4f);
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    }
                    EditorUtility.SetDirty(mat);
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
                }
                mi.SaveAndReimport();

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
                var rs = inst.GetComponentsInChildren<Renderer>();
                long tris = inst.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh).Sum(f => { long t = 0; for (int i = 0; i < f.sharedMesh.subMeshCount; i++) t += (long)f.sharedMesh.GetIndexCount(i) / 3; return t; });
                Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
                foreach (var x in rs) b.Encapsulate(x.bounds);
                string groups = string.Join(", ", inst.transform.Cast<Transform>().Select(t => t.name));
                Object.DestroyImmediate(inst);
                Debug.Log($"[RidesPark] {Path.GetFileName(fbx)}: 재질 {names.Count}개(텍스처 {withTex}), 삼각형 {tris:N0}, 크기 {b.size.x:F1} x {b.size.y:F1} x {b.size.z:F1} m, 그룹 {groups}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[RidesPark] 끝. 이어서 Tools > LotteWorld > Rides (놀이기구 작동) > 1 설정하기 를 한 번 눌러 줘");
        }

        // 기구별: 좌석까지 거리, 발이 좌석보다 얼마나 아래까지 허용(해적선은 데크 위에서만), 내리는 곳까지 거리 (블렌더 m, 기구 크기 따라 자동으로 줄어듦)
        static readonly (string name, float reach, float below, float leave)[] Seat =
        {
            ("Carousel_A13", 2.0f, 2.1f, 6.5f), ("PirateShip_A07", 5.3f, 1.5f, 6.0f), ("Basket_A09", 2.0f, 2.1f, 6.0f), ("Flume_A08", 2.4f, 2.1f, 2.6f),
        };

        [MenuItem("Tools/LotteWorld/Look/놀이기구 고퀄 2. 탑승 붙이기")]
        public static void AddSeats()
        {
            Undo.IncrementCurrentGroup(); int grp = Undo.GetCurrentGroup();
            var log = new List<string>(); var scenes = new HashSet<Scene>();
            foreach (var cfg in Seat)
            {
                var roots = FindInScenes(cfg.name);
                if (roots.Count == 0) { log.Add(cfg.name + " 씬에 없음"); continue; }
                foreach (var go in roots)
                {
                    var rs = go.GetComponent<RideSeats>();
                    if (rs == null) rs = Undo.AddComponent<RideSeats>(go);
                    Undo.RecordObject(rs, "RideSeats");
                    rs.interactDistance = cfg.reach; rs.maxBelowSeat = cfg.below; rs.leaveRadius = cfg.leave;
                    EditorUtility.SetDirty(rs);
                    int seats = 0, exits = 0, cols = 0;
                    foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.StartsWith("Seat_") && !t.name.EndsWith("_Fwd")) seats++;
                        else if (t.name.StartsWith("Exit_")) exits++;
                        else if ((t.name.StartsWith("Static_") || t.name.StartsWith("Base_")) && t.GetComponent<MeshFilter>() && !t.GetComponent<Collider>())
                        { Undo.AddComponent<MeshCollider>(t.gameObject); cols++; }
                    }
                    scenes.Add(go.scene);
                    log.Add(string.Format("{0}: 좌석 {1}, 내리는 곳 {2}, 콜라이더 추가 {3}{4}", go.name, seats, exits, cols, seats == 0 ? " ← 좌석 없음! 새 FBX 가 들어갔는지 확인" : ""));
                }
            }
            Undo.CollapseUndoOperations(grp);
            foreach (var sc in scenes) EditorSceneManager.MarkSceneDirty(sc);
            Debug.Log("[RidesPark] 탑승: " + string.Join(" / ", log.ToArray()) + "\n플레이 → 좌석 가까이서 E 로 타고, 다시 E 로 내림. 씬 저장은 직접.");
        }

        static List<GameObject> FindInScenes(string name)
        {
            var r = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i); if (!sc.isLoaded) continue;
                foreach (var root in sc.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name == name || t.name.StartsWith(name + " (")) r.Add(t.gameObject);
            }
            // FBX 안 루트 빈 오브젝트도 같은 이름이라 바깥쪽 것만 (안 그러면 E 한 번에 두 번 앉음)
            return r.FindAll(g => { for (var p = g.transform.parent; p != null; p = p.parent) if (r.Contains(p.gameObject)) return false; return true; });
        }
    }
}
#endif
