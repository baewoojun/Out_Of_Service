// BalloonRail.cs - 천장 레일에 매달린 열기구(BalloonRail_<색> 프리팹)를 레일 따라 한 바퀴 돌림. 루트 'BalloonRail'에 붙음.
// 레일 중심선 = BalloonRailPathData (rail.py v3 경로: 실제 씬 건물·기둥 피함, 블렌더 (x, y) = Unity 월드 (x, z), 높이 RailZ = 레일 아래 플랜지 밑면).
// 열기구 하나 = Car_NN (경로 위 위치·방향) / 그 아래 FBX 인스턴스 / Static(대차) + Swing(팔·풍선·바구니, 힌지 피벗).
// 레일 메시 = BalloonRail_Track.fbx (설정 메뉴가 루트 밑 Track 으로 놓음, 월드 좌표 그대로).
// 기본: RideDirector.ActivateAll() (탈출 열쇠 다 모음) 뒤에 출발 (RideDirector 에 등록 → 다른 놀이기구처럼 출발 시차·Shift+F8 되돌리기).
//       alwaysMove 켜면 시작부터 돎.
// 가속·감속 때 풍선이 힌지에서 앞뒤로 흔들림(진자), 달리는 동안 살짝 흔들림.
// 11/20 v3: 집컴의 옛 생성기 버전(Balloon Rail (Ceiling))을 대체. RideDirector.Register/Unregister 사용.
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.Rides
{
    [DisallowMultipleComponent]
    public class BalloonRail : MonoBehaviour
    {
        [Header("움직임")]
        [Tooltip("레일 위 속도 (m/s). 1.2 이면 한 바퀴 약 11분")] public float speed = 1.2f;
        [Tooltip("반대 방향으로 돌기")] public bool reverse = false;
        [Tooltip("켜지고 최고 속도까지 걸리는 시간(초)")] public float spinUpTime = 10f;
        [Tooltip("켜질 때 멈칫멈칫")] public bool stutter = true;
        [Tooltip("RideDirector 가 출발 시차를 더함")] public float startDelay = 0f;

        [Header("언제 움직이나")]
        [Tooltip("끄면 RideDirector.ActivateAll() 때 출발 (열쇠 다 모았을 때)")] public bool alwaysMove = false;

        [Header("흔들림")]
        [Tooltip("힌지에서 풍선 무게중심까지 (m) - 진자 길이")] public float pendulumLength = 7f;
        [Tooltip("감쇠 (클수록 빨리 멈춤)")] public float damping = 0.6f;
        [Tooltip("달리는 동안 잔흔들림 (도)")] public float idleSway = 0.8f;
        [Tooltip("흔들림 최대 (도)")] public float maxSwing = 12f;

        [Header("레일 위치 맞추기")]
        [Tooltip("레일 높이 보정 (m). 실제 레일 메시 밑면과 대차 바퀴가 어긋나면 조절")] public float heightOffset = 0f;
        public bool flipX = false, flipZ = false;
        [Tooltip("Scene 뷰에 레일 중심선 그리기")] public bool drawGizmo = true;

        [Header("열기구 (설정 메뉴가 채움)")]
        public List<Transform> cars = new List<Transform>();
        public AudioSource loopAudio;

        public float Power { get; private set; }

        Vector3[] P; float[] S; float L;
        readonly List<float> s = new List<float>();
        readonly List<Quaternion> q0 = new List<Quaternion>();
        readonly List<Transform> swing = new List<Transform>();
        readonly List<Quaternion> swingBase = new List<Quaternion>();
        readonly List<Vector3> swingAxis = new List<Vector3>();
        readonly List<float> th = new List<float>(), thv = new List<float>(), phase = new List<float>();
        readonly List<Vector3> startPos = new List<Vector3>();
        readonly List<Quaternion> startRot = new List<Quaternion>();
        float activatedAt = -1f, vPrev, aSmooth, baseDelay;
        bool ready, started;

        // ------------------------------------------------------------------ 경로
        public static Vector3 Map(float bx, float by, float z, bool fx, bool fz) { return new Vector3(fx ? -bx : bx, z, fz ? -by : by); }

        public void BuildPath()
        {
            var B = BalloonRailPathData.BlenderXY; int n = B.Length / 2;
            P = new Vector3[n]; S = new float[n + 1];
            for (int i = 0; i < n; i++) P[i] = Map(B[2 * i], B[2 * i + 1], BalloonRailPathData.RailZ + heightOffset, flipX, flipZ);
            if (reverse) System.Array.Reverse(P);
            for (int i = 0; i < n; i++) S[i + 1] = S[i] + Vector3.Distance(P[i], P[(i + 1) % n]);
            L = S[n];
        }
        public float Length { get { if (P == null) BuildPath(); return L; } }

        /// <summary>경로 위 d (m) 지점, 루트 로컬 좌표</summary>
        public Vector3 PointAt(float d)
        {
            if (P == null) BuildPath();
            d = Mathf.Repeat(d, L); int n = P.Length;
            int lo = 0, hi = n;
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (S[mid] <= d) lo = mid; else hi = mid; }
            float f = (d - S[lo]) / Mathf.Max(1e-4f, S[lo + 1] - S[lo]);
            return Vector3.Lerp(P[lo], P[(lo + 1) % n], f);
        }

        /// <summary>경로 위 d 지점의 월드 방향 (진행 방향 = +Z, 위 = +Y)</summary>
        public Quaternion FrameAt(float d)
        {
            Vector3 a = transform.TransformPoint(PointAt(d - 3f)), c = transform.TransformPoint(PointAt(d + 3f));
            Vector3 t = c - a; t.y = 0f; if (t.sqrMagnitude < 1e-6f) t = transform.forward;
            return Quaternion.LookRotation(t.normalized, Vector3.up);
        }

        public float Nearest(Vector3 localPos)
        {
            if (P == null) BuildPath();
            int k = 0; float m = float.MaxValue;
            for (int i = 0; i < P.Length; i++) { float d = (P[i] - localPos).sqrMagnitude; if (d < m) { m = d; k = i; } }
            return S[k];
        }

        // ------------------------------------------------------------------ 준비
        void OnEnable() { RideDirector.Register(this); }
        void OnDisable() { RideDirector.Unregister(this); }

        void Start()
        {
            if (!started) { baseDelay = startDelay; started = true; }
            BuildPath();
            s.Clear(); q0.Clear(); swing.Clear(); swingBase.Clear(); swingAxis.Clear(); th.Clear(); thv.Clear(); phase.Clear(); startPos.Clear(); startRot.Clear();
            float worst = 0f; var rng = new System.Random(1120);
            foreach (var c in cars)
            {
                if (!c) continue;
                Vector3 lp = transform.InverseTransformPoint(c.position);
                float d = Nearest(lp); s.Add(d);
                worst = Mathf.Max(worst, Vector3.Distance(PointAt(d), lp));
                q0.Add(Quaternion.Inverse(FrameAt(d)) * c.rotation);
                startPos.Add(c.position); startRot.Add(c.rotation);
                var sw = FindDeep(c, "Swing"); swing.Add(sw);
                swingBase.Add(sw ? sw.localRotation : Quaternion.identity);
                Vector3 lateral = FrameAt(d) * Vector3.right;                       // 진행 방향의 옆 축 = 앞뒤 흔들림 축
                swingAxis.Add(sw && sw.parent ? sw.parent.InverseTransformDirection(lateral).normalized : Vector3.right);
                th.Add(0f); thv.Add(0f); phase.Add((float)rng.NextDouble() * 10f);
            }
            ready = s.Count > 0;
            Debug.Log(string.Format("[BalloonRail] 레일 {0:0} m, 열기구 {1}개, 레일과 떨어진 정도 최대 {2:0.00} m{3}{4}",
                L, s.Count, worst, worst > 1.5f ? "  ← 크다! 메뉴 1 다시 실행하거나 flip 확인" : "",
                alwaysMove ? ", 시작부터 움직임" : ", RideDirector.ActivateAll() 대기"));
            if (alwaysMove) Activate();
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r) return r; }
            return null;
        }

        /// <summary>RideDirector.ActivateAll() 이 부름 (여러 번 불러도 한 번)</summary>
        public void Activate() { if (activatedAt < 0f) activatedAt = Time.time; }

        /// <summary>RideDirector.ResetAll() (Shift+F8) 이 부름: 멈추고 처음 자리로</summary>
        public void ResetMotor()
        {
            activatedAt = -1f; Power = 0f; vPrev = 0f; aSmooth = 0f; startDelay = baseDelay;
            for (int i = 0; i < s.Count && i < cars.Count; i++)
            {
                if (!cars[i]) continue;
                cars[i].SetPositionAndRotation(startPos[i], startRot[i]);
                if (swing[i]) swing[i].localRotation = swingBase[i];
                th[i] = 0f; thv[i] = 0f;
            }
            if (loopAudio) loopAudio.Stop();
            if (alwaysMove) Activate();
        }

        // ------------------------------------------------------------------ 매 프레임
        void Update()
        {
            if (!ready || activatedAt < 0f) return;
            float since = Time.time - activatedAt - startDelay;
            if (since < 0f) return;
            if (loopAudio && !loopAudio.isPlaying) { loopAudio.loop = true; loopAudio.Play(); }

            float ramp = spinUpTime > 0.01f ? Mathf.Clamp01(since / spinUpTime) : 1f;
            float pw = ramp * ramp * (3f - 2f * ramp);
            if (stutter && since < 3.2f && ((since > 0.5f && since < 1.1f) || (since > 1.8f && since < 2.3f))) pw = 0f;   // 출발할 때 두 번 멈칫
            Power = pw;

            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            float v = speed * Power;
            float a = (v - vPrev) / dt; vPrev = v;
            aSmooth = Mathf.Lerp(aSmooth, a, 1f - Mathf.Exp(-dt * 6f));
            float g = 9.81f, len = Mathf.Max(1f, pendulumLength), lim = maxSwing * Mathf.Deg2Rad;

            for (int i = 0; i < s.Count; i++)
            {
                var c = cars[i]; if (!c) continue;
                s[i] = Mathf.Repeat(s[i] + v * dt, L);
                c.SetPositionAndRotation(transform.TransformPoint(PointAt(s[i])), FrameAt(s[i]) * q0[i]);
                if (!swing[i]) continue;
                // 진자: 앞으로 가속하면 풍선이 뒤로 처짐 (+각 = 아래쪽이 뒤로)
                float acc = -(g / len) * th[i] - damping * thv[i] + aSmooth / len;
                thv[i] += acc * dt; th[i] = Mathf.Clamp(th[i] + thv[i] * dt, -lim, lim);
                float idle = idleSway * Mathf.Deg2Rad * Power * Mathf.Sin(Time.time * 0.9f + phase[i]);
                float roll = idleSway * 0.6f * Power * Mathf.Sin(Time.time * 0.7f + phase[i] * 1.7f);
                swing[i].localRotation = Quaternion.AngleAxis((th[i] + idle) * Mathf.Rad2Deg, swingAxis[i])
                                       * Quaternion.AngleAxis(roll, swing[i].parent ? swing[i].parent.InverseTransformDirection(c.forward) : Vector3.forward)
                                       * swingBase[i];
            }
            if (loopAudio) loopAudio.volume = Mathf.Lerp(0.2f, 1f, Power);
        }

        void OnValidate() { P = null; }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            BuildPath();
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            for (int i = 0; i < P.Length; i++) Gizmos.DrawLine(transform.TransformPoint(P[i]), transform.TransformPoint(P[(i + 1) % P.Length]));
        }
    }
}
