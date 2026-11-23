"""롯데월드 맵(Map3) 효과음을 코드로 합성해 WAV 로 저장한다. (외부 음원 없음 → 저작권 걱정 없음)

실행:  python -I Tools/GenerateLotteWorldSfx.py
출력:  Assets/Maps/LotteWorld/Sound/Resources/LotteWorldSfx/*.wav
       (Resources 폴더라 런타임 LotteWorldSfx.cs 가 이름으로 불러 씀. 파일 이름 바꾸면 그쪽도 같이 바꿀 것)

_loop 로 끝나는 파일은 이음매 없이 반복되게 만든다 (주파수를 길이에 맞추거나 꼬리를 앞으로 감음).
"""
import os
import wave

import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Maps", "LotteWorld",
                   "Sound", "Resources", "LotteWorldSfx")
rng = np.random.default_rng(1120)


# ---------------------------------------------------------------- 도구
def t_axis(sec):
    return np.arange(int(SR * sec)) / SR


def noise(n):
    return rng.standard_normal(n)


def onepole_lp(x, cutoff):
    """1차 저역통과. cutoff 는 숫자 또는 샘플마다 값이 바뀌는 배열(Hz)."""
    c = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = 1.0 - np.exp(-2.0 * np.pi * c / SR)
    y = np.empty_like(x)
    s = 0.0
    for i in range(len(x)):
        s += a[i] * (x[i] - s)
        y[i] = s
    return y


def onepole_hp(x, cutoff):
    return x - onepole_lp(x, cutoff)


def resonator(x, freq, q):
    """2차 공진(대역통과) 필터. freq 는 숫자 또는 배열."""
    f = np.broadcast_to(np.asarray(freq, dtype=float), x.shape)
    y = np.zeros_like(x)
    y1 = y2 = 0.0
    for i in range(len(x)):
        w = 2.0 * np.pi * f[i] / SR
        r = np.exp(-w / (2.0 * q))
        b1 = 2.0 * r * np.cos(w)
        b2 = -r * r
        v = (1.0 - r) * x[i] + b1 * y1 + b2 * y2
        y2, y1 = y1, v
        y[i] = v
    return y


def env_exp(n, decay_sec, attack_sec=0.002):
    t = np.arange(n) / SR
    a = np.clip(t / max(attack_sec, 1e-5), 0, 1)
    return a * np.exp(-t / decay_sec)


def place(buf, clip, at_sample, wrap=False):
    """clip 을 buf 의 at_sample 위치에 더함. wrap=True 면 넘친 꼬리를 앞쪽으로 감아 루프 이음매를 없앰."""
    n = len(buf)
    idx = at_sample + np.arange(len(clip))
    if wrap:
        np.add.at(buf, idx % n, clip)
    else:
        keep = idx < n
        buf[idx[keep]] += clip[keep]


def normalize(x, peak=0.89):
    m = np.max(np.abs(x))
    return x if m < 1e-9 else x * (peak / m)


def fade(x, fin=0.003, fout=0.02):
    x = x.copy()
    a, b = int(SR * fin), int(SR * fout)
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def save(name, x):
    os.makedirs(OUT, exist_ok=True)
    pcm = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"{name}.wav  {len(x) / SR:.2f}s")


def loop_crossfade(x, xfade_sec=0.25):
    """끝부분을 앞부분에 겹쳐 섞어 이음매 없는 루프로 만듦 (길이는 xfade 만큼 줄어듦)."""
    k = int(SR * xfade_sec)
    head, body, tail = x[:k], x[k:-k], x[-k:]
    ramp = np.linspace(0, 1, k)
    mixed = tail * (1 - ramp) + head * ramp
    return np.concatenate([mixed, body, ])


# ---------------------------------------------------------------- 전등
def light_buzz_loop():
    """형광등/네온 '지이잉' — 120Hz 전원 험 + 홀수 배음 + 지직거림. 2초, 정수 주기라 그대로 반복됨."""
    sec = 2.0
    t = t_axis(sec)
    hum = np.zeros_like(t)
    for h, amp in [(1, 1.0), (2, 0.35), (3, 0.5), (5, 0.28), (7, 0.16), (9, 0.09), (13, 0.05)]:
        hum += amp * np.sin(2 * np.pi * 120 * h * t + h * 0.7)
    # 안정기 특유의 거친 느낌: 살짝 찌그러뜨림
    hum = np.tanh(hum * 1.6)
    # 지직 소리: 드문드문 짧은 잡음 덩어리
    crackle = np.zeros_like(t)
    for _ in range(14):
        at = rng.integers(0, len(t))
        n = rng.integers(60, 500)
        crackle_seg = onepole_hp(noise(n), 2500) * env_exp(n, n / SR / 3) * rng.uniform(0.3, 1.0)
        place(crackle, crackle_seg, at, wrap=True)
    # 밝기 울렁임 (0.5Hz, 2초에 정확히 한 번)
    wobble = 0.85 + 0.15 * np.sin(2 * np.pi * 0.5 * t)
    return normalize(hum * 0.55 * wobble + crackle * 0.5, 0.6)


def light_click(seed):
    """전등이 '탁/지직' 꺼지거나 켜지는 소리 — 계전기 딸깍 + 짧은 아크."""
    r = np.random.default_rng(seed)
    n = int(SR * r.uniform(0.18, 0.3))
    x = np.zeros(n)
    tick_n = int(SR * 0.012)
    tick = noise(tick_n) * env_exp(tick_n, 0.0025, 0.0002)
    tick = resonator(tick, r.uniform(2200, 3600), 6) * 6 + onepole_hp(tick, 1500)
    place(x, tick, 0)
    # 아크 '지직': 120Hz 단위로 튀는 잡음
    arc_n = int(SR * r.uniform(0.05, 0.14))
    ta = np.arange(arc_n) / SR
    gate = (np.sin(2 * np.pi * 120 * ta) > r.uniform(0.2, 0.7)).astype(float)
    arc = onepole_hp(noise(arc_n), 1800) * gate * env_exp(arc_n, arc_n / SR / 2.5)
    place(x, arc * 0.5, int(SR * r.uniform(0.004, 0.02)))
    # 유리관 '팅' 잔향
    ring_n = n
    ring = np.sin(2 * np.pi * r.uniform(4200, 5200) * np.arange(ring_n) / SR) * env_exp(ring_n, 0.04)
    x += ring * 0.08
    return fade(normalize(x, 0.8), 0.0005, 0.03)


# ---------------------------------------------------------------- 물
def splash(size, seed):
    """첨벙. size 1 = 몸이 물에 빠짐, 0.4 = 발 담그기/나올 때."""
    r = np.random.default_rng(seed)
    sec = 0.5 + 1.0 * size
    n = int(SR * sec)
    t = np.arange(n) / SR
    x = np.zeros(n)
    # 1) 부딪힘: 넓은 대역 잡음, 밝았다가 빠르게 어두워짐
    body_env = env_exp(n, 0.08 + 0.18 * size, 0.004)
    cutoff = 300 + 7000 * np.exp(-t / (0.05 + 0.1 * size))
    x += onepole_lp(noise(n), cutoff) * body_env * 1.2
    # 2) 물보라가 떨어지는 '쏴아'
    spray_env = np.clip((t - 0.05) / 0.08, 0, 1) * np.exp(-np.maximum(t - 0.1, 0) / (0.15 + 0.3 * size))
    x += onepole_hp(noise(n), 3000) * spray_env * 0.35
    # 3) 꼬르륵 거품: 짧게 위로 올라가는 사인 처프 여러 개
    for _ in range(int(10 + 30 * size)):
        f0 = r.uniform(400, 1400) / (0.6 + size * 0.6)
        d = r.uniform(0.01, 0.04)
        bn = int(SR * d * 3)
        bt = np.arange(bn) / SR
        f = f0 * (1 + bt / d * 0.6)
        bub = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-bt / d)
        place(x, bub * r.uniform(0.1, 0.35), int(SR * r.uniform(0.02, sec * 0.6)))
    # 4) 늦게 떨어지는 물방울
    for _ in range(int(3 + 8 * size)):
        f0 = r.uniform(900, 2400)
        dn = int(SR * 0.05)
        dt = np.arange(dn) / SR
        drop = np.sin(2 * np.pi * np.cumsum(f0 * (1 + dt * 25)) / SR) * np.exp(-dt / 0.012)
        place(x, drop * r.uniform(0.05, 0.18), int(SR * r.uniform(0.25, sec * 0.95)))
    return fade(normalize(x, 0.9), 0.001, 0.08)


def water_flow_loop():
    """폭포/수로 물 흐르는 소리. 6초 → 교차 페이드로 이음매 없는 루프."""
    n = int(SR * 6.5)
    t = np.arange(n) / SR
    rumble = onepole_lp(noise(n), 250) * 2.5
    hiss = onepole_hp(onepole_lp(noise(n), 6000), 600)
    mod = 0.8 + 0.2 * onepole_lp(noise(n), 1.5) * 8
    x = rumble + hiss * mod * 0.6
    # 흘러내리는 물살 사이사이 꼴깍
    for _ in range(60):
        f0 = rng.uniform(300, 900)
        bn = int(SR * 0.06)
        bt = np.arange(bn) / SR
        bub = np.sin(2 * np.pi * np.cumsum(f0 * (1 + bt * 8)) / SR) * np.exp(-bt / 0.015)
        place(x, bub * rng.uniform(0.05, 0.15), rng.integers(0, n - bn))
    return normalize(loop_crossfade(x, 0.5), 0.55)


def wade_loop():
    """물속을 헤치며 걷는 '촤악 촤악'. 2걸음 = 1.2초 루프."""
    sec = 1.2
    n = int(SR * sec)
    x = np.zeros(n)
    for step in range(2):
        at = int(SR * (0.05 + step * sec / 2))
        sn = int(SR * 0.45)
        st = np.arange(sn) / SR
        cutoff = 500 + 2500 * np.sin(np.pi * np.clip(st / 0.35, 0, 1))
        swish = onepole_lp(noise(sn), cutoff) * np.sin(np.pi * np.clip(st / 0.45, 0, 1)) ** 2
        place(x, swish * (1.0 if step == 0 else 0.85), at, wrap=True)
    return normalize(x, 0.6)


# ---------------------------------------------------------------- 놀이기구
def music_box_note(freq, sec, r):
    n = int(SR * sec)
    t = np.arange(n) / SR
    # 오르골 빗살: 기음 + 비정수 배음(빨리 사라짐)
    x = np.sin(2 * np.pi * freq * t) * np.exp(-t / 1.1)
    x += 0.35 * np.sin(2 * np.pi * freq * 2.76 * t) * np.exp(-t / 0.25)
    x += 0.18 * np.sin(2 * np.pi * freq * 5.4 * t) * np.exp(-t / 0.08)
    x += 0.10 * np.sin(2 * np.pi * freq * 8.9 * t) * np.exp(-t / 0.03)
    # 빗살 튕기는 '틱'
    k = int(SR * 0.004)
    x[:k] += noise(k) * np.linspace(0.4, 0, k)
    return x * r.uniform(0.85, 1.0)


def carousel_music_loop():
    """회전목마 오르골 왈츠 (이 프로젝트용 창작 선율). 3/4박, 16마디.
    살짝 음이 어긋나고 테이프처럼 흔들려서 '고장 난 놀이공원' 느낌. RideMotor 가 속도에 맞춰 pitch 를 낮춤."""
    r = np.random.default_rng(7)
    bpm = 132.0
    beat = 60.0 / bpm
    bars = 16
    total = int(SR * beat * 3 * bars)
    x = np.zeros(total)

    def hz(midi):
        return 440.0 * 2 ** ((midi - 69) / 12.0)

    # 가단조 쪽으로 기우는 선율: (마디 안 박 위치, 음, 길이(박)). 숫자는 MIDI 음높이
    melody = [
        [(0, 76, 1), (1, 75, 1), (2, 76, 1)], [(0, 72, 2), (2, 69, 1)],
        [(0, 71, 1), (1, 72, 1), (2, 74, 1)], [(0, 76, 3)],
        [(0, 77, 1), (1, 76, 1), (2, 74, 1)], [(0, 72, 2), (2, 71, 1)],
        [(0, 69, 1), (1, 71, 1), (2, 72, 1)], [(0, 71, 3)],
        [(0, 76, 1), (1, 75, 1), (2, 76, 1)], [(0, 79, 2), (2, 77, 1)],
        [(0, 76, 1), (1, 74, 1), (2, 72, 1)], [(0, 74, 3)],
        [(0, 72, 1), (1, 71, 1), (2, 69, 1)], [(0, 68, 2), (2, 71, 1)],
        [(0, 69, 1), (1, 64, 1), (2, 68, 1)], [(0, 69, 3)],
    ]
    # 왈츠 반주: 1박 베이스, 2·3박 화음
    chords = [(45, [57, 60, 64]), (45, [57, 60, 64]), (43, [55, 59, 62]), (48, [55, 60, 64]),
              (50, [57, 62, 65]), (45, [57, 60, 64]), (41, [57, 60, 65]), (40, [56, 59, 64]),
              (45, [57, 60, 64]), (48, [55, 60, 64]), (45, [57, 60, 64]), (43, [55, 59, 62]),
              (41, [57, 60, 65]), (40, [56, 59, 62]), (45, [57, 60, 64]), (45, [57, 60, 64])]

    detune = {}  # 빗살마다 고정된 어긋남 (진짜 낡은 오르골처럼)

    def note(midi, start_beat, length_beats, vol):
        cents = detune.setdefault(midi, r.uniform(-18, 18))
        f = hz(midi) * 2 ** (cents / 1200)
        clip = music_box_note(f, min(2.5, length_beats * beat + 1.2), r) * vol
        place(x, clip, int(SR * start_beat * beat), wrap=True)

    for b in range(bars):
        for (pos, m, ln) in melody[b]:
            note(m, b * 3 + pos, ln, 0.9)
        bass, ch = chords[b]
        note(bass, b * 3, 1, 0.6)
        for beat_i in (1, 2):
            for m in ch:
                note(m, b * 3 + beat_i, 1, 0.22)
    # 테이프처럼 출렁이는 음정: 루프 길이에 맞춘 정수 주기 사인으로 시간 축을 살짝 흔듦
    tt = np.arange(total)
    loops = 3
    warp = tt + 0.0035 * SR * np.sin(2 * np.pi * loops * tt / total)
    x = np.interp(warp % total, tt, x)
    # 낡은 스피커: 저역·고역 깎고 약간 찌그러짐 + 기계 잡음
    x = onepole_hp(onepole_lp(x, 5500), 180)
    x = np.tanh(x * 1.4)
    x += onepole_lp(noise(total), 900) * 0.015
    return normalize(x, 0.75)


def ride_motor_loop():
    """큰 전동기 '우우웅' + 톱니 '딱딱'. 2초 정수 주기 루프."""
    sec = 2.0
    t = t_axis(sec)
    n = len(t)
    hum = np.zeros(n)
    for h, a in [(1, 1.0), (2, 0.6), (3, 0.3), (4, 0.25), (6, 0.1)]:
        hum += a * np.sin(2 * np.pi * 50 * h * t + h)
    hum *= 0.8 + 0.2 * np.sin(2 * np.pi * 1.5 * t)          # 1.5Hz 회전 울렁임 (2초에 3번)
    whine = 0.12 * np.sin(2 * np.pi * 750 * t) * (0.6 + 0.4 * np.sin(2 * np.pi * 0.5 * t))
    rumble = onepole_lp(noise(n), 120) * 3
    x = hum * 0.5 + whine + rumble * 0.4
    # 톱니: 초당 6번
    for k in range(12):
        cn = int(SR * 0.03)
        click = resonator(noise(cn) * env_exp(cn, 0.004, 0.0003), rng.uniform(1500, 2200), 8) * 3
        place(x, click * rng.uniform(0.15, 0.3), int(SR * k / 6), wrap=True)
    return normalize(x, 0.6)


def creak(seed, sec=0.9, pitch=1.0):
    """나무/쇠가 '끼이익' — 마찰 펄스열을 공진 필터에 통과."""
    r = np.random.default_rng(seed)
    n = int(SR * sec)
    t = np.arange(n) / SR
    # 펄스 간격이 불규칙하게 변함 (stick-slip)
    rate = (80 + 140 * np.sin(np.pi * t / sec) ** 0.7 + 30 * onepole_lp(r.standard_normal(n), 6) * 10) * pitch
    phase = np.cumsum(np.maximum(rate, 20) / SR)
    pulses = (np.diff(np.floor(phase), prepend=0) > 0).astype(float)
    pulses *= r.uniform(0.6, 1.0, n)
    x = resonator(pulses, 380 * pitch, 12) * 0.6 + resonator(pulses, 1150 * pitch, 18) * 0.4 \
        + resonator(pulses, 2600 * pitch, 25) * 0.15
    x *= np.sin(np.pi * t / sec) ** 0.8
    return x


def ride_creak_loop():
    """바이킹·풍차가 흔들리며 내는 '끼익… 끼이익'. 8초 = 바이킹 한 번 왕복과 같은 길이 (정확히 맞진 않음)."""
    sec = 8.0
    n = int(SR * sec)
    x = np.zeros(n)
    for i, (at, ln, p) in enumerate([(0.3, 1.1, 1.0), (2.2, 0.7, 1.25), (4.3, 1.3, 0.9), (6.3, 0.6, 1.15)]):
        place(x, creak(30 + i, ln, p), int(SR * at), wrap=True)
    # 바람/쇠 울림 바닥
    x += onepole_lp(noise(n), 200) * 0.02
    t = np.arange(n) / SR
    x += 0.04 * np.sin(2 * np.pi * 55 * t) * (0.5 + 0.5 * np.sin(2 * np.pi * t / sec))
    return normalize(x, 0.7)


def ride_start_clunk():
    """놀이기구가 켜지는 '쿵-덜컥' + 계전기 + 모터가 걸리는 '우웅'."""
    sec = 2.2
    n = int(SR * sec)
    t = np.arange(n) / SR
    x = np.zeros(n)
    # 쿵: 낮은 사인 + 잡음
    kn = int(SR * 0.6)
    kt = np.arange(kn) / SR
    thud = np.sin(2 * np.pi * (55 + 40 * np.exp(-kt / 0.03)) * kt) * np.exp(-kt / 0.18)
    thud += onepole_lp(noise(kn), 600) * np.exp(-kt / 0.05) * 0.6
    place(x, thud, 0)
    # 덜컥 두 번 (쇠 부딪힘)
    for at, f in [(0.12, 900), (0.31, 1300)]:
        cn = int(SR * 0.25)
        metal = resonator(noise(cn) * env_exp(cn, 0.01, 0.0005), f, 30) * 4
        metal += resonator(noise(cn) * env_exp(cn, 0.01, 0.0005), f * 2.7, 40) * 2
        place(x, metal * 0.5, int(SR * at))
    # 모터가 걸리며 올라가는 소리
    f = 20 + 30 * np.clip((t - 0.4) / 1.6, 0, 1)
    spin = np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.4 * np.sin(2 * np.pi * np.cumsum(f * 2) / SR)
    x += spin * np.clip((t - 0.4) / 0.4, 0, 1) * np.exp(-np.maximum(t - 1.4, 0) / 0.4) * 0.35
    return fade(normalize(x, 0.9), 0.001, 0.2)


if __name__ == "__main__":
    save("light_buzz_loop", light_buzz_loop())
    for i in range(3):
        save(f"light_click_{i + 1}", light_click(100 + i))
    save("water_splash_big", splash(1.0, 11))
    save("water_splash_small_1", splash(0.4, 12))
    save("water_splash_small_2", splash(0.35, 13))
    save("water_flow_loop", water_flow_loop())
    save("water_wade_loop", wade_loop())
    save("carousel_music_loop", carousel_music_loop())
    save("ride_motor_loop", ride_motor_loop())
    save("ride_creak_loop", ride_creak_loop())
    save("ride_start_clunk", ride_start_clunk())
