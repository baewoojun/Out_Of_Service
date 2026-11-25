using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace OutOfService
{
    // Esc 일시정지 메뉴. 이 메뉴가 씬에 있으면 Esc 는 여기서 받고,
    // 기존 설정 화면(MarketSettings)은 '설정' 버튼으로만 연다.
    [DefaultExecutionOrder(-150)]
    public sealed class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public static bool IsOpen => Instance && Instance.open;

        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text objective;
        [SerializeField] UnityEngine.UI.Button resumeButton;
        [SerializeField] UnityEngine.UI.Button settingsButton;
        [SerializeField] UnityEngine.UI.Button restartButton;
        [SerializeField] UnityEngine.UI.Button titleButton;
        [SerializeField] UnityEngine.UI.Button quitButton;

        bool open, inSettings;
        float previousTimeScale = 1f;
        bool previousAudioPause;
        float fade;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(OpenSettings);
            restartButton.onClick.AddListener(() => { Close(false); GameFlow.Restart(); });
            titleButton.onClick.AddListener(() => { Close(false); GameFlow.ToTitle(); });
            quitButton.onClick.AddListener(GameFlow.Quit);
            SetVisible(0f);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            if (open) Close(false);
            Instance = null;
        }

        void Update()
        {
            fade = Mathf.MoveTowards(fade, open && !inSettings ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            SetVisible(fade);

            if (inSettings)
            {
                // 설정 화면을 닫으면(Esc 또는 '게임으로 돌아가기') 일시정지 메뉴로 돌아온다.
                if (!MarketSettings.IsOpen)
                {
                    inSettings = false;
                    ShowCursor();
                    SetVisible(fade);
                    Select(settingsButton);
                }
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (open) { Resume(); return; }
            if (MarketSettings.InputBlocked || !MarketRenderPreparation.IsReady) return;
            if (MarketGame.Instance && (MarketGame.Instance.failed || MarketGame.Instance.finished)) return;
            Pause();
        }

        public void Pause()
        {
            if (open || inSettings) return;
            if (MarketGame.Instance && (MarketGame.Instance.failed || MarketGame.Instance.finished)) return;
            open = true;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            MarketPlayer.Instance?.SuspendInspection();
            ShowCursor();

            settingsButton.gameObject.SetActive(MarketSettings.Instance);
            titleButton.gameObject.SetActive(GameFlow.HasTitle);
            if (objective) objective.text = ObjectiveText();
            SetVisible(fade);
            Select(resumeButton);
        }

        public void Resume() => Close(true);

        void Close(bool lockCursor)
        {
            if (!open) return;
            open = false;
            inSettings = false;
            if (MarketSettings.IsOpen) MarketSettings.Instance.Close();
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OpenSettings()
        {
            if (!MarketSettings.Instance) return;
            inSettings = true;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            MarketSettings.Instance.Open();
        }

        static string ObjectiveText()
        {
            var game = MarketGame.Instance;
            if (!game) return "";
            return game.fuses >= 3
                ? "목표  ·  정문으로 돌아가 셔터를 연다"
                : $"목표  ·  전원 퓨즈를 찾는다  ({game.fuses}/3)";
        }

        void SetVisible(float alpha)
        {
            group.alpha = alpha;
            bool visible = alpha > 0.001f || (open && !inSettings);
            group.blocksRaycasts = open && !inSettings;
            group.interactable = open && !inSettings;
            if (group.gameObject != gameObject && group.gameObject.activeSelf != visible) group.gameObject.SetActive(visible);
        }

        static void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static void Select(UnityEngine.UI.Button button)
        {
            if (EventSystem.current && button && button.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
