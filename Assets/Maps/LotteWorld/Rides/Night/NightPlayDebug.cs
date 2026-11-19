// 임시 Play 모드 조명 디버그 — 콘솔에 [NightPlayDebug] 로 덤프 후 제거해도 됨
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LotteWorld.Night
{
    public class NightPlayDebug : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("~NightPlayDebug");
            DontDestroyOnLoad(go);
            go.AddComponent<NightPlayDebug>();
        }

        void Start() { Dump("Start"); }
        void LateUpdate()
        {
            if (Time.frameCount == 3) Dump("Frame3");
        }

        void Dump(string tag)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[NightPlayDebug] ===== " + tag + " =====");
            sb.AppendLine("loadedScenes=" + SceneManager.sceneCount);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                sb.AppendLine("  scene[" + i + "] " + s.path + " active=" + (s == SceneManager.GetActiveScene()));
            }

            sb.AppendLine("-- RenderSettings --");
            sb.AppendLine("ambientMode=" + RenderSettings.ambientMode);
            sb.AppendLine("ambientSky=" + C(RenderSettings.ambientSkyColor));
            sb.AppendLine("ambientEquator=" + C(RenderSettings.ambientEquatorColor));
            sb.AppendLine("ambientGround=" + C(RenderSettings.ambientGroundColor));
            sb.AppendLine("ambientIntensity=" + RenderSettings.ambientIntensity);
            sb.AppendLine("ambientLight=" + C(RenderSettings.ambientLight));
            sb.AppendLine("skybox=" + (RenderSettings.skybox != null ? RenderSettings.skybox.name : "null"));
            sb.AppendLine("sun=" + (RenderSettings.sun != null ? RenderSettings.sun.name : "null"));
            sb.AppendLine("fog=" + RenderSettings.fog + " mode=" + RenderSettings.fogMode
                + " dens=" + RenderSettings.fogDensity + " color=" + C(RenderSettings.fogColor));
            sb.AppendLine("reflectionIntensity=" + RenderSettings.reflectionIntensity
                + " defaultReflectionMode=" + RenderSettings.defaultReflectionMode);

            sb.AppendLine("-- Directional Lights (enabled) --");
            int dirN = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (l.type != LightType.Directional || !l.enabled || !l.gameObject.activeInHierarchy) continue;
                dirN++;
                sb.AppendLine("  " + l.name + " path=" + PathOf(l.transform)
                    + " intensity=" + l.intensity.ToString("F3")
                    + " color=" + C(l.color) + " shadows=" + l.shadows);
            }
            if (dirN == 0) sb.AppendLine("  (none)");

            sb.AppendLine("-- Volumes --");
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
            {
                if (!v.isActiveAndEnabled) continue;
                string pe = "-", tone = "-";
                var profile = v.sharedProfile != null ? v.sharedProfile : v.profile;
                if (profile != null)
                {
                    if (profile.TryGet(out ColorAdjustments ca) && ca.active)
                        pe = "override=" + ca.postExposure.overrideState + " val=" + ca.postExposure.value;
                    if (profile.TryGet(out Tonemapping tm) && tm.active)
                        tone = "override=" + tm.mode.overrideState + " mode=" + tm.mode.value;
                }
                sb.AppendLine("  " + v.name + " global=" + v.isGlobal + " weight=" + v.weight
                    + " priority=" + v.priority
                    + " profile=" + (profile != null ? profile.name : "null")
                    + " postExposure=[" + pe + "] tonemap=[" + tone + "]");
            }

            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
                    if (c.CompareTag("MainCamera")) { cam = c; break; }
            }
            sb.AppendLine("-- Camera --");
            if (cam == null) sb.AppendLine("  MainCamera=null");
            else
            {
                var uad = cam.GetComponent<UniversalAdditionalCameraData>();
                sb.AppendLine("  name=" + cam.name + " path=" + PathOf(cam.transform)
                    + " renderPostProcessing=" + (uad != null && uad.renderPostProcessing));
            }

            sb.AppendLine("-- Lightmaps / Probes --");
            sb.AppendLine("lightmaps.Length=" + LightmapSettings.lightmaps.Length);
            int probes = 0;
            foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude))
            {
                if (!p.isActiveAndEnabled) continue;
                probes++;
                sb.AppendLine("  probe " + p.name + " mode=" + p.mode + " intensity=" + p.intensity);
            }
            sb.AppendLine("activeReflectionProbes=" + probes);

            Debug.Log(sb.ToString());
        }

        static string C(Color c)
        {
            return "(" + c.r.ToString("F3") + "," + c.g.ToString("F3") + "," + c.b.ToString("F3") + ")";
        }

        static string PathOf(Transform t)
        {
            var s = new System.Collections.Generic.List<string>();
            while (t != null) { s.Add(t.name); t = t.parent; }
            s.Reverse();
            return string.Join("/", s);
        }
    }
}
