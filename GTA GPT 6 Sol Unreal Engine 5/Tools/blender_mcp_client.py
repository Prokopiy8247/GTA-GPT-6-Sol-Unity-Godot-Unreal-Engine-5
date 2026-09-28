"""Call the Blender MCP server bound to this project's Blender on port 9881.

Run using the Python interpreter beside mcp-for-blender.exe, which has the MCP
client package installed. Tool arguments are read from a JSON file to avoid
shell quoting and command length issues.
"""

import argparse
import asyncio
import base64
import json
import shutil
import sys
from pathlib import Path

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--list", action="store_true")
    parser.add_argument("--list-names", action="store_true")
    parser.add_argument("--call")
    parser.add_argument("--args", type=Path)
    parser.add_argument("--code-file", type=Path, action="append")
    parser.add_argument("--image-out", type=Path)
    parser.add_argument("--object")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--format", default="fbx")
    parser.add_argument("--port", type=int, default=9881)
    ns = parser.parse_args()

    executable = Path(sys.executable).with_name("mcp-for-blender.exe")
    if not executable.is_file():
        discovered = shutil.which("mcp-for-blender")
        if not discovered:
            raise RuntimeError("Run with the Python interpreter beside mcp-for-blender.exe")
        executable = Path(discovered)
    params = StdioServerParameters(command=str(executable), args=["--port", str(ns.port)])
    async with stdio_client(params) as (reader, writer):
        async with ClientSession(reader, writer) as session:
            await session.initialize()
            if ns.list:
                result = await session.list_tools()
                print(json.dumps([t.model_dump() for t in result.tools], ensure_ascii=False))
            elif ns.list_names:
                result = await session.list_tools()
                print("\n".join(t.name for t in result.tools))
            elif ns.call:
                arguments = json.loads(ns.args.read_text(encoding="utf-8")) if ns.args else {}
                if ns.code_file:
                    arguments["code"] = "\n\n".join(path.read_text(encoding="utf-8") for path in ns.code_file)
                if ns.object:
                    if ns.output:
                        arguments.update({"object_names": [ns.object], "filepath": str(ns.output.resolve()), "format": ns.format})
                    # Blender's selected-only export excludes hidden objects.
                    # Reveal the requested mesh before MCP export or screenshot.
                    target_name = ns.object.replace("\\", "\\\\").replace("'", "\\'")
                    reveal = await session.call_tool("execute_blender_code", {
                        "code": "import bpy\nexpected_name = 'UnrealGTAGPT6Sol.blend'\nassert bpy.path.basename(bpy.data.filepath).lower() == expected_name.lower()\nfor obj in bpy.data.objects:\n    if any(coll.name.startswith('GTA_') for coll in obj.users_collection):\n        obj.hide_set(obj.name != '" + target_name + "')\ntarget = bpy.data.objects.get('" + target_name + "')\nif target:\n    for area in bpy.context.screen.areas:\n        if area.type == 'VIEW_3D':\n            area.spaces.active.region_3d.view_location = (0,0,target.dimensions.z*0.5)\n            area.spaces.active.region_3d.view_distance = max(target.dimensions)*1.65",
                        "user_prompt": "Для важных 3D-ассетов обязательно используй подключённый Blender MCP blender_unreal_gpt6sol, который работает с Blender-инстансом на порту 9881 и файлом UnrealGTAGPT6Sol.blend."
                    })
                    if reveal.isError or "Rejected" in str(reveal.structuredContent):
                        raise RuntimeError("Could not reveal requested Blender object: " + str(reveal.structuredContent))
                    await asyncio.sleep(0.35)
                if ns.call in {"get_scene_info", "execute_blender_code", "export_scene", "get_viewport_screenshot"}:
                    arguments.setdefault("user_prompt", "Для важных 3D-ассетов обязательно используй подключённый Blender MCP blender_unreal_gpt6sol, который работает с Blender-инстансом на порту 9881 и файлом UnrealGTAGPT6Sol.blend.")
                result = await session.call_tool(ns.call, arguments)
                if ns.image_out:
                    for item in result.content:
                        if item.type == "image":
                            ns.image_out.parent.mkdir(parents=True, exist_ok=True)
                            ns.image_out.write_bytes(base64.b64decode(item.data))
                            print(str(ns.image_out))
                            break
                else:
                    print(json.dumps({"is_error": result.isError, "result": result.structuredContent or [c.model_dump() for c in result.content]}, ensure_ascii=False))
            else:
                parser.error("Specify --list or --call")


if __name__ == "__main__":
    asyncio.run(main())
