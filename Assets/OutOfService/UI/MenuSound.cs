using UnityEngine;

namespace OutOfService
{
    // 메뉴 클릭음. 파일 없이 짧은 저음 '툭' 소리를 만들어 쓴다.
    // 일시정지 중(AudioListener.pause)에도 들리도록 ignoreListenerPause 를 켠다.
    public static class MenuSound
    {
        static AudioSource source;
        static AudioClip click;

        public static void Click()
        {
            if (!source)
            {
                var go = new GameObject("Menu sound") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = true;
                source.volume = 0.35f;
            }
            if (!click) click = MakeClick();
            source.pitch = Random.Range(0.94f, 1.04f);
            source.PlayOneShot(click);
        }

        static AudioClip MakeClick()
        {
            const int rate = 44100;
            int length = rate / 12;
            var data = new float[length];
            var random = new System.Random(7);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-t * 55f);
                float body = Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.8f;
                float grit = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 260f) * 0.5f;
                data[i] = (body + grit) * envelope;
            }
            var clip = AudioClip.Create("Menu click", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
