// 메뉴: Tools > LotteWorld > Cut Stair Opening (A11)
// 지하로 내려가는 계단(LotteWorld_A11)의 난간 안쪽만큼 바닥 메시(보이는 면 + 콜라이더)에 구멍을 낸다.
//  - 구멍 크기는 난간(Static_LW_CreamWhite) 메시를 계단 방향으로 잰 값을 그대로 쓴다 -> 계단을 옮겨도 다시 누르면 맞음
//  - 위·아래를 보는 면(|n.y| > 0.7)만 자르고, UV·노멀·라이트맵 UV는 원래 삼각형에서 보간한다
//  - 에셋 메시는 <이름>_StairCut.asset 으로 따로 저장해서 바꿔 끼운다 (원본 에셋은 그대로), 씬 메시는 씬에 저장됨
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class LotteWorldStairOpening
{
    const string StairPath = "LotteWorld_Baked/LotteWorld_A11";
    const string RailName = "Static_LW_CreamWhite";
    const string Suffix = "_StairCut";

    [MenuItem("Tools/LotteWorld/Cut Stair Opening (A11)")]
    static void Run()
    {
        GameObject stair = GameObject.Find(StairPath);
        if (stair == null) { Debug.LogError("[StairOpening] " + StairPath + " 를 못 찾음"); return; }
        Vector2[] hole = OpeningPolygon(stair.transform);
        if (hole == null) { Debug.LogError("[StairOpening] 난간 메시(" + RailName + ")를 못 찾음"); return; }

        Bounds hb = new Bounds(new Vector3(hole[0].x, 0f, hole[0].y), Vector3.zero);
        foreach (Vector2 p in hole) hb.Encapsulate(new Vector3(p.x, 0f, p.y));
        hb.Expand(new Vector3(0.01f, 2f, 0.01f));

        var remap = new Dictionary<Mesh, Mesh>();
        var log = new List<string>();
        foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))
        {
            if (!IsFloor(mf.transform, stair.transform) || mf.sharedMesh == null) continue;
            Mesh cut = CutCached(mf.sharedMesh, mf.transform, hole, hb, remap);
            if (cut == null) continue;
            Undo.RecordObject(mf, "Cut Stair Opening"); mf.sharedMesh = cut; log.Add(Path(mf.transform));
        }
        foreach (MeshCollider mc in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include))
        {
            if (!IsFloor(mc.transform, stair.transform) || mc.sharedMesh == null) continue;
            Mesh cut = CutCached(mc.sharedMesh, mc.transform, hole, hb, remap);
            if (cut == null) continue;
            Undo.RecordObject(mc, "Cut Stair Opening"); mc.sharedMesh = null; mc.sharedMesh = cut; log.Add(Path(mc.transform) + " (collider)");
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(stair.scene);
        Debug.Log("[StairOpening] 구멍 " + string.Join(" ", System.Array.ConvertAll(hole, p => p.ToString("F2"))) +
                  "\n잘린 바닥 " + log.Count + "개:\n" + string.Join("\n", log));
    }

    // 난간을 계단 방향(yaw)으로 잰 직사각형 -> 월드 XZ 다각형 (반시계)
    static Vector2[] OpeningPolygon(Transform stair)
    {
        Transform rail = null;
        foreach (Transform t in stair.GetComponentsInChildren<Transform>(true)) if (t.name == RailName) rail = t;
        MeshFilter mf = rail ? rail.GetComponent<MeshFilter>() : null;
        if (mf == null || mf.sharedMesh == null) return null;
        Quaternion yaw = Quaternion.Euler(0f, stair.eulerAngles.y, 0f), inv = Quaternion.Inverse(yaw);
        Vector3 piv = stair.position, mn = Vector3.one * 1e9f, mx = -mn;
        foreach (Vector3 v in mf.sharedMesh.vertices)
        {
            Vector3 l = inv * (rail.TransformPoint(v) - piv); mn = Vector3.Min(mn, l); mx = Vector3.Max(mx, l);
        }
        Vector3[] c = { new Vector3(mn.x, 0, mn.z), new Vector3(mx.x, 0, mn.z), new Vector3(mx.x, 0, mx.z), new Vector3(mn.x, 0, mx.z) };
        var poly = new Vector2[4];
        for (int i = 0; i < 4; i++) { Vector3 w = piv + yaw * c[i]; poly[i] = new Vector2(w.x, w.z); }
        if (SignedArea(poly) < 0f) System.Array.Reverse(poly);
        return poly;
    }

    static bool IsFloor(Transform t, Transform stair)
    {
        if (t.IsChildOf(stair)) return false;
        string p = Path(t);
        return p.StartsWith("LotteWorld_Shell/Floor/") || p == "Blockout/Floor1/Floor";
    }

    static Mesh CutCached(Mesh src, Transform tr, Vector2[] hole, Bounds hb, Dictionary<Mesh, Mesh> remap)
    {
        Mesh done;
        if (remap.TryGetValue(src, out done)) return done;
        Mesh cut = Cut(src, tr.localToWorldMatrix, hole, hb);
        if (cut != null)
        {
            string ap = AssetDatabase.GetAssetPath(src);
            if (!string.IsNullOrEmpty(ap) && ap.EndsWith(".asset"))
            {
                string baseName = src.name.EndsWith(Suffix) ? src.name.Substring(0, src.name.Length - Suffix.Length) : src.name;
                cut.name = baseName + Suffix;
                string np = System.IO.Path.GetDirectoryName(ap).Replace('\\', '/') + "/" + cut.name + ".asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(np);
                if (existing != null && existing != src) AssetDatabase.DeleteAsset(np);
                if (existing == src) { EditorUtility.CopySerialized(cut, src); EditorUtility.SetDirty(src); cut = src; }
                else AssetDatabase.CreateAsset(cut, np);
            }
            else cut.name = src.name.EndsWith(Suffix) ? src.name : src.name + Suffix;
        }
        remap[src] = cut;
        return cut;
    }

    // ------------------------------------------------------------------ mesh clipping
    struct BV { public int a, b, c; public Vector3 w; }   // 원래 삼각형 꼭짓점 a,b,c 의 무게(w)

    static Mesh Cut(Mesh src, Matrix4x4 l2w, Vector2[] hole, Bounds hb)
    {
        if (!src.isReadable) { Debug.LogWarning("[StairOpening] 읽을 수 없는 메시: " + src.name); return null; }
        Bounds wb = TransformBounds(src.bounds, l2w);
        if (!wb.Intersects(hb)) return null;

        var pos = new List<Vector3>(); src.GetVertices(pos);
        var nrm = new List<Vector3>(); src.GetNormals(nrm);
        var tan = new List<Vector4>(); src.GetTangents(tan);
        var col = new List<Color>(); src.GetColors(col);
        var uvs = new List<Vector4>[4];
        for (int ch = 0; ch < 4; ch++) { uvs[ch] = new List<Vector4>(); src.GetUVs(ch, uvs[ch]); }
        var wpos = new Vector2[pos.Count]; var wy = new float[pos.Count];
        for (int i = 0; i < pos.Count; i++) { Vector3 w = l2w.MultiplyPoint3x4(pos[i]); wpos[i] = new Vector2(w.x, w.z); wy[i] = w.y; }

        var oV = new List<Vector3>(); var oN = new List<Vector3>(); var oT = new List<Vector4>(); var oC = new List<Color>();
        var oU = new List<Vector4>[4]; for (int ch = 0; ch < 4; ch++) oU[ch] = new List<Vector4>();
        var vmap = new Dictionary<int, int>();
        System.Func<int, int> keep = i =>
        {
            int r; if (vmap.TryGetValue(i, out r)) return r;
            r = oV.Count; oV.Add(pos[i]);
            if (nrm.Count > 0) oN.Add(nrm[i]); if (tan.Count > 0) oT.Add(tan[i]); if (col.Count > 0) oC.Add(col[i]);
            for (int ch = 0; ch < 4; ch++) if (uvs[ch].Count > 0) oU[ch].Add(uvs[ch][i]);
            vmap[i] = r; return r;
        };
        System.Func<BV, int> blend = bv =>
        {
            int r = oV.Count; Vector3 w = bv.w;
            oV.Add(pos[bv.a] * w.x + pos[bv.b] * w.y + pos[bv.c] * w.z);
            if (nrm.Count > 0) oN.Add((nrm[bv.a] * w.x + nrm[bv.b] * w.y + nrm[bv.c] * w.z).normalized);
            if (tan.Count > 0) oT.Add(tan[bv.a] * w.x + tan[bv.b] * w.y + tan[bv.c] * w.z);
            if (col.Count > 0) oC.Add(col[bv.a] * w.x + col[bv.b] * w.y + col[bv.c] * w.z);
            for (int ch = 0; ch < 4; ch++) if (uvs[ch].Count > 0) oU[ch].Add(uvs[ch][bv.a] * w.x + uvs[ch][bv.b] * w.y + uvs[ch][bv.c] * w.z);
            return r;
        };

        int changed = 0;
        var subs = new List<int>[src.subMeshCount];
        for (int s = 0; s < src.subMeshCount; s++)
        {
            int[] tri = src.GetTriangles(s); var outT = new List<int>(tri.Length);
            for (int t = 0; t < tri.Length; t += 3)
            {
                int a = tri[t], b = tri[t + 1], c = tri[t + 2];
                if (!Clippable(a, b, c, pos, l2w, wy, hb) || !TouchesHole(wpos[a], wpos[b], wpos[c], hole))
                { outT.Add(keep(a)); outT.Add(keep(b)); outT.Add(keep(c)); continue; }
                changed++;
                foreach (List<BV> piece in Subtract(a, b, c, wpos, hole))
                {
                    if (piece.Count < 3) continue;
                    var ids = new int[piece.Count];
                    for (int k = 0; k < piece.Count; k++) ids[k] = blend(piece[k]);
                    for (int k = 1; k + 1 < piece.Count; k++)
                    {
                        if (Mathf.Abs(Cross(PieceXZ(piece[0], a, b, c, wpos), PieceXZ(piece[k], a, b, c, wpos), PieceXZ(piece[k + 1], a, b, c, wpos))) < 1e-6f) continue;
                        outT.Add(ids[0]); outT.Add(ids[k]); outT.Add(ids[k + 1]);
                    }
                }
            }
            subs[s] = outT;
        }
        if (changed == 0) return null;

        var m = new Mesh { indexFormat = IndexFormat.UInt32 };
        m.SetVertices(oV);
        if (oN.Count > 0) m.SetNormals(oN); if (oT.Count > 0) m.SetTangents(oT); if (oC.Count > 0) m.SetColors(oC);
        for (int ch = 0; ch < 4; ch++) if (oU[ch].Count > 0) m.SetUVs(ch, oU[ch]);
        m.subMeshCount = subs.Length;
        for (int s = 0; s < subs.Length; s++) m.SetTriangles(subs[s], s, false);
        m.RecalculateBounds();
        return m;
    }

    // 위·아래를 보는 바닥 면이고 구멍 높이대에 있는 것만 자름 (슬래브 옆면 등은 그대로)
    static bool Clippable(int a, int b, int c, List<Vector3> pos, Matrix4x4 l2w, float[] wy, Bounds hb)
    {
        Vector3 pa = l2w.MultiplyPoint3x4(pos[a]), pb = l2w.MultiplyPoint3x4(pos[b]), pc = l2w.MultiplyPoint3x4(pos[c]);
        Vector3 n = Vector3.Cross(pb - pa, pc - pa); float len = n.magnitude;
        if (len < 1e-8f || Mathf.Abs(n.y / len) < 0.7f) return false;
        float ymin = Mathf.Min(wy[a], Mathf.Min(wy[b], wy[c])), ymax = Mathf.Max(wy[a], Mathf.Max(wy[b], wy[c]));
        return ymax >= hb.min.y && ymin <= hb.max.y;
    }

    static bool TouchesHole(Vector2 a, Vector2 b, Vector2 c, Vector2[] hole)
    {
        for (int e = 0; e < hole.Length; e++)        // 한 변의 바깥에 통째로 있으면 안 닿음
        {
            Vector2 p = hole[e], q = hole[(e + 1) % hole.Length];
            if (Side(p, q, a) < 0f && Side(p, q, b) < 0f && Side(p, q, c) < 0f) return false;
        }
        return true;
    }

    // tri - convex hole = 조각들 (조각 k = 변 0..k-1 안쪽 ∩ 변 k 바깥쪽)
    static List<List<BV>> Subtract(int a, int b, int c, Vector2[] wpos, Vector2[] hole)
    {
        var res = new List<List<BV>>();
        var cur = new List<BV>
        {
            new BV { a = a, b = b, c = c, w = new Vector3(1, 0, 0) },
            new BV { a = a, b = b, c = c, w = new Vector3(0, 1, 0) },
            new BV { a = a, b = b, c = c, w = new Vector3(0, 0, 1) },
        };
        for (int e = 0; e < hole.Length && cur.Count >= 3; e++)
        {
            Vector2 p = hole[e], q = hole[(e + 1) % hole.Length];
            res.Add(Clip(cur, p, q, false, a, b, c, wpos));
            cur = Clip(cur, p, q, true, a, b, c, wpos);
        }
        return res;   // 남은 cur 는 구멍 안 -> 버림
    }

    static List<BV> Clip(List<BV> poly, Vector2 p, Vector2 q, bool keepInside, int a, int b, int c, Vector2[] wpos)
    {
        var o = new List<BV>();
        for (int i = 0; i < poly.Count; i++)
        {
            BV s = poly[i], e = poly[(i + 1) % poly.Count];
            float ds = Side(p, q, PieceXZ(s, a, b, c, wpos)), de = Side(p, q, PieceXZ(e, a, b, c, wpos));
            if (!keepInside) { ds = -ds; de = -de; }
            bool sIn = ds >= 0f, eIn = de >= 0f;
            if (sIn) o.Add(s);
            if (sIn != eIn)
            {
                float t = ds / (ds - de);
                o.Add(new BV { a = a, b = b, c = c, w = Vector3.Lerp(s.w, e.w, t) });
            }
        }
        return o;
    }

    static Vector2 PieceXZ(BV v, int a, int b, int c, Vector2[] wpos) { return wpos[a] * v.w.x + wpos[b] * v.w.y + wpos[c] * v.w.z; }
    static float Side(Vector2 p, Vector2 q, Vector2 x) { return (q.x - p.x) * (x.y - p.y) - (q.y - p.y) * (x.x - p.x); }   // >0 = 왼쪽(반시계 다각형의 안)
    static float Cross(Vector2 a, Vector2 b, Vector2 c) { return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x); }
    static float SignedArea(Vector2[] p) { float s = 0; for (int i = 0; i < p.Length; i++) { Vector2 a = p[i], b = p[(i + 1) % p.Length]; s += a.x * b.y - b.x * a.y; } return s * 0.5f; }

    static Bounds TransformBounds(Bounds b, Matrix4x4 m)
    {
        Bounds r = new Bounds(m.MultiplyPoint3x4(b.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
            r.Encapsulate(m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
        return r;
    }

    static string Path(Transform t)
    {
        string p = t.name; while (t.parent) { t = t.parent; p = t.name + "/" + p; } return p;
    }
}
