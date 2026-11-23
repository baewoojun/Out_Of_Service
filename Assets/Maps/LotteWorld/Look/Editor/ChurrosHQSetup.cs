#if UNITY_EDITOR
// ChurrosHQSetup.cs - 고퀄 츄러스 가게(Deco_Food_Churros_HQ)를 씬의 기존 츄러스 가게에 씌운다.
// 넣는 곳: Assets/Maps/LotteWorld/Rides/ChurrosHQ/ (FBX + Textures) , 이 파일은 아무 Editor 폴더
// 메뉴 Tools > LotteWorld > Look > 츄러스 고퀄 ...
//   1. 재질 설정       : 이 폴더 FBX만 URP Lit 재질 연결 (Setup Ride Materials 와 같은 방식, 다른 놀이기구는 안 건드림)
//   2. 씬에 적용       : 이름이 Deco_Food_Churros 로 시작하는 오브젝트마다 자식 Churros_HQ 를 넣고, 옛 모델 렌더러만 끈다
//                        (옛 콜라이더·조명·프리팹 연결은 그대로, Undo 됨)
//   3. 180도 돌리기    : 앞뒤가 반대로 들어갔을 때 (진열장이 옛 모델과 같은 쪽 = 앞 왼쪽이어야 함)
//   4. 옛 모델 보이기/숨기기 : 비교용
//   5. 되돌리기        : Churros_HQ 지우고 옛 렌더러 다시 켬
// 씬 저장은 직접.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LotteWorldLook
{
    public static class ChurrosHQSetup
    {
        const string Dir = "Assets/Maps/LotteWorld/Rides/ChurrosHQ";
        const string Fbx = Dir + "/Deco_Food_Churros_HQ.fbx";
        const string OldPrefix = "Deco_Food_Churros";
        const string Child = "Churros_HQ";

        [MenuItem("Tools/LotteWorld/Look/츄러스 고퀄 1. 재질 설정")]
        public static void SetupMaterials()
        {
            if (!File.Exists(Fbx)) { Debug.LogError("[ChurrosHQ] FBX가 없어: " + Fbx); return; }
            string tex = Dir + "/Textures", matDir = Dir + "/Materials";
            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(Dir, "Materials");
            if (Directory.Exists(tex))
                foreach (var p in Directory.GetFiles(tex, "*.png"))
                {
                    string path = p.Replace('\\', '/');
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
                    bool dirty = false;
                    if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                    if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                    if (dirty) ti.SaveAndReimport();
                }
            var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var names = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
            foreach (var n in names)
            {
                string mp = matDir + "/" + n + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
                mat.shader = shader; mat.SetColor("_BaseColor", Color.white);
                var bc = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_BaseColor.png");
                var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_Normal.png");
                var ms = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_MS.png");
                if (bc) mat.SetTexture("_BaseMap", bc);
                if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
                if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                if (n.EndsWith("On"))
                {
                    mat.SetColor("_BaseColor", new Color(1f, .86f, .58f));
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1f, .8f, .45f) * 4f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                if (n.Contains("Glass")) MakeGlass(mat);
                EditorUtility.SetDirty(mat);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
            }
            mi.SaveAndReimport();
            AssetDatabase.SaveAssets();
            Debug.Log("[ChurrosHQ] 재질 " + names.Count + "개 설정");
        }

        static void MakeGlass(Material mat)
        {
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(.85f, .9f, .88f, .22f));
            mat.SetFloat("_Smoothness", .85f);
            mat.DisableKeyword("_METALLICSPECGLOSSMAP"); mat.SetTexture("_MetallicGlossMap", null);
        }

        static List<Transform> OldKiosks()
        {
            var list = new List<Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith(OldPrefix) || t.name.StartsWith(OldPrefix + "_HQ")) continue;
                if (EditorUtility.IsPersistent(t)) continue;
                bool nested = false;
                for (var p = t.parent; p != null; p = p.parent) if (p.name.StartsWith(OldPrefix)) { nested = true; break; }
                if (!nested) list.Add(t);
            }
            return list;
        }

        static void SetOldRenderers(Transform kiosk, bool on)
        {
            var hq = kiosk.Find(Child);
            foreach (var r in kiosk.GetComponentsInChildren<Renderer>(true))
            {
                if (hq != null && r.transform.IsChildOf(hq)) continue;
                Undo.RecordObject(r, "Churros HQ"); r.enabled = on;
            }
        }

        [MenuItem("Tools/LotteWorld/Look/츄러스 고퀄 2. 씬에 적용")]
        public static void Apply()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (prefab == null) { Debug.LogError("[ChurrosHQ] FBX가 없어: " + Fbx); return; }
            int n = 0, skip = 0;
            foreach (var t in OldKiosks())
            {
                if (t.Find(Child) != null) { skip++; continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(inst, "Churros HQ");
                inst.name = Child;
                inst.transform.SetParent(t, false);           // 프리팹 루트의 로컬 회전 유지
                inst.transform.localPosition = Vector3.zero;
                SetOldRenderers(t, false);
                n++;
                Debug.Log("[ChurrosHQ] 적용: " + Path(t), t);
            }
            EditorSceneManager.MarkAllScenesDirty();
            if (n + skip == 0) Debug.LogWarning("[ChurrosHQ] 이름이 " + OldPrefix + " 로 시작하는 오브젝트를 못 찾았어. Hierarchy 에서 츄러스 가게 이름을 알려줘.");
            else Debug.Log("[ChurrosHQ] " + n + "곳 적용, " + skip + "곳은 이미 있음. 옛 콜라이더·조명은 그대로 둠. 씬 저장은 직접.");
        }

        [MenuItem("Tools/LotteWorld/Look/츄러스 고퀄 3. 180도 돌리기")]
        public static void Flip()
        {
            int n = 0;
            foreach (var t in OldKiosks())
            {
                var hq = t.Find(Child); if (hq == null) continue;
                Undo.RecordObject(hq, "Churros HQ flip");
                hq.localRotation = Quaternion.AngleAxis(180f, t.InverseTransformDirection(Vector3.up)) * hq.localRotation;
                n++;
            }
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[ChurrosHQ] " + n + "곳 180도 돌림");
        }

        static bool showOld;
        [MenuItem("Tools/LotteWorld/Look/츄러스 고퀄 4. 옛 모델 보이기·숨기기")]
        public static void ToggleOld()
        {
            showOld = !showOld;
            foreach (var t in OldKiosks()) if (t.Find(Child) != null) SetOldRenderers(t, showOld);
            Debug.Log("[ChurrosHQ] 옛 모델 " + (showOld ? "보임" : "숨김"));
        }

        [MenuItem("Tools/LotteWorld/Look/츄러스 고퀄 5. 되돌리기")]
        public static void Revert()
        {
            int n = 0;
            foreach (var t in OldKiosks())
            {
                var hq = t.Find(Child); if (hq == null) continue;
                Undo.DestroyObjectImmediate(hq.gameObject);
                SetOldRenderers(t, true); n++;
            }
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[ChurrosHQ] " + n + "곳 되돌림");
        }

        static string Path(Transform t) { return t.parent == null ? t.name : Path(t.parent) + "/" + t.name; }
    }
}
#endif
