"""롯데월드 테마송 → 공포 게임용 트랙 (v7). 원곡을 비트는 대신 '다시 연주'한다.

실행:  python -I Tools/MakeHorrorTheme.py <원곡.wav> <출력.wav> [--version 8|7] [--seed 62] [--notes 1]
       --version : 8 (기본, 더 어둡고 무전기·글리치 많음) / 7 (첫 미리 듣기판)
       --notes 1 : 뽑아낸 멜로디 음 목록을 출력

목표 (요청 문구 요약): 밝은 놀이공원 행진곡을 62 BPM 의 소름 끼치는 공포 트랙으로.
  - 멜로디: 원곡에서 뽑은 음을 음이 어긋난 낡은 오르골과 썩어 가는 금관으로 느리게 연주. 음정이 떠다니고 테이프가 출렁임
  - 가끔 0.5초 정적, 갑작스러운 글리치(판 튐·끊김·디지털 깨짐)
  - 밑에: 웅웅거리는 초저음, 녹슨 쇠 긁는 소리, 희미한 웃음소리
  - 노래: 깨끗한 노래 대신 고장 난 무전기/낡은 구내방송 스피커 — 잡음, 스켈치(무전 '치익'), 좁은 대역, 비트 깨짐,
          꾸르륵거리는 알아들을 수 없는 사람 소리. 밤의 폐장 놀이공원에서 고장 난 인형이 말하는 느낌
원곡 126 BPM → 62 BPM 이라 원곡 시간을 126/62 배로 늘려 배치한다.
"""
import os
import sys

import numpy as np
from scipy import signal

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import MakeCreepyTheme as C  # noqa: E402  (load/save/butter/reverb/pitch_shift/metal_shriek/sub_drone ...)

SR = C.SR
SRC_BPM, BPM = 126.0, 62.0
STRETCH = SRC_BPM / BPM            # 원곡 1초 → 출력 2.03초
BEAT = 60.0 / BPM


# ================================================================== 멜로디 뽑기
def viterbi_track(sal, jump_cost=0.06):
    """프레임마다 후보 중 하나를 고르되, 크게 뛰면 감점 → 베이스·반주로 튀지 않고 멜로디 선을 따라감"""
    nc, nt = sal.shape
    obs = np.log(sal / (sal.max(axis=0, keepdims=True) + 1e-12) + 1e-6)
    d = np.abs(np.arange(nc)[:, None] - np.arange(nc)[None, :]) / 5.0  # 반음 단위 거리
    trans = -jump_cost * d - 2.0 * (d > 7)                            # 7반음 넘게 뛰면 크게 감점
    score = obs[:, 0].copy()
    back = np.zeros((nc, nt), dtype=np.int32)
    for i in range(1, nt):
        tot = score[None, :] + trans                                  # [지금, 이전]
        back[:, i] = tot.argmax(axis=1)
        score = tot.max(axis=1) + obs[:, i]
    path = np.zeros(nt, dtype=int)
    path[-1] = score.argmax()
    for i in range(nt - 1, 0, -1):
        path[i - 1] = back[path[i], i]
    return path


def extract_melody(x, fmin=260.0, fmax=1100.0):
    """프레임마다 '배음 합'이 가장 큰 기본음을 고르고, 같은 음이 이어지는 구간을 음표로 묶음.
    반환: [(시작초, 길이초, MIDI 음높이, 세기 0~1)] — 원곡 시간 기준"""
    mono = x.mean(axis=1)
    mono = C.butter(mono, "high", 120)
    nfft, hop = 4096, 512
    f, t, Z = signal.stft(mono, fs=SR, nperseg=nfft, noverlap=nfft - hop)
    mag = np.abs(Z) ** 0.6                                           # 큰 소리가 다 먹지 않게 눌러 줌
    cands = 440.0 * 2 ** ((np.arange(36 * 5, 84 * 5) / 5.0 - 69) / 12)  # MIDI 36~84, 1/5 반음 간격
    cands = cands[(cands >= fmin) & (cands <= fmax)]
    sal = np.zeros((len(cands), mag.shape[1]))
    binhz = SR / nfft
    for h in range(1, 7):
        idx = cands * h / binhz
        lo = np.floor(idx).astype(int)
        a = (idx - lo)[:, None]
        ok = lo + 1 < mag.shape[0]
        v = np.zeros_like(sal)
        v[ok] = (1 - a[ok]) * mag[lo[ok]] + a[ok] * mag[lo[ok] + 1]
        sal += v * 0.8 ** (h - 1)
    best = viterbi_track(sal)
    f0 = cands[best]
    strength = sal[best, np.arange(sal.shape[1])]
    strength = strength / np.percentile(strength, 98)
    voiced = strength > 0.35
    midi = 69 + 12 * np.log2(f0 / 440.0)
    midi = signal.medfilt(midi, 7)
    q = np.round(midi).astype(int)
    notes = []
    fs = hop / SR
    i = 0
    while i < len(q):
        if not voiced[i]:
            i += 1
            continue
        j = i
        while j + 1 < len(q) and voiced[j + 1] and abs(q[j + 1] - q[i]) == 0:
            j += 1
        dur = (j - i + 1) * fs
        if dur >= 0.09:
            notes.append((t[i], dur, int(q[i]), float(np.clip(strength[i:j + 1].mean(), 0, 1))))
        i = j + 1
    # 너무 짧은 틈으로 쪼개진 같은 음은 이어 붙임
    merged = []
    for n in notes:
        if merged and n[2] == merged[-1][2] and n[0] - (merged[-1][0] + merged[-1][1]) < 0.06:
            s, d, m, v = merged[-1]
            merged[-1] = (s, n[0] + n[1] - s, m, max(v, n[3]))
        else:
            merged.append(n)
    return merged


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


# ================================================================== 장조 → 단조
MAJOR_PROFILE = np.array([6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88])


def detect_key(notes):
    """음 길이로 가중한 음이름 분포를 장조 표준 분포와 맞춰 으뜸음을 찾음"""
    hist = np.zeros(12)
    for s, d, m, v in notes:
        hist[m % 12] += d
    scores = [np.corrcoef(np.roll(MAJOR_PROFILE, k), hist)[0, 1] for k in range(12)]
    return int(np.argmax(scores))


def to_minor(notes, tonic, phrygian=False):
    """같은 으뜸음의 단조로: 장3도→단3도, 장6도→단6도, 장7도→단7도 (밝은 느낌을 만드는 음들만 반음 내림)
    phrygian: 2도까지 반음 내림 → 으뜸음 바로 위에 반음이 붙어 훨씬 어둡고 불길한 소리 (공포 영화에서 흔한 음계)"""
    low = (2, 4, 9, 11) if phrygian else (4, 9, 11)
    out = []
    for s, d, m, v in notes:
        rel = (m - tonic) % 12
        out.append((s, d, m - (1 if rel in low else 0), v))
    return out


# ================================================================== 악기
def music_box_tone(freq, sec, rng, bend_cents=0.0):
    """오르골 빗살 하나: 기음 + 어긋난 배음(빨리 사라짐) + 튕기는 소리. 녹슬어 살짝 떨림"""
    n = int(SR * sec)
    t = np.arange(n) / SR
    f = freq * 2 ** ((bend_cents * t / sec + 6 * np.sin(2 * np.pi * rng.uniform(4, 7) * t) * np.exp(-t / 0.4)) / 1200)
    ph = 2 * np.pi * np.cumsum(f) / SR
    y = np.sin(ph) * np.exp(-t / 1.3)
    y += 0.32 * np.sin(ph * 2.76) * np.exp(-t / 0.3)
    y += 0.16 * np.sin(ph * 5.40) * np.exp(-t / 0.09)
    y += 0.08 * np.sin(ph * 8.93) * np.exp(-t / 0.035)
    k = int(SR * 0.004)
    y[:k] += rng.standard_normal(k) * np.linspace(0.35, 0, k)
    return y


def brass_tone(freq, sec, rng):
    """썩어 가는 금관: 톱니파 둘을 어긋나게 겹치고, 숨소리, 느린 소리 열림, 끝에서 음이 처지며 꺼짐"""
    n = int(SR * (sec + 0.6))
    t = np.arange(n) / SR
    sag = np.where(t > sec * 0.6, -(t - sec * 0.6) / (sec * 0.4 + 0.6) * rng.uniform(60, 160), 0.0)  # 센트
    wob = 9 * np.sin(2 * np.pi * rng.uniform(4.5, 5.5) * t) + 12 * C.smooth_noise(n, 2.0, rng)
    y = np.zeros(n)
    for det in (-9, +7):
        f = freq * 2 ** ((sag + wob + det) / 1200)
        ph = np.cumsum(f) / SR
        y += 2 * (ph % 1.0) - 1                                       # 톱니파
    env = np.clip(t / 0.18, 0, 1) * np.where(t > sec, np.exp(-(t - sec) / 0.25), 1.0)
    b = np.clip(t / 0.3, 0, 1) * np.exp(-t / (sec + 0.5))              # 불기 시작하면 밝아졌다가 어두워짐
    raw = y * env
    y = C.butter(raw, "low", 800) * (1 - b) + C.butter(raw, "low", 2200) * b
    y += C.butter(rng.standard_normal(n), "band", [600, 2500]) * env * 0.05  # 숨 새는 소리
    return y


def render_notes(notes, n, rng, kind, transpose=0, min_dur=0.0, keep=1.0, detune_cents=28):
    """음표 목록(출력 시간 기준)을 악기로 연주. 건반마다 고정된 음 어긋남 + 곡 전체에 떠다니는 음정"""
    y = np.zeros(n)
    detune = {}
    for s, d, m, v in notes:
        if d < min_dur or rng.random() > keep:
            continue
        mm = m + transpose
        cents = detune.setdefault(mm, rng.uniform(-detune_cents, detune_cents))
        f = hz(mm) * 2 ** (cents / 1200)
        if kind == "box":
            tone = music_box_tone(f, min(3.0, d + 1.8), rng, bend_cents=rng.uniform(-35 - max(0, detune_cents - 28), 5))
            amp = 0.55 + 0.45 * v
        else:
            tone = brass_tone(f, d, rng)
            amp = 0.35 + 0.4 * v
        k = int(s * SR)
        if k >= n:
            continue
        e = min(n, k + len(tone))
        y[k:e] += tone[: e - k] * amp
    return y


# ================================================================== 무전기 목소리
def bitcrush(x, rate=7000, bits=5):
    """샘플을 rate 로 듬성듬성 잡고(계단) 높이를 2^bits 단계로 깎음 → 싸구려 디지털 깨짐"""
    hold = max(1, int(SR / rate))
    idx = (np.arange(len(x)) // hold) * hold
    y = x[idx]
    q = 2 ** (bits - 1)
    return np.round(y * q) / q


def squelch(rng, loud=1.0):
    """무전 송신 시작·끝의 '치익-' (잡음 폭발 + 딸깍)"""
    n = int(SR * rng.uniform(0.09, 0.2))
    t = np.arange(n) / SR
    y = C.butter(rng.standard_normal(n), "band", [900, 4200]) * np.exp(-t / (t[-1] * 0.5)) * 1.4
    k = int(SR * 0.003)
    y[:k] += np.linspace(1, 0, k)
    return np.tanh(y * 2) * loud


def radio_voice(x, n, rng, P=None):
    """원곡(=노래)을 62 BPM 으로 늘린 뒤, 고장 난 무전기로: 꾸르륵 떨림 · 알갱이 뒤섞기 · 좁은 대역 · 비트 깨짐 · 잡음.
    켜졌다 꺼졌다 하는 송신 단위로 나오고, 송신마다 스켈치. 반환: (소리, 송신 구간 목록)"""
    P = P or {}
    t = np.arange(n) / SR
    if P.get("pitch"):
        x = C.pitch_shift(x, P["pitch"])                              # 목소리 음높이를 내림 (포먼트까지 같이 → 괴물 같음)
    gd = P.get("gurgle", 0.045)
    # 꾸르륵: 빠르고 깊은 음 떨림 (물속에서 말하는 듯)
    gurgle = 1 + gd * np.sin(2 * np.pi * (9 + 4 * C.smooth_noise(n, 0.5, rng)) * t) + 0.02 * C.smooth_noise(n, 3, rng)
    sp = gurgle / STRETCH
    pos = np.cumsum(sp)
    pos = np.minimum(pos, len(x) - 2)
    idx = np.arange(len(x))
    v = np.stack([np.interp(pos, idx, x[:, c]) for c in range(2)], axis=1).mean(axis=1)
    # 알갱이 뒤섞기: 0.06~0.14초 조각 일부를 거꾸로 / 반복 / 음 튐 → 알아들을 수 없는 웅얼거림
    out = v.copy()
    k = 0
    while k < n:
        g = int(SR * rng.uniform(0.06, 0.14))
        if k + g >= n:
            break
        r = rng.random()
        seg = v[k:k + g]
        pr, pp, ps = P.get("scramble", (0.22, 0.12, 0.10))
        if r < pr:
            out[k:k + g] = seg[::-1]
        elif r < pr + pp and k > g:
            out[k:k + g] = v[k - g:k]                                 # 앞 조각 다시
        elif r < pr + pp + ps:
            ii = np.arange(0, g - 1, rng.choice([0.7, 1.4]))
            sh = np.interp(ii, np.arange(g), seg)
            out[k:k + g] = np.resize(sh, g)
        fade = int(SR * 0.004)
        out[k:k + fade] *= np.linspace(0, 1, fade)
        k += g
    # 낡은 스피커 대역 + 링 변조 조금 + 비트 깨짐 + 찌그러짐
    y = C.butter(out, "band", [450, 2600], order=4)
    y = y * (0.8 + 0.2 * np.sin(2 * np.pi * 47 * t))
    y = bitcrush(y / (np.abs(y).max() + 1e-9), rate=6500, bits=5)
    y = np.tanh(y * 3.0)
    y = C.butter(y, "band", [400, 3200], order=2)
    # 송신 구간: 켜졌다 꺼졌다
    gate = np.zeros(n)
    spans = []
    sp = P.get("spans", (16, 22, 2.5, 7.0, 2.0, 6.5))
    a = rng.uniform(sp[0], sp[1])
    while a < t[-1] - 8:
        ln = rng.uniform(sp[2], sp[3])
        spans.append((a, a + ln))
        a += ln + rng.uniform(sp[4], sp[5])
    for s0, s1 in spans:
        i0, i1 = int(s0 * SR), int(s1 * SR)
        gate[i0:i1] = 1
        # 송신 중에도 가끔 뚝뚝 끊김
        for _ in range(int(rng.integers(0, 4))):
            c = int(rng.uniform(s0 + 0.3, s1 - 0.3) * SR)
            gate[c:c + int(SR * rng.uniform(0.05, 0.25))] = 0
    gate = C.butter(gate, "low", 200, order=1)
    static = C.butter(rng.standard_normal(n), "band", [500, 5000]) * 0.18
    crackle = (rng.random(n) < 0.002) * rng.standard_normal(n) * 0.8
    voice = (y * 0.9 + static + C.butter(crackle, "high", 1500)) * gate
    voice += static * 0.12                                            # 꺼져 있을 때도 아주 작은 지지직
    for s0, s1 in spans:
        for at in (s0, s1):
            sq = squelch(rng)
            i = int(at * SR)
            e = min(n, i + len(sq))
            voice[i:e] += sq[: e - i] * 0.6
    return voice, spans


# ================================================================== 웃음 · 북 · 바닥
def laugh(rng, syllables=None):
    """'하 하 하' 아이 같은 웃음: 성대 떨림(톱니) → '아' 모음 울림대, 음이 점점 내려가며 숨이 섞임"""
    syl = int(syllables or rng.integers(4, 8))
    f0 = rng.uniform(290, 380)
    parts = []
    for i in range(syl):
        on = int(SR * rng.uniform(0.09, 0.14))
        off = int(SR * rng.uniform(0.05, 0.1))
        t = np.arange(on) / SR
        f = f0 * (1 - 0.06 * i) * (1 + 0.08 * np.exp(-t / 0.03))
        ph = np.cumsum(f) / SR
        src = 2 * (ph % 1.0) - 1 + rng.standard_normal(on) * 0.5     # 숨 섞인 소리
        v = C.resonator(src, 780, 5) * 1.0 + C.resonator(src, 1250, 7) * 0.7 + C.resonator(src, 2700, 9) * 0.35
        v *= np.sin(np.pi * np.linspace(0, 1, on)) ** 0.8
        parts += [v, np.zeros(off)]
    y = np.concatenate(parts)
    return y / (np.abs(y).max() + 1e-9)


def march_drum(rng):
    """멀리서 울리는 행진 북 (먹먹하게)"""
    n = int(SR * 0.9)
    t = np.arange(n) / SR
    body = np.sin(2 * np.pi * (58 + 40 * np.exp(-t / 0.03)) * t) * np.exp(-t / 0.28)
    skin = C.butter(rng.standard_normal(n), "band", [150, 900]) * np.exp(-t / 0.06) * 0.5
    return body + skin


def tape_wobble(y, rng, depth=0.012):
    """테이프 출렁임: 재생 속도가 느리게 흔들림 (음정이 떠다님)"""
    n = len(y)
    t = np.arange(n) / SR
    sp = 1 + depth * np.sin(2 * np.pi * 0.31 * t) + 0.6 * depth * C.smooth_noise(n, 0.9, rng)
    pos = np.cumsum(sp)
    pos = np.minimum(pos, n - 2)
    idx = np.arange(n)
    if y.ndim == 1:
        return np.interp(pos, idx, y)
    return np.stack([np.interp(pos, idx, y[:, c]) for c in range(y.shape[1])], axis=1)


# ================================================================== 글리치
def glitches(mix, rng, start, end, counts=(6, 5, 4, 3, 0)):
    """62 BPM 박자 위에: 0.5초 정적 · 판 튐(짧은 조각 반복) · 테이프 멈칫 · 디지털 깨짐 · 긴 끊김(1.5~2.3초, 지지직만)
    counts = (정적, 판 튐, 테이프 멈칫, 깨짐, 긴 끊김) 개수"""
    y = mix.copy()
    beats = np.arange(start, end, BEAT)
    kinds = ["silence"] * counts[0] + ["stutter"] * counts[1] + ["tapestop"] * counts[2] + ["crush"] * counts[3]         + ["dropout"] * counts[4]
    picks = rng.choice(len(beats), size=min(len(beats), len(kinds)), replace=False)
    rng.shuffle(kinds)
    log = []
    for b, kind in zip(sorted(picks), kinds):
        i = int(beats[b] * SR)
        if kind == "dropout":
            ln = int(SR * rng.uniform(1.5, 2.3))
            k = int(SR * 0.004)
            e = min(len(y), i + ln)
            hiss = C.butter(rng.standard_normal(e - i), "band", [800, 4000]) * 0.015
            y[i:e] = hiss[:, None]                                    # 스피커가 죽은 듯: 아주 작은 지지직만
            y[i - k:i] *= np.linspace(1, 0, k)[:, None]
        elif kind == "silence":
            ln = int(SR * 0.5)
            k = int(SR * 0.004)
            y[i:i + ln] = 0
            y[i - k:i] *= np.linspace(1, 0, k)[:, None]
        elif kind == "stutter":
            g = int(SR * BEAT / rng.choice([4, 8]))
            reps = int(rng.integers(4, 9))
            frag = mix[i:i + g].copy()
            for r in range(reps):
                j = i + r * g
                if j + g < len(y):
                    y[j:j + g] = frag * (1 - 0.08 * r)
            ln = g * reps
        elif kind == "tapestop":
            ln = int(SR * 0.45)
            seg = mix[i:i + ln * 2]
            sp = np.linspace(1, 0.05, ln) ** 1.5
            pos = np.cumsum(sp)
            idx = np.arange(len(seg))
            y[i:i + ln] = np.stack([np.interp(pos, idx, seg[:, c]) for c in range(2)], axis=1)
        else:
            ln = int(SR * rng.uniform(0.2, 0.5))
            seg = y[i:i + ln]
            y[i:i + ln] = np.stack([bitcrush(seg[:, c] / (np.abs(seg).max() + 1e-9), 2500, 3) for c in range(2)], axis=1) * np.abs(seg).max()
        log.append((beats[b], kind, ln / SR))
    return y, log


# ================================================================== 배치
V7 = dict(phrygian=False, box_tr=12, box_detune=28, box_lp=3500, ghost=0.0, pad=0.0, brass_at=35, brass=0.35,
          music_lp=4500, winddown=0.0, voice=0.55, drone=0.1,
          radio=dict(), glitch=(6, 5, 4, 3, 0))
# v8: 더 어둡게 (2도까지 내린 음계, 오르골을 한 옥타브 낮추고 더 어긋나게, 3온음 아래 유령 오르골, 낮은 불협 화음 깔개,
#     금관 일찍, 전체를 더 먹먹하게, 끝에서 태엽 풀림) · 무전기 더 많이·크게·낮게·더 꾸르륵 · 글리치 더 많이 + 긴 끊김
V8 = dict(phrygian=True, box_tr=0, box_detune=45, box_lp=2600, ghost=0.32, pad=0.12, brass_at=22, brass=0.5,
          music_lp=3000, winddown=0.55, voice=1.15, drone=0.16,
          radio=dict(pitch=-4, gurgle=0.08, scramble=(0.3, 0.15, 0.15), spans=(8, 11, 4.0, 10.0, 1.0, 3.5)),
          glitch=(9, 8, 6, 5, 3))


def dark_pad(n, rng, tonic_hz):
    """낮은 불협 화음 깔개: 으뜸음 · 반음 위 · 3온음 (서로 부딪혀 울렁거림). 천천히 커졌다 작아졌다"""
    t = np.arange(n) / SR
    y = np.zeros((n, 2))
    for semi, g in ((0, 1.0), (1, 0.55), (6, 0.6), (-12, 0.8)):
        for det in (-7, 6):
            f = tonic_hz * 2 ** ((semi * 100 + det) / 1200)
            ph = np.cumsum(f * (1 + 0.003 * C.smooth_noise(n, 0.2, rng))) / SR
            saw = 2 * (ph % 1.0) - 1
            pan = rng.uniform(0.2, 0.8)
            y[:, 0] += saw * g * (1 - pan)
            y[:, 1] += saw * g * pan
    y = C.butter(y, "low", 420)
    swell = 0.55 + 0.45 * C.smooth_noise(n, 0.07, rng)
    return y * swell[:, None] / 6


def wind_down(y, start, end, amount):
    """태엽이 풀리는 오르골: start~end 사이 재생 속도를 1 → 1-amount 로 떨어뜨림 (음이 처지며 느려짐)"""
    n = len(y)
    t = np.arange(n) / SR
    sp = 1 - amount * np.clip((t - start) / (end - start), 0, 1) ** 1.6
    pos = np.minimum(np.cumsum(sp), n - 2)
    idx = np.arange(n)
    return np.stack([np.interp(pos, idx, y[:, c]) for c in range(y.shape[1])], axis=1)


def make(x, rng, show_notes=False, P=V7):
    x = x / max(1e-9, np.abs(x).max())
    src_len = len(x) / SR
    n = int(SR * (src_len * STRETCH + 10))                            # 끝 잔향 10초
    notes = extract_melody(x)
    tonic = detect_key(notes)
    minor = to_minor(notes, tonic, phrygian=P["phrygian"])
    names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]
    print(f"멜로디 음 {len(notes)}개, 조: {names[tonic]} 장조 → {names[tonic]} {'프리지아(2도도 내린) ' if P['phrygian'] else ''}단조로 바꿈")
    if show_notes:
        for s0, d, m, v in minor[:60]:
            print(f"  {s0 * STRETCH:6.2f}s  {d * STRETCH:4.2f}s  MIDI {m}")
    sched = [(s0 * STRETCH, d * STRETCH, m, v) for s0, d, m, v in minor]
    t = np.arange(n) / SR
    end_music = src_len * STRETCH

    # 오르골: 한 옥타브 위 (작은 오르골), 처음엔 멀리서
    box = render_notes(sched, n, rng, "box", transpose=P["box_tr"], keep=0.92, detune_cents=P["box_detune"])
    if P["ghost"]:                                                    # 3온음 아래에서 따라오는 유령 오르골 (반 박 늦게)
        late = [(s0 + BEAT * 0.5, d, m, v) for s0, d, m, v in sched]
        box += render_notes(late, n, rng, "box", transpose=P["box_tr"] - 6, keep=0.6, detune_cents=P["box_detune"]) * P["ghost"]
    box_env = np.clip((t - 4) / 14, 0, 1) ** 1.5
    box_env *= np.where(t > end_music - 14, np.clip((end_music - t) / 14, 0, 1), 1)
    box = C.butter(box, "low", P["box_lp"]) * box_env
    # 오르골 태엽이 풀리듯 마지막 14초는 느려짐
    # 금관: 긴 음만, 한 옥타브 아래, 35초부터 서서히
    brass = render_notes(sched, n, rng, "brass", transpose=-12, min_dur=0.35, keep=0.85)
    brass *= np.clip((t - P["brass_at"]) / 25, 0, 1) * np.where(t > end_music - 6, np.clip((end_music - t) / 6, 0, 1), 1)
    # 행진 북: 62 BPM 박마다 (1·3박 크게), 25초부터
    drum = np.zeros(n)
    dr = march_drum(rng)
    for i, bt in enumerate(np.arange(25, end_music - 4, BEAT)):
        k = int(bt * SR)
        if k + len(dr) < n:
            drum[k:k + len(dr)] += dr * (1.0 if i % 2 == 0 else 0.45)
    drum = C.butter(drum, "low", 600) * 0.5

    bg = P["brass"] / 0.35
    music = np.stack([box * 0.8 + brass * 0.35 * bg + drum * 0.9, box * 0.7 + brass * 0.4 * bg + drum * 0.9], axis=1)
    if P["pad"]:
        pad = dark_pad(n, rng, hz(tonic + 36))                        # 으뜸음 낮은 옥타브 (G 면 98Hz)
        pad *= (np.clip((t - 2) / 20, 0, 1) * np.where(t > end_music, np.clip(1 - (t - end_music) / 8, 0, 1), 1))[:, None]
        music += pad * P["pad"] / 0.12 * 0.6
    music = tape_wobble(music, rng, depth=0.012 if not P["phrygian"] else 0.018)
    if P["winddown"]:
        music = wind_down(music, end_music - 16, end_music + 4, P["winddown"])
    music = music * 0.55 + C.reverb(music, rng, sec=5.0) * 0.6
    music = C.butter(music, "low", P["music_lp"])

    # 무전기 목소리
    voice, spans = radio_voice(x, n, rng, P["radio"])
    voice = np.stack([voice * 0.9, voice * 0.75], axis=1)
    voice = voice + C.reverb(voice, rng, sec=2.5) * 0.25

    # 바닥: 웅웅 초저음, 녹슨 쇠 긁힘, 웃음
    drone = C.sub_drone(n, rng) * P["drone"]
    drone[-int(SR * 8):] *= np.linspace(1, 0, int(SR * 8))[:, None]
    scrape = np.zeros((n, 2))
    for at in list(rng.uniform(6, end_music - 12, 6)):
        ln = int(SR * rng.uniform(4, 8))
        k = int(at * SR)
        sh = C.metal_shriek(ln, rng)
        grit = C.butter(rng.standard_normal(ln), "band", [1800, 5000]) * (rng.random(ln) < 0.04) * 1.2
        sh[:, 0] += grit * 0.3
        sh[:, 1] += grit * 0.3
        scrape[k:k + ln] += sh[: n - k] * 0.16
    laughs = np.zeros((n, 2))
    for at in [11.0] + list(rng.uniform(40, end_music - 5, 3)) + [end_music + 1.5]:
        lg = laugh(rng)
        lg = C.butter(lg, "band", [300, 3000])
        lg = bitcrush(lg, 9000, 7) * 0.5 + lg * 0.5
        pan = rng.uniform(0.15, 0.85)
        k = int(at * SR)
        e = min(n, k + len(lg))
        laughs[k:e, 0] += lg[: e - k] * (1 - pan)
        laughs[k:e, 1] += lg[: e - k] * pan
    laughs = laughs * 0.25 + C.reverb(laughs, rng, sec=6.0) * 0.45      # 멀리서, 어딘가에서

    mix = music * 1.0 + voice * P["voice"] + drone + scrape + laughs * 0.6
    mix += C.noise_bed(n, np.full(n, 0.6), rng) * 0.6
    mix, glog = glitches(mix, rng, 20, end_music - 6, P["glitch"])
    fi = int(SR * 3)
    mix[:fi] *= np.linspace(0, 1, fi)[:, None]
    mix[-int(SR * 3):] *= np.linspace(1, 0, int(SR * 3))[:, None]
    print(f"무전 송신 {len(spans)}번, 글리치 {len(glog)}번 (" + ", ".join(f"{a:.1f}s {k}" for a, k, l in glog[:8]) + " ...)")
    return mix


def main():
    argv = sys.argv[1:]
    opts, files, i = {}, [], 0
    while i < len(argv):
        if argv[i].startswith("--"):
            opts[argv[i][2:]] = argv[i + 1]
            i += 2
        else:
            files.append(argv[i])
            i += 1
    rng = np.random.default_rng(int(opts.get("seed", 62)))
    x = C.load(files[0])
    mix = make(x, rng, show_notes="notes" in opts, P=V8 if opts.get("version", "8") == "8" else V7)
    mix *= 0.89 / np.abs(mix).max()
    C.save(files[1], mix)
    print(f"{files[1]}: {len(mix) / SR:.1f}s, {BPM:.0f} BPM")


if __name__ == "__main__":
    main()
