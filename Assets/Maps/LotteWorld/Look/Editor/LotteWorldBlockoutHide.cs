// LotteWorldBlockoutHide.cs - 완성 모델과 겹쳐 보이는 블록아웃 상자의 렌더러만 끈다 (충돌체·오브젝트는 그대로)
// 메뉴: Tools > LotteWorld > Look > 겹친 블록아웃 숨기기 / 겹친 블록아웃 다시 보이기
// - 대상은 실내 씬(Map3_LotteWorld)의 Blockout 아래 두 그룹뿐
//     VoidRailing : 링크 둘레의 흰 상자 난간. 링크 모델(LotteWorld_Rink)의 유리 난간을 가리고 있었다
//     ParadeLoop  : 퍼레이드 길 노란 판. 껍데기 바닥(LotteWorld_Shell/Floor) 바로 밑에 깔려 있다
// - 대신할 모델이 씬에 없으면 그 그룹은 건드리지 않는다
// - 오브젝트를 끄거나 지우지 않는 이유: 난간 상자의 충돌체가 추락을 막고, 껍데기 도구가 ParadeLoop 크기를 읽는다
// - 씬 저장은 직접. Undo 됨
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Look
{
    public static class LotteWorldBlockoutHide
    {
        const string IndoorScene = "Map3_LotteWorld";
        //                                  블록아웃 그룹     대신하는 모델 (루트 아래 경로)
        static readonly string[,] Groups =
        {
            { "Blockout/Floor1/VoidRailing", "LotteWorld_Rink" },
            { "Blockout/Floor1/ParadeLoop",  "LotteWorld_Shell/Floor" },
        };

        [MenuItem("Tools/LotteWorld/Look/겹친 블록아웃 숨기기", false, 60)]
        static void HideMenu() => Debug.Log(Set(false));

        [MenuItem("Tools/LotteWorld/Look/겹친 블록아웃 다시 보이기", false, 61)]
        static void ShowMenu() => Debug.Log(Set(true));

        public static string Set(bool visible)
        {
            var scene = SceneManager.GetSceneByName(IndoorScene);
            if (!scene.IsValid() || !scene.isLoaded) return "[LW Blockout] " + IndoorScene + " 씬이 열려 있지 않습니다.";
            var log = new System.Text.StringBuilder("[LW Blockout] ");
            for (int i = 0; i < Groups.GetLength(0); i++)
            {
                var group = Find(scene, Groups[i, 0]);
                if (group == null) { log.Append(Groups[i, 0]).Append(": 없음 / "); continue; }
                if (!visible)
                {
                    var stand = Find(scene, Groups[i, 1]);
                    if (stand == null || !stand.gameObject.activeInHierarchy)
                    { log.Append(group.name).Append(": 대신할 ").Append(Groups[i, 1]).Append(" 이(가) 없어 그대로 둠 / "); continue; }
                }
                int n = 0;
                foreach (var r in group.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.enabled == visible) continue;
                    Undo.RecordObject(r, "LW 블록아웃 렌더러");
                    r.enabled = visible;
                    EditorUtility.SetDirty(r);
                    n++;
                }
                log.Append(group.name).Append(": ").Append(n).Append(visible ? "개 다시 보임 / " : "개 숨김 / ");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return log.Append("씬 저장은 직접 하세요.").ToString();
        }

        static Transform Find(Scene scene, string path)
        {
            int slash = path.IndexOf('/');
            string rootName = slash < 0 ? path : path.Substring(0, slash);
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != rootName) continue;
                return slash < 0 ? go.transform : go.transform.Find(path.Substring(slash + 1));
            }
            return null;
        }
    }
}
#endif
