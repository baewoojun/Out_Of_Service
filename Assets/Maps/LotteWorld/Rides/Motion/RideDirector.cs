// RideDirector.cs - 탈출 열쇠를 다 모으면 놀이기구가 한꺼번에(조금씩 엇갈려) 켜지게 하는 관리자.
//
// ▶ 팀원(열쇠/게임 진행 담당)이 부를 것: 열쇠를 다 모은 순간 한 줄
//       LotteWorld.Rides.RideDirector.ActivateAll();
//   (씬에 RideDirector 가 없어도 정적 함수라 안전. 그 경우 놀이기구만 바로 켜짐)
//
// ▶ 켜질 때 다른 걸 같이 하고 싶으면 (음악·조명 등)
//       인스펙터의 onActivated 이벤트에 연결하거나, 코드에서 RideDirector.Activated += ...;
//
// ▶ 테스트: 에디터 플레이 중 F8 (testKey) = 켜기, Shift+F8 = 처음 상태로
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LotteWorld.Rides
{
    [DisallowMultipleComponent]
    public class RideDirector : MonoBehaviour
    {
        [Header("엇갈려 켜지기")]
        [Tooltip("놀이기구마다 켜지는 시간 차 최대(초)")] public float maxStagger = 3f;
        [Tooltip("플레이어에서 가까운 놀이기구부터 켜짐 (끄면 무작위)")] public bool nearestFirst = true;
        [Tooltip("비워 두면 MainCamera")] public Transform player;
        public int seed = 1120;

        [Header("테스트 (에디터·개발 빌드만)")]
        public bool testKeyEnabled = true;
        [Tooltip("켜기=F8, Shift+F8=되돌리기")] public bool activateOnStart = false;

        [Header("켜질 때 같이 할 것")]
        public UnityEvent onActivated = new UnityEvent();

        public static bool Active { get; private set; }
        public static event Action Activated;

        static readonly List<RideMotor> motors = new List<RideMotor>();
        static readonly List<FlumePath> flumes = new List<FlumePath>();
        static RideDirector instance;

        internal static void Register(RideMotor m) { if (!motors.Contains(m)) motors.Add(m); if (Active) m.Activate(); }
        internal static void Unregister(RideMotor m) { motors.Remove(m); }
        internal static void Register(FlumePath f) { if (!flumes.Contains(f)) flumes.Add(f); if (Active) f.Activate(); }
        internal static void Unregister(FlumePath f) { flumes.Remove(f); }
        static readonly List<BalloonRail> balloonRails = new List<BalloonRail>();
        internal static void Register(BalloonRail r) { if (!balloonRails.Contains(r)) balloonRails.Add(r); if (Active) r.Activate(); }
        internal static void Unregister(BalloonRail r) { balloonRails.Remove(r); }
        static int CountRails() { return balloonRails.Count; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = false; Activated = null; motors.Clear(); flumes.Clear(); balloonRails.Clear(); instance = null; }

        void Awake() { instance = this; }

        // 플레이 시작 때 상태를 Console 에 알림 + 씬에 RideDirector 가 없으면 하나 만들어 F8 이 먹게 함
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (GameObject.Find("LotteWorld_Baked") == null) return;   // 롯데월드 맵이 아닌 씬에서는 아무것도 안 함
            if (instance == null)
            {
                var go = new GameObject("LotteWorld_RideDirector (자동)");
                go.AddComponent<RideDirector>();
            }
            int rails = CountRails();
            if (motors.Count == 0 && flumes.Count == 0 && rails == 0)
                Debug.LogWarning("[Rides] 씬에 놀이기구 동작이 하나도 안 붙어 있어 F8 을 눌러도 아무것도 안 움직임 → 플레이를 끄고 "
                    + "Tools > LotteWorld > Rides (놀이기구 작동) > 1 설정하기 를 누른 뒤 씬 저장");
            else
                Debug.Log(string.Format("[Rides] 준비됨: 부품 {0}개, 후룸 {1}개, 열기구 레일 {2}개 → Game 창을 한 번 클릭하고 F8 (안 되면 메뉴 3 지금 켜기)",
                    motors.Count, flumes.Count, rails));
        }
        void Start() { if (activateOnStart) ActivateAll(); }

        /// <summary>열쇠를 다 모았을 때 부름. 여러 번 불러도 한 번만 켜짐.</summary>
        public static void ActivateAll()
        {
            if (Active) return;
            Active = true;
            if (instance != null) instance.Stagger();
            foreach (var m in motors.ToArray()) if (m) m.Activate();
            foreach (var f in flumes.ToArray()) if (f) f.Activate();
            foreach (var r in balloonRails.ToArray()) if (r) r.Activate();
            Debug.Log(string.Format("[Rides] 놀이기구 켜짐: 부품 {0}개, 후룸 {1}개, 열기구 레일 {2}개", motors.Count, flumes.Count, balloonRails.Count));
            if (instance != null) instance.onActivated.Invoke();
            if (Activated != null) Activated();
        }

        /// <summary>테스트용: 전부 멈추고 처음 자세로</summary>
        public static void ResetAll()
        {
            Active = false;
            foreach (var m in motors.ToArray()) if (m) m.ResetMotor();
            foreach (var f in flumes.ToArray()) if (f) f.ResetMotor();
            foreach (var r in balloonRails.ToArray()) if (r) r.ResetMotor();
        }

        // 같은 놀이기구(루트)의 부품은 같은 지연을 받음 → 바구니 큰 판·작은 판이 같이 출발
        void Stagger()
        {
            var rng = new System.Random(seed);
            Transform p = player;
            if (p == null && Camera.main != null) p = Camera.main.transform;
            var roots = new List<Transform>();
            foreach (var m in motors) if (m && !roots.Contains(RootOf(m.transform))) roots.Add(RootOf(m.transform));
            foreach (var f in flumes) if (f && !roots.Contains(f.transform)) roots.Add(f.transform);
            foreach (var r in balloonRails) if (r && !roots.Contains(r.transform)) roots.Add(r.transform);
            if (nearestFirst && p != null) roots.Sort((a, b) => (a.position - p.position).sqrMagnitude.CompareTo((b.position - p.position).sqrMagnitude));
            else for (int i = roots.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = roots[i]; roots[i] = roots[j]; roots[j] = t; }
            var delay = new Dictionary<Transform, float>();
            for (int i = 0; i < roots.Count; i++)
                delay[roots[i]] = roots.Count > 1 ? maxStagger * i / (roots.Count - 1) + (float)rng.NextDouble() * 0.4f : 0f;
            foreach (var m in motors) if (m) m.startDelay += delay[RootOf(m.transform)];
            foreach (var f in flumes) if (f) f.startDelay += delay[f.transform];
            foreach (var r in balloonRails) if (r) r.startDelay += delay[r.transform];
        }

        static Transform RootOf(Transform t)
        {
            // RideMotor 의 놀이기구 루트 = RideRoot 표시가 있는 조상, 없으면 맨 위
            var r = t.GetComponentInParent<RideRoot>();
            return r != null ? r.transform : t.root;
        }

        void Update()
        {
            if (!testKeyEnabled || !(Application.isEditor || Debug.isDebugBuild)) return;
            bool f8 = false, shift = false;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) { f8 = kb.f8Key.wasPressedThisFrame; shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed; }
#elif ENABLE_LEGACY_INPUT_MANAGER
            f8 = Input.GetKeyDown(KeyCode.F8); shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
            if (!f8) return;
            Debug.Log(shift ? "[Rides] Shift+F8 → 되돌리기" : (Active ? "[Rides] F8 (이미 켜져 있음, 되돌리려면 Shift+F8)" : "[Rides] F8 → 켜기"));
            if (shift) ResetAll(); else ActivateAll();
        }
    }
}
