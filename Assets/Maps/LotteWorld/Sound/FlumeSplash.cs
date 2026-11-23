// FlumeSplash.cs - LotteWorldSfx.cs 가 런타임에 자동으로 붙임. 설명은 LotteWorldSfx.cs 맨 위 참고.
using System.Collections;
using System.Collections.Generic;
using LotteWorld.Rides;
using UnityEngine;

namespace LotteWorld.Sound
{
    /// <summary>후룸 배가 내리막 끝에서 물에 부딪히면 첨벙.</summary>
    [RequireComponent(typeof(FlumePath))]
    public class FlumeSplash : MonoBehaviour
    {
        public float volume = 1f;
        [Tooltip("이 속도(m/s) 이상으로 내려오다 멈추면 첨벙")] public float dropSpeed = 2.5f;
        FlumePath path; Vector3[] last; float[] fall; float[] cool;

        IEnumerator Start()
        {
            path = GetComponent<FlumePath>();
            yield return null;                                              // FlumePath.Start 에서 배를 찾은 뒤
            int n = path.boats.Count;
            last = new Vector3[n]; fall = new float[n]; cool = new float[n];
            for (int i = 0; i < n; i++) if (path.boats[i]) last[i] = path.boats[i].position;
        }

        void Update()
        {
            if (last == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            for (int i = 0; i < last.Length && i < path.boats.Count; i++)
            {
                var b = path.boats[i];
                if (!b) continue;
                float vy = (b.position.y - last[i].y) / dt;
                last[i] = b.position;
                if (Mathf.Abs(vy) > 50f) { fall[i] = 0f; continue; }          // 되돌리기 등 순간이동
                if (vy < -dropSpeed) fall[i] = Mathf.Max(fall[i], -vy);
                else if (fall[i] > 0f && vy > -0.8f)
                {
                    if (Time.time > cool[i])
                    {
                        float loud = Mathf.Clamp01(fall[i] / (dropSpeed * 3f));
                        LotteWorldSfx.OneShot(LotteWorldSfx.Clip("water_splash_big"), b.position, volume * Mathf.Lerp(0.6f, 1f, loud), 5f, 60f, Random.Range(0.75f, 0.9f));
                        cool[i] = Time.time + 1.5f;
                    }
                    fall[i] = 0f;
                }
            }
        }
    }
}
