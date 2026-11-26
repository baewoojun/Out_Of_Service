// LotteWorldBenchmark.cs - 롯데월드 맵 성능 측정 (런타임). 실행 인자에 -lwbench 가 있을 때만 동작한다.
// 정해진 지점마다 카메라를 세워 방향별로 프레임 시간을 재고, 실행 파일 옆 CSV 에 쓴 뒤 종료한다.
// 예) Game.exe -lwbench -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
//
// 실험 옵션 (설정 조합별 비교용, 모두 선택):
//   -lwquick            지점마다 가장 무거운 방향 하나만 잰다
//   -lwout <이름>       결과 파일 이름 (기본 lwbench) -> <이름>.csv
//   -lwquality <이름>   품질 단계 이름으로 전환 (예: Low)
//   -lwscale <0.3~1>    렌더 해상도 배율
//   -lwhdr <0|1>        HDR
//   -lwssao <0|1>       SSAO 렌더러 기능
//   -lwpp <0|1>         카메라 포스트 프로세싱
//   -lwshadowdist <m>   그림자 거리 (0 = 그림자 없음)
//   -lwopaque <0|1>     불투명 텍스처 복사
//   -lwlights <0|1>     점광원 전부 끄기/켜기
//   -lwzone <0|1>       멀리서 실내 물체 끄기 (InteriorVisibilityZone)
//   -lwfar <m>          카메라 원거리 클립
//   -lwlod <값>         LOD bias
//   -lwmip <0~3>        텍스처 밉 제한
//   -lwnomi             매직아일랜드 씬을 불러오지 않는다
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LotteWorld.Benchmark
{
    public class LotteWorldBenchmark : MonoBehaviour
    {
        const string MagicIslandScene = "Map3_MagicIsland";
        const float WarmupSeconds = 1.5f;
        const float SampleSeconds = 4f;

        // 이름, 눈 위치, 가장 무거운 방향(yaw). 기본은 각 지점에서 네 방향을 모두 잰다.
        static readonly (string name, Vector3 pos, float heavyYaw)[] Points =
        {
            ("Spawn",            new Vector3(5f, 1.5f, 40f),      180f),
            ("Indoor_S_Center",  new Vector3(0f, 2.6f, -100f),    0f),
            ("Indoor_SW",        new Vector3(-100f, 1.6f, -120f), 0f),
            ("Indoor_E",         new Vector3(100f, 1.6f, 40f),    270f),
            ("Indoor_S_West",    new Vector3(-60f, 1.7f, -80f),   0f),
            ("MagicIsland_N",    new Vector3(-100f, 1.6f, 420f),  180f),
            ("MagicIsland_NE",   new Vector3(-20f, 1.6f, 380f),   180f),
        };

        static string[] args;

        static bool Has(string flag) => args.Contains(flag);
        static string Arg(string flag)
        {
            int i = System.Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
        static bool ArgF(string flag, out float v)
        {
            v = 0f;
            var s = Arg(flag);
            return s != null && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            args = System.Environment.GetCommandLineArgs();
            if (!Has("-lwbench")) return;
            var go = new GameObject("LotteWorldBenchmark");
            DontDestroyOnLoad(go);
            go.AddComponent<LotteWorldBenchmark>();
        }

        // 실험 옵션을 적용하고, 적용한 내용을 한 줄로 돌려준다.
        static string ApplyOverrides(Camera cam)
        {
            var applied = new List<string>();
            float f;

            var q = Arg("-lwquality");
            if (q != null)
            {
                int idx = System.Array.IndexOf(QualitySettings.names, q);
                if (idx >= 0) { QualitySettings.SetQualityLevel(idx, true); applied.Add("quality=" + q); }
                else applied.Add("quality=" + q + "(없음)");
            }

            var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset
                      ?? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp)
            {
                if (ArgF("-lwscale", out f)) { urp.renderScale = f; applied.Add("scale=" + f); }
                if (ArgF("-lwhdr", out f)) { urp.supportsHDR = f > 0; applied.Add("hdr=" + f); }
                if (ArgF("-lwshadowdist", out f)) { urp.shadowDistance = f; applied.Add("shadowDist=" + f); }
                if (ArgF("-lwopaque", out f)) { urp.supportsCameraOpaqueTexture = f > 0; applied.Add("opaque=" + f); }
                if (ArgF("-lwssao", out f))
                {
                    foreach (var rd in urp.rendererDataList)
                        if (rd)
                            foreach (var feat in rd.rendererFeatures)
                                if (feat && feat.GetType().Name == "ScreenSpaceAmbientOcclusion") feat.SetActive(f > 0);
                    applied.Add("ssao=" + f);
                }
            }
            if (ArgF("-lwpp", out f))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data) data.renderPostProcessing = f > 0;
                applied.Add("pp=" + f);
            }
            if (ArgF("-lwlights", out f))
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                    if (l.type == LightType.Point || l.type == LightType.Spot) l.enabled = f > 0;
                applied.Add("lights=" + f);
            }
            if (ArgF("-lwzone", out f)) { LotteWorld.Performance.InteriorVisibilityZone.Enabled = f > 0; applied.Add("zone=" + f); }
            if (ArgF("-lwfar", out f)) { cam.farClipPlane = f; applied.Add("far=" + f); }
            if (ArgF("-lwlod", out f)) { QualitySettings.lodBias = f; applied.Add("lod=" + f); }
            if (ArgF("-lwmip", out f)) { QualitySettings.globalTextureMipmapLimit = (int)f; applied.Add("mip=" + f); }
            return applied.Count == 0 ? "default" : string.Join(" ", applied);
        }

        IEnumerator Start()
        {
            Application.runInBackground = true; // 창이 포커스를 잃어도 측정이 멈추지 않게
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            bool loadMi = !Has("-lwnomi");
            if (loadMi && !SceneManager.GetSceneByName(MagicIslandScene).isLoaded && Application.CanStreamedLevelBeLoaded(MagicIslandScene))
                yield return SceneManager.LoadSceneAsync(MagicIslandScene, LoadSceneMode.Additive);

            // 플레이어 조작·시네머신이 카메라를 움직이지 않게 끈다.
            foreach (var n in new[] { "PlayerCapsule", "PlayerFollowCamera" })
            {
                var g = GameObject.Find(n);
                if (g) g.SetActive(false);
            }
            var cam = Camera.main;
            foreach (var b in cam.GetComponents<Behaviour>())
                if (b.GetType().Name == "CinemachineBrain") b.enabled = false;

            string applied = ApplyOverrides(cam) + (loadMi ? "" : " nomi");
            QualitySettings.vSyncCount = 0; // 품질 단계 전환이 되돌렸을 수 있다

            yield return new WaitForSeconds(3f); // 첫 로딩·셰이더 워밍업

            bool quick = Has("-lwquick");
            var csv = new StringBuilder("point,yaw,heavy,avgFps,low1Fps,avgMs,p99Ms,cpuMainMs,cpuRenderMs,gpuMs,frames\n");
            var timings = new FrameTiming[1];
            foreach (var p in Points)
            {
                if (!loadMi && p.name.StartsWith("MagicIsland")) continue;
                var yaws = quick ? new[] { p.heavyYaw } : new[] { 0f, 90f, 180f, 270f };
                foreach (var yaw in yaws)
                {
                    cam.transform.SetPositionAndRotation(p.pos, Quaternion.Euler(0f, yaw, 0f));
                    yield return new WaitForSeconds(WarmupSeconds);

                    var dts = new List<float>();
                    double main = 0, render = 0, gpu = 0; int ftCount = 0;
                    float end = Time.realtimeSinceStartup + SampleSeconds;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        dts.Add(Time.unscaledDeltaTime * 1000f);
                        FrameTimingManager.CaptureFrameTimings();
                        if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                        {
                            main += timings[0].cpuMainThreadFrameTime;
                            render += timings[0].cpuRenderThreadFrameTime;
                            gpu += timings[0].gpuFrameTime;
                            ftCount++;
                        }
                    }
                    dts.Sort();
                    float avgMs = dts.Average();
                    float p99 = dts[Mathf.Clamp(Mathf.CeilToInt(dts.Count * 0.99f) - 1, 0, dts.Count - 1)];
                    int n = Mathf.Max(1, ftCount);
                    csv.AppendLine(string.Join(",",
                        p.name, yaw.ToString("F0"), Mathf.Approximately(yaw, p.heavyYaw) ? "1" : "0",
                        (1000f / avgMs).ToString("F1"), (1000f / p99).ToString("F1"),
                        avgMs.ToString("F2"), p99.ToString("F2"),
                        (main / n).ToString("F2"), (render / n).ToString("F2"), (gpu / n).ToString("F2"),
                        dts.Count.ToString()));
                }
            }

            var info = $"# {SystemInfo.graphicsDeviceName} | {SystemInfo.processorType} | {Screen.width}x{Screen.height} | quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} | {applied}\n";
            var dir = Path.GetDirectoryName(Application.dataPath);
            File.WriteAllText(Path.Combine(dir, (Arg("-lwout") ?? "lwbench") + ".csv"), info + csv);
            Application.Quit();
        }
    }
}
