// 롯데월드 건물 FBX(Rides/*)의 *_Detail_* 메시에 잘못 구워진 "바닥 타일 테두리 상자"를 임포트할 때 빼낸다.
// 증상: 체크무늬 바닥 칸마다 두께 0.1~0.2 m, 높이 0.6 m, 길이 1.2 m 이하의 상자 4개가 서서 바닥이 칸막이 미로처럼 보임.
// (2026-11 재출력분 NorthGate·SouthTerrace 에서 확인. 벽 밑 걸레받이는 높이 0.56 m 라 건드리지 않는다.)
// 원본(bpy) 굽기에서 고쳐지면 걸리는 상자가 없어 아무 일도 하지 않는다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DetailTileRimStrip : AssetPostprocessor
{
    const string Root = "Assets/Maps/LotteWorld/Rides/";

    public override uint GetVersion() => 1;

    void OnPostprocessModel(GameObject g)
    {
        if (!assetPath.StartsWith(Root)) return;
        int total = 0;
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = mf.sharedMesh;
            if (m == null || !m.name.Contains("Detail_")) continue;
            int n = Strip(m, mf.transform.localToWorldMatrix);
            if (n > 0) total += n;
        }
        if (total > 0) Debug.Log($"[DetailTileRimStrip] {assetPath}: 바닥 타일 테두리 상자 {total}개를 뺐다.");
    }

    // 상자(삼각형 12개) 덩어리 중 높이 0.58~0.62, 두께 0.08~0.22, 길이 0.6~1.25 m 인 것을 지운다. 돌려주는 값은 지운 상자 수.
    static int Strip(Mesh m, Matrix4x4 toModel)
    {
        var lv = m.vertices;
        var w = new Vector3[lv.Length];
        for (int i = 0; i < lv.Length; i++) w[i] = toModel.MultiplyPoint3x4(lv[i]);

        var weld = new Dictionary<Vector3Int, int>();
        var id = new int[w.Length];
        for (int i = 0; i < w.Length; i++)
        {
            var k = new Vector3Int(Mathf.RoundToInt(w[i].x * 500), Mathf.RoundToInt(w[i].y * 500), Mathf.RoundToInt(w[i].z * 500));
            if (!weld.TryGetValue(k, out int j)) { j = weld.Count; weld[k] = j; }
            id[i] = j;
        }
        var par = new int[weld.Count];
        for (int i = 0; i < par.Length; i++) par[i] = i;
        int Find(int x) { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; }

        var subs = new List<int[]>();
        for (int s = 0; s < m.subMeshCount; s++)
        {
            var t = m.GetTriangles(s);
            subs.Add(t);
            for (int i = 0; i < t.Length; i += 3)
            {
                int a = Find(id[t[i]]);
                par[Find(id[t[i + 1]])] = a;
                par[Find(id[t[i + 2]])] = Find(a);
            }
        }

        var count = new Dictionary<int, int>();
        var box = new Dictionary<int, Bounds>();
        foreach (var t in subs)
            for (int i = 0; i < t.Length; i += 3)
            {
                int r = Find(id[t[i]]);
                count[r] = count.TryGetValue(r, out int c) ? c + 1 : 1;
                var b = box.TryGetValue(r, out var bb) ? bb : new Bounds(w[t[i]], Vector3.zero);
                b.Encapsulate(w[t[i]]); b.Encapsulate(w[t[i + 1]]); b.Encapsulate(w[t[i + 2]]);
                box[r] = b;
            }

        var kill = new HashSet<int>();
        foreach (var kv in count)
        {
            if (kv.Value != 12) continue;
            var s = box[kv.Key].size;
            float thin = Mathf.Min(s.x, s.z), len = Mathf.Max(s.x, s.z);
            if (s.y > 0.58f && s.y < 0.62f && thin > 0.08f && thin < 0.22f && len > 0.6f && len < 1.25f) kill.Add(kv.Key);
        }
        if (kill.Count == 0) return 0;

        for (int s = 0; s < subs.Count; s++)
        {
            var t = subs[s];
            var keep = new List<int>(t.Length);
            for (int i = 0; i < t.Length; i += 3)
            {
                if (kill.Contains(Find(id[t[i]]))) continue;
                keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
            }
            m.SetTriangles(keep, s, false);
        }
        m.RecalculateBounds();
        return kill.Count;
    }
}
