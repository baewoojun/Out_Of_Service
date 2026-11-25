using System.IO;
using UnityEditor;
using UnityEngine;

namespace OutOfService.EditorTools
{
    // 잡혔을 때 컷신에 쓸 괴물 프리팹을 Resources 에 만든다.
    // 먼저 Tools ▸ Bunny Monster ▸ 1. Setup 으로 BunnyMonster.prefab 을 만들어 두어야 한다.
    public static class CaughtCutsceneBuilder
    {
        const string Folder = "Assets/OutOfService/Cutscene/Resources/OutOfServiceCutscene";
        const string Target = Folder + "/CaughtMonster.prefab";

        [MenuItem("Out Of Service/Cutscene/Build Caught Monster (Bunny)")]
        public static void BuildBunny() => Build("BunnyMonster");

        [MenuItem("Out Of Service/Cutscene/Build Caught Monster (Twin)")]
        public static void BuildTwin() => Build("BunnyTwin");

        [MenuItem("Out Of Service/Cutscene/Build Caught Monster (Turtle)")]
        public static void BuildTurtle() => Build("TurtleMonster");

        public static string Build(string sourcePrefabName)
        {
            string source = null;
            foreach (var guid in AssetDatabase.FindAssets(sourcePrefabName + " t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sourcePrefabName) { source = path; break; }
            }
            if (source == null)
            {
                Debug.LogError($"[Cutscene] {sourcePrefabName}.prefab 이 없습니다. Tools ▸ Bunny Monster ▸ 1. Setup 을 먼저 실행하세요.");
                return null;
            }

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "CaughtMonster";
            PrefabUtility.SaveAsPrefabAsset(instance, Target);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Cutscene] {Target} 을(를) {source} 의 변형으로 만들었습니다.");
            return Target;
        }
    }
}
