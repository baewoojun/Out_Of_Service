using UnityEngine;
using UnityEngine.SceneManagement;

namespace OutOfService
{
    // 타이틀 → 맵 → 재시작 / 타이틀 복귀 / 종료를 한곳에서 처리한다.
    // 씬 이름을 바꾸면 여기 두 상수만 고치면 된다.
    public static class GameFlow
    {
        public const string TitleScene = "Title";
        public const string FirstLevelScene = "MarketPrototype_v7";

        public static bool HasTitle => Application.CanStreamedLevelBeLoaded(TitleScene);

        public static void StartGame() => Load(FirstLevelScene);
        public static void ToTitle() => Load(TitleScene);
        public static void Restart() => Load(SceneManager.GetActiveScene().path);

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Load(string scene)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (MarketSettings.IsOpen) MarketSettings.Instance.Close();
#if UNITY_EDITOR
            // 빌드 목록에 없는 씬도 에디터 플레이 중에는 다시 열 수 있게 한다.
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                string path = scene.EndsWith(".unity") ? scene : FindScenePath(scene);
                if (!string.IsNullOrEmpty(path))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                    return;
                }
            }
#endif
            SceneManager.LoadScene(scene);
        }

#if UNITY_EDITOR
        static string FindScenePath(string name)
        {
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets(name + " t:Scene"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return path;
            }
            return null;
        }
#endif
    }
}
