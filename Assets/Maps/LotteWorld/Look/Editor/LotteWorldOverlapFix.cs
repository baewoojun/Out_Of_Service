// LotteWorldOverlapFix.cs - 실내 씬에서 서로 뚫고 들어간 소품·나무·바위를 찾아 가장 가까운 빈자리로 옮긴다
// 메뉴: Tools > LotteWorld > Look > 겹침 검사 (바꾸지 않음) / 겹침 자동 조정 / 마지막 겹침 조정 되돌리기
// - 검사 방법: 소품 메시의 삼각형 모서리를 뽑아 다른 오브젝트의 충돌체를 가로지르는지 센다 (바닥에 닿은 것은 제외)
// - 옮기는 대상: 소품(Props, Fest, Interior, Deco), 나무·화분(FoliageNew), 바위(RocksNew), 매점
//   건물·놀이기구(LotteWorld_Baked, Bld_*, Rink, 기둥, 외벽)는 기준이라 옮기지 않는다
// - 건드리지 않는 것: 벽에 붙는 얇은 것(액자·징두리판·벽등), 줄 깃발·현수막·매단 등, 바닥 쓰레기. 바위끼리 겹친 것(돌무더기)
// - 옮기는 규칙: 수평으로만, 가까운 자리부터 0.1 m ~ 최대 4 m, 벽을 넘어가지 않고 바닥이 있는 자리로. 빈자리가 없으면 그대로 두고 보고한다
// - 같은 자리에 똑같은 것이 두 개 있으면 하나를 끈다 (지우지 않음)
// - 한 일은 Look/OverlapFixLog.txt 에 적어 두고 [되돌리기]가 그걸 읽어 원래대로 돌린다. 씬 저장은 직접
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Look
{
    public static class LotteWorldOverlapFix
    {
        const string IndoorScene = "Map3_LotteWorld";
        const string LogPath = "Assets/Maps/LotteWorld/Look/OverlapFixLog.txt";
        static readonly HashSet<string> StructRoots = new HashSet<string> { "LotteWorld_Baked", "LotteWorld_Rink", "LotteWorld_Shell_Columns", "LotteWorld_Facade", "LotteWorld_CliffNew" };
        static readonly HashSet<string> SkipRoots = new HashSet<string> { "Blockout", "LotteWorld_Shell", "PlayerCapsule", "MainCamera", "LW_DayLook", "LW_WaterFX", "PlayerFollowCamera", "Directional Light", "LotteWorld_RideDirector", "BalloonRail" };
        static readonly string[] NeverMove = { "Prop_Bunting", "Prop_Banner", "Prop_Pendant", "Prop_Wainscot", "Prop_Frame", "Prop_Litter", "Prop_RoomLitter", "Prop_MenuBoard", "Prop_Sconce", "Decal" };

        const int EdgesPerUnit = 750;
        const float SevereFrac = 0.04f; const int SevereCount = 8;     // 이보다 많이 가로지르면 "뚫고 들어감"
        const float ClearFrac = 0.012f; const int ClearCount = 3;       // 이보다 적으면 "빈자리"
        static readonly float[] Radii = { 0.1f, 0.2f, 0.3f, 0.45f, 0.6f, 0.8f, 1f, 1.5f, 2f, 2.5f, 3f, 4f };
        const int Dirs = 12;

        class Unit
        {
            public Transform t; public bool isStruct; public Bounds bounds;
            public List<MeshRenderer> rends = new List<MeshRenderer>();
            public Vector3[] ea, eb;   // 뽑아 둔 모서리 (월드 좌표)
        }

        static Dictionary<Transform, Unit> units;
        static readonly RaycastHit[] Buf = new RaycastHit[48];

        [MenuItem("Tools/LotteWorld/Look/겹침 검사 (바꾸지 않음)", false, 100)]
        static void CheckMenu() => Debug.Log(Run(false));
        [MenuItem("Tools/LotteWorld/Look/겹침 자동 조정", false, 101)]
        static void FixMenu() => Debug.Log(Run(true));
        [MenuItem("Tools/LotteWorld/Look/마지막 겹침 조정 되돌리기", false, 102)]
        static void RevertMenu() => Debug.Log(Revert());

        // ------------------------------------------------------------ 유닛 묶기

        static bool Build(out Scene scene)
        {
            scene = SceneManager.GetSceneByName(IndoorScene);
            if (!scene.IsValid() || !scene.isLoaded) return false;
            units = new Dictionary<Transform, Unit>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!root.activeSelf || SkipRoots.Contains(root.name)) continue;
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!r.enabled) continue;
                    var ut = UnitTransform(r.transform, out bool st);
                    if (!units.TryGetValue(ut, out var u)) { u = new Unit { t = ut, isStruct = st, bounds = r.bounds }; units[ut] = u; }
                    else u.bounds.Encapsulate(r.bounds);
                    u.rends.Add(r);
                }
            }
            return true;
        }

        static Transform UnitTransform(Transform t, out bool isStruct)
        {
            var chain = new List<Transform>(); for (var q = t; q != null; q = q.parent) chain.Insert(0, q);
            string rn = chain[0].name;
            isStruct = StructRoots.Contains(rn) || rn.StartsWith("Bld_");
            if (rn.StartsWith("Bld_") || rn.StartsWith("Deco_") || rn == "LotteWorld_Rink") return chain[0];
            if (rn == "LotteWorld_Deco" || rn == "LotteWorld_Fest" || rn == "LotteWorld_Interior") return chain[Mathf.Min(2, chain.Count - 1)];
            return chain[Mathf.Min(1, chain.Count - 1)];
        }

        static Unit UnitOfCollider(Collider c)
        {
            var mr = c.GetComponent<MeshRenderer>();
            if (mr != null && !mr.enabled) return null;          // 렌더러를 꺼 둔 옛 모델의 충돌체는 "안 보이는 것"이라 겹침으로 치지 않는다
            for (var q = c.transform; q != null; q = q.parent) if (units.TryGetValue(q, out var u)) return u;
            return null;
        }

        static bool Movable(Unit u)
        {
            if (u.isStruct) return false;
            if (u.bounds.size.x > 30f || u.bounds.size.z > 30f) return false;
            if (Mathf.Min(u.bounds.size.x, Mathf.Min(u.bounds.size.y, u.bounds.size.z)) < 0.15f) return false;   // 벽·바닥에 붙는 얇은 것
            foreach (var n in NeverMove) if (u.t.name.StartsWith(n)) return false;
            return true;
        }

        static void SampleEdges(Unit u)
        {
            if (u.ea != null) return;
            var a = new List<Vector3>(); var b = new List<Vector3>();
            int totalTris = 0; var meshes = new List<(Mesh, Matrix4x4)>();
            foreach (var r in u.rends)
            {
                if (r.name.Contains("LOD1") || r.name.Contains("LOD2") || r.name.Contains("LOD3")) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                meshes.Add((mf.sharedMesh, r.transform.localToWorldMatrix)); totalTris += mf.sharedMesh.triangles.Length / 3;
            }
            int step = Mathf.Max(1, totalTris * 3 / EdgesPerUnit);
            foreach (var (m, l2w) in meshes)
            {
                var vs = m.vertices; var ts = m.triangles; int T = ts.Length / 3;
                for (int ti = 0; ti < T; ti += step)
                    for (int e = 0; e < 3; e++)
                    {
                        var p = l2w.MultiplyPoint3x4(vs[ts[ti * 3 + e]]); var q = l2w.MultiplyPoint3x4(vs[ts[ti * 3 + (e + 1) % 3]]);
                        if ((q - p).sqrMagnitude < 0.0004f) continue;
                        a.Add(p); b.Add(q);
                    }
            }
            u.ea = a.ToArray(); u.eb = b.ToArray();
        }

        /// <summary>유닛을 offset 만큼 옮겼다고 치고, 다른 오브젝트를 가로지르는 모서리 수를 센다. worst = 가장 많이 걸린 상대.</summary>
        static int Crossings(Unit u, Vector3 offset, out Unit worst)
        {
            SampleEdges(u);
            worst = null; int total = 0; Dictionary<Unit, int> per = null;
            float floorY = u.bounds.min.y + 0.2f;
            bool rock = u.t.name.StartsWith("Rock_");
            for (int i = 0; i < u.ea.Length; i++)
            {
                var p = u.ea[i] + offset; var d = u.eb[i] - u.ea[i]; float len = d.magnitude;
                int n = Physics.RaycastNonAlloc(p, d / len, Buf, len, ~0, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++)
                {
                    if (Mathf.Abs(Buf[k].normal.y) > 0.7f && Buf[k].point.y < floorY) continue;     // 바닥에 닿은 것
                    var ou = UnitOfCollider(Buf[k].collider); if (ou == null || ou == u) continue;
                    if (rock && ou.t.name.StartsWith("Rock_")) continue;                             // 돌무더기
                    total++;
                    if (per == null) per = new Dictionary<Unit, int>();
                    per.TryGetValue(ou, out int c); per[ou] = c + 1;
                }
            }
            if (per != null) { int best = 0; foreach (var kv in per) if (kv.Value > best) { best = kv.Value; worst = kv.Key; } }
            return total;
        }

        static bool Severe(Unit u, int count) { return count >= SevereCount && count > u.ea.Length * SevereFrac; }
        static bool Clear(Unit u, int count) { return count <= ClearCount || count <= u.ea.Length * ClearFrac; }

        static bool PathBlocked(Unit u, Vector3 offset)
        {
            var from = u.bounds.center; float len = offset.magnitude;
            int n = Physics.RaycastNonAlloc(from, offset / len, Buf, len, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++) { var ou = UnitOfCollider(Buf[k].collider); if (ou != null && ou != u && ou.isStruct) return true; }
            return false;
        }

        static bool HasFloor(Unit u, Vector3 offset)
        {
            var p = new Vector3(u.bounds.center.x, u.bounds.min.y + 0.5f, u.bounds.center.z) + offset;
            int n = Physics.RaycastNonAlloc(p, Vector3.down, Buf, 1.2f, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++) { var c = Buf[k].collider; bool own = false; for (var q = c.transform; q != null; q = q.parent) if (q == u.t) own = true; if (!own) return true; }
            return false;
        }

        // ------------------------------------------------------------ 검사 / 조정

        public static string Run(bool fix)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "[LW Overlap] Play 모드에서는 쓸 수 없습니다.";
            if (!Build(out var scene)) return "[LW Overlap] " + IndoorScene + " 씬이 열려 있지 않습니다.";
            bool oldBack = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            var log = new StringBuilder(); var report = new StringBuilder();
            int dup = 0, found = 0, moved = 0, stuck = 0;
            try
            {
                Physics.SyncTransforms();

                // 1) 같은 자리의 똑같은 것
                var seen = new Dictionary<string, Unit>();
                foreach (var u in units.Values)
                {
                    var mf = u.rends[0].GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                    var c = u.bounds.center; var s = u.bounds.size;
                    string key = mf.sharedMesh.name + "|" + u.rends.Count + "|" + Mathf.Round(c.x * 4) + "," + Mathf.Round(c.y * 4) + "," + Mathf.Round(c.z * 4) + "|" + Mathf.Round(s.x * 5) + "," + Mathf.Round(s.y * 5) + "," + Mathf.Round(s.z * 5);
                    if (!seen.TryGetValue(key, out var first)) { seen[key] = u; continue; }
                    dup++;
                    report.AppendLine("중복\t" + PathOf(u.t) + "\t= " + PathOf(first.t) + "\t" + c.ToString("0.0"));
                    if (!fix) continue;
                    Undo.RecordObject(u.t.gameObject, "LW 겹침 조정");
                    u.t.gameObject.SetActive(false); EditorUtility.SetDirty(u.t.gameObject);
                    log.AppendLine("OFF\t" + IndexPath(u.t) + "\t" + PathOf(u.t));
                }
                if (fix && dup > 0) { Build(out scene); Physics.SyncTransforms(); }

                // 2) 뚫고 들어간 것
                var list = new List<Unit>(); foreach (var u in units.Values) if (Movable(u)) list.Add(u);
                for (int i = 0; i < list.Count; i++)
                {
                    var u = list[i];
                    if (i % 50 == 0 && EditorUtility.DisplayCancelableProgressBar("겹침 검사", u.t.name, (float)i / list.Count)) break;
                    int count = Crossings(u, Vector3.zero, out var worst);
                    if (u.ea.Length == 0 || !Severe(u, count)) continue;
                    found++;
                    string what = PathOf(u.t) + "\t>< " + (worst != null ? PathOf(worst.t) : "?") + "\t" + count + "/" + u.ea.Length + "\t" + u.bounds.center.ToString("0.0");
                    if (!fix) { report.AppendLine("관통\t" + what); continue; }

                    Vector3 best = Vector3.zero; bool ok = false;
                    for (int ri = 0; ri < Radii.Length && !ok; ri++)
                        for (int di = 0; di < Dirs && !ok; di++)
                        {
                            float ang = (di + (ri % 2) * 0.5f) * Mathf.PI * 2f / Dirs;
                            var off = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Radii[ri];
                            if (PathBlocked(u, off) || !HasFloor(u, off)) continue;
                            if (!Clear(u, Crossings(u, off, out _))) continue;
                            best = off; ok = true;
                        }
                    if (!ok) { stuck++; report.AppendLine("못 옮김\t" + what); continue; }

                    var from = u.t.position;
                    Undo.RecordObject(u.t, "LW 겹침 조정");
                    u.t.position = from + best; EditorUtility.SetDirty(u.t);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(u.t);
                    for (int k = 0; k < u.ea.Length; k++) { u.ea[k] += best; u.eb[k] += best; }
                    u.bounds.center += best;
                    Physics.SyncTransforms();
                    moved++;
                    log.AppendLine("MOVE\t" + IndexPath(u.t) + "\t" + V(from) + "\t" + V(from + best) + "\t" + PathOf(u.t));
                    report.AppendLine("옮김 " + best.magnitude.ToString("0.0") + " m\t" + what);
                }
            }
            finally { Physics.queriesHitBackfaces = oldBack; EditorUtility.ClearProgressBar(); }

            if (fix && log.Length > 0)
            {
                File.WriteAllText(LogPath, log.ToString());
                AssetDatabase.ImportAsset(LogPath);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            string reportPath = "Temp/LW_OverlapReport.txt";
            File.WriteAllText(reportPath, report.ToString());
            return fix
                ? string.Format("[LW Overlap] 중복 {0}개 끔, 관통 {1}개 중 {2}개 옮김, {3}개는 빈자리가 없어 그대로 둠. 목록: {4} / 되돌리기 기록: {5}. 씬 저장은 직접 하세요.", dup, found, moved, stuck, reportPath, LogPath)
                : string.Format("[LW Overlap] 중복 {0}개, 관통 {1}개 (바꾸지 않음). 목록: {2}", dup, found, reportPath);
        }

        /// <summary>아직 뚫고 들어가 있는 것마다 무엇에 어디서 걸렸는지, 4 m 안에서 가장 덜 걸리는 자리가 어디인지 적는다 (바꾸지 않음).</summary>
        public static string Diagnose()
        {
            if (!Build(out var scene)) return "씬 없음";
            bool oldBack = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            var sb = new StringBuilder();
            try
            {
                Physics.SyncTransforms();
                foreach (var u in units.Values)
                {
                    if (!Movable(u)) continue;
                    int count = Crossings(u, Vector3.zero, out var worst);
                    if (u.ea.Length == 0 || !Severe(u, count)) continue;
                    // 걸린 자리의 범위
                    Vector3 mn = Vector3.one * 1e9f, mx = Vector3.one * -1e9f; var names = new Dictionary<string, int>();
                    for (int i = 0; i < u.ea.Length; i++)
                    {
                        var d = u.eb[i] - u.ea[i]; float len = d.magnitude;
                        int n = Physics.RaycastNonAlloc(u.ea[i], d / len, Buf, len, ~0, QueryTriggerInteraction.Ignore);
                        for (int k = 0; k < n; k++)
                        {
                            if (Mathf.Abs(Buf[k].normal.y) > 0.7f && Buf[k].point.y < u.bounds.min.y + 0.2f) continue;
                            var ou = UnitOfCollider(Buf[k].collider); if (ou == null || ou == u) continue;
                            mn = Vector3.Min(mn, Buf[k].point); mx = Vector3.Max(mx, Buf[k].point);
                            names.TryGetValue(Buf[k].collider.name, out int c); names[Buf[k].collider.name] = c + 1;
                        }
                    }
                    int bestAny = count, bestOk = int.MaxValue; Vector3 offAny = Vector3.zero, offOk = Vector3.zero; int blocked = 0, noFloor = 0, tried = 0;
                    foreach (float r in Radii)
                        for (int di = 0; di < 16; di++)
                        {
                            float ang = di * Mathf.PI * 2f / 16; var off = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r; tried++;
                            int c = Crossings(u, off, out _);
                            if (c < bestAny) { bestAny = c; offAny = off; }
                            bool pb = PathBlocked(u, off), hf = HasFloor(u, off);
                            if (pb) blocked++; if (!hf) noFloor++;
                            if (!pb && hf && c < bestOk) { bestOk = c; offOk = off; }
                        }
                    var hits = new StringBuilder(); foreach (var kv in names) hits.Append(kv.Key).Append(':').Append(kv.Value).Append(' ');
                    sb.AppendLine(IndexPath(u.t) + "\t" + PathOf(u.t) + "\tpos " + u.t.position.ToString("0.00") + " rotY " + u.t.eulerAngles.y.ToString("0") + " size " + u.bounds.size.ToString("0.0") + " yMin " + u.bounds.min.y.ToString("0.00")
                        + "\tcross " + count + "/" + u.ea.Length + " hitBox " + ((mn + mx) * 0.5f).ToString("0.0") + " ext " + (mx - mn).ToString("0.0") + " [" + hits + "]"
                        + "\tbestAny " + bestAny + " @" + offAny.ToString("0.0") + " bestAllowed " + (bestOk == int.MaxValue ? "none" : bestOk + " @" + offOk.ToString("0.0")) + " blocked " + blocked + "/" + tried + " noFloor " + noFloor);
                }
            }
            finally { Physics.queriesHitBackfaces = oldBack; }
            File.WriteAllText("Temp/LW_OverlapDiagnose.txt", sb.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// 자동 조정이 못 옮긴 것만 다시 본다 (규칙을 조금 풀어서). 순서대로 시도한다:
        ///  1) 벽을 넘지 않는 자리 중 거의 안 걸리는 곳(모서리의 2.5% 이하) — 벽 굽도리에 살짝 닿는 정도는 받아들인다
        ///  2) 0.3 m 이하로 들어 올리면 풀리는 경우 (데크·단 위에 놓인 것)
        ///  3) 벽 속에 파묻혀 있어 어느 쪽으로 가도 "벽을 넘는" 경우: 벽 규칙을 풀고 가장 가까운 빈자리 (실내 소품은 같은 방 소품들이 모인 쪽으로만)
        /// 그래도 안 되면 그대로 둔다. 기록은 같은 OverlapFixLog.txt 뒤에 붙인다.
        /// </summary>
        public static string FixRelaxed()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "[LW Overlap] Play 모드에서는 쓸 수 없습니다.";
            if (!Build(out var scene)) return "[LW Overlap] " + IndoorScene + " 씬이 열려 있지 않습니다.";
            bool oldBack = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            var log = new StringBuilder(); var report = new StringBuilder(); int moved = 0, left = 0;
            var radii = new List<float>(Radii); radii.AddRange(new[] { 5f, 6f, 8f });
            try
            {
                Physics.SyncTransforms();
                var list = new List<Unit>(); foreach (var u in units.Values) if (Movable(u)) list.Add(u);
                foreach (var u in list)
                {
                    int count = Crossings(u, Vector3.zero, out var worst);
                    if (u.ea.Length == 0 || !Severe(u, count)) continue;
                    string what = PathOf(u.t) + "\t" + count + "/" + u.ea.Length + "\t" + u.bounds.center.ToString("0.0");
                    int loose = Mathf.Max(ClearCount, Mathf.FloorToInt(u.ea.Length * 0.025f));

                    // 같은 방 소품들이 모인 쪽
                    Vector3 toRoom = Vector3.zero;
                    if (u.t.root.name == "LotteWorld_Interior" && u.t.parent != null && u.t.parent.childCount > 2)
                    {
                        Vector3 sum = Vector3.zero; int n = 0;
                        foreach (Transform s in u.t.parent) if (s != u.t) { sum += s.position; n++; }
                        toRoom = sum / n - u.t.position; toRoom.y = 0f;
                    }

                    Vector3 best = Vector3.zero; string how = null;
                    for (int ri = 0; ri < radii.Count && how == null; ri++)
                        for (int di = 0; di < 16 && how == null; di++)
                        {
                            float ang = di * Mathf.PI * 2f / 16; var off = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radii[ri];
                            if (PathBlocked(u, off) || !HasFloor(u, off)) continue;
                            if (Crossings(u, off, out _) <= loose) { best = off; how = "벽 안 넘음"; }
                        }
                    if (how == null)
                        foreach (float up in new[] { 0.1f, 0.2f, 0.3f })
                            if (Clear(u, Crossings(u, Vector3.up * up, out _))) { best = Vector3.up * up; how = "들어 올림"; break; }
                    if (how == null)
                        for (int ri = 0; ri < radii.Count && how == null && radii[ri] <= 5f; ri++)
                            for (int di = 0; di < 16 && how == null; di++)
                            {
                                float ang = di * Mathf.PI * 2f / 16; var off = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radii[ri];
                                if (toRoom != Vector3.zero && Vector3.Dot(off, toRoom) <= 0f) continue;
                                if (!HasFloor(u, off)) continue;
                                if (Clear(u, Crossings(u, off, out _))) { best = off; how = "벽 속에서 꺼냄"; }
                            }
                    if (how == null) { left++; report.AppendLine("그대로 둠\t" + what + "\t>< " + (worst != null ? PathOf(worst.t) : "?")); continue; }

                    var from = u.t.position;
                    Undo.RecordObject(u.t, "LW 겹침 조정");
                    u.t.position = from + best; EditorUtility.SetDirty(u.t);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(u.t);
                    for (int k = 0; k < u.ea.Length; k++) { u.ea[k] += best; u.eb[k] += best; }
                    u.bounds.center += best;
                    Physics.SyncTransforms();
                    moved++;
                    log.AppendLine("MOVE\t" + IndexPath(u.t) + "\t" + V(from) + "\t" + V(from + best) + "\t" + PathOf(u.t));
                    report.AppendLine("옮김 " + best.magnitude.ToString("0.0") + " m (" + how + ")\t" + what);
                }
            }
            finally { Physics.queriesHitBackfaces = oldBack; }
            if (log.Length > 0) { File.AppendAllText(LogPath, log.ToString()); AssetDatabase.ImportAsset(LogPath); EditorSceneManager.MarkSceneDirty(scene); }
            File.WriteAllText("Temp/LW_OverlapRelaxed.txt", report.ToString());
            return "[LW Overlap] 다시 본 것 중 " + moved + "개 옮김, " + left + "개 그대로 둠.\n" + report;
        }

        public static string Revert()
        {
            if (!File.Exists(LogPath)) return "[LW Overlap] 되돌릴 기록이 없습니다: " + LogPath;
            var scene = SceneManager.GetSceneByName(IndoorScene);
            if (!scene.IsValid() || !scene.isLoaded) return "[LW Overlap] " + IndoorScene + " 씬이 열려 있지 않습니다.";
            int n = 0, miss = 0;
            foreach (var line in File.ReadAllLines(LogPath))
            {
                var p = line.Split('\t'); if (p.Length < 2) continue;
                var t = Resolve(scene, p[1]);
                if (t == null) { miss++; continue; }
                if (p[0] == "OFF") { Undo.RecordObject(t.gameObject, "LW 겹침 되돌리기"); t.gameObject.SetActive(true); EditorUtility.SetDirty(t.gameObject); n++; }
                else if (p[0] == "MOVE" && p.Length >= 4)
                {
                    var from = P(p[2]); var to = P(p[3]);
                    if ((t.position - to).sqrMagnitude > 0.01f) { miss++; continue; }   // 그 뒤에 손으로 옮긴 것은 건드리지 않는다
                    Undo.RecordObject(t, "LW 겹침 되돌리기"); t.position = from; EditorUtility.SetDirty(t);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t); n++;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return "[LW Overlap] " + n + "개 되돌림" + (miss > 0 ? ", " + miss + "개는 못 찾았거나 그 뒤에 옮겨져 그대로 둠" : "") + ". 씬 저장은 직접 하세요.";
        }

        // ------------------------------------------------------------ 경로

        static string PathOf(Transform t) { string p = t.name; for (var q = t.parent; q != null; q = q.parent) p = q.name + "/" + p; return p; }

        static string IndexPath(Transform t)
        {
            var idx = new List<int>(); var q = t;
            for (; q.parent != null; q = q.parent) idx.Insert(0, q.GetSiblingIndex());
            var roots = q.gameObject.scene.GetRootGameObjects(); int ri = System.Array.IndexOf(roots, q.gameObject);
            var sb = new StringBuilder(ri.ToString()); foreach (int i in idx) sb.Append('/').Append(i);
            return sb.ToString();
        }

        static Transform Resolve(Scene scene, string indexPath)
        {
            var parts = indexPath.Split('/'); var roots = scene.GetRootGameObjects();
            if (!int.TryParse(parts[0], out int ri) || ri < 0 || ri >= roots.Length) return null;
            var t = roots[ri].transform;
            for (int i = 1; i < parts.Length; i++) { if (!int.TryParse(parts[i], out int ci) || ci < 0 || ci >= t.childCount) return null; t = t.GetChild(ci); }
            return t;
        }

        static string V(Vector3 v) { return v.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + v.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + v.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }
        static Vector3 P(string s) { var a = s.Split(','); var ci = System.Globalization.CultureInfo.InvariantCulture; return new Vector3(float.Parse(a[0], ci), float.Parse(a[1], ci), float.Parse(a[2], ci)); }
    }
}
#endif
