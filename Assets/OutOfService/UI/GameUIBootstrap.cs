using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace OutOfService
{
    // 플레이 맵(MarketGame 이 있는 씬)이 열리면 일시정지 메뉴와 사망·탈출 화면 프리팹을 자동으로 붙인다.
    // 씬에 직접 넣어 둔 것이 있으면 그것을 쓴다. 프리팹: Assets/OutOfService/UI/Resources/OutOfServiceUI/
    public static class GameUIBootstrap
    {
        const string Folder = "OutOfServiceUI/";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterFirstScene() => Attach();

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

        static void Attach()
        {
            if (!MarketGame.Instance) return;
            bool added = false;
            if (!Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include)) added |= Spawn("PauseMenu");
            if (!Object.FindAnyObjectByType<EndScreen>(FindObjectsInactive.Include)) added |= Spawn("EndScreen");
            if (added) EnsureEventSystem();
        }

        static bool Spawn(string name)
        {
            var prefab = Resources.Load<GameObject>(Folder + name);
            if (!prefab)
            {
                Debug.LogWarning($"[UI] {Folder}{name} 프리팹이 없습니다. 메뉴 Out Of Service ▸ UI ▸ Build Menus 를 실행하세요.");
                return false;
            }
            Object.Instantiate(prefab).name = name;
            return true;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>()) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            go.GetComponent<EventSystem>().sendNavigationEvents = true;
        }
    }
}
