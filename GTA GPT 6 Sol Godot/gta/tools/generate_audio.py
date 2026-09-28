"""Generate small, original PCM sound effects for Harborline. No samples used."""

from __future__ import annotations

import math
import random
import struct
import wave
from pathlib import Path

RATE = 22050
OUT = Path(__file__).resolve().parents[1] / "generated" / "audio"
OUT.mkdir(parents=True, exist_ok=True)
rng = random.Random(9880)


def write(name: str, seconds: float, make_sample) -> None:
    count = int(seconds * RATE)
    data = bytearray()
    for i in range(count):
        t = i / RATE
        sample = max(-1.0, min(1.0, make_sample(t, i, count)))
        data.extend(struct.pack("<h", int(sample * 28000)))
    with wave.open(str(OUT / f"{name}.wav"), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(data)


def noise() -> float:
    return rng.uniform(-1.0, 1.0)


def shot(t: float, _i: int, _n: int) -> float:
    decay = math.exp(-t * 32.0)
    crack = noise() * math.exp(-t * 100.0)
    body = math.sin(2 * math.pi * (110.0 - 60.0 * t) * t)
    return 0.8 * crack + 0.52 * body * decay


def explosion(t: float, _i: int, _n: int) -> float:
    envelope = min(1.0, t * 80.0) * math.exp(-t * 3.2)
    rumble = math.sin(2 * math.pi * (48.0 - 18.0 * t) * t)
    return envelope * (0.50 * noise() + 0.42 * rumble)


def ambience(t: float, _i: int, _n: int) -> float:
    # A cyclic city/ocean bed with distant synthesized traffic, no recordings.
    fade = min(1.0, t * 2.0, (18.0 - t) * 2.0)
    sea = noise() * 0.09 * (0.5 + 0.5 * math.sin(t * 0.63) ** 2)
    traffic = math.sin(2 * math.pi * 62.0 * t + 4.0 * math.sin(t * 0.6)) * 0.025
    breeze = math.sin(2 * math.pi * 1.4 * t) * 0.026
    gull = math.sin(2 * math.pi * (620.0 + 160.0 * math.sin(t * 2.4)) * t) * 0.015 * max(0.0, math.sin(t * 1.1))
    return fade * (sea + traffic + breeze + gull)


def engine(t: float, _i: int, _n: int) -> float:
    freq = 74.0 + 9.0 * math.sin(t * 0.75)
    return 0.25 * math.sin(2 * math.pi * freq * t) + 0.12 * math.sin(2 * math.pi * freq * 2.0 * t) + 0.05 * noise()


def horn(t: float, _i: int, _n: int) -> float:
    envelope = min(1.0, t * 30.0, (0.55 - t) * 20.0)
    return envelope * (0.22 * math.sin(2 * math.pi * 392.0 * t) + 0.2 * math.sin(2 * math.pi * 494.0 * t))


def siren(t: float, _i: int, _n: int) -> float:
    freq = 560.0 + 210.0 * math.sin(t * 5.2)
    return 0.23 * math.sin(2 * math.pi * freq * t)


def rain(t: float, _i: int, _n: int) -> float:
    return 0.16 * noise() * (0.7 + 0.3 * math.sin(t * 5.0) ** 2)


def footstep(t: float, _i: int, _n: int) -> float:
    return 0.45 * noise() * math.exp(-t * 22.0) + 0.22 * math.sin(2 * math.pi * 74.0 * t) * math.exp(-t * 18.0)


for name, duration, function in [
    ("gunshot", 0.36, shot),
    ("explosion", 1.4, explosion),
    ("ambience", 18.0, ambience),
    ("engine", 3.0, engine),
    ("horn", 0.55, horn),
    ("siren", 3.0, siren),
    ("rain", 4.0, rain),
    ("footstep", 0.25, footstep),
]:
    write(name, duration, function)

print(f"Generated {len(list(OUT.glob('*.wav')))} original WAV files in {OUT}")
