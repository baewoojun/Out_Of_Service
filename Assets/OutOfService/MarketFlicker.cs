using UnityEngine;
using UnityEngine.InputSystem;

namespace OutOfService
{
    public class MarketFlicker : MonoBehaviour
    {
        Light lamp;
        float baseIntensity;
        void Awake() { lamp = GetComponent<Light>(); baseIntensity = lamp.intensity; }
        void Update()
        {
            float pulse = Mathf.PerlinNoise(Time.time * 7f, transform.position.x * 0.4f);
            lamp.intensity = baseIntensity * (pulse > 0.18f ? 1f : 0.2f);
        }
    }
}

