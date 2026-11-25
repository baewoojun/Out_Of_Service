using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OutOfService
{
    // 타이틀 화면. 제목은 고장 난 간판처럼 가끔 깜빡이고,
    // 아래 줄에서는 마감 방송이 한 글자씩 끝없이 반복된다.
    public sealed class TitleMenu : MonoBehaviour
    {
        const string Broadcast = "고객 여러분, 오늘도 저희 매장을 찾아주셔서 감사합니다. 영업이 모두 종료되었습니다…";
        const string Glitch = "■□▒░▓#%&?";

        [SerializeField] CanvasGroup screen;
        [SerializeField] UnityEngine.UI.Image blackout;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text broadcast;
        [SerializeField] UnityEngine.UI.RawImage grain;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Button controlsButton;
        [SerializeField] UnityEngine.UI.Button quitButton;
        [SerializeField] CanvasGroup controlsPanel;
        [SerializeField] TMP_Text controlsBody;
        [SerializeField] UnityEngine.UI.Button controlsCloseButton;

        float flickerUntil, nextFlicker, broadcastClock, leaving = -1f;
        bool controlsOpen;
        Texture2D grainTexture;
        Color32[] grainPixels;
        readonly StringBuilder builder = new StringBuilder();
        readonly System.Random random = new System.Random();

        void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            startButton.onClick.AddListener(StartGame);
            controlsButton.onClick.AddListener(() => SetControls(true));
            controlsCloseButton.onClick.AddListener(() => SetControls(false));
            quitButton.onClick.AddListener(GameFlow.Quit);
            SetControls(false);
            blackout.color = Color.black;
            nextFlicker = 2.5f;
            if (grain) CreateGrain();
        }

        void Start()
        {
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        void OnDestroy()
        {
            if (grainTexture) Destroy(grainTexture);
        }

        void Update()
        {
            float now = Time.unscaledTime;

            // 처음에는 검은 화면에서 서서히 밝아지고, 시작을 누르면 다시 어두워진다.
            float black = leaving >= 0f
                ? Mathf.Clamp01((now - leaving) / 1.1f)
                : 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - 0.3f) / 2.2f);
            blackout.color = new Color(0f, 0f, 0f, black);
            if (leaving >= 0f && now - leaving > 1.25f)
            {
                leaving = float.MaxValue;
                GameFlow.StartGame();
            }

            // 간판 깜빡임: 몇 초에 한 번, 짧게 두세 번 꺼졌다 켜진다.
            if (now > nextFlicker)
            {
                flickerUntil = now + Random.Range(0.12f, 0.35f);
                nextFlicker = now + Random.Range(2.5f, 6.5f);
            }
            float lit = now < flickerUntil ? (Mathf.PerlinNoise(now * 40f, 0f) > 0.5f ? 1f : 0.18f) : 1f;
            title.alpha = lit;
            subtitle.alpha = Mathf.Lerp(0.35f, 0.8f, lit);

            UpdateBroadcast();
            if (grain) UpdateGrain();

            if (controlsOpen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                SetControls(false);
        }

        void StartGame()
        {
            if (leaving >= 0f) return;
            leaving = Time.unscaledTime;
            screen.interactable = false;
        }

        void SetControls(bool show)
        {
            controlsOpen = show;
            controlsPanel.alpha = show ? 1f : 0f;
            controlsPanel.interactable = show;
            controlsPanel.blocksRaycasts = show;
            controlsPanel.gameObject.SetActive(show);
            if (show && controlsBody) controlsBody.text = ControlsText();
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(show ? controlsCloseButton.gameObject : controlsButton.gameObject);
        }

        void UpdateBroadcast()
        {
            // 한 글자씩 찍히고 → 잠시 멈췄다가 → 꺼지고 → 처음부터 다시.
            broadcastClock += Time.unscaledDeltaTime;
            float typing = Broadcast.Length / 14f;
            float cycle = typing + 3.2f + 1.4f;
            float t = broadcastClock % cycle;
            int count = Mathf.Clamp(Mathf.FloorToInt(t * 14f), 0, Broadcast.Length);
            bool off = t > typing + 3.2f;

            builder.Clear();
            if (!off)
            {
                for (int i = 0; i < count; i++)
                {
                    char c = Broadcast[i];
                    builder.Append(c != ' ' && random.NextDouble() < 0.006 ? Glitch[random.Next(Glitch.Length)] : c);
                }
                if (count < Broadcast.Length && (int)(Time.unscaledTime * 3f) % 2 == 0) builder.Append('_');
            }
            broadcast.SetText(builder);
        }

        void CreateGrain()
        {
            const int size = 128;
            grainTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            grainPixels = new Color32[size * size];
            grain.texture = grainTexture;
            UpdateGrain();
        }

        void UpdateGrain()
        {
            if (Time.frameCount % 3 != 0 && grainPixels[0].a != 0) return;
            for (int i = 0; i < grainPixels.Length; i++)
            {
                // Linear 색 공간이라 아주 작은 값도 눈에 잘 보인다. 알파를 낮게 유지할 것.
                byte v = (byte)random.Next(256);
                grainPixels[i] = new Color32(v, v, v, (byte)(1 + random.Next(v > 220 ? 7 : 3)));
            }
            grainTexture.SetPixels32(grainPixels);
            grainTexture.Apply(false);
            grain.uvRect = new Rect((float)random.NextDouble(), (float)random.NextDouble(), Screen.width / 256f, Screen.height / 256f);
        }

        static string ControlsText()
        {
            // 게임 안 설정에서 키를 바꿨다면 바꾼 키를 보여 준다.
            string path = Path.Combine(Application.persistentDataPath, "market-settings-v1.json");
            var data = MarketSettingsStore.Load(path, out _);
            data.Validate();
            string[] names = { "앞으로", "뒤로", "왼쪽", "오른쪽", "달리기", "점프", "상호작용 · 집기", "손전등 켜기 / 끄기", "맨손", "손전등 들기", "물품 돌려보기 (누르고 유지)", "실패 후 다시 시작" };
            var text = new StringBuilder();
            text.Append("<color=#9DA39C>이동</color>    ")
                .Append(data.bindings[0].Display).Append(' ').Append(data.bindings[2].Display).Append(' ')
                .Append(data.bindings[1].Display).Append(' ').Append(data.bindings[3].Display).Append('\n');
            for (int i = 4; i < names.Length; i++)
                text.Append("<color=#9DA39C>").Append(names[i]).Append("</color>    ").Append(data.bindings[i].Display).Append('\n');
            text.Append("<color=#9DA39C>일시정지</color>    Esc\n\n");
            text.Append("<size=80%><color=#7A807A>키는 게임 중 Esc → 설정에서 바꿀 수 있습니다.</color></size>");
            return text.ToString();
        }
    }
}
