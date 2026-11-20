// LotteWorldFlashlight — 1인칭 MainCamera 자식 Spot 손전등 (F 토글)
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LotteWorld.Night
{
    [DisallowMultipleComponent]
    public class LotteWorldFlashlight : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Light spotLight;
        [SerializeField] Transform followTarget; // 보통 MainCamera
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip toggleClip;

        [Header("State")]
        [Tooltip("시작 시 손전등 켜짐")]
        [SerializeField] bool startOn = true;

        [Header("Follow lag")]
        [SerializeField] float followSpeed = 12f;

        [Header("Battery (optional)")]
        [SerializeField] bool useBattery;
        [Tooltip("배터리 지속 시간(분), 켜져 있는 동안만 소모")]
        [SerializeField] float batteryMinutes = 8f;
        [SerializeField, Range(0f, 1f)] float lowBatteryThreshold = 0.2f;
        [SerializeField] float flickerIntervalMin = 0.35f;
        [SerializeField] float flickerIntervalMax = 1.2f;

        public bool IsOn { get; private set; }

        float _battery01 = 1f;
        float _baseIntensity;
        float _flickerTimer;
        bool _flickerDim;
        Quaternion _lagRotation;
        bool _lagInited;

        void Awake()
        {
            if (spotLight == null)
                spotLight = GetComponent<Light>();
            if (followTarget == null && transform.parent != null)
                followTarget = transform.parent;
            if (spotLight != null)
            {
                _baseIntensity = spotLight.intensity;
                // Start 전/에디터에서 Light.enabled 만 켜져 맵이 밝아지는 것 방지
                spotLight.enabled = false;
            }
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            IsOn = false;
        }

        void Start()
        {
            _battery01 = 1f;
            SetOn(startOn, playSound: false);
        }

        void OnDisable()
        {
            if (spotLight != null)
                spotLight.enabled = false;
        }

        void Update()
        {
            if (WasTogglePressed())
                SetOn(!IsOn);

            if (!IsOn || spotLight == null) return;

            if (useBattery)
            {
                float drainPerSec = batteryMinutes > 0.01f ? (1f / (batteryMinutes * 60f)) : 1f;
                _battery01 = Mathf.Max(0f, _battery01 - drainPerSec * Time.deltaTime);
                if (_battery01 <= 0f)
                {
                    SetOn(false);
                    return;
                }

                if (_battery01 <= lowBatteryThreshold)
                    TickLowBatteryFlicker();
                else
                {
                    _flickerDim = false;
                    spotLight.intensity = _baseIntensity;
                }
            }
        }

        void LateUpdate()
        {
            if (followTarget == null) return;

            // 위치는 카메라 자식 localPosition 유지. 회전만 늦게 따라감.
            if (!_lagInited)
            {
                _lagRotation = followTarget.rotation;
                _lagInited = true;
            }

            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            _lagRotation = Quaternion.Slerp(_lagRotation, followTarget.rotation, t);
            transform.rotation = _lagRotation;
        }

        public void SetOn(bool on) => SetOn(on, playSound: true);

        public void SetOn(bool on, bool playSound)
        {
            bool changed = IsOn != on;
            IsOn = on;

            if (spotLight != null)
            {
                spotLight.enabled = on;
                if (on)
                {
                    spotLight.intensity = _baseIntensity;
                    _flickerDim = false;
                    _flickerTimer = 0f;
                }
            }

            if (playSound && changed)
                PlayToggleSound();
        }

        void TickLowBatteryFlicker()
        {
            _flickerTimer -= Time.deltaTime;
            if (_flickerTimer <= 0f)
            {
                _flickerDim = !_flickerDim;
                _flickerTimer = Random.Range(flickerIntervalMin, flickerIntervalMax);
                if (_flickerDim)
                    _flickerTimer *= 0.25f; // 꺼진 느낌은 짧게
            }
            spotLight.intensity = _flickerDim ? _baseIntensity * Random.Range(0.05f, 0.35f) : _baseIntensity;
        }

        void PlayToggleSound()
        {
            if (toggleClip == null) return;
            if (audioSource != null)
                audioSource.PlayOneShot(toggleClip);
            else
                AudioSource.PlayClipAtPoint(toggleClip, transform.position);
        }

        static bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }

#if UNITY_EDITOR
        /// <summary>에디터에서 Spot Light 기본값 세팅.</summary>
        public void EditorBind(Light light, Transform cam, bool startEnabled)
        {
            spotLight = light;
            followTarget = cam;
            startOn = startEnabled;
            _baseIntensity = light != null ? light.intensity : 280f;
        }
#endif
    }
}
