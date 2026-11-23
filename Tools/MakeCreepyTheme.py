"""밝은 노래를 '폐장한 놀이공원 스피커에서 흘러나오는 노래'처럼 무섭게 바꾼다. (롯데월드 테마송용)

실행:  python -I Tools/MakeCreepyTheme.py <입력.wav> <출력.wav> [--seed 1120]
       입력은 44.1kHz 16bit WAV (스테레오/모노). mp3 는 먼저 WAV 로 풀 것.

처리 순서 (뒤로 갈수록 노래가 점점 더 망가짐 = deg 곡선 0.25 → 1):
  1. 테이프 감속: 전체를 0.84배로 느리게 (음도 같이 낮아짐) + 출렁임(wow/flutter)
     + 가끔 테이프가 늘어지듯 확 처졌다 돌아옴 + 마지막 몇 초는 전원이 꺼지듯 멈춤
  2. 유령 층: 한 옥타브 아래로 내린 같은 노래 + 거꾸로 재생한 잔향이 다음 소절 앞에서 빨려 들어오듯 부풂
  3. 낡은 확성기: 저역/고역 깎기, 찌그러짐, 순간 끊김(접촉 불량)
  4. 텅 빈 공원: 긴 잔향 + 건물에 맞고 돌아오는 메아리
  5. 테이프 잡음 · 지직거림 · 전원 험
"""
import sys
import wave

import numpy as np
from scipy import signal

SR = 44100


def load(path):
    with wave.open(path, "rb") as w:
        assert w.getframerate() == SR and w.getsampwidth() == 2, "44.1kHz 16bit WAV 만 받음"
        ch = w.getnchannels()
        x = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(np.float64) / 32768.0
    x = x.reshape(-1, ch)
    if ch == 1:
        x = np.repeat(x, 2, axis=1)
    return x[:, :2]


def save(path, x):
    x = np.clip(x, -1, 1)
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype("<i2").tobytes())


def smooth_noise(n, hz, rng):
    """hz 정도로 천천히 움직이는 -1~1 잡음"""
    k = max(2, int(n * hz / SR) + 2)
    pts = rng.uniform(-1, 1, k)
    return np.interp(np.linspace(0, k - 1, n), np.arange(k), pts)


def butter(x, kind, hz, order=2):
    sos = signal.butter(order, hz, btype=kind, fs=SR, output="sos")
    return signal.sosfilt(sos, x, axis=0)


# ------------------------------------------------------------------ 1. 테이프 감속
def tape_warp(x, rng, base=0.84):
    n_in = len(x)
    est = int(n_in / base * 1.15)
    t = np.arange(est) / SR
    speed = base * (1 + 0.007 * np.sin(2 * np.pi * 0.45 * t) + 0.005 * smooth_noise(est, 1.2, rng)
                    + 0.0015 * np.sin(2 * np.pi * 7.5 * t))          # flutter
    # 테이프가 늘어지는 순간: 곡 길이 비율로 몇 군데
    total_sec = n_in / SR / base
    for frac, depth in [(0.33, 0.30), (0.58, 0.42), (0.79, 0.55)]:
        c = frac * total_sec + rng.uniform(-2, 2)
        dip = np.exp(-0.5 * ((t - c) / 0.45) ** 2)                   # 0.45초 폭
        speed *= 1 - depth * dip
    pos = np.cumsum(speed)                                            # 원본 샘플 위치
    n_out = int(np.searchsorted(pos, n_in - 1))
    pos, speed, t = pos[:n_out], speed[:n_out], t[:n_out]
    # 끝 6초: 전원이 꺼지듯 테이프가 멈춤 (위치 증가량을 0까지 줄임)
    stop = int(SR * 6)
    if n_out > stop * 2:
        ramp = np.linspace(1, 0, stop) ** 1.6
        inc = np.diff(pos[-stop - 1:]) * ramp
        pos[-stop:] = pos[-stop - 1] + np.cumsum(inc)
    idx = np.arange(n_in)
    y = np.stack([np.interp(pos, idx, x[:, c]) for c in range(2)], axis=1)
    return y


# ------------------------------------------------------------------ 2. 유령 층
def pitch_down_octave(x):
    """위상 보코더로 2배 늘린 뒤 2배 빠르게 재생 → 길이는 그대로, 한 옥타브 아래"""
    out = []
    nfft, hop = 2048, 512
    for c in range(2):
        f, t, Z = signal.stft(x[:, c], fs=SR, nperseg=nfft, noverlap=nfft - hop)
        rate = 0.5
        steps = np.arange(0, Z.shape[1] - 1, rate)
        mag = np.abs(Z)
        ph = np.angle(Z)
        expected = 2 * np.pi * hop * np.arange(Z.shape[0]) / nfft
        acc = ph[:, 0].copy()
        Y = np.empty((Z.shape[0], len(steps)), dtype=complex)
        for i, s in enumerate(steps):
            k = int(s)
            a = s - k
            m = (1 - a) * mag[:, k] + a * mag[:, k + 1]
            Y[:, i] = m * np.exp(1j * acc)
            d = ph[:, k + 1] - ph[:, k] - expected
            d -= 2 * np.pi * np.round(d / (2 * np.pi))
            acc += expected + d
        _, stretched = signal.istft(Y, fs=SR, nperseg=nfft, noverlap=nfft - hop)
        out.append(stretched[::2][: len(x)])
    n = min(len(o) for o in out)
    y = np.zeros_like(x)
    y[:n] = np.stack([o[:n] for o in out], axis=1)
    return y


def reverse_swells(x, rng, count=7):
    """짧은 구간을 거꾸로 + 잔향 → 그 소절이 시작되기 직전에 '스으읍' 빨려 들어오는 소리"""
    y = np.zeros_like(x)
    ir = impulse(2.5, rng, predelay=0.0)
    for _ in range(count):
        at = int(rng.uniform(0.12, 0.9) * len(x))
        ln = int(SR * rng.uniform(1.0, 1.8))
        seg = x[at:at + ln][::-1]
        if len(seg) < ln:
            continue
        wet = np.stack([signal.fftconvolve(seg[:, c], ir[:, c])[:ln * 2] for c in range(2)], axis=1)[::-1]
        start = at - len(wet)
        if start < 0:
            continue
        env = np.linspace(0, 1, len(wet))[:, None] ** 2
        y[start:at] += wet * env
    return y


# ------------------------------------------------------------------ 4. 잔향
def impulse(sec, rng, predelay=0.06):
    n = int(SR * sec)
    t = np.arange(n) / SR
    ir = rng.standard_normal((n, 2)) * np.exp(-t / (sec / 6.5))[:, None]
    ir = butter(ir, "low", 5000)
    ir[: int(SR * 0.01)] *= np.linspace(0, 1, int(SR * 0.01))[:, None]
    pd = int(SR * predelay)
    ir = np.concatenate([np.zeros((pd, 2)), ir])
    return ir / np.sqrt((ir ** 2).sum(axis=0))


def reverb(x, rng, sec=4.5):
    ir = impulse(sec, rng)
    return np.stack([signal.fftconvolve(x[:, c], ir[:, c])[: len(x)] for c in range(2)], axis=1)


def echoes(x, delays=(0.37, 0.81), gains=(0.32, 0.16)):
    y = x.copy()
    for d, g in zip(delays, gains):
        k = int(SR * d)
        e = butter(x, "low", 2500)
        y[k:] += e[:-k] * g * np.array([1.0, 0.85]) if d < 0.5 else e[:-k] * g * np.array([0.85, 1.0])
    return y


# ------------------------------------------------------------------ 3. 확성기
def speaker(x, deg, rng):
    band = butter(butter(x, "high", 280), "low", 4200)
    worse = butter(butter(x, "high", 450), "low", 2300)
    y = band * (1 - deg[:, None]) + worse * deg[:, None]
    drive = 1.4 + 2.2 * deg[:, None]
    y = np.tanh(y * drive) / np.tanh(drive)
    # 접촉 불량: 짧게 소리가 뚝 끊기거나 작아짐 (뒤로 갈수록 잦음)
    gate = np.ones(len(x))
    i = int(SR * 4)
    while i < len(x):
        i += int(SR * rng.uniform(1.5, 9) * (1.3 - deg[min(i, len(x) - 1)]))
        if i >= len(x):
            break
        ln = int(SR * rng.uniform(0.03, 0.18))
        floor = rng.uniform(0.0, 0.25)
        gate[i:i + ln] = np.minimum(gate[i:i + ln], floor)
    gate = butter(gate, "low", 120, order=1)
    return y * gate[:, None]


# ------------------------------------------------------------------ 5. 잡음
def noise_bed(n, deg, rng):
    t = np.arange(n) / SR
    hiss = butter(rng.standard_normal((n, 2)), "high", 3000) * 0.012
    hum = (np.sin(2 * np.pi * 60 * t) + 0.4 * np.sin(2 * np.pi * 180 * t))[:, None] * 0.010
    crackle = np.zeros((n, 2))
    pops = rng.random(n) < (0.00012 + 0.0006 * deg)
    crackle[pops] = rng.standard_normal((pops.sum(), 2)) * 0.5
    crackle = butter(crackle, "high", 1500)
    return hiss + hum * (0.5 + deg[:, None]) + crackle


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    seed = 1120
    if "--seed" in sys.argv:
        seed = int(sys.argv[sys.argv.index("--seed") + 1])
    src, dst = args[0], args[1]
    rng = np.random.default_rng(seed)

    x = load(src)
    x = x / max(1e-9, np.abs(x).max())
    slow = tape_warp(x, rng)
    n = len(slow)
    deg = np.linspace(0.25, 1.0, n) ** 1.3                           # 뒤로 갈수록 망가짐

    ghost = butter(pitch_down_octave(slow), "low", 1800) * (0.18 + 0.32 * deg[:, None])
    swells = reverse_swells(slow, rng) * 0.5
    pa = speaker(slow + ghost, deg, rng)
    # 꼬리: 잔향이 사라질 때까지 4초 더
    pad = np.zeros((int(SR * 4), 2))
    pa_p = np.concatenate([pa, pad])
    deg_p = np.concatenate([deg, np.ones(len(pad))])
    wet = reverb(echoes(pa_p), rng)
    mix = pa_p * 0.55 + wet * (0.55 + 0.35 * deg_p[:, None]) + reverb(np.concatenate([swells, pad]), rng, 3.0) * 0.6
    # 처음 2초는 멀리서 다가오듯 페이드 인
    fi = int(SR * 2.0)
    mix[:fi] *= np.linspace(0, 1, fi)[:, None] ** 2
    bed = noise_bed(len(mix), deg_p, rng)
    bed[-len(pad):] *= np.linspace(1, 0, len(pad))[:, None]
    mix += bed
    mix *= 0.89 / np.abs(mix).max()
    save(dst, mix)
    print(f"{dst}: {len(mix) / SR:.1f}s (원본 {len(x) / SR:.1f}s)")


if __name__ == "__main__":
    main()
