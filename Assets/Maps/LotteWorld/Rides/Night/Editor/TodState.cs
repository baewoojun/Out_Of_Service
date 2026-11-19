// TodState — 낮/밤 전환 상태 (파일명=클래스명 필수)
using UnityEngine;

namespace LotteWorld.Night
{
    public class TodState : ScriptableObject
    {
        public bool isNight;
        public bool bootstrapped;
    }
}
