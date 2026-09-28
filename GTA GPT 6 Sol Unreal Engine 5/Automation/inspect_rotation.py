"""Small UE Python diagnostic for Rotator constructor argument order."""

import json
from pathlib import Path
import unreal

r = unreal.Rotator(0.0, 80.0, 0.0)
result = {"pitch": r.pitch, "yaw": r.yaw, "roll": r.roll}
output = Path(unreal.Paths.project_dir()) / "Saved" / "Logs" / "RotatorDiagnostic.json"
output.write_text(json.dumps(result), encoding="utf-8")
