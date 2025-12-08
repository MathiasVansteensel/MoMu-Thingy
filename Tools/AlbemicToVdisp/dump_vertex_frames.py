import sys, json, os
import bpy

def parse_args():
    argv = sys.argv
    if "--" in argv:
        idx = argv.index("--")
        script_args = argv[idx+1:]
    else:
        script_args = []

    if len(script_args) < 2:
        print("Usage: blender -b --python dump_vertex_frames.py -- <input.abc> <output.json>")
        sys.exit(1)

    return script_args[0], script_args[1]


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_alembic(path):
    print("[ABC2DISP]: Importing Alembic:", path)
    bpy.ops.wm.alembic_import(filepath=path, as_background_job=False)


def collect_mesh_objects():
    return [o for o in bpy.context.scene.objects if o.type == 'MESH']


def obj_local_vertex_coords(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    eval_obj = obj.evaluated_get(deps)
    mesh = eval_obj.to_mesh()
    coords = [list(v.co) for v in mesh.vertices]
    eval_obj.to_mesh_clear()
    return coords


def main():
    in_abc, out_json = parse_args()

    clear_scene()
    import_alembic(in_abc)

    scene = bpy.context.scene
    start, end, fps = scene.frame_start, scene.frame_end, scene.render.fps
    mesh_objs = collect_mesh_objects()

    if not mesh_objs:
        print("[ABC2DISP]: No mesh objects found in Alembic file.")
        return 2

    # Record base positions at the first frame
    base_frame = start
    scene.frame_set(base_frame)

    # Collect frame data
    frames_out = []
    for f in range(start, end + 1):
        scene.frame_set(f)
        frame_data = {"frame": f, "objects": []}

        for o in mesh_objs:
            coords = obj_local_vertex_coords(o)
            flat = [c for v in coords for c in v]
            frame_data["objects"].append({
                "name": o.name,
                "vertex_count": len(coords),
                "positions": flat
            })

        frames_out.append(frame_data)
        print(f"[ABC2DISP]: Collected frame {f} ({len(mesh_objs)} objects)")

    # Write JSON
    out_data = {
        "meta": {
            "frame_start": start,
            "frame_end": end,
            "fps": fps,
            "base_frame": base_frame
        },
        "frames": frames_out
    }

    with open(out_json, "w") as fh:
        json.dump(out_data, fh, indent=2)

    print(f"[ABC2DISP]: Dumped {len(frames_out)} frames to {out_json}")
    return 0


if __name__ == "__main__":
    sys.exit(main())