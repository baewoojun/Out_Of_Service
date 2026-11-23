// FlumePath.cs - 후룸라이드 배(Boat1~5)를 물길 따라 움직임. Flume_A08 루트에 붙음.
// 물길 = FlumePathData (블렌더 좌표). 루트 로컬 좌표로 바꿀 때 축 뒤집힘은 배 처음 위치로 자동 판별.
// 오르막(끌어올리기)은 느리게, 내리막(낙하)은 빠르게. RideDirector.ActivateAll() 뒤에 출발.
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.Rides
{
    [DisallowMultipleComponent]
    public class FlumePath : MonoBehaviour
    {
        [Tooltip("비워 두면 자식에서 Boat1~Boat5 를 찾음")] public List<Transform> boats = new List<Transform>();
        [Tooltip("평지 속도 (m/s)")] public float speed = 3f;
        [Tooltip("오르막 속도 배율")] public float liftFactor = 0.45f;
        [Tooltip("내리막 최고 속도 배율")] public float dropFactor = 3f;
        public float startDelay = 0f;
        public float spinUpTime = 6f;
        [Tooltip("배 위아래 출렁임 (m)")] public float bob = 0.04f;
        [Header("배 크기·자세 (좁은 U자에서 수로 밖으로 안 튀어나오게)")]
        [Tooltip("배 크기 배율. 1 = 원래(4.5 m). 0.68 (3.05 m) 이면 제일 좁은 U자(반지름 2.5 m)까지 수로 벽에 안 닿음 (전 구간 메시 겹침 검사)")] public float boatScale = 0.68f;
        [Tooltip("굽은 곳에서 배 가운데가 안쪽으로 처지는 만큼 중 바깥으로 되돌리는 비율")] [Range(0, 1)] public float sagShift = 0.75f;
        [Tooltip("피벗에서 뱃머리 끝까지 (m, 배율 1 기준)")] public float bowLength = 2.56f;
        [Tooltip("피벗에서 배 꼬리 끝까지 (m, 배율 1 기준)")] public float sternLength = 1.93f;
        public AudioSource loopAudio;
        [Tooltip("Scene 뷰에 물길 선 그리기")] public bool drawGizmo = true;

        public float Power { get; private set; }

        Vector3[] P;          // 루트 로컬
        float[] S;            // 누적 길이
        float L;
        readonly List<float> s = new List<float>();
        readonly List<float> dz = new List<float>();
        readonly List<Quaternion> q0 = new List<Quaternion>();
        readonly List<Vector3> startPos = new List<Vector3>();
        readonly List<Quaternion> startRot = new List<Quaternion>();
        readonly List<Vector3> startScale = new List<Vector3>();
        float activatedAt = -1f, baseDelay;
        bool ready;

        void OnEnable() { RideDirector.Register(this); }
        void OnDisable() { RideDirector.Unregister(this); }

        void Awake() { baseDelay = startDelay; }
        void Start() { Build(); }

        // 블렌더 (x, y, z=위) → 루트 로컬. FBX 루트가 X 90° 누워 있으면 로컬이 z=위 그대로라 두 경우 다 시험
        static Vector3 Map(float bx, float by, float bz, int sx, int sy, bool zUp)
        { return zUp ? new Vector3(sx * bx, sy * by, bz) : new Vector3(sx * bx, bz, sy * by); }
        static Vector3 Map(float bx, float by, float bz, int sx, int sy) { return Map(bx, by, bz, sx, sy, false); }
        Vector3 upL = Vector3.up;     // 루트 로컬에서 세상 위쪽
        [Tooltip("배 피벗을 물 위 몇 m에 둘지")] public float waterLine = 0.12f;
        static Vector3 MapP(float bx, float by, float bz, int[] perm, int sign)
        {
            var v = new Vector3();
            float[] src = { (sign & 1) != 0 ? -bx : bx, (sign & 2) != 0 ? -by : by, (sign & 4) != 0 ? -bz : bz };
            for (int k = 0; k < 3; k++) v[perm[k]] = src[k];
            return v;
        }

        void Build()
        {
            if (boats.Count == 0)
                for (int i = 1; i <= 9; i++) { var b = FindDeep(transform, "Boat" + i); if (b) boats.Add(b); }
            var B = FlumePathData.Blender; int n = B.Length / 3;
            if (n < 4 || boats.Count == 0) { Debug.LogWarning("[Flume] 물길 또는 배를 못 찾음"); return; }

            // 축 판별: 블렌더 (x, y, z위) → 루트 로컬을 모든 경우(축 순서 6 × 부호 8)로 시험해서
            // 씬에 실제로 깔린 물 메시(Static_Water)와 가장 잘 겹치는 것을 고름. 물 메시를 못 읽으면 배 처음 위치로.
            upL = transform.InverseTransformDirection(Vector3.up).normalized;
            var refPts = new List<Vector3>();     // 루트 로컬
            string refName = "물 메시";
            var water = FindDeep(transform, "Static_Water");
            var wmf = water ? water.GetComponent<MeshFilter>() : null;
            if (wmf && wmf.sharedMesh && wmf.sharedMesh.isReadable)
            {
                var vs = wmf.sharedMesh.vertices;
                for (int i = 0; i < vs.Length; i += Mathf.Max(1, vs.Length / 600)) refPts.Add(transform.InverseTransformPoint(wmf.transform.TransformPoint(vs[i])));
            }
            if (refPts.Count < 20) { refName = "배 처음 위치"; refPts.Clear(); foreach (var b0 in boats) refPts.Add(transform.InverseTransformPoint(b0.position)); }
            int[][] perms = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            int bestPerm = 0, bestSign = 0; float best = float.MaxValue;
            int step = Mathf.Max(1, n / 60);
            for (int pi = 0; pi < perms.Length; pi++)
                for (int sg = 0; sg < 8; sg++)
                {
                    float err = 0f; int cnt = 0;
                    for (int i = 0; i < n; i += step)
                    {
                        Vector3 q = MapP(B[3 * i], B[3 * i + 1], B[3 * i + 2], perms[pi], sg);
                        float m = float.MaxValue;
                        foreach (var r in refPts) { float d = (r - q).sqrMagnitude; if (d < m) m = d; }
                        err += m; cnt++;
                        if (err / cnt > best) break;   // 이미 더 나쁨
                    }
                    err /= Mathf.Max(1, cnt);
                    if (err < best) { best = err; bestPerm = pi; bestSign = sg; }
                }
            var PP = perms[bestPerm]; int SG = bestSign;
            P = new Vector3[n]; S = new float[n + 1];
            for (int i = 0; i < n; i++) P[i] = MapP(B[3 * i], B[3 * i + 1], B[3 * i + 2], PP, SG);
            for (int i = 0; i < n; i++) S[i + 1] = S[i] + Vector3.Distance(P[i], P[(i + 1) % n]);
            L = S[n];
            float rms = Mathf.Sqrt(best);

            s.Clear(); dz.Clear(); q0.Clear(); startPos.Clear(); startRot.Clear(); startScale.Clear();
            foreach (var b in boats)
            {
                startScale.Add(b.localScale);
                Vector3 lp = transform.InverseTransformPoint(b.position);
                int k = 0; float m = float.MaxValue;
                for (int i = 0; i < n; i++) { float d = (P[i] - lp).sqrMagnitude; if (d < m) { m = d; k = i; } }
                s.Add(S[k]);
                dz.Add(waterLine);
                Vector3 piv; Quaternion frame = Pose(S[k], out piv);
                q0.Add(Quaternion.Inverse(frame) * b.rotation);
                startPos.Add(b.position); startRot.Add(b.rotation);
            }
            ready = true;
            for (int i = 0; i < boats.Count; i++) { boats[i].localScale = startScale[i] * boatScale; Place(i, 0f); }
            Vector3 w0 = transform.TransformPoint(P[0]), w1 = transform.TransformPoint(P[n / 2]);
            Debug.Log(string.Format("[Flume] 물길 {0}점, 길이 {1:0} m, 배 {2}척, 축 순서 {3}{4}{5} 부호 {6}, {7}와 평균 오차 {8:0.00} m, 물길 높이 {9:0.0}~{10:0.0} m (세상){11}",
                n, L, boats.Count, PP[0], PP[1], PP[2], SG, refName, rms, Mathf.Min(w0.y, w1.y), Mathf.Max(w0.y, w1.y),
                rms > 1.6f ? "  ← 크다! 물길이 안 맞음" : ""));
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r) return r; }
            return null;
        }

        Vector3 PointAt(float d)
        {
            d = Mathf.Repeat(d, L); int n = P.Length;
            int lo = 0, hi = n;                              // S[lo] <= d < S[lo+1]
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (S[mid] <= d) lo = mid; else hi = mid; }
            float f = (d - S[lo]) / Mathf.Max(1e-4f, S[lo + 1] - S[lo]);
            return Vector3.Lerp(P[lo], P[(lo + 1) % n], f);
        }

        // 배 자세: 뱃머리·꼬리를 물길 선 위에 두고(기차 칸처럼) 그 사이 선을 따라 놓은 뒤, 굽은 곳에서는
        // 가운데가 안쪽으로 처지는 만큼의 sagShift(0.75)를 바깥으로 되돌림 → 좁은 U자에서도 앞뒤·가운데가 고르게 수로 안
        Quaternion Pose(float d, out Vector3 pivotLocal)
        {
            float bl = bowLength * boatScale, sl = sternLength * boatScale;
            Vector3 b = PointAt(d + bl), st = PointAt(d - sl), mid = PointAt(d + (bl - sl) * 0.5f);
            Vector3 ax = b - st; if (ax.sqrMagnitude < 1e-6f) ax = PointAt(d + 1f) - PointAt(d - 1f);
            ax = ax.normalized;
            Vector3 sag = mid - (st + b) * 0.5f;
            pivotLocal = b - ax * bl + sag * sagShift;
            Vector3 axW = transform.TransformDirection(ax);
            return Quaternion.LookRotation(axW, Vector3.up);
        }

        void Place(int i, float wob)
        {
            Vector3 piv; Quaternion fr = Pose(s[i], out piv);
            boats[i].SetPositionAndRotation(transform.TransformPoint(piv + upL * (dz[i] + wob)), fr * q0[i]);
        }

        float Slope(float d)
        {
            Vector3 h = transform.TransformVector(PointAt(d + 2f) - PointAt(d - 2f));   // 세상 기준
            float run = new Vector2(h.x, h.z).magnitude;
            return run > 1e-3f ? h.y / run : 0f;
        }

        public void Activate() { if (activatedAt < 0f) activatedAt = Time.time; }

        public void ResetMotor()
        {
            activatedAt = -1f; Power = 0f; startDelay = baseDelay;
            if (!ready) return;
            for (int i = 0; i < boats.Count; i++) { boats[i].SetPositionAndRotation(startPos[i], startRot[i]); boats[i].localScale = startScale[i]; }
            Build();
            if (loopAudio) loopAudio.Stop();
        }

        void Update()
        {
            if (!ready || activatedAt < 0f) return;
            float since = Time.time - activatedAt - startDelay;
            if (since < 0f) return;
            if (loopAudio && !loopAudio.isPlaying) { loopAudio.loop = true; loopAudio.Play(); }
            float ramp = spinUpTime > 0.01f ? Mathf.Clamp01(since / spinUpTime) : 1f;
            Power = ramp * ramp * (3f - 2f * ramp);

            for (int i = 0; i < boats.Count; i++)
            {
                float sl = Slope(s[i]);
                float fac = sl > 0.04f ? liftFactor : (sl < -0.04f ? Mathf.Min(dropFactor, 1f + 8f * -sl) : 1f);
                s[i] = Mathf.Repeat(s[i] + speed * fac * Power * Time.deltaTime, L);
                Place(i, bob * Power * Mathf.Sin(Time.time * 1.7f + i * 1.3f));
            }
            if (loopAudio) loopAudio.volume = Mathf.Lerp(0.2f, 1f, Power);
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            var B = FlumePathData.Blender; int n = B.Length / 3;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector3 a = P != null ? P[i] : Map(B[3 * i], B[3 * i + 1], B[3 * i + 2], 1, 1);
                Vector3 b = P != null ? P[j] : Map(B[3 * j], B[3 * j + 1], B[3 * j + 2], 1, 1);
                Gizmos.DrawLine(transform.TransformPoint(a), transform.TransformPoint(b));
            }
        }
    }
}
