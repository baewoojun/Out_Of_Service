// PlayerWaterSound.cs - LotteWorldSfx.cs 가 런타임에 자동으로 붙임. 설명은 LotteWorldSfx.cs 맨 위 참고.
using System.Collections;
using System.Collections.Generic;
using LotteWorld.Rides;
using UnityEngine;

namespace LotteWorld.Sound
{
    /// <summary>카메라(플레이어)에 붙음. 발이 수면 아래로 들어가면 첨벙, 물속을 걸으면 촤악, 나오면 작게 첨벙.</summary>
    public class PlayerWaterSound : MonoBehaviour
    {
        [Tooltip("CharacterController 가 없을 때 카메라에서 발까지 (m)")] public float eyeHeight = 1.6f;
        public float volume = 0.9f;
        CharacterController cc; AudioSource oneShot, wade;
        Vector3 lastFeet; bool inWater, hasLast; float surfaceY, lastSplash;

        void Start()
        {
            cc = GetComponentInParent<CharacterController>();
            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false; oneShot.spatialBlend = 0f;
            wade = gameObject.AddComponent<AudioSource>();
            wade.clip = LotteWorldSfx.Clip("water_wade_loop"); wade.loop = true; wade.playOnAwake = false; wade.spatialBlend = 0f; wade.volume = 0f;
        }

        Vector3 Feet()
        {
            if (cc) return cc.transform.TransformPoint(cc.center) - Vector3.up * (cc.height * 0.5f * cc.transform.lossyScale.y);
            return transform.position - Vector3.up * eyeHeight;
        }

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 feet = Feet();
            Vector3 vel = hasLast ? (feet - lastFeet) / dt : Vector3.zero;
            lastFeet = feet; hasLast = true;
            if (vel.sqrMagnitude > 2500f) vel = Vector3.zero;              // 순간이동은 무시

            bool found = WaterSurface.Query(feet, 3f, eyeHeight + 0.5f, out float h);
            if (!inWater)
            {
                if (found && feet.y < h - 0.03f && Time.time - lastSplash > 0.4f)
                {
                    inWater = true; surfaceY = h; lastSplash = Time.time;
                    bool big = vel.y < -3f || h - feet.y > 0.7f;
                    oneShot.pitch = Random.Range(0.92f, 1.08f);
                    oneShot.PlayOneShot(big ? LotteWorldSfx.Clip("water_splash_big") : LotteWorldSfx.Pick("water_splash_small_1", "water_splash_small_2"), volume);
                }
            }
            else
            {
                if (found) surfaceY = h;
                if (!found || feet.y > surfaceY + 0.05f)
                {
                    inWater = false; lastSplash = Time.time;
                    oneShot.pitch = Random.Range(1.0f, 1.2f);
                    oneShot.PlayOneShot(LotteWorldSfx.Pick("water_splash_small_1", "water_splash_small_2"), volume * 0.5f);
                }
            }

            float speed = new Vector2(vel.x, vel.z).magnitude;
            float target = inWater ? Mathf.Clamp01((speed - 0.3f) / 3f) * volume * 0.8f : 0f;
            wade.volume = Mathf.MoveTowards(wade.volume, target, dt * 2.5f);
            wade.pitch = Mathf.Lerp(0.85f, 1.2f, Mathf.Clamp01(speed / 5f));
            if (wade.volume > 0.001f) { if (!wade.isPlaying) wade.Play(); }
            else if (wade.isPlaying) wade.Stop();
        }
    }
}
