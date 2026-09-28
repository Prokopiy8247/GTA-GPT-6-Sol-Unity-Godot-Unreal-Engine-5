"""Import project-owned Blender FBX exports into /Game/GTA/Generated.

Run with UnrealEditor-Cmd.exe <uproject> -run=pythonscript
    -script=Automation/import_assets.py -unattended -nop4

Each FBX or GLB should contain one named asset or an intentionally combined asset. The
source files remain in SourceAssets/BlenderExports; Unreal packages are saved in
Content/GTA/Generated. Re-running the script replaces the packages in place.
"""

from pathlib import Path
import re
import unreal


PROJECT_ROOT = Path(unreal.Paths.project_dir())
EXPORT_ROOT = PROJECT_ROOT / "SourceAssets" / "BlenderExports"
DESTINATION = "/Game/GTA/Generated"


def asset_name(path):
    stem = re.sub(r"[^A-Za-z0-9_]+", "_", path.stem).strip("_")
    if not stem:
        raise ValueError("Empty asset name from " + str(path))
    if stem[0].isdigit():
        stem = "Asset_" + stem
    if stem.lower() == "playerrig":
        return "SK_PlayerRig"
    return stem if stem.startswith(("SM_", "SK_")) else "SM_" + stem


def make_fbx_options(skeletal=False):
    options = unreal.FbxImportUI()
    options.set_editor_property("import_mesh", True)
    options.set_editor_property("import_as_skeletal", skeletal)
    options.set_editor_property("import_materials", True)
    options.set_editor_property("import_textures", True)
    options.set_editor_property("mesh_type_to_import",
                                unreal.FBXImportType.FBXIT_SKELETAL_MESH if skeletal
                                else unreal.FBXImportType.FBXIT_STATIC_MESH)
    if skeletal:
        options.set_editor_property("import_animations", False)
        try:
            options.set_editor_property("create_physics_asset", True)
        except Exception:
            pass
        return options
    static_options = options.get_editor_property("static_mesh_import_data")
    static_options.set_editor_property("combine_meshes", True)
    static_options.set_editor_property("generate_lightmap_u_vs", True)
    static_options.set_editor_property("auto_generate_collision", True)
    try:
        static_options.set_editor_property("convert_scene", True)
    except Exception:
        # This property moved between FBX option classes in some UE builds.
        pass
    options.set_editor_property("static_mesh_import_data", static_options)
    return options


def import_mesh(path, tools):
    name = asset_name(path)
    skeletal = name.startswith("SK_")
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", str(path))
    task.set_editor_property("destination_path", DESTINATION)
    task.set_editor_property("destination_name", name)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("automated", True)
    task.set_editor_property("save", True)
    if path.suffix.lower() == ".fbx":
        task.set_editor_property("options", make_fbx_options(skeletal))
    # GLB is handled by the engine's Interchange translator. It chooses its
    # import pipeline from the extension, so do not attach FBX options.
    tools.import_asset_tasks([task])
    paths = list(task.get_editor_property("imported_object_paths"))
    expected = DESTINATION + "/" + name
    if not paths and unreal.EditorAssetLibrary.does_asset_exist(expected):
        paths = [expected]
    meshes = []
    for asset_path in paths:
        obj = unreal.EditorAssetLibrary.load_asset(asset_path)
        if isinstance(obj, unreal.SkeletalMesh if skeletal else unreal.StaticMesh):
            meshes.append(asset_path)
            unreal.EditorAssetLibrary.save_loaded_asset(obj)
    if not meshes:
        raise RuntimeError("No {} produced for {}".format(
            "SkeletalMesh" if skeletal else "StaticMesh", path))
    return meshes


def main():
    if not EXPORT_ROOT.exists():
        raise FileNotFoundError("Blender export directory missing: " + str(EXPORT_ROOT))
    files = sorted(path for path in EXPORT_ROOT.rglob("*")
                   if path.is_file() and path.suffix.lower() in (".fbx", ".glb"))
    if not files:
        raise RuntimeError("No FBX/GLB files found in " + str(EXPORT_ROOT))
    unreal.EditorAssetLibrary.make_directory(DESTINATION)
    tools = unreal.AssetToolsHelpers.get_asset_tools()
    imported = []
    failed = []
    for file_path in files:
        try:
            assets = import_mesh(file_path, tools)
            imported.extend(assets)
            unreal.log("HARBOR IMPORT " + str(file_path) + " -> " + ", ".join(assets))
        except Exception as exc:
            failed.append((str(file_path), str(exc)))
            unreal.log_error("HARBOR IMPORT FAILED " + str(file_path) + ": " + str(exc))
    unreal.EditorAssetLibrary.save_directory(DESTINATION, only_if_is_dirty=True, recursive=True)
    unreal.log("HARBOR IMPORT SUMMARY: {} source files, {} meshes, {} errors".format(
        len(files), len(imported), len(failed)))
    if failed:
        raise RuntimeError("Blender import failed for {} file(s)".format(len(failed)))


if __name__ == "__main__":
    main()
