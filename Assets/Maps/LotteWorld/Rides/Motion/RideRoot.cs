// RideRoot.cs - 놀이기구 루트 표시. RideDirector 가 이 단위로 엇갈려 켬 (같은 놀이기구 부품은 같이 출발)
using UnityEngine;

namespace LotteWorld.Rides
{
    [DisallowMultipleComponent]
    public class RideRoot : MonoBehaviour
    {
        public string rideName;
    }
}
