using UnityEngine;

namespace OutOfService
{
    // Disable the whole GUI behaviour while playing; an early return inside
    // OnGUI still incurs Unity's IMGUI event setup on every frame.
    public sealed class MarketSettingsView:MonoBehaviour
    {
        internal MarketSettings settings;
        void OnGUI(){if(settings)settings.DrawMenu();}
    }
}
