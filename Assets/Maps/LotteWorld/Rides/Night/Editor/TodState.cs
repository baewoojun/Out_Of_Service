// TodState — 낮/밤 전환 상태 (파일명=클래스명 필수)
using UnityEngine;

namespace LotteWorld.Night
{
    public class TodState : ScriptableObject
    {
        public bool isNight;
        public bool bootstrapped;
        // 밤 밝기 (LW_NightProfile 의 Post Exposure, EV). 낮·밤 창의 슬라이더로 바꾼다.
        public float nightExposure = -0.75f;
        // 밤 주변광 / 달빛 배율 (NightSnapshot 기준값에 곱함, 1 = 원래 밤)
        public float nightAmbient = 1f;
        public float nightMoon = 1f;
        // 밤 안개 짙기 배율 (0 = 안개 없음, 1 = 원래 밤)
        public float nightFog = 1f;
    }
}
