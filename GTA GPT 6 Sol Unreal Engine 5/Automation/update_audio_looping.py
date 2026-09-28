"""Set SoundWave loop flags for runtime audio components."""

import unreal


LOOPING = {
    "SFX_Gunshot": False,
    "SFX_EngineLoop": True,
    "SFX_Horn": False,
    "SFX_SirenLoop": False,
    "SFX_Footstep": False,
    "SFX_RainLoop": True,
    "SFX_CityAmbient": True,
}

for name, value in LOOPING.items():
    path = "/Game/GTA/Audio/" + name
    sound = unreal.EditorAssetLibrary.load_asset(path)
    if not isinstance(sound, unreal.SoundWave):
        raise RuntimeError("Missing SoundWave " + path)
    sound.set_editor_property("looping", value)
    unreal.EditorAssetLibrary.save_loaded_asset(sound)
    actual = bool(sound.get_editor_property("looping"))
    if actual != value:
        raise RuntimeError("Looping flag did not persist for " + path)
    unreal.log("AUDIO LOOP {} = {}".format(path, actual))
