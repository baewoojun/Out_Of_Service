// GardenStageBackDetail.cs - 가든스테이지 무대 건물의 민벽(옆·뒷면)에 덧붙이는 디테일 메시
// 메뉴: Tools > LotteWorld > Look > 가든스테이지 옆·뒷면 디테일 만들기 / 지우기
// - 구운 FBX(Rides/GardenStage)는 건드리지 않는다. 같은 자리에 별도 오브젝트 "GardenStage_BackDetail" 을 만들어 벽에 붙인다
//   → FBX 를 다시 구워 넣어도 남는다. 블렌더 쪽에서 옆·뒷면을 제대로 만들게 되면 [지우기]
// - 넣는 것: 기단(돌) + 벽기둥 + 중간 띠 + 처마 밑 띠 + 칸마다 액자 몰딩, 뒷면 반입문·쪽문, 옆면 쪽문, 빗물 홈통, 문 위 등
// - 재질은 건물 공용 타일 재질(Rides/BldShared, 1타일 약 5 m)을 그대로 쓴다. 새 텍스처 없음
// - 치수는 FBX 인스턴스 위치 기준(벽 위치를 씬에서 잰 값). 건물을 옮겼으면 다시 [만들기]
// - 충돌체는 없다(벽에서 0.4 m 이내로만 튀어나옴). 씬 저장은 직접. Undo 됨
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LotteWorld.Look
{
    public static class GardenStageBackDetail
    {
        const string ObjName = "GardenStage_BackDetail";
        const string StagePath = "LotteWorld_Baked/GardenStage";
        const string MeshDir = "Assets/Maps/LotteWorld/Rides/GardenStage/Generated";
        const string MeshPath = MeshDir + "/GardenStage_BackDetail.asset";
        const string MatDir = "Assets/Maps/LotteWorld/Rides/BldShared/Materials/";
        static readonly string[] MatNames = { "Tile_LW_StoneGrey", "Tile_LW_WarmBeige", "Tile_LW_DarkTeal" };
        const int Stone = 0, Trim = 1, Dark = 2;
        const float TileMetres = 5f;

        // 벽 위치 (무대 FBX 루트 기준, m). 뒷면 z = BackZ, 옆면 x = ±SideX
        const float BackZ = 18.0f, SideX = 26.0f;
        const float SideZ0 = -1.5f;     // 옆면 앞쪽 끝 (앞 탑 포함)
        const float TowerZ = 3.9f;      // 앞 탑이 끝나고 본체 벽이 시작하는 곳
        const float WallTop = 19.9f;    // 금색 처마 밑

        class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>(), N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int>[] T = { new List<int>(), new List<int>(), new List<int>() };

            public void Box(int mat, Vector3 a, Vector3 b)
            {
                Vector3 mn = Vector3.Min(a, b), mx = Vector3.Max(a, b);
                Quad(mat, new Vector3(mx.x, mn.y, mn.z), new Vector3(mx.x, mn.y, mx.z), new Vector3(mx.x, mx.y, mx.z), new Vector3(mx.x, mx.y, mn.z), Vector3.right);
                Quad(mat, new Vector3(mn.x, mn.y, mx.z), new Vector3(mn.x, mn.y, mn.z), new Vector3(mn.x, mx.y, mn.z), new Vector3(mn.x, mx.y, mx.z), Vector3.left);
                Quad(mat, new Vector3(mx.x, mn.y, mx.z), new Vector3(mn.x, mn.y, mx.z), new Vector3(mn.x, mx.y, mx.z), new Vector3(mx.x, mx.y, mx.z), Vector3.forward);
                Quad(mat, new Vector3(mn.x, mn.y, mn.z), new Vector3(mx.x, mn.y, mn.z), new Vector3(mx.x, mx.y, mn.z), new Vector3(mn.x, mx.y, mn.z), Vector3.back);
                Quad(mat, new Vector3(mn.x, mx.y, mn.z), new Vector3(mx.x, mx.y, mn.z), new Vector3(mx.x, mx.y, mx.z), new Vector3(mn.x, mx.y, mx.z), Vector3.up);
                Quad(mat, new Vector3(mn.x, mn.y, mx.z), new Vector3(mx.x, mn.y, mx.z), new Vector3(mx.x, mn.y, mn.z), new Vector3(mn.x, mn.y, mn.z), Vector3.down);
            }

            void Quad(int mat, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
            {
                int i = V.Count;
                foreach (var p in new[] { p0, p1, p2, p3 })
                {
                    V.Add(p); N.Add(n);
                    Vector2 uv = Mathf.Abs(n.y) > 0.5f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > 0.5f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                    UV.Add(uv / TileMetres);
                }
                // 바깥에서 볼 때 앞면이 되게 (법선과 감는 방향을 맞춘다)
                bool flip = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) < 0f;
                if (flip) { T[mat].AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 }); }
                else { T[mat].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); }
            }

            // 뒷면(+z 쪽을 보는 벽)에 붙는 상자: 가로 x0..x1, 세로 y0..y1, 벽에서 out 만큼 튀어나옴
            public void Back(int mat, float x0, float x1, float y0, float y1, float o)
            { Box(mat, new Vector3(x0, y0, BackZ - 0.02f), new Vector3(x1, y1, BackZ + o)); }

            // 옆면(s = +1 동쪽, -1 서쪽)에 붙는 상자: 가로 z0..z1
            public void Side(int mat, float s, float z0, float z1, float y0, float y1, float o)
            { Box(mat, new Vector3(s * (SideX - 0.02f), y0, z0), new Vector3(s * (SideX + o), y1, z1)); }

            // 액자 몰딩(가는 띠 네 줄)
            public void FrameBack(float cx, float w, float y0, float y1)
            {
                const float t = 0.18f, o = 0.09f;
                Back(Trim, cx - w / 2, cx + w / 2, y0, y0 + t, o); Back(Trim, cx - w / 2, cx + w / 2, y1 - t, y1, o);
                Back(Trim, cx - w / 2, cx - w / 2 + t, y0, y1, o); Back(Trim, cx + w / 2 - t, cx + w / 2, y0, y1, o);
            }
            public void FrameSide(float s, float cz, float w, float y0, float y1)
            {
                const float t = 0.18f, o = 0.09f;
                Side(Trim, s, cz - w / 2, cz + w / 2, y0, y0 + t, o); Side(Trim, s, cz - w / 2, cz + w / 2, y1 - t, y1, o);
                Side(Trim, s, cz - w / 2, cz - w / 2 + t, y0, y1, o); Side(Trim, s, cz + w / 2 - t, cz + w / 2, y0, y1, o);
            }
        }

        [MenuItem("Tools/LotteWorld/Look/가든스테이지 옆·뒷면 디테일 만들기", false, 80)]
        static void BuildMenu() => Debug.Log(Build());

        [MenuItem("Tools/LotteWorld/Look/가든스테이지 옆·뒷면 디테일 지우기", false, 81)]
        static void RemoveMenu() => Debug.Log(Remove());

        public static string Build()
        {
            var stage = GameObject.Find(StagePath);
            if (stage == null) return "[GardenStage] 씬에 " + StagePath + " 가 없습니다.";

            var b = new Builder();
            const float plinthH = 1.2f, capH = 0.15f, y0 = plinthH + capH;          // 벽기둥이 시작하는 높이
            const float friezeY0 = 18.3f, friezeY1 = 19.1f, bandY0 = 9.6f, bandY1 = 9.9f;

            // ---------- 뒷면 ----------
            float bx0 = -SideX - 0.32f, bx1 = SideX + 0.32f;                        // 모서리를 감싸게 조금 넘긴다
            b.Back(Stone, bx0, bx1, 0f, plinthH, 0.30f);
            b.Back(Trim, bx0 - 0.06f, bx1 + 0.06f, plinthH, y0, 0.38f);
            b.Back(Trim, bx0, bx1, bandY0, bandY1, 0.14f);
            b.Back(Trim, bx0, bx1, friezeY0, friezeY1, 0.26f);
            b.Back(Trim, bx0, bx1, friezeY1, friezeY1 + 0.12f, 0.34f);

            const int bays = 8; float first = -SideX + 0.6f, step = (SideX * 2f - 1.2f) / bays;
            for (int i = 0; i <= bays; i++)
            {
                float x = first + i * step;
                b.Back(Trim, x - 0.5f, x + 0.5f, y0, friezeY0, 0.22f);              // 벽기둥
                b.Back(Trim, x - 0.65f, x + 0.65f, y0, y0 + 0.55f, 0.30f);          // 주춧돌
                b.Back(Trim, x - 0.65f, x + 0.65f, friezeY0 - 0.6f, friezeY0, 0.30f); // 기둥머리
            }
            const int loadBay = 3, staffBay = 5;
            for (int i = 0; i < bays; i++)
            {
                float cx = first + (i + 0.5f) * step;
                b.FrameBack(cx, 3.4f, 11.2f, 16.8f);                                // 위 칸 액자
                if (i == loadBay)
                {   // 무대 뒤 반입문 (닫힘)
                    b.Back(Dark, cx - 1.7f, cx + 1.7f, 0f, 4.2f, 0.33f);
                    b.Back(Dark, cx - 0.04f, cx + 0.04f, 0f, 4.2f, 0.36f);          // 두 짝 사이 이음
                    b.Back(Stone, cx - 2.05f, cx - 1.7f, 0f, 4.55f, 0.40f); b.Back(Stone, cx + 1.7f, cx + 2.05f, 0f, 4.55f, 0.40f);
                    b.Back(Stone, cx - 2.05f, cx + 2.05f, 4.2f, 4.55f, 0.40f);
                    b.Back(Trim, cx - 2.5f, cx + 2.5f, 4.9f, 5.08f, 1.3f);          // 차양
                    b.Back(Trim, cx - 0.17f, cx + 0.17f, 5.3f, 5.6f, 0.32f);        // 문 위 등
                }
                else if (i == staffBay)
                {   // 직원 쪽문
                    b.Back(Dark, cx - 0.6f, cx + 0.6f, 0f, 2.3f, 0.33f);
                    b.Back(Stone, cx - 0.85f, cx - 0.6f, 0f, 2.55f, 0.38f); b.Back(Stone, cx + 0.6f, cx + 0.85f, 0f, 2.55f, 0.38f);
                    b.Back(Stone, cx - 0.85f, cx + 0.85f, 2.3f, 2.55f, 0.38f);
                    b.Back(Trim, cx - 0.15f, cx + 0.15f, 2.85f, 3.1f, 0.28f);
                    b.FrameBack(cx, 3.4f, 3.9f, 8.6f);
                }
                else b.FrameBack(cx, 3.4f, 2.8f, 8.6f);                             // 아래 칸 액자
            }
            foreach (int i in new[] { 2, 7 })
            {   // 빗물 홈통
                float x = first + i * step + (i == 2 ? 0.85f : -0.85f);
                b.Back(Dark, x - 0.08f, x + 0.08f, y0, WallTop, 0.20f);
                for (float y = 3f; y < WallTop; y += 4f) b.Back(Dark, x - 0.14f, x + 0.14f, y, y + 0.1f, 0.24f);
            }

            // ---------- 옆면 (동·서) ----------
            foreach (float s in new[] { 1f, -1f })
            {
                b.Side(Stone, s, SideZ0 - 0.3f, BackZ + 0.02f, 0f, plinthH, 0.30f);
                b.Side(Trim, s, SideZ0 - 0.36f, BackZ + 0.02f, plinthH, y0, 0.38f);
                b.Side(Trim, s, TowerZ, BackZ + 0.02f, bandY0, bandY1, 0.14f);
                b.Side(Trim, s, TowerZ, BackZ + 0.02f, friezeY0, friezeY1, 0.26f);
                b.Side(Trim, s, TowerZ, BackZ + 0.02f, friezeY1, friezeY1 + 0.12f, 0.34f);
                float[] pz = { TowerZ + 0.5f, (TowerZ + BackZ) * 0.5f, BackZ - 0.5f };
                foreach (float z in pz)
                {
                    b.Side(Trim, s, z - 0.5f, z + 0.5f, y0, friezeY0, 0.22f);
                    b.Side(Trim, s, z - 0.65f, z + 0.65f, y0, y0 + 0.55f, 0.30f);
                    b.Side(Trim, s, z - 0.65f, z + 0.65f, friezeY0 - 0.6f, friezeY0, 0.30f);
                }
                for (int i = 0; i < 2; i++)
                {
                    float cz = (pz[i] + pz[i + 1]) * 0.5f;
                    b.FrameSide(s, cz, 3.4f, 11.2f, 16.8f);
                    if (i == 0)
                    {   // 옆 쪽문
                        b.Side(Dark, s, cz - 0.6f, cz + 0.6f, 0f, 2.3f, 0.33f);
                        b.Side(Stone, s, cz - 0.85f, cz - 0.6f, 0f, 2.55f, 0.38f); b.Side(Stone, s, cz + 0.6f, cz + 0.85f, 0f, 2.55f, 0.38f);
                        b.Side(Stone, s, cz - 0.85f, cz + 0.85f, 2.3f, 2.55f, 0.38f);
                        b.Side(Trim, s, cz - 0.15f, cz + 0.15f, 2.85f, 3.1f, 0.28f);
                        b.FrameSide(s, cz, 3.4f, 3.9f, 8.6f);
                    }
                    else b.FrameSide(s, cz, 3.4f, 2.8f, 8.6f);
                }
            }

            // ---------- 메시 에셋 ----------
            if (!AssetDatabase.IsValidFolder(MeshDir)) AssetDatabase.CreateFolder("Assets/Maps/LotteWorld/Rides/GardenStage", "Generated");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh { name = ObjName };
            mesh.Clear();
            mesh.SetVertices(b.V); mesh.SetNormals(b.N); mesh.SetUVs(0, b.UV);
            mesh.subMeshCount = b.T.Length;
            for (int i = 0; i < b.T.Length; i++) mesh.SetTriangles(b.T[i], i);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            if (isNew) AssetDatabase.CreateAsset(mesh, MeshPath); else EditorUtility.SetDirty(mesh);

            var mats = new Material[MatNames.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = AssetDatabase.LoadAssetAtPath<Material>(MatDir + MatNames[i] + ".mat");
                if (mats[i] == null) return "[GardenStage] 재질이 없습니다: " + MatDir + MatNames[i] + ".mat";
            }

            // ---------- 씬 오브젝트 ----------
            var parent = stage.transform.parent;
            var t = parent.Find(ObjName);
            GameObject go;
            if (t != null) go = t.gameObject;
            else
            {
                go = new GameObject(ObjName);
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "GardenStage 옆·뒷면 디테일");
            }
            go.transform.SetPositionAndRotation(stage.transform.position, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            var mf = go.GetComponent<MeshFilter>(); if (mf == null) mf = go.AddComponent<MeshFilter>();
            var mr = go.GetComponent<MeshRenderer>(); if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh; mr.sharedMaterials = mats;
            go.isStatic = true;
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            AssetDatabase.SaveAssets();
            return string.Format("[GardenStage] 옆·뒷면 디테일 만듦: 삼각형 {0}개, 위치 {1}. 씬 저장은 직접 하세요.", mesh.triangles.Length / 3, go.transform.position);
        }

        public static string Remove()
        {
            var stage = GameObject.Find(StagePath);
            var t = stage != null ? stage.transform.parent.Find(ObjName) : null;
            if (t == null) return "[GardenStage] 지울 " + ObjName + " 가 없습니다.";
            var scene = t.gameObject.scene;
            Undo.DestroyObjectImmediate(t.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            return "[GardenStage] " + ObjName + " 지움 (메시 에셋은 남겨 둠). 씬 저장은 직접 하세요.";
        }
    }
}
#endif
