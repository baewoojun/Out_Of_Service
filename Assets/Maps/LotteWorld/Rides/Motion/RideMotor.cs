// RideMotor.cs - 놀이기구 움직이는 부품 하나 (회전 / 진자)
// RideDirector.ActivateAll() 이 불리면 startDelay 뒤에 삐걱대며(멈칫멈칫) 천천히 빨라짐.
// 처음 각도(Start 시점 localRotation)를 기준으로 axis(로컬) 둘레로 돌림.
using UnityEngine;

namespace LotteWorld.Rides
{
    [DisallowMultipleComponent]
    public class RideMotor : MonoBehaviour
    {
        public enum Mode { Spin, Pendulum }
        /// <summary>축 정하는 법. FBX 루트가 X 90° 누워 들어오는 경우가 있어 로컬 축을 짐작하지 않고 실제 모습으로 정함</summary>
        public enum AxisMode { Local, WorldUp, Thinnest, NarrowHorizontal }

        [Header("동작")]
        public Mode mode = Mode.Spin;
        [Tooltip("축 정하는 법: Local = 아래 axis 그대로 / WorldUp = 세상 위쪽(회전목마·바구니) / Thinnest = 부품이 제일 얇은 방향(풍차 날개) / NarrowHorizontal = 수평 방향 중 짧은 쪽(바이킹 배 폭)")]
        public AxisMode axisMode = AxisMode.Local;
        [Tooltip("로컬 회전축 (axisMode = Local 일 때). 다른 모드면 시작할 때 계산해서 여기 적음")] public Vector3 axis = Vector3.up;
        [Tooltip("Spin: 초당 도")] public float degPerSec = 20f;
        [Tooltip("Pendulum: 최대 각도(도)")] public float amplitude = 40f;
        [Tooltip("Pendulum: 한 번 왕복 시간(초)")] public float period = 8f;

        [Header("켜지는 연출")]
        [Tooltip("ActivateAll 뒤 기다리는 시간(초). RideDirector 가 덮어씀")] public float startDelay = 0f;
        [Tooltip("최고 속도까지 걸리는 시간(초)")] public float spinUpTime = 8f;
        [Tooltip("켜질 때 멈칫거림 정도 0~1")] [Range(0, 1)] public float stutter = 0.6f;

        [Header("소리 (선택)")]
        [Tooltip("돌 때 반복 재생(삐걱·오르골). 속도에 맞춰 음 높이가 변함")] public AudioSource loopAudio;
        [Tooltip("켜지는 순간 한 번(덜컥)")] public AudioSource startAudio;

        public bool Running { get; private set; }
        public float Power { get; private set; }     // 0~1

        Quaternion rest;
        float baseDelay, noiseSeed;
        float angle, activatedAt = -1f, phase;
        bool started;

        void Awake()
        {
            rest = transform.localRotation;
            baseDelay = startDelay;
            ResolveAxis();
            noiseSeed = Random.Range(0f, 100f);
            if (axis.sqrMagnitude < 1e-6f) axis = Vector3.up;
        }

        void ResolveAxis()
        {
            if (axisMode == AxisMode.WorldUp)
            {
                axis = transform.InverseTransformDirection(Vector3.up).normalized;
                return;
            }
            if (axisMode == AxisMode.Local) return;
            // 자식 메시 범위를 이 부품의 로컬 좌표로 모음
            Vector3 mn = new Vector3(1e9f, 1e9f, 1e9f), mx = new Vector3(-1e9f, -1e9f, -1e9f); bool any = false;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Bounds b = mf.sharedMesh.bounds;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 c = new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 2) == 0 ? b.min.y : b.max.y, (k & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 p = transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                    mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); any = true;
                }
            }
            if (!any) { Debug.LogWarning("[Rides] " + name + ": 메시가 없어 축을 못 정함, axis 그대로 씀"); return; }
            Vector3 size = mx - mn;
            Vector3[] basis = { Vector3.right, Vector3.up, Vector3.forward };
            float[] ext = { size.x, size.y, size.z };
            int best = -1;
            for (int k = 0; k < 3; k++)
            {
                if (axisMode == AxisMode.NarrowHorizontal)
                {
                    Vector3 w = transform.TransformDirection(basis[k]).normalized;
                    if (Mathf.Abs(Vector3.Dot(w, Vector3.up)) > 0.5f) continue;   // 수평인 축만
                }
                if (best < 0 || ext[k] < ext[best]) best = k;
            }
            if (best >= 0) axis = basis[best];
            Debug.Log(string.Format("[Rides] {0}: 축 {1} (부품 크기 {2:0.0} x {3:0.0} x {4:0.0} m)", name, axis, size.x, size.y, size.z));
        }

        void OnEnable() { RideDirector.Register(this); }
        void OnDisable() { RideDirector.Unregister(this); }

        /// <summary>RideDirector 가 부름. 이미 켜졌으면 무시.</summary>
        public void Activate()
        {
            if (activatedAt >= 0f) return;
            activatedAt = Time.time;
        }

        /// <summary>멈추고 처음 자세로 (테스트용)</summary>
        public void ResetMotor()
        {
            activatedAt = -1f; started = false; Running = false; Power = 0f; angle = 0f; phase = 0f; startDelay = baseDelay;
            transform.localRotation = rest;
            if (loopAudio) loopAudio.Stop();
        }

        void Update()
        {
            if (activatedAt < 0f) return;
            float since = Time.time - activatedAt - startDelay;
            if (since < 0f) return;
            if (!started)
            {
                started = true; Running = true;
                if (startAudio) startAudio.Play();
                if (loopAudio && !loopAudio.isPlaying) { loopAudio.loop = true; loopAudio.Play(); }
            }

            // 켜지는 곡선: 0→1 (smoothstep) + 처음엔 멈칫멈칫 (1초에 몇 번 거의 멈춤)
            float ramp = spinUpTime > 0.01f ? Mathf.Clamp01(since / spinUpTime) : 1f;
            float target = ramp * ramp * (3f - 2f * ramp);
            if (stutter > 0f && ramp < 1f)
            {
                float n = Mathf.PerlinNoise(since * 2.3f, noiseSeed);
                float hitch = Mathf.Clamp01((n - 0.55f) * 4f);       // 가끔 0→1
                target *= 1f - stutter * hitch * (1f - ramp);
            }
            Power = Mathf.MoveTowards(Power, target, Time.deltaTime * 1.5f);

            if (mode == Mode.Spin)
            {
                angle = Mathf.Repeat(angle + degPerSec * Power * Time.deltaTime, 360f);
            }
            else
            {
                // 진자: 진폭이 Power 따라 커짐. 위상은 Power 가 낮을 때 천천히 감 (그네 밀듯)
                float w = 2f * Mathf.PI / Mathf.Max(0.5f, period);
                phase += w * Time.deltaTime * Mathf.Lerp(0.6f, 1f, Power);
                angle = amplitude * Power * Mathf.Sin(phase);
            }
            transform.localRotation = rest * Quaternion.AngleAxis(angle, axis.normalized);

            if (loopAudio) { loopAudio.pitch = Mathf.Lerp(0.6f, 1f, Power); loopAudio.volume = Mathf.Lerp(0.2f, 1f, Power); }
        }
    }
}
