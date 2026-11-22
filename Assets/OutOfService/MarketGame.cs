using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OutOfService
{
    public class MarketGame : MonoBehaviour
    {
        public static MarketGame Instance { get; private set; }
        public int fuses;
        public bool finished;
        public bool failed;
        public string message = "마감 방송이 반복된다. 출구를 열 방법을 찾아라.";
        float messageUntil;
        GUIStyle titleStyle, bodyStyle, slotStyle;
        public bool showHud = false;
        Texture2D bloodVignette;
        Texture2D barTexture;
        float damageFlash;
        int damageStacks;

        void Awake()
        {
            Instance = this;
            Show(message, 7f);
            CreateBloodTexture();
            barTexture = Texture2D.whiteTexture;
        }

        void Update()
        {
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 0.38f);
            if (failed && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ShowDamage(int stacks)
        {
            damageStacks = Mathf.Clamp(stacks, 0, 3);
            damageFlash = Mathf.Min(0.8f, damageFlash + 0.46f);
        }

        public void Lose()
        {
            failed = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Show(string text, float seconds = 3f)
        {
            message = text;
            messageUntil = Time.time + seconds;
        }

        public void CollectFuse()
        {
            fuses++;
            Show($"전원 퓨즈 {fuses}/3 확보. 정문으로 돌아가라.", 4f);
        }

        public void Finish()
        {
            finished = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnGUI()
        {
            if (!showHud)
            {
                if (bloodVignette != null && (damageFlash > 0f || damageStacks > 0))
                {
                    Color previous = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(damageStacks * 0.19f + damageFlash));
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodVignette, ScaleMode.StretchToFill, true);
                    GUI.color = previous;
                }
                return;
            }
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
                bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
                slotStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            }
            MarketPlayer player = MarketPlayer.Instance;
            float hudWidth = Mathf.Min(310, Screen.width - 32);
            GUI.Box(new Rect(16, 16, hudWidth, 72), "");
            GUI.Label(new Rect(29, 20, hudWidth - 26, 30), "영업 종료", titleStyle);
            GUI.Label(new Rect(29, 52, hudWidth - 26, 25), $"퓨즈 {fuses}/3", bodyStyle);
            float barWidth = Mathf.Min(180, Screen.width * 0.18f);
            GUI.Box(new Rect(16, Screen.height - 101, barWidth + 32, 65), "");
            GUI.Label(new Rect(27, Screen.height - 96, barWidth, 23), "스태미나", bodyStyle);
            if (player != null)
            {
                DrawBar(new Rect(27, Screen.height - 67, barWidth, 14), player.Stamina, new Color(0.45f, 0.72f, 0.65f));
            }
            float inventoryWidth = Mathf.Min(318, Screen.width - 32);
            float inventoryX = (Screen.width - inventoryWidth) / 2f;
            GUI.Box(new Rect(inventoryX, Screen.height - 130, inventoryWidth, 120), "");
            GUI.Label(new Rect(inventoryX + 12, Screen.height - 123, inventoryWidth - 24, 22), "손전등 배터리", bodyStyle);
            if (player != null)
                DrawBar(new Rect(inventoryX + 12, Screen.height - 98, inventoryWidth - 24, 12), player.Battery, new Color(0.82f, 0.72f, 0.32f));
            string[] slots = { "1  맨손", "2  손전등", "3  계산기" };
            for (int i = 0; i < 3; i++)
            {
                Rect slot = new Rect(inventoryX + 8 + i * ((inventoryWidth - 16) / 3f), Screen.height - 70, (inventoryWidth - 22) / 3f, 53);
                Color previous = GUI.color;
                GUI.color = player != null && player.SelectedSlot == i
                    ? new Color(0.38f, 0.28f, 0.12f, 0.96f)
                    : new Color(0.08f, 0.1f, 0.12f, 0.94f);
                GUI.DrawTexture(slot, barTexture);
                GUI.color = previous;
                GUI.Label(slot, slots[i], slotStyle);
            }
            if (Time.time < messageUntil)
            {
                float messageWidth = Mathf.Min(500, Screen.width - 32);
                GUI.Box(new Rect((Screen.width - messageWidth) / 2f, Screen.height - 190, messageWidth, 55), "");
                GUI.Label(new Rect((Screen.width - messageWidth) / 2f + 15, Screen.height - 178, messageWidth - 30, 35), message, bodyStyle);
            }
            if (bloodVignette != null && (damageFlash > 0f || damageStacks > 0))
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(damageStacks * 0.19f + damageFlash));
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodVignette, ScaleMode.StretchToFill, true);
                GUI.color = previous;
            }
            if (failed)
            {
                GUI.Box(new Rect(Screen.width / 2 - 230, Screen.height / 2 - 65, 460, 130), "");
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 40, 420, 40), "의식을 잃었다", titleStyle);
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 + 5, 420, 35), "R을 눌러 다시 시작", bodyStyle);
            }
            else if (finished)
            {
                GUI.Box(new Rect(Screen.width / 2 - 230, Screen.height / 2 - 65, 460, 130), "");
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 40, 420, 40), "탈출 성공", titleStyle);
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 + 5, 420, 35), "마감 방송이 멈췄다. Esc로 커서를 사용할 수 있다.", bodyStyle);
            }
            else
            {
                GUI.Label(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 12, 20, 25), "+", titleStyle);
            }
#if UNITY_EDITOR
            if (!finished && !failed && GUI.Button(new Rect(Screen.width - 142, Screen.height - 38, 128, 26), "피해 효과 테스트"))
                MarketPlayer.Instance?.TakeDamage(25f);
#endif
        }

        void DrawBar(Rect rect, float fraction, Color fill)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.05f, 0.06f, 0.07f, 0.9f);
            GUI.DrawTexture(rect, barTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(fraction), rect.height - 4), barTexture);
            GUI.color = previous;
        }

        void CreateBloodTexture()
        {
            const int size = 256;
            bloodVignette = new Texture2D(size, size, TextureFormat.RGBA32, false);
            bloodVignette.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float edge = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                    float noise = Mathf.PerlinNoise(x * 0.085f, y * 0.085f) * 0.12f;
                    float alpha = Mathf.SmoothStep(0f, 0.84f, Mathf.Clamp01((edge - 0.43f + noise) / 0.57f));
                    bloodVignette.SetPixel(x, y, new Color(0.42f, 0.015f, 0.02f, alpha));
                }
            bloodVignette.Apply();
        }
    }
}

