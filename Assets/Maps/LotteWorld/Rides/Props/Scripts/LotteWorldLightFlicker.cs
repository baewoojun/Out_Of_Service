// LotteWorldLightFlicker.cs - 고장 난 등 깜빡임 (런타임). Rides/Props/Scripts/ 에 둠 (Editor 폴더 아님)
// LotteWorldProps.cs 가 Prop_Lamp_Half 의 점광원에 자동으로 붙임. 다른 등에도 손으로 붙여 써도 됨.
using UnityEngine;

namespace LotteWorld.Props
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [Tooltip("평소 밝기 배율")] public float baseScale = 1f;
        [Tooltip("초당 깜빡임 시도 횟수")] public float rate = 6f;
        [Range(0f, 1f), Tooltip("꺼질 확률")] public float dropChance = 0.25f;
        Light lt; float baseIntensity, next, target;

        void Awake() { lt = GetComponent<Light>(); baseIntensity = lt.intensity; target = baseIntensity; }

        void Update()
        {
            if (Time.time >= next)
            {
                next = Time.time + Random.Range(0.3f, 1.7f) / Mathf.Max(0.01f, rate);
                target = Random.value < dropChance ? baseIntensity * Random.Range(0f, 0.15f) : baseIntensity * baseScale * Random.Range(0.7f, 1.1f);
            }
            lt.intensity = Mathf.Lerp(lt.intensity, target, Time.deltaTime * 30f);
        }
    }
}
