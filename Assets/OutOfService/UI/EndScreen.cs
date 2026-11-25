using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OutOfService
{
    // 사망('사망') / 탈출('탈출 성공') 화면.
    // MarketGame.Lost / Finished 이벤트를 받거나, 다른 맵에서는 ShowDeath() / ShowEscape() 를 직접 부른다.
    public sealed class EndScreen : MonoBehaviour
    {
        public static EndScreen Instance { get; private set; }

        [SerializeField] CanvasGroup group;
        [SerializeField] UnityEngine.UI.Image backdrop;
        [SerializeField] UnityEngine.UI.Graphic accentLine;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text stats;
        [SerializeField] CanvasGroup buttonGroup;
        [SerializeField] UnityEngine.UI.Button retryButton;
        [SerializeField] UnityEngine.UI.Button titleButton;
        [SerializeField] UnityEngine.UI.Button quitButton;
        [SerializeField] TMP_Text retryLabel;

        [Header("사망")]
        [SerializeField] Color deathBackdrop = new Color(0.035f, 0.001f, 0.002f, 0.95f);
        [SerializeField] Color deathAccent = new Color(0.72f, 0.07f, 0.06f);
        [Header("탈출")]
        [SerializeField] Color escapeBackdrop = new Color(0.012f, 0.018f, 0.02f, 0.96f);
        [SerializeField] Color escapeAccent = new Color(0.78f, 0.70f, 0.45f);

        bool shown, buttonsShown;
        float shownAt;
        string fullSubtitle = "";

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            retryButton.onClick.AddListener(GameFlow.Restart);
            titleButton.onClick.AddListener(GameFlow.ToTitle);
            quitButton.onClick.AddListener(GameFlow.Quit);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            group.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            MarketGame.Lost += ShowDeath;
            MarketGame.Finished += ShowEscape;
        }

        void OnDisable()
        {
            MarketGame.Lost -= ShowDeath;
            MarketGame.Finished -= ShowEscape;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowDeath()
        {
            Show(deathBackdrop, deathAccent, "사망", "마감 방송은 아직 끝나지 않았다.", "다시 시작");
        }

        public void ShowEscape()
        {
            Show(escapeBackdrop, escapeAccent, "탈출 성공", "셔터가 올라가고, 반복되던 마감 방송이 멈췄다.", "처음부터 다시");
        }

        void Show(Color back, Color accent, string heading, string line, string retryText)
        {
            if (shown) return;
            if (PauseMenu.IsOpen) PauseMenu.Instance.Resume();
            shown = true;
            buttonsShown = false;
            shownAt = Time.unscaledTime;
            backdrop.color = back;
            accentLine.color = accent;
            title.text = heading;
            fullSubtitle = line;
            subtitle.text = "";
            if (retryLabel) retryLabel.text = retryText;
            stats.text = StatsText();
            titleButton.gameObject.SetActive(GameFlow.HasTitle);
            buttonGroup.alpha = 0f;
            buttonGroup.interactable = false;
            buttonGroup.blocksRaycasts = false;
            group.gameObject.SetActive(true);
            group.blocksRaycasts = true;
            group.interactable = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (!shown) return;
            float t = Time.unscaledTime - shownAt;
            group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.4f));

            // 제목은 화면이 어두워진 뒤 천천히 떠오르고, 부제는 한 글자씩 찍힌다.
            title.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.6f) / 1.2f));
            int letters = Mathf.Clamp(Mathf.FloorToInt((t - 1.6f) * 22f), 0, fullSubtitle.Length);
            if (subtitle.text.Length != letters) subtitle.text = fullSubtitle.Substring(0, letters);
            stats.alpha = Mathf.Clamp01((t - 2.4f) / 0.8f);

            float buttons = Mathf.Clamp01((t - 2.8f) / 0.6f);
            buttonGroup.alpha = buttons;
            if (!buttonsShown && buttons > 0.5f)
            {
                buttonsShown = true;
                buttonGroup.interactable = true;
                buttonGroup.blocksRaycasts = true;
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
            }
        }

        static string StatsText()
        {
            float seconds = Time.timeSinceLevelLoad;
            string time = $"{(int)(seconds / 60f):00}:{(int)(seconds % 60f):00}";
            var game = MarketGame.Instance;
            return game ? $"버틴 시간  {time}      전원 퓨즈  {game.fuses}/3" : $"버틴 시간  {time}";
        }
    }
}
