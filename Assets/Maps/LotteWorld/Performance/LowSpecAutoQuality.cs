// LowSpecAutoQuality.cs - 게임 시작 때 내장 그래픽이면 품질 단계를 "Low" 로 바꾼다 (런타임).
// 외장 그래픽카드에서는 아무것도 하지 않는다 (기본 단계 "PC" 유지).
// 실행 인자로 강제할 수 있다:  -quality-low  /  -quality-pc
using System.Linq;
using UnityEngine;

namespace LotteWorld.Performance
{
    public static class LowSpecAutoQuality
    {
        const string LowLevelName = "Low";
        const string PcLevelName = "PC";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            var args = System.Environment.GetCommandLineArgs();
            string want = null, reason;
            if (args.Contains("-quality-low")) { want = LowLevelName; reason = "실행 인자 -quality-low"; }
            else if (args.Contains("-quality-pc")) { want = PcLevelName; reason = "실행 인자 -quality-pc"; }
            else if (IsIntegratedGpu(SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceVendor)) { want = LowLevelName; reason = "내장 그래픽 감지"; }
            else reason = "외장 그래픽";

            if (want != null)
            {
                int idx = System.Array.IndexOf(QualitySettings.names, want);
                if (idx >= 0 && idx != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(idx, true);
            }
            Debug.Log($"[LowSpecAutoQuality] GPU='{SystemInfo.graphicsDeviceName}' -> 품질 '{QualitySettings.names[QualitySettings.GetQualityLevel()]}' ({reason})");
        }

        // 이름으로 내장 그래픽을 가려낸다. 확실하지 않으면 외장으로 본다 (화질을 괜히 낮추지 않게).
        public static bool IsIntegratedGpu(string deviceName, string vendor)
        {
            string n = (deviceName ?? "").ToLowerInvariant();
            string v = (vendor ?? "").ToLowerInvariant();

            if (n.Contains("nvidia") || n.Contains("geforce") || n.Contains("rtx") || n.Contains("gtx") || n.Contains("quadro")) return false;

            if (v.Contains("intel") || n.Contains("intel"))
            {
                // Intel Arc A/B 시리즈(예: Arc A750, Arc B580)는 외장. "Arc Graphics"(Core Ultra 내장)는 내장.
                if (System.Text.RegularExpressions.Regex.IsMatch(n, @"arc\(tm\)\s*[ab]\d{3}|arc\s*[ab]\d{3}")) return false;
                return true;
            }

            if (n.Contains("radeon") || v.Contains("amd") || v.Contains("ati"))
            {
                // 외장 Radeon 은 RX/Pro 같은 모델명이 붙는다. "Radeon(TM) Graphics", "Vega 8" 등은 내장.
                if (n.Contains(" rx ") || n.Contains("rx ") || n.Contains("radeon pro") || n.Contains("firepro")) return false;
                if (n.Contains("radeon(tm) graphics") || n.Contains("radeon graphics") || n.Contains("vega") ||
                    System.Text.RegularExpressions.Regex.IsMatch(n, @"radeon(\(tm\))?\s*\d{3}m")) return true;
                return false;
            }

            if (n.Contains("microsoft basic render")) return true; // 드라이버 없는 소프트웨어 렌더러
            return false;
        }
    }
}
