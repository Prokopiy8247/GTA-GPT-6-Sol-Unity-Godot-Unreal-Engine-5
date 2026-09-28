"""Create original synthesized WAV effects and import them into Unreal.

Plain Python generates SourceAssets/Audio/*.wav. Running the same script via
UnrealEditor-Cmd's Python commandlet also imports SoundWaves to /Game/GTA/Audio.
All sound synthesis uses Python's standard library and deterministic seeds.
"""

import math
from pathlib import Path
import random
import struct
import wave

try:
    import unreal
except ImportError:
    unreal = None


ROOT = Path(unreal.Paths.project_dir()) if unreal else Path(__file__).resolve().parents[1]
SOURCE = ROOT / "SourceAssets" / "Audio"
DESTINATION = "/Game/GTA/Audio"
RATE = 24000
TAU = 2.0 * math.pi


def shot(t, rng, state):
    white = rng.uniform(-1.0, 1.0)
    crack = white * math.exp(-34.0 * t)
    low = math.sin(TAU * (170.0 * t - 75.0 * t * t)) * math.exp(-13.0 * t)
    echo = math.sin(TAU * 66.0 * t) * math.exp(-5.5 * t)
    return 0.77 * crack + 0.60 * low + 0.15 * echo


def engine(t, rng, state):
    phase = TAU * 64.0 * t + 0.22 * math.sin(TAU * 4.0 * t)
    return (0.55 * math.sin(phase) + 0.25 * math.sin(2.0 * phase) +
            0.14 * math.sin(3.0 * phase) + 0.11 * math.sin(TAU * 28.0 * t))


def horn(t, rng, state):
    envelope = min(1.0, t * 35.0) * min(1.0, (0.72 - t) * 12.0)
    a = math.sin(TAU * 392.0 * t)
    b = math.sin(TAU * 523.25 * t)
    buzz = 0.24 * math.sin(TAU * 784.0 * t)
    return envelope * (0.43 * a + 0.45 * b + buzz)


def siren(t, rng, state):
    # Frequency sweeps smoothly through two complete cycles in a 3 s loop.
    sweep_phase = TAU * t / 1.5
    phase = TAU * 740.0 * t - 170.0 * 1.5 * math.cos(sweep_phase)
    return 0.58 * math.sin(phase) + 0.15 * math.sin(2.0 * phase)


def footstep(t, rng, state):
    white = rng.uniform(-1.0, 1.0)
    state["low"] = 0.75 * state["low"] + 0.25 * white
    body = math.sin(TAU * 78.0 * t) * math.exp(-20.0 * t)
    grit = state["low"] * math.exp(-28.0 * t)
    return 0.72 * body + 0.46 * grit


def rain(t, rng, state):
    white = rng.uniform(-1.0, 1.0)
    state["low"] = 0.88 * state["low"] + 0.12 * white
    drop = (rng.uniform(-1.0, 1.0) if rng.random() < 0.015 else 0.0)
    return 0.38 * white + 0.32 * state["low"] + 0.30 * drop


def city(t, rng, state):
    white = rng.uniform(-1.0, 1.0)
    state["low"] = 0.997 * state["low"] + 0.003 * white
    traffic = (0.20 * math.sin(TAU * 72.0 * t) +
               0.12 * math.sin(TAU * 97.0 * t) +
               0.07 * math.sin(TAU * 180.0 * t))
    distant_horn = 0.08 * math.sin(TAU * 390.0 * t) if 1.7 < t < 2.0 else 0.0
    return traffic + 0.32 * state["low"] + 0.06 * white + distant_horn


SOUNDS = (
    ("SFX_Gunshot", 0.62, shot, False),
    ("SFX_EngineLoop", 2.50, engine, True),
    ("SFX_Horn", 0.72, horn, False),
    ("SFX_SirenLoop", 3.00, siren, False),
    ("SFX_Footstep", 0.30, footstep, False),
    ("SFX_RainLoop", 4.00, rain, True),
    ("SFX_CityAmbient", 5.00, city, True),
)


def generate(name, duration, fn, looping):
    count = int(RATE * duration)
    rng = random.Random(6761 + sum(ord(c) for c in name))
    state = {"low": 0.0}
    samples = [fn(i / RATE, rng, state) for i in range(count)]
    if not looping:
        # No click when one-shot playback starts or finishes.
        fade = min(int(RATE * 0.015), count // 8)
        for i in range(fade):
            samples[i] *= i / fade
        fade_out = min(int(RATE * 0.075), count // 5)
        for i in range(fade_out):
            samples[-i - 1] *= i / fade_out
    elif name in ("SFX_RainLoop", "SFX_CityAmbient"):
        # Noise is not periodic, so fade the ends of ambient loops.
        fade = int(RATE * 0.06)
        for i in range(fade):
            samples[i] *= i / fade
            samples[-i - 1] *= i / fade
    peak = max(max(abs(value) for value in samples), 0.01)
    gain = min(1.0, 0.92 / peak)
    output = SOURCE / (name + ".wav")
    with wave.open(str(output), "wb") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(RATE)
        for start in range(0, count, 4096):
            chunk = samples[start:start + 4096]
            wav_file.writeframesraw(struct.pack("<{}h".format(len(chunk)),
                *(int(max(-1.0, min(1.0, value * gain)) * 32767) for value in chunk)))
    return output


def import_sound(path, looping, tools):
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", str(path))
    task.set_editor_property("destination_path", DESTINATION)
    task.set_editor_property("destination_name", path.stem)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("automated", True)
    task.set_editor_property("save", True)
    tools.import_asset_tasks([task])
    asset_path = DESTINATION + "/" + path.stem
    sound = unreal.EditorAssetLibrary.load_asset(asset_path)
    if not isinstance(sound, unreal.SoundWave):
        raise RuntimeError("SoundWave import failed: " + asset_path)
    try:
        sound.set_editor_property("looping", looping)
    except Exception:
        pass
    unreal.EditorAssetLibrary.save_loaded_asset(sound)
    return asset_path


def main():
    SOURCE.mkdir(parents=True, exist_ok=True)
    files = [(generate(name, duration, fn, looping), looping)
             for name, duration, fn, looping in SOUNDS]
    if unreal:
        unreal.EditorAssetLibrary.make_directory(DESTINATION)
        tools = unreal.AssetToolsHelpers.get_asset_tools()
        for file_path, looping in files:
            asset_path = import_sound(file_path, looping, tools)
            unreal.log("HARBOR AUDIO " + str(file_path) + " -> " + asset_path)
        unreal.EditorAssetLibrary.save_directory(DESTINATION,
                                                only_if_is_dirty=True, recursive=True)
    else:
        for file_path, _ in files:
            print(file_path, file_path.stat().st_size, "bytes")


if __name__ == "__main__":
    main()
