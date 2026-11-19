// Tools > LotteWorld > Night > Add Flashlight To Player
// MainCamera 자식에 Flashlight Spot 을 붙임. Undo 등록, 씬 저장 안 함.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Night
{
    public static class LotteWorldFlashlightSetup
    {
        const string FlashlightName = "Flashlight";

        [MenuItem("Tools/LotteWorld/Night/Add Flashlight To Player")]
        public static void AddFlashlightToPlayer()
        {
            var cam = FindPlayerCamera();
            if (cam == null)
            {
                Debug.LogError("[Flashlight] MainCamera(tag=MainCamera) / MainCamera 오브젝트를 찾지 못했습니다. Map3_LotteWorld 를 연 뒤 다시 실행하세요.");
                return;
            }

            Transform existing = cam.Find(FlashlightName);
            if (existing != null)
            {
                var light = existing.GetComponent<Light>();
                var fx = existing.GetComponent<LotteWorldFlashlight>();
                if (light == null) light = Undo.AddComponent<Light>(existing.gameObject);
                if (fx == null) fx = Undo.AddComponent<LotteWorldFlashlight>(existing.gameObject);
                ConfigureLight(light);
                ConfigureTransform(existing);
                EnsureAudio(existing.gameObject);
                fx.EditorBind(light, cam, startEnabled: true);
                EditorUtility.SetDirty(existing.gameObject);
                EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[Flashlight] 기존 Flashlight 갱신 — " + PathOf(existing) + " (저장하지 않음)");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add LotteWorld Flashlight");

            var go = new GameObject(FlashlightName);
            Undo.RegisterCreatedObjectUndo(go, "Create Flashlight");
            Undo.SetTransformParent(go.transform, cam, "Parent Flashlight");
            ConfigureTransform(go.transform);

            var lightNew = Undo.AddComponent<Light>(go);
            ConfigureLight(lightNew);

            EnsureAudio(go);

            var script = Undo.AddComponent<LotteWorldFlashlight>(go);
            script.EditorBind(lightNew, cam, startEnabled: true);

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            Undo.CollapseUndoOperations(undo);

            Debug.Log("[Flashlight] 추가됨 — " + PathOf(go.transform)
                + " (카메라: " + PathOf(cam) + ", 씬 저장 안 함. Ctrl+Z 로 Undo 가능)");
        }

        static Transform FindPlayerCamera()
        {
            // 1) tag MainCamera
            var tagged = GameObject.FindGameObjectWithTag("MainCamera");
            if (tagged != null)
            {
                var c = tagged.GetComponent<Camera>();
                if (c != null) return tagged.transform;
            }

            // 2) name MainCamera
            var named = GameObject.Find("MainCamera");
            if (named != null && named.GetComponent<Camera>() != null)
                return named.transform;

            // 3) Camera.main
            if (Camera.main != null) return Camera.main.transform;

            // 4) any camera under / near PlayerCapsule
            var player = GameObject.Find("PlayerCapsule");
            if (player != null)
            {
                var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
                foreach (var c in cams)
                    if (c.CompareTag("MainCamera")) return c.transform;
            }

            return null;
        }

        static void ConfigureTransform(Transform t)
        {
            Undo.RecordObject(t, "Flashlight Transform");
            t.localPosition = new Vector3(0.25f, -0.2f, 0.1f);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        static void ConfigureLight(Light light)
        {
            Undo.RecordObject(light, "Flashlight Light");
            light.type = LightType.Spot;
            light.intensity = 70f;
            light.range = 150f;
            light.spotAngle = 45f;
            light.innerSpotAngle = 25f;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.shadows = LightShadows.Soft;
            light.shadowNearPlane = 0.2f;
            // 에디터에서 항상 켜져 맵이 밝아지지 않게 — 런타임 Start 가 startOn 따름
            light.enabled = false;
        }

        static void EnsureAudio(GameObject go)
        {
            if (go.GetComponent<AudioSource>() != null) return;
            var src = Undo.AddComponent<AudioSource>(go);
            src.playOnAwake = false;
            src.spatialBlend = 0f;
        }

        static string PathOf(Transform t)
        {
            string scene = t.gameObject.scene.name;
            string p = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }
            return p + " [" + scene + "]";
        }
    }
}
