// FlickerSound.cs - LotteWorldSfx.cs 가 런타임에 자동으로 붙임. 설명은 LotteWorldSfx.cs 맨 위 참고.
using System.Collections;
using System.Collections.Generic;
using LotteWorld.Rides;
using UnityEngine;

namespace LotteWorld.Sound
{
    /// <summary>깜빡이는 전등 소리. Light 밝기를 따라 험 크기를 바꾸고, 꺼지거나 켜지는 순간 '탁' 소리.</summary>
    [RequireComponent(typeof(Light))]
    public class FlickerSound : MonoBehaviour
    {
        public float buzzVolume = 0.35f;
        public float clickVolume = 0.7f;
        Light lt; AudioSource buzz, click; float baseline, nextClick; bool dark;

        void Start()
        {
            lt = GetComponent<Light>();
            baseline = Mathf.Max(0.01f, lt.intensity);
            buzz = LotteWorldSfx.Source(gameObject, LotteWorldSfx.Clip("light_buzz_loop"), true, 0f, 1f, 10f, 200);
            click = LotteWorldSfx.Source(gameObject, null, false, clickVolume, 1.5f, 18f, 150);
        }

        void Update()
        {
            bool on = lt.isActiveAndEnabled;
            if (!on) { if (buzz.isPlaying) buzz.Stop(); return; }
            if (!buzz.isPlaying) buzz.Play();
            baseline = Mathf.Max(baseline * (1f - 0.02f * Time.deltaTime), lt.intensity);
            float ratio = Mathf.Clamp01(lt.intensity / baseline);
            buzz.volume = buzzVolume * ratio * ratio;
            buzz.pitch = 0.97f + 0.06f * ratio;

            if (!dark && ratio < 0.3f) { dark = true; Click(1f); }
            else if (dark && ratio > 0.55f) { dark = false; Click(0.55f); }
        }

        void Click(float loud)
        {
            if (Time.time < nextClick) return;
            nextClick = Time.time + 0.09f;
            click.pitch = Random.Range(0.9f, 1.15f);
            click.PlayOneShot(LotteWorldSfx.Pick("light_click_1", "light_click_2", "light_click_3"), clickVolume * loud);
        }
    }
}
