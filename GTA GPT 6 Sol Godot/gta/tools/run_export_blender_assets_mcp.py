"""Export Blender assets through the MCP server attached to port 9880.

Run from the project root:
    uv run --no-project --with mcp-for-blender python gta/tools/run_export_blender_assets_mcp.py

To check a subset:
    uv run --no-project --with mcp-for-blender python gta/tools/run_export_blender_assets_mcp.py --only car_suv,player
"""

import argparse
import asyncio
import tempfile
from pathlib import Path

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


PROJECT_ROOT = Path(__file__).resolve().parents[2]
EXPECTED_BLEND = PROJECT_ROOT / "GodotGTAGPT6Sol.blend"
BLENDER_SOURCE = Path(__file__).with_name("export_blender_assets.py")
USER_PROMPT = (
    "Для важных 3D-ассетов обязательно используй подключённый Blender MCP "
    "blender_godot_gpt6sol, который работает с Blender-инстансом на порту 9880 "
    "и файлом GodotGTAGPT6Sol.blend."
)


def tool_text(result: object) -> str:
    return "\n".join(block.text for block in result.content if block.type == "text")


async def export(only: str) -> None:
    server = StdioServerParameters(
        command="uvx",
        args=["mcp-for-blender", "--port", "9880"],
        env={
            "BLENDER_HOST": "127.0.0.1",
            "BLENDER_MCP_SAFE_MODE": "1",
            "DISABLE_TELEMETRY": "true",
        },
    )
    with tempfile.TemporaryFile(mode="w+", encoding="utf-8") as server_log:
        async with stdio_client(server, errlog=server_log) as (reader, writer):
            async with ClientSession(reader, writer) as session:
                await session.initialize()
                await session.call_tool("get_addon_status", {"user_prompt": USER_PROMPT})
                await session.call_tool("get_scene_info", {"user_prompt": USER_PROMPT})
                probe = await session.call_tool(
                    "execute_blender_code",
                    {"code": "import bpy\nprint(bpy.data.filepath)", "user_prompt": USER_PROMPT},
                )
                actual = tool_text(probe).removeprefix("Code executed successfully:").strip()
                normalize = lambda path: path.replace("\\", "/").casefold()
                if normalize(actual) != normalize(str(EXPECTED_BLEND)):
                    raise RuntimeError(f"Port 9880 has the wrong Blender file: {actual}")

                if only:
                    selected = ",".join(name.strip() for name in only.split(",") if name.strip())
                    code = f"import bpy\nbpy.context.scene['gta_export_subset'] = {selected!r}"
                    response = await session.call_tool(
                        "execute_blender_code", {"code": code, "user_prompt": USER_PROMPT}
                    )
                    if not tool_text(response).startswith("Code executed successfully:"):
                        raise RuntimeError(tool_text(response))

                try:
                    response = await session.call_tool(
                        "execute_blender_code",
                        {"code": BLENDER_SOURCE.read_text(encoding="utf-8"), "user_prompt": USER_PROMPT},
                    )
                    output = tool_text(response)
                    for line in output.splitlines():
                        if line.startswith(("EXPORTED ", "GTA_MASTER_SAVED ")):
                            print(line)
                    if not output.startswith("Code executed successfully:"):
                        raise RuntimeError(output)
                    await session.call_tool("get_scene_info", {"user_prompt": USER_PROMPT})
                    await session.call_tool("get_viewport_screenshot", {"user_prompt": USER_PROMPT})
                finally:
                    if only:
                        await session.call_tool(
                            "execute_blender_code",
                            {
                                "code": "import bpy\nif 'gta_export_subset' in bpy.context.scene: del bpy.context.scene['gta_export_subset']",
                                "user_prompt": USER_PROMPT,
                            },
                        )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--only", default="", help="Comma-separated asset names; default exports all 33")
    options = parser.parse_args()
    asyncio.run(export(options.only))
