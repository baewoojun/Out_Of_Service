"""밝은 노래를 '폐장한 놀이공원 스피커에서 흘러나오는 노래'처럼 무섭게 바꾼다. (롯데월드 테마송용)

실행:  python -I Tools/MakeCreepyTheme.py <입력.wav> <출력.wav> [--preset v1|v2|v3] [--seed 1120]
       v1 = park_theme_creepy.wav (2026-11-23 커밋본),  v2 = park_theme_creepy_v2.wav (더 무서운 판, 맨 아래 make_v2 설명)
       v3 = park_theme_creepy_v3.wav (v2 + 여자 보컬을 남자 목소리처럼: 음높이만 5반음 더 내리고 포먼트 유지, male_voice)
       v4 = park_theme_creepy_v4.wav (v3 보다 더 느리게 0.66배, 7반음 더 낮게, 원곡 19.8~23.7초 넘어가는 구간을 1.6배로 늘려 뭉갬)
       v5 = 공포판 (곡보다 공포 사운드 위주: 불협화음 겹치기·속삭임·쇠 비명·빨라지는 심장·정적 뒤 한 방, make_v5 설명)
       v6 = v5 + 뭉개진 곡 전체를 바닥에 깔고 노래 층의 가사를 번지게 뭉갬 (smear_bed, spectral_blur)
       --events 1 을 붙이면 효과가 몇 초에 들어갔는지 출력
       입력은 44.1kHz 16bit WAV (스테레오/모노). mp3 는 먼저 WAV 로 풀 것.

처리 순서 (뒤로 갈수록 노래가 점점 더 망가짐 = deg 곡선 0.25 → 1):
  1. 테이프 감속: 전체를 0.84배로 느리게 (음도 같이 낮아짐) + 출렁임(wow/flutter)
     + 가끔 테이프가 늘어지듯 확 처졌다 돌아옴 + 마지막 몇 초는 전원이 꺼지듯 멈춤
  2. 유령 층: (v1) 한 옥타브 위로 올려 어둡게 깎은 같은 노래 (원래 아래로 내리려던 것. v1 소리를 그대로 남기려고 고치지 않음) + 거꾸로 재생한 잔향이 다음 소절 앞에서 빨려 들어오듯 부풂
  3. 낡은 확성기: 저역/고역 깎기, 찌그러짐, 순간 끊김(접촉 불량)
  4. 텅 빈 공원: 긴 잔향 + 건물에 맞고 돌아오는 메아리
  5. 테이프 잡음 · 지직거림 · 전원 험
"""
import sys
import wave

import numpy as np
from scipy import signal

SR = 44100
EVENTS = []   # (시작초, 길이초, 설명) — 어느 효과가 몇 초에 있는지 확인용 (--events 로 출력). 시간은 판 튐 삽입 전 기준
FINAL_EVENTS = []  # 판 튐 뒤에 적용되는 효과 (시간이 이미 최종 기준)
INSERTS = []  # (삽입 위치초, 삽입 길이초) — 판 튐 반복이 끼어들며 뒤쪽 시간이 밀린 만큼 (최종 시간 계산용)


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
def tape_warp(x, rng, base=0.84, dips=((0.33, 0.30), (0.58, 0.42), (0.79, 0.55)), dip_width=0.45):
    n_in = len(x)
    est = int(n_in / base * 1.15)
    t = np.arange(est) / SR
    speed = base * (1 + 0.007 * np.sin(2 * np.pi * 0.45 * t) + 0.005 * smooth_noise(est, 1.2, rng)
                    + 0.0015 * np.sin(2 * np.pi * 7.5 * t))          # flutter
    # 테이프가 늘어지는 순간: 곡 길이 비율로 몇 군데
    total_sec = n_in / SR / base
    for frac, depth in dips:
        c = frac * total_sec + rng.uniform(-2, 2)
        dip = np.exp(-0.5 * ((t - c) / dip_width) ** 2)
        speed *= 1 - depth * dip
        EVENTS.append((c - 2 * dip_width, 4 * dip_width, f"테이프 늘어짐 깊이 {depth:.2f}"))
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
    """(v1 전용, 이름과 달리) 위상 보코더로 2배 늘린 뒤 한 칸씩 건너뛰어 2배 빠르게 → 길이 그대로, 한 옥타브 '위'.
    v1 결과를 그대로 재현하려고 남겨 둠. 제대로 된 음높이 변경은 pitch_shift."""
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
        FINAL_EVENTS.append((start / SR, (at - start) / SR, "거꾸로 잔향 빨려듦"))
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


def make_v1(x, rng):
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
    return mix


# ================================================================== v2 (더 무서운 판)
def pitch_shift(x, semitones):
    """위상 보코더 음높이 변경 (길이 그대로). semitones < 0 이면 낮아짐."""
    p = 2 ** (semitones / 12.0)
    nfft, hop = 2048, 512
    out = np.zeros_like(x)
    for c in range(x.shape[1]):
        f, t, Z = signal.stft(x[:, c], fs=SR, nperseg=nfft, noverlap=nfft - hop)
        steps = np.arange(0, Z.shape[1] - 1, 1.0 / p)                # 길이를 p 배로 (음높이 그대로)
        mag, ph = np.abs(Z), np.angle(Z)
        expected = 2 * np.pi * hop * np.arange(Z.shape[0]) / nfft
        acc = ph[:, 0].copy()
        Y = np.empty((Z.shape[0], len(steps)), dtype=complex)
        for i, st in enumerate(steps):
            k = int(st)
            a = st - k
            Y[:, i] = ((1 - a) * mag[:, k] + a * mag[:, k + 1]) * np.exp(1j * acc)
            d = ph[:, k + 1] - ph[:, k] - expected
            d -= 2 * np.pi * np.round(d / (2 * np.pi))
            acc += expected + d
        _, y = signal.istft(Y, fs=SR, nperseg=nfft, noverlap=nfft - hop)
        # 짧아진 것을 원래 길이로 늘려 느리게 재생 → 음높이가 p 배
        out[:, c] = np.interp(np.linspace(0, len(y) - 1, len(x)), np.arange(len(y)), y)
    return out


def spectral_envelope(mag, lifter=40):
    """프레임마다 켑스트럼을 매끈하게 깎아 스펙트럼 윤곽(포먼트)만 남김. mag: (주파수, 프레임)"""
    logm = np.log(mag + 1e-9)
    cep = np.fft.irfft(logm, axis=0)
    cep[lifter:-lifter] = 0
    return np.exp(np.fft.rfft(cep, axis=0).real[: mag.shape[0]])


def male_voice(x, semitones=-5.0):
    """음높이만 내리고 포먼트(목소리 음색을 정하는 울림 위치)는 그대로 → 여자 목소리가 남자 목소리처럼.
    그냥 음을 내리면 포먼트까지 같이 내려가 괴물/슬로모션 소리가 나므로, 내린 소리의 윤곽을 원래 윤곽으로 되돌림."""
    shifted = pitch_shift(x, semitones)
    nfft, hop = 2048, 512
    out = np.zeros_like(x)
    for c in range(x.shape[1]):
        _, _, X = signal.stft(x[:, c], fs=SR, nperseg=nfft, noverlap=nfft - hop)
        _, _, Y = signal.stft(shifted[:, c], fs=SR, nperseg=nfft, noverlap=nfft - hop)
        m = min(X.shape[1], Y.shape[1])
        X, Y = X[:, :m], Y[:, :m]
        gain = spectral_envelope(np.abs(X)) / spectral_envelope(np.abs(Y))
        Y = Y * np.clip(gain, 0.2, 5.0)
        _, y = signal.istft(Y, fs=SR, nperseg=nfft, noverlap=nfft - hop)
        out[: min(len(y), len(x)), c] = y[: len(x)]
    return out * (np.sqrt((x ** 2).mean()) / max(1e-9, np.sqrt((out ** 2).mean())))


def chorus_detune(x, rng, depth_ms=9.0, rate=0.23):
    """천천히 흔들리는 지연 복사본을 섞음 → 음이 조금씩 어긋난 낡은 악기처럼"""
    n = len(x)
    t = np.arange(n) / SR
    y = x.copy()
    for c, ph in ((0, 0.0), (1, 1.7)):
        d = (18 + depth_ms * np.sin(2 * np.pi * rate * t + ph) + 3 * smooth_noise(n, 0.7, rng)) * SR / 1000
        y[:, c] = 0.6 * x[:, c] + 0.55 * np.interp(np.arange(n) - d, np.arange(n), x[:, c])
    return y


def broken_record(x, rng, count=5):
    """판이 튀듯 짧은 조각을 3~5번 반복 (반복할수록 늘어지고 어두워짐). 전체 길이가 늘어남."""
    n = len(x)
    cuts = sorted(rng.uniform(0.22, 0.9, count))
    pieces, last = [], 0
    k = int(SR * 0.004)
    for c in cuts:
        at = int(c * n)
        ln = int(SR * rng.uniform(0.28, 0.55))
        if at - last < ln * 2 or at + ln > n:
            continue
        pieces.append(x[last:at + ln])
        frag = x[at:at + ln]
        reps = int(rng.integers(3, 6))
        added = sum(len(np.arange(0, ln - 1, 1 - 0.035 * (r + 1))) for r in range(reps)) / SR
        EVENTS.append((at / SR, ln / SR + added, f"판 튐 {reps}번 반복"))
        INSERTS.append(((at + ln) / SR, added))
        for r in range(reps):
            sp = 1 - 0.035 * (r + 1)
            idx = np.arange(0, ln - 1, sp)
            g = np.stack([np.interp(idx, np.arange(ln), frag[:, ch]) for ch in range(2)], axis=1)
            g = butter(g, "low", 4000 - 600 * r) * (0.95 - 0.1 * r)
            g[:k] *= np.linspace(0, 1, k)[:, None]
            g[-k:] *= np.linspace(1, 0, k)[:, None]
            pieces.append(g)
        last = at + ln
    pieces.append(x[last:])
    return np.concatenate(pieces)


def reverse_chunk(x, rng):
    """후반에 3초 정도를 거꾸로 재생"""
    at = int(rng.uniform(0.55, 0.65) * len(x))
    ln = int(SR * 3.2)
    y = x.copy()
    FINAL_EVENTS.append((at / SR, ln / SR, "거꾸로 재생"))
    seg = y[at:at + ln][::-1].copy()
    k = int(SR * 0.05)
    seg[:k] *= np.linspace(0, 1, k)[:, None]
    seg[-k:] *= np.linspace(1, 0, k)[:, None]
    y[at:at + ln] = seg
    return y


def holes(n, rng, count=3):
    """음악이 몇 초 동안 쑥 빠지고 (잔향·웅웅거림만 남았다가) 갑자기 돌아옴"""
    g = np.ones(n)
    for c in sorted(rng.uniform(0.3, 0.85, count)):
        a = int(c * n)
        out_ln, hold, back = int(SR * 1.2), int(SR * rng.uniform(1.8, 3.0)), int(SR * 0.03)
        if a + out_ln + hold + back >= n:
            continue
        FINAL_EVENTS.append((a / SR, (out_ln + hold) / SR, "음악이 빠지는 구멍"))
        g[a:a + out_ln] = np.minimum(g[a:a + out_ln], np.linspace(1, 0.02, out_ln) ** 2)
        g[a + out_ln:a + out_ln + hold] = 0.02
        g[a + out_ln + hold:a + out_ln + hold + back] = np.linspace(0.02, 1, back)
    return g


def drone(n, deg, rng):
    """낮게 맥놀이하는 웅웅거림 + 땅울림 + 뒤쪽 1/3 에서 점점 느려지는 심장 박동"""
    t = np.arange(n) / SR
    d = 0.5 * np.sin(2 * np.pi * 41 * t) + 0.5 * np.sin(2 * np.pi * 43.3 * t) + 0.25 * np.sin(2 * np.pi * 61.7 * t)
    rumble = butter(rng.standard_normal(n), "low", 90) * 6
    y = (d * 0.6 + rumble) * (0.15 + 0.85 * deg) * 0.12
    beat = np.zeros(n)
    tt, period = 0.62 * n / SR, 0.95
    ln = int(SR * 0.25)
    bt = np.arange(ln) / SR
    thump = np.sin(2 * np.pi * (48 + 30 * np.exp(-bt / 0.02)) * bt) * np.exp(-bt / 0.07)
    while tt < n / SR - 1:
        for off, amp in ((0.0, 1.0), (0.24, 0.65)):                   # 쿵-쿵
            k = int((tt + off) * SR)
            if k + ln < n:
                beat[k:k + ln] += amp * thump
        tt += period
        period *= 1.035
    y += beat * 0.22
    return np.stack([y, y * 0.97], axis=1)


def pv_stretch(x, factor):
    """위상 보코더로 음높이는 그대로 두고 길이만 factor 배. 길게 늘리면 소리가 번지며 뭉개짐."""
    nfft, hop = 2048, 512
    outs = []
    for c in range(x.shape[1]):
        _, _, Z = signal.stft(x[:, c], fs=SR, nperseg=nfft, noverlap=nfft - hop)
        steps = np.arange(0, Z.shape[1] - 1, 1.0 / factor)
        mag, ph = np.abs(Z), np.angle(Z)
        expected = 2 * np.pi * hop * np.arange(Z.shape[0]) / nfft
        acc = ph[:, 0].copy()
        Y = np.empty((Z.shape[0], len(steps)), dtype=complex)
        for i, st in enumerate(steps):
            k = int(st)
            a = st - k
            Y[:, i] = ((1 - a) * mag[:, k] + a * mag[:, k + 1]) * np.exp(1j * acc)
            d = ph[:, k + 1] - ph[:, k] - expected
            d -= 2 * np.pi * np.round(d / (2 * np.pi))
            acc += expected + d
        _, y = signal.istft(Y, fs=SR, nperseg=nfft, noverlap=nfft - hop)
        outs.append(y)
    m = min(len(o) for o in outs)
    return np.stack([o[:m] for o in outs], axis=1)


def stretch_section(x, a_sec, b_sec, factor):
    """원곡의 a~b 초 구간만 factor 배로 늘려 끼워 넣음 (경계는 0.08초 교차 페이드)"""
    a, b = int(a_sec * SR), int(b_sec * SR)
    k = int(SR * 0.08)
    mid = pv_stretch(x[a - k:b + k], factor)
    ramp = np.linspace(0, 1, k)[:, None]
    head = x[:a].copy()
    head[-k:] = head[-k:] * (1 - ramp) + mid[:k] * ramp
    tail = x[b:].copy()
    tail[:k] = mid[-k:] * (1 - ramp) + tail[:k] * ramp
    EVENTS.append((-1.0, 0.0, f"(원곡 {a_sec}~{b_sec}초를 {factor}배로 늘림)"))
    return np.concatenate([head, mid[k:-k], tail])


def make_v2(x, rng, male=False, male_semitones=-5.0, base=0.74, smear=None):
    """v1 보다 더 느리고(0.74배) 더 자주·깊게 늘어지며,
    판 튀듯 반복 · 거꾸로 재생 구간 · 음악이 사라지는 구멍 · 3온음/옥타브 아래 유령 · 금속성 링 변조 ·
    웅웅거리는 저음과 느려지는 심장 박동 · 더 긴 잔향. 다 멈춘 뒤 첫 소절이 아주 느리고 낮게 한 번 더 들림."""
    dips = ((0.18, 0.35), (0.31, 0.5), (0.47, 0.45), (0.6, 0.62), (0.72, 0.55), (0.86, 0.7))
    if smear:                                                         # v4: 특정 구간을 늘려 뭉갬
        x = stretch_section(x, *smear)
    slow = tape_warp(x, rng, base=base, dips=dips, dip_width=0.7)
    if male:                                                          # v3·v4: 남자 목소리처럼 (반주도 같이 내려감)
        slow = male_voice(slow, male_semitones)
    slow = broken_record(slow, rng)
    slow = reverse_chunk(slow, rng)
    slow = chorus_detune(slow, rng)
    n = len(slow)
    deg = 0.45 + 0.55 * np.linspace(0, 1, n) ** 1.2

    low = butter(slow, "low", 1400)
    ghost = pitch_shift(low, -6) * (0.25 + 0.35 * deg[:, None]) + pitch_shift(low, -12) * (0.1 + 0.5 * deg[:, None])
    swells = reverse_swells(slow, rng, count=11) * 0.7
    t = np.arange(n) / SR
    ring = slow * np.sin(2 * np.pi * 37 * t)[:, None]
    pa = speaker(slow + ghost + ring * (0.08 + 0.25 * deg[:, None]), deg, rng)
    pa = butter(pa, "low", 3200)                                      # 벽 너머처럼 더 먹먹하게
    pa *= holes(n, rng)[:, None]

    pad = np.zeros((int(SR * 6), 2))
    pa_p = np.concatenate([pa, pad])
    deg_p = np.concatenate([deg, np.ones(len(pad))])
    wet = reverb(echoes(pa_p, delays=(0.41, 0.93, 1.6), gains=(0.38, 0.22, 0.12)), rng, sec=6.5)
    mix = pa_p * 0.4 + wet * (0.75 + 0.35 * deg_p[:, None]) + reverb(np.concatenate([swells, pad]), rng, 4.0) * 0.8
    fi = int(SR * 3.0)
    mix[:fi] *= np.linspace(0, 1, fi)[:, None] ** 2
    fade_tail = np.linspace(1, 0, len(pad))[:, None]
    dr = drone(len(mix), deg_p, rng)
    dr[-len(pad):] *= fade_tail
    bed = noise_bed(len(mix), deg_p, rng) * 1.3
    bed[-len(pad):] *= fade_tail
    mix += dr + bed

    # 끝: 정적 2초 뒤 첫 소절이 절반 속도 + 한 옥타브 더 아래(=두 옥타브 아래)로 희미하게
    first = x[int(SR * 0.3):int(SR * 4.3)]
    idx = np.arange(0, len(first) - 1, 0.5)
    last = np.stack([np.interp(idx, np.arange(len(first)), first[:, c]) for c in range(2)], axis=1)
    last = butter(pitch_shift(last, -12), "low", 900)
    last *= np.sin(np.pi * np.linspace(0, 1, len(last)))[:, None] * 0.35
    last = np.concatenate([last, np.zeros((int(SR * 6), 2))])
    last = reverb(last, rng, sec=6.5) * 0.9 + last * 0.3
    return np.concatenate([mix, np.zeros((int(SR * 2), 2)), last])


# ================================================================== v5 (공포판: 곡보다 공포 사운드 위주)
def resonator(x, freq, q):
    """고정 주파수 2차 대역통과 (울림통)"""
    b, a = signal.iirpeak(freq, q, fs=SR)
    return signal.lfilter(b, a, x)


def vari_speed(x, speed):
    """speed: 출력 샘플마다의 재생 속도 배열(1 = 원래). 음높이·빠르기가 같이 바뀜 (테이프처럼)."""
    pos = np.cumsum(speed)
    pos = pos[pos < len(x) - 1]
    idx = np.arange(len(x))
    return np.stack([np.interp(pos, idx, x[:, c]) for c in range(2)], axis=1)


def dissonance(seg, amounts=((+1, 0.42), (-6, 0.34), (-12, 0.4))):
    """같은 소리를 반음 위 · 3온음 아래 · 옥타브 아래로 겹침 → 밝은 장조 화음이 서로 부딪혀 불안해짐"""
    out = seg.copy()
    for st, g in amounts:
        out += pitch_shift(seg, st) * g
    return out


def whispers(n, rng, density=1.0):
    """숨소리 섞인 속삭임: 잡음을 말소리 울림대(포먼트) 2~3개로 걸러 음절처럼 끊어 좌우로 흩뿌림"""
    y = np.zeros((n, 2))
    t = 0
    while t < n:
        t += int(SR * rng.uniform(0.6, 3.0) / density)
        burst = int(SR * rng.uniform(0.6, 1.8))                       # 한 마디 중얼거림
        if t + burst >= n:
            break
        seg = np.zeros(burst)
        k = 0
        while k < burst:                                              # 음절
            ln = int(SR * rng.uniform(0.07, 0.2))
            if k + ln > burst:
                break
            src = rng.standard_normal(ln)
            f1, f2, f3 = rng.uniform(350, 800), rng.uniform(900, 2200), rng.uniform(2400, 3400)
            syl = resonator(src, f1, 6) * 0.6 + resonator(src, f2, 8) * 0.8 + resonator(src, f3, 10) * 0.4
            syl += butter(src, "high", 4500) * rng.uniform(0.1, 0.5)   # ㅅ·ㅎ 같은 바람 소리
            syl *= np.sin(np.pi * np.linspace(0, 1, ln)) ** 1.5
            seg[k:k + ln] += syl
            k += ln + int(SR * rng.uniform(0.0, 0.06))
        pan = rng.uniform(0.1, 0.9)
        y[t:t + burst, 0] += seg * (1 - pan)
        y[t:t + burst, 1] += seg * pan
    return butter(y, "high", 250)


def metal_shriek(n, rng, swell=True):
    """쇠를 활로 긋는 듯한 끼이이익: 어긋난 높은 배음들이 천천히 미끄러지며 커졌다 작아짐"""
    t = np.arange(n) / SR
    y = np.zeros(n)
    for _ in range(7):
        f0 = rng.uniform(900, 3200)
        glide = 1 + 0.06 * smooth_noise(n, 0.3, rng) + rng.uniform(-0.04, 0.04) * t / max(t[-1], 1e-6)
        ph = np.cumsum(2 * np.pi * f0 * glide / SR)
        am = np.clip(0.5 + 0.8 * smooth_noise(n, rng.uniform(0.5, 2.5), rng), 0, 1)
        y += np.sin(ph) * am * rng.uniform(0.3, 1.0)
    grit = resonator(rng.standard_normal(n), rng.uniform(2200, 3000), 40) * 0.6
    y = np.tanh((y + grit) * 0.8)
    if swell:
        y *= np.sin(np.pi * np.linspace(0, 1, n)) ** 2
    pan = 0.5 + 0.45 * smooth_noise(n, 0.2, rng)
    return np.stack([y * (1 - pan), y * pan], axis=1)


def cluster(n, rng, lo=98.0, count=11, rise=True):
    """반음씩 붙은 음 덩어리 (현악기 군집처럼). rise 면 점점 위로 끌려 올라가며 커짐"""
    t = np.arange(n) / SR
    y = np.zeros((n, 2))
    bend = 1 + (0.5 * (t / t[-1]) ** 2 if rise else 0)
    for i in range(count):
        f = lo * 2 ** (i / 12) * (1 + rng.uniform(-0.006, 0.006))
        ph = np.cumsum(2 * np.pi * f * bend / SR)
        tone = sum(np.sin(ph * h) / h for h in range(1, 9))          # 톱니파 비슷 (현악기 거친 소리)
        vib = 1 + 0.004 * np.sin(2 * np.pi * rng.uniform(4, 6.5) * t)
        tone = tone * vib
        pan = rng.uniform(0.15, 0.85)
        y[:, 0] += tone * (1 - pan)
        y[:, 1] += tone * pan
    y = butter(y, "low", 3500) / count
    if rise:
        y *= (np.linspace(0, 1, n) ** 2.2)[:, None]
    return np.tanh(y * 2.5)


def heartbeat(n, bpm_from, bpm_to, gain=1.0):
    y = np.zeros(n)
    ln = int(SR * 0.3)
    bt = np.arange(ln) / SR
    thump = np.sin(2 * np.pi * (45 + 35 * np.exp(-bt / 0.02)) * bt) * np.exp(-bt / 0.08)
    tt = 0.0
    while True:
        frac = tt / (n / SR)
        if frac >= 1:
            break
        bpm = bpm_from + (bpm_to - bpm_from) * frac ** 1.5
        for off, amp in ((0.0, 1.0), (0.22 * 70 / bpm, 0.6)):
            k = int((tt + off) * SR)
            if k + ln < n:
                y[k:k + ln] += thump * amp
        tt += 60.0 / bpm
    return np.stack([y, y], axis=1) * gain


def sub_drone(n, rng):
    t = np.arange(n) / SR
    d = (np.sin(2 * np.pi * 36.7 * t) + np.sin(2 * np.pi * 38.9 * t) + 0.6 * np.sin(2 * np.pi * 51.9 * t)
         + 0.4 * np.sin(2 * np.pi * 55.0 * t + 1))                    # 서로 부딪히는 낮은 음들
    rumble = butter(rng.standard_normal(n), "low", 70) * 8
    y = (d * 0.5 + rumble) * (0.7 + 0.3 * smooth_noise(n, 0.15, rng))
    return np.stack([y, y * 0.95], axis=1)


def stinger(rng):
    """깜짝 놀라는 한 방: 낮은 쾅 + 높은 현 비명 덩어리 + 잡음 폭발, 아래로 무너지며 사라짐"""
    sec = 3.5
    n = int(SR * sec)
    t = np.arange(n) / SR
    boom = np.sin(2 * np.pi * (30 + 60 * np.exp(-t / 0.08)) * t) * np.exp(-t / 0.9)
    burst = butter(rng.standard_normal(n), "low", 6000) * np.exp(-t / 0.15)
    hi = np.zeros(n)
    for i in range(9):
        f = 1100 * 2 ** (i / 12) * (1 + rng.uniform(-0.01, 0.01))
        fall = 1 - 0.35 * (t / sec) ** 1.3                            # 비명이 아래로 무너짐
        hi += np.sign(np.sin(np.cumsum(2 * np.pi * f * fall / SR))) * 0.5 + np.sin(np.cumsum(2 * np.pi * f * 2.01 * fall / SR)) * 0.3
    hi = butter(hi, "low", 7000) / 9 * np.exp(-t / 1.1)
    y = np.tanh((boom * 1.4 + burst * 0.8 + hi * 1.6) * 1.8)
    return np.stack([y, y * 0.97], axis=1)


def spectral_blur(x, smooth_sec, rng, phase_rand=1.0):
    """소리를 구름처럼 번지게: 시간 방향으로 스펙트럼을 뭉개고 위상을 흩뜨림 → 가사 발음·박자감이 사라지고 웅웅 번지는 덩어리만 남음"""
    from scipy.ndimage import uniform_filter1d
    nfft, hop = 4096, 1024
    size = max(1, int(smooth_sec * SR / hop))
    out = np.zeros_like(x)
    for c in range(x.shape[1]):
        _, _, Z = signal.stft(x[:, c], fs=SR, nperseg=nfft, noverlap=nfft - hop)
        mag = uniform_filter1d(np.abs(Z), size, axis=1, mode="nearest")
        ph = np.angle(Z) + phase_rand * rng.uniform(-np.pi, np.pi, Z.shape)
        _, y = signal.istft(mag * np.exp(1j * ph), fs=SR, nperseg=nfft, noverlap=nfft - hop)
        m = min(len(y), len(x))
        out[:m, c] = y[:m]
    return out


def smear_bed(x, n, rng):
    """곡 전체를 n 길이(약 반 속도 = 한 옥타브 아래)로 늘리면서 수시로 테이프가 죽듯 음이 축 처지게 하고,
    구름처럼 번지게 만든 바닥 층. v5 의 28초에서 뭉개지던 그 느낌을 처음부터 끝까지 깔기 위한 것."""
    t = np.arange(n) / SR
    sp = 1 - 0.4 * (0.5 + 0.5 * np.sin(2 * np.pi * t / 7.3)) ** 4      # 7초마다 크게 처짐
    c = rng.uniform(2, 6)
    while c < t[-1]:                                                  # 사이사이 불규칙하게 또 처짐
        sp *= 1 - rng.uniform(0.3, 0.6) * np.exp(-0.5 * ((t - c) / rng.uniform(0.6, 1.5)) ** 2)
        c += rng.uniform(3, 8)
    sp *= (len(x) - 2) / sp.sum()
    bed = vari_speed(x, sp)
    if len(bed) < n:
        bed = np.concatenate([bed, np.zeros((n - len(bed), 2))])
    bed = spectral_blur(bed[:n], 0.7, rng, 1.0)
    bed = butter(bed, "low", 1600)
    bed = bed + pitch_shift(bed, -12) * 0.6 + pitch_shift(bed, +1) * 0.25
    bed = bed * 0.6 + reverb(bed, rng, sec=6.0) * 0.7
    return bed / max(1e-9, np.sqrt((bed ** 2).mean()))


def make_v5(x, rng, v6=False):
    """공포판. 구성 (대략):
      0~24초   아주 멀리 물속에서 들리듯 첫 소절이 반 속도·한 옥타브 아래로, 저음 웅웅거림과 쇠 긁는 소리가 스며듦
      24~95초  노래가 0.6배 속도로, 반음 위·3온음 아래·옥타브 아래와 부딪히며 어둡게. 수시로 테이프가 죽어 가고
               음악이 빠진 자리에 속삭임. 후반부터 심장 박동이 점점 빨라짐
      95~113초 노래가 죽어 가며 느려지고, 현악기 덩어리와 쇠 비명이 위로 끌려 올라가며 조여 옴 → 뚝 끊김
      정적 1.4초 → 깜짝 놀라는 한 방
      끝      텅 빈 곳에 첫 소절만 혼자, 점점 느려지다 멈춤. 속삭임과 웅웅거림만 남고 사라짐
    v6=True: 위 구성에 더해, 뭉개진 곡 전체를 처음부터 끝까지 바닥에 깔고(smear_bed),
             노래 층은 번지게 해서 가사가 또렷하게 안 들리게 하고 음량도 낮춤 (밝은 느낌 제거)"""
    out = []
    rng2 = np.random.default_rng(7)                                   # v6 전용 난수 (v5 결과가 안 바뀌게 따로)

    # ---- A: 멀리서
    a_src = x[: int(SR * 8.0)]
    a = vari_speed(a_src, np.full(int(SR * 17), 0.5))
    a = butter(dissonance(a, ((+1, 0.35), (-6, 0.3))), "low", 650)
    na = len(a)
    a *= (np.linspace(0, 1, na) ** 1.5)[:, None]
    a_mix = reverb(np.concatenate([a, np.zeros((int(SR * 6), 2))]), rng, sec=8.0)[:na + int(SR * 6)] * 1.2
    a_mix[:na] += a * 0.25
    out.append(a_mix * 1.6)

    # ---- B: 망가진 노래
    b_src = x[int(SR * 8.0): int(SR * 50.0)]
    nb_est = int(len(b_src) / 0.6 * 1.3)
    tb = np.arange(nb_est) / SR
    sp = 0.6 * (1 + 0.012 * np.sin(2 * np.pi * 0.37 * tb) + 0.01 * smooth_noise(nb_est, 1.5, rng))
    for c in rng.uniform(4, tb[-1] * 0.75, 9):                        # 테이프가 죽어 가는 순간들
        sp *= 1 - rng.uniform(0.35, 0.7) * np.exp(-0.5 * ((tb - c) / rng.uniform(0.5, 1.2)) ** 2)
    b = vari_speed(b_src, sp)
    b = dissonance(b)
    if v6:
        b = spectral_blur(b, 0.22, rng2, 0.8)                         # 가사 발음을 뭉갬
    nbb = len(b)
    deg = np.linspace(0.5, 1.0, nbb)
    b = speaker(b, deg, rng)
    b = butter(b, "low", 1500)                                        # 밝은 기운을 걷어냄
    g = holes(nbb, rng, count=5)
    b *= g[:, None]
    b_wet = reverb(echoes(b, delays=(0.43, 1.1), gains=(0.35, 0.2)), rng, sec=6.0)
    b_mix = b * 0.3 + b_wet * 0.55 if v6 else b * 0.55 + b_wet * 0.8
    wh = whispers(nbb, rng, density=0.6) * 0.5
    b_mix += wh * (0.3 + 0.7 * (1 - g))[:, None]                      # 음악이 빠진 자리에 속삭임이 커짐
    hb_start = int(nbb * 0.45)
    b_mix[hb_start:] += heartbeat(nbb - hb_start, 58, 96, 0.5)
    out.append(b_mix)

    # ---- C: 조여 옴 (노래가 죽어 가며 느려지고 군집·비명이 올라감)
    c_src = x[int(SR * 50.0): int(SR * 60.0)]
    nc = int(SR * 18)
    sp = np.linspace(0.6, 0.28, nc)
    c = vari_speed(c_src, sp)
    c = butter(dissonance(c), "low", 1200)
    if v6:
        c = spectral_blur(c, 0.3, rng2, 0.8)
    nc = len(c)
    c_mix = reverb(c, rng, sec=5.0) * 0.8 + c * 0.4
    c_mix += cluster(nc, rng) * 0.9
    c_mix += metal_shriek(nc, rng, swell=False) * (np.linspace(0, 1, nc) ** 2)[:, None] * 0.7
    c_mix += heartbeat(nc, 96, 150, 0.8)
    out.append(c_mix)

    # ---- 정적 → 한 방
    out.append(np.zeros((int(SR * 1.4), 2)))
    st = stinger(rng)
    st = st + reverb(np.concatenate([st, np.zeros((int(SR * 3), 2))]), rng, sec=4.0)[: len(st)] * 0.6
    body_peak = max(np.abs(o).max() for o in out)
    out.append(st * (2.6 * body_peak / np.abs(st).max()))            # 앞 구간보다 확 크게 (약 +8dB)

    # ---- D: 혼자 남은 첫 소절
    d_src = x[int(SR * 0.3): int(SR * 7.5)]
    nd = int(SR * 22)
    sp = 0.5 * np.linspace(1.0, 0.0, nd) ** 0.7 + 0.001
    d = vari_speed(d_src, sp)
    d = butter(dissonance(d, ((+1, 0.25), (-6, 0.2))), "low", 900)
    if v6:
        d = spectral_blur(d, 0.15, rng2, 0.6)
    d *= np.sin(np.pi * np.linspace(0, 1, len(d)) * 0.5 + np.pi / 2)[:, None] ** 0.5
    pad = np.zeros((int(SR * 8), 2))
    d = np.concatenate([d, pad])
    d_mix = reverb(d, rng, sec=8.0) * 1.1 + d * 0.3
    d_mix += whispers(len(d_mix), rng, density=0.5) * 0.35 * np.linspace(1, 0, len(d_mix))[:, None]
    out.append(d_mix)

    mix = np.concatenate(out)
    n = len(mix)
    # 처음부터 끝까지 깔리는 것: 웅웅거리는 저음, 가끔 쇠 긁는 소리, 테이프 잡음
    dr = sub_drone(n, rng) * 0.09
    stinger_at = sum(len(o) for o in out[:4])
    dr[stinger_at - int(SR * 1.4): stinger_at] = 0                    # 정적은 진짜 정적
    dr[-int(SR * 6):] *= np.linspace(1, 0, int(SR * 6))[:, None]
    mix += dr
    for at in rng.uniform(5, (stinger_at / SR) - 25, 4):
        ln = int(SR * rng.uniform(5, 9))
        k = int(at * SR)
        mix[k:k + ln] += metal_shriek(ln, rng) * 0.22
    bed = noise_bed(n, np.full(n, 0.8), rng) * 0.8
    bed[stinger_at - int(SR * 1.4): stinger_at] = 0
    mix += bed
    if v6:
        sm = smear_bed(x, n, rng2)
        b_rms = np.sqrt((out[1] ** 2).mean())
        env = np.ones(n)
        k = int(SR * 20)
        env[:k] = np.linspace(0, 1, k) ** 1.5                         # 멀리서 시작
        sil = stinger_at - int(SR * 1.4)
        env[sil - int(SR * 0.3): stinger_at + len(out[4])] = 0        # 정적·한 방 동안은 없음
        env[sil - int(SR * 0.3): sil] = np.linspace(1, 0, int(SR * 0.3))
        back = stinger_at + len(out[4])
        env[back: back + int(SR * 3)] = np.linspace(0, 1, int(SR * 3))
        env[-int(SR * 8):] *= np.linspace(1, 0, int(SR * 8))
        mix += sm * env[:, None] * b_rms * 1.1
    return mix


def main():
    argv = sys.argv[1:]
    opts = {}
    files = []
    i = 0
    while i < len(argv):
        if argv[i].startswith("--"):
            opts[argv[i][2:]] = argv[i + 1]
            i += 2
        else:
            files.append(argv[i])
            i += 1
    seed = int(opts.get("seed", 1120))
    preset = opts.get("preset", "v1")
    src, dst = files[0], files[1]
    rng = np.random.default_rng(seed)

    x = load(src)
    x = x / max(1e-9, np.abs(x).max())
    if preset == "v6":
        mix = make_v5(x, rng, v6=True)
    elif preset == "v5":
        mix = make_v5(x, rng)
    elif preset == "v4":
        mix = make_v2(x, rng, male=True, male_semitones=-7.0, base=0.66, smear=(19.8, 23.7, 1.6))
    elif preset == "v3":
        mix = make_v2(x, rng, male=True)
    elif preset == "v2":
        mix = make_v2(x, rng)
    else:
        mix = make_v1(x, rng)
    mix *= 0.89 / np.abs(mix).max()
    save(dst, mix)
    if "events" in opts:
        for st, ln, what in sorted(EVENTS):
            shift = sum(a for at, a in INSERTS if at <= st)           # 판 튐 자신은 이미 삽입 전 위치
            print(f"  {st + shift:6.1f}s ~ {st + shift + ln:6.1f}s  {what}")
        for st, ln, what in sorted(FINAL_EVENTS):
            print(f"  {st:6.1f}s ~ {st + ln:6.1f}s  {what}")
    print(f"{preset}: {dst}: {len(mix) / SR:.1f}s (원본 {len(x) / SR:.1f}s)")


if __name__ == "__main__":
    main()
