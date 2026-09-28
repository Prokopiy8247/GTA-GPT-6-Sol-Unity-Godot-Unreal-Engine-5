# Blender source pipeline

The master source scene is `../../UnrealGTAGPT6Sol.blend`. Every game asset has
its own collection under `GTA_Assets`, with geometry at the origin and metres as
Blender units. Vehicles and weapons face +X; Z is up. FBX exports are separate
from Unreal's imported assets under `/Game/GTA/Generated`.

The dedicated Blender addon socket is **127.0.0.1:9881**. The Codex runtime did
not expose this server as a built-in MCP tool, so `../../Tools/blender_mcp_client.py`
opens a standard local MCP stdio session with `mcp-for-blender.exe --port 9881`.
It verifies the exact master path before any scene edit. Run it with the Python
interpreter installed beside `mcp-for-blender.exe` so the MCP SDK is available.

For example, from the project root:

```powershell
& '<path-to-mcp-for-blender-Scripts>\python.exe' .\Tools\blender_mcp_client.py --call get_scene_info
& '<path-to-mcp-for-blender-Scripts>\python.exe' .\Tools\blender_mcp_client.py --call export_scene --object Car --output .\SourceAssets\BlenderExports\Car.fbx --format fbx
```

The individual `Tools/blender_asset_*.py` files are sent to Blender via the MCP
`execute_blender_code` tool alongside `Tools/blender_assets_core.py`. They are
source recipes and are not executed as standalone Blender background scripts.
`PlayerRig.fbx` contains an armature and skinned character; import it as a
skeletal mesh. The plain `Player.fbx` is a static character mesh.

Key source pivots: `CarWheel_FL/FR/RL/RR`, `HelicopterMainRotor`, and
`HelicopterTailRotor` are separate editable objects in the master scene.
