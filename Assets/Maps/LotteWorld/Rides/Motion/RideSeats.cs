// RideSeats.cs - 어드벤처 놀이기구 탑승 (회전목마·해적선·회전바구니·후룸, 11/24)
// FBX 안의 이름으로 찾음 (bake_rides_park.py 가 넣음):
//   좌석 "Seat_<그룹>_NN" (엉덩이 자리, 움직이는 그룹 아래라 기구와 같이 움직임) + 자식 "Seat_<그룹>_NN_Fwd" (앞쪽 0.5 m)
//   내리는 곳 "Exit_NN" (고정된 바닥: 회전목마 받침, 해적선 탑승 데크, 바구니 울타리 안, 후룸 승강장·리프트 옆 계단)
// 조작: 좌석 가까이서 E → 앉음 (기구 그룹의 자식이 되어 같이 돎/흔들림/흘러감), 다시 E → 가장 가까운 Exit 로 내림.
//   내리는 곳이 leaveRadius 안에 없으면 못 내림 ("운행 중") — 해적선이 높이 흔들릴 때, 후룸 배가 물길 중간일 때.
// 매직아일랜드 GyroDrop·CometExpress 와 같은 방식 (E 키, CharacterController 끄기, disableWhileSeated).
// 붙이는 건 메뉴 Tools > LotteWorld > Look > 놀이기구 고퀄 2. 탑승 붙이기 (기구별 거리 값도 그 메뉴가 넣음).
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LotteWorld.Rides
{
    [DisallowMultipleComponent]
    public class RideSeats : MonoBehaviour
    {
        [Header("탑승")]
        public bool boardable = true;
        [Tooltip("비우면 Tag Player, 없으면 MainCamera 의 맨 위 부모")] public Transform player;
        [Tooltip("좌석까지 거리 (m). 해적선은 데크에서 배 안쪽 좌석까지라 큼")] public float interactDistance = 2.0f;
        [Tooltip("발이 좌석보다 이만큼 넘게 아래면 못 탐 (해적선: 바닥이 아니라 데크에 올라와야 함)")] public float maxBelowSeat = 2.1f;
        [Tooltip("내릴 때 가장 가까운 Exit 가 이 거리 안에 있어야 함")] public float leaveRadius = 6f;
        [Tooltip("앉았을 때 플레이어 루트 위치 (좌석점 기준: x 옆, y 위, z 앞)")] public Vector3 seatedOffset = new Vector3(0f, -0.55f, 0.1f);
        [Tooltip("앉은 동안 끌 컴포넌트 (이동 스크립트 등). 카메라 마우스 보기는 넣지 말 것")] public Behaviour[] disableWhileSeated;
        [Tooltip("disableWhileSeated 가 비었으면 플레이어 루트의 이동 스크립트(이름에 Move·Movement·Locomotion·Controller 포함)를 자동으로 끔")] public bool autoDisableMovement = true;
        [Tooltip("열쇠 전이라도 앉으면 놀이기구가 켜짐 (공포 연출용)")] public bool seatingStartsRide = false;
        public bool showPrompt = true;
        public AudioSource sitOneShot;

        Transform[] seats, fwds, exits;
        int seatedAt = -1, nearSeat = -1, leaveExit = -1;
        Transform oldParent; CharacterController cc; Rigidbody rb; bool rbWasKinematic;
        readonly List<Behaviour> autoOff = new List<Behaviour>();

        void Awake()
        {
            var s = new List<Transform>(); var x = new List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Seat_") && !t.name.EndsWith("_Fwd")) s.Add(t);
                else if (t.name.StartsWith("Exit_")) x.Add(t);
            }
            s.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); x.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            seats = s.ToArray(); exits = x.ToArray(); fwds = new Transform[seats.Length];
            for (int i = 0; i < seats.Length; i++) fwds[i] = seats[i].Find(seats[i].name + "_Fwd");
            Debug.Log(string.Format("[RideSeats] {0}: 좌석 {1}, 내리는 곳 {2}", name, seats.Length, exits.Length));
        }

        Transform FindPlayer()
        {
            if (player) return player;
            var go = GameObject.FindWithTag("Player");
            if (go) return player = go.transform;
            if (Camera.main) return player = Camera.main.transform.root;
            return null;
        }

        static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current; return kb != null && kb.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        Vector3 Forward(int i)
        {
            Vector3 f = fwds[i] ? fwds[i].position - seats[i].position : seats[i].forward;
            f.y = 0f; return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        }

        int NearestExit(Vector3 p, float within)
        {
            int best = -1; float bd = within * within;
            for (int i = 0; i < exits.Length; i++) { float d = (exits[i].position - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }

        void Update()
        {
            if (!boardable || seats == null || seats.Length == 0) return;
            var p = FindPlayer(); if (p == null) return;
            nearSeat = -1; leaveExit = -1;
            float k = Mathf.Abs(transform.lossyScale.y); if (k < 1e-3f) k = 1f;     // 씬에서 기구를 줄여 놓았으면 거리도 같이 줄임
            if (seatedAt < 0)
            {
                float best = interactDistance * k * interactDistance * k;
                for (int i = 0; i < seats.Length; i++)
                {
                    Vector3 sp = seats[i].position;
                    if (p.position.y < sp.y - maxBelowSeat * k) continue;
                    float d = Mathf.Min((sp - p.position).sqrMagnitude, (sp - (p.position + Vector3.up * 1.0f)).sqrMagnitude);
                    if (d < best) { best = d; nearSeat = i; }
                }
                if (nearSeat >= 0 && InteractPressed()) Sit(p, nearSeat);
            }
            else
            {
                leaveExit = NearestExit(seats[seatedAt].position, leaveRadius * k);
                if (leaveExit >= 0 && InteractPressed()) Stand(p, leaveExit);
            }
        }

        void Sit(Transform p, int i)
        {
            seatedAt = i; oldParent = p.parent;
            cc = p.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
            rb = p.GetComponent<Rigidbody>(); if (rb) { rbWasKinematic = rb.isKinematic; rb.isKinematic = true; }
            if (disableWhileSeated != null && disableWhileSeated.Length > 0) { foreach (var b in disableWhileSeated) if (b) b.enabled = false; }
            else if (autoDisableMovement)
            {
                autoOff.Clear();
                foreach (var b in p.GetComponents<MonoBehaviour>())
                {
                    if (!b || !b.enabled || b == this) continue;
                    string n = b.GetType().Name;
                    if ((n.Contains("Move") || n.Contains("Locomotion") || n.Contains("Controller")) && !n.Contains("Camera") && !n.Contains("Look")
                        && !n.Contains("Flash") && !n.Contains("Light") && !n.Contains("Audio") && !n.Contains("Health") && !n.Contains("Inventory"))
                    { b.enabled = false; autoOff.Add(b); }
                }
                if (autoOff.Count > 0) Debug.Log("[RideSeats] 앉은 동안 끈 것: " + string.Join(", ", autoOff.ConvertAll(b => b.GetType().Name).ToArray()));
            }
            var s = seats[i]; var rot = Quaternion.LookRotation(Forward(i), Vector3.up);
            p.SetParent(s, true);
            p.position = s.position + rot * seatedOffset; p.rotation = rot;
            if (sitOneShot) sitOneShot.Play();
            Debug.Log("[RideSeats] " + name + " 탑승: " + s.name);
            if (seatingStartsRide && !RideDirector.Active) RideDirector.ActivateAll();
        }

        void Stand(Transform p, int e)
        {
            var x = exits[e]; Vector3 pos = x.position;
            p.SetParent(oldParent, true);
            if (Physics.Raycast(pos + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) pos = hit.point;
            Vector3 look = seats[seatedAt].position - pos; look.y = 0f;
            p.position = pos + Vector3.up * 0.05f;
            p.rotation = Quaternion.LookRotation(look.sqrMagnitude > 1e-4f ? -look.normalized : Vector3.forward, Vector3.up);
            if (cc) cc.enabled = true;
            if (rb) rb.isKinematic = rbWasKinematic;
            if (disableWhileSeated != null && disableWhileSeated.Length > 0) { foreach (var b in disableWhileSeated) if (b) b.enabled = true; }
            foreach (var b in autoOff) if (b) b.enabled = true;
            autoOff.Clear();
            Debug.Log("[RideSeats] " + name + " 내림: " + seats[seatedAt].name + " → " + x.name);
            seatedAt = -1;
        }

        void OnGUI()
        {
            if (!showPrompt || !boardable) return;
            string msg = null;
            if (seatedAt >= 0) msg = leaveExit >= 0 ? "E : 내리기" : "운행 중";
            else if (nearSeat >= 0) msg = "E : 탑승";
            if (msg == null) return;
            var st = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height * 0.62f, 300, 40), msg, st);
        }

        /// <summary>디버그: 앉은 좌석</summary>
        public string Status => seatedAt >= 0 ? seats[seatedAt].name : "-";
    }
}
