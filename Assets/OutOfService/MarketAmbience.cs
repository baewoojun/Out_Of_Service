using UnityEngine;
using UnityEngine.InputSystem;

namespace OutOfService
{
    public class MarketAmbience : MonoBehaviour
    {
        void Awake()
        {
            const int sampleRate = 22050;
            const int count = sampleRate * 2;
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = 0.055f * Mathf.Sin(2 * Mathf.PI * 60 * t)
                    + 0.02f * Mathf.Sin(2 * Mathf.PI * 120 * t)
                    + 0.005f * Mathf.Sin(2 * Mathf.PI * 180 * t);
            }
            AudioClip clip = AudioClip.Create("fluorescent electrical hum", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0.18f;
            source.Play();
        }
    }
}

