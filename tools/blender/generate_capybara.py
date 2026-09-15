import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


COLORS = {
    "MAT_Fur": (0.55, 0.16, 0.025, 1.0),
    "MAT_InnerEar": (0.18, 0.035, 0.02, 1.0),
    "MAT_Muzzle": (0.18, 0.055, 0.02, 1.0),
    "MAT_Cream": (0.80, 0.67, 0.48, 1.0),
    "MAT_Ivory": (1.0, 0.965, 0.88, 1.0),
    "MAT_Ochre": (0.76, 0.25, 0.012, 1.0),
    "MAT_Plum": (0.075, 0.014, 0.07, 1.0),
    "MAT_Black": (0.012, 0.009, 0.012, 1.0),
}


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--blend", required=True)
    parser.add_argument("--fbx", required=True)
    parser.add_argument("--preview", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def make_material(name, color):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = color
    principled.inputs["Roughness"].default_value = 0.68
    return material


def apply_scale_and_smooth(obj, smooth=True):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    obj.select_set(False)


def assign_material_and_bone(obj, material, bone_name):
    obj.data.materials.append(material)
    vertex_group = obj.vertex_groups.new(name=bone_name)
    vertex_group.add(range(len(obj.data.vertices)), 1.0, "REPLACE")


def add_sphere(parts, name, location, scale, material, bone, segments, rings, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale_and_smooth(obj)
    assign_material_and_bone(obj, material, bone)
    parts.append(obj)
    return obj


def add_cube(parts, name, location, scale, material, bone, bevel, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale_and_smooth(obj, smooth=False)
    modifier = obj.modifiers.new("Soft Bevel", "BEVEL")
    modifier.width = bevel
    modifier.segments = 3 if bevel >= 0.025 else 2
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)
    assign_material_and_bone(obj, material, bone)
    parts.append(obj)
    return obj


def add_cylinder(parts, name, location, radius, depth, material, bone, vertices):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    apply_scale_and_smooth(obj)
    assign_material_and_bone(obj, material, bone)
    parts.append(obj)
    return obj


def create_armature():
    armature_data = bpy.data.armatures.new("CapybaraChef_Rig")
    armature = bpy.data.objects.new("CapybaraChef_Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    bones = {}

    def add_bone(name, head, tail, parent=None):
        bone = armature_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        if parent:
            bone.parent = bones[parent]
        bones[name] = bone

    add_bone("root", (0, 0, 0.02), (0, 0, 0.22))
    add_bone("body", (0, 0, 0.20), (0, 0, 0.98), "root")
    add_bone("head", (0, 0, 0.96), (0, 0, 1.48), "body")
    add_bone("cap", (0, 0, 1.45), (-0.10, 0, 1.80), "head")
    add_bone("arm_L", (-0.32, 0, 0.78), (-0.46, -0.08, 0.50), "body")
    add_bone("arm_R", (0.32, 0, 0.78), (0.46, -0.08, 0.50), "body")
    add_bone("foot_L", (-0.22, 0, 0.24), (-0.22, -0.11, 0.07), "root")
    add_bone("foot_R", (0.22, 0, 0.24), (0.22, -0.11, 0.07), "root")
    add_bone("brow_L", (-0.22, -0.30, 1.40), (-0.22, -0.41, 1.40), "head")
    add_bone("brow_R", (0.22, -0.30, 1.40), (0.22, -0.41, 1.40), "head")

    bpy.ops.object.mode_set(mode="OBJECT")
    armature.select_set(False)
    return armature


def create_lod(lod_index, armature, materials, segments, rings, bevel):
    prefix = f"LOD{lod_index}_"
    parts = []

    add_sphere(parts, prefix + "Body", (0, 0, 0.59), (0.41, 0.32, 0.38), materials["MAT_Cream"], "body", segments, rings)
    add_cube(parts, prefix + "ApronBib", (0, -0.315, 0.70), (0.27, 0.055, 0.22), materials["MAT_Plum"], "body", bevel * 1.7)
    add_cube(parts, prefix + "ApronSkirt", (0, -0.285, 0.46), (0.38, 0.08, 0.18), materials["MAT_Plum"], "body", bevel * 2.1)
    if lod_index < 2:
        add_cube(parts, prefix + "ApronPocket", (0, -0.37, 0.47), (0.20, 0.025, 0.085), materials["MAT_Plum"], "body", bevel * 1.4)
        for x in (-0.245, 0.245):
            add_cube(parts, prefix + f"ApronStrap_{x}", (x, -0.335, 0.75), (0.035, 0.035, 0.22), materials["MAT_Plum"], "body", bevel * 0.65)
    add_cube(parts, prefix + "NeckTab", (0, -0.33, 0.91), (0.075, 0.045, 0.085), materials["MAT_Ochre"], "body", bevel * 0.85)
    for x in (-0.15, 0.15):
        add_sphere(parts, prefix + f"Button_{x}", (x, -0.35, 0.82), (0.063, 0.035, 0.063), materials["MAT_Plum"], "body", max(12, segments // 2), max(6, rings // 2))
    for x in (-0.37, 0.37):
        add_sphere(parts, prefix + f"Sleeve_{x}", (x, -0.04, 0.72), (0.17, 0.17, 0.18), materials["MAT_Cream"], "body", max(12, segments // 2), max(6, rings // 2))

    add_sphere(parts, prefix + "ArmL", (-0.46, -0.10, 0.58), (0.155, 0.16, 0.23), materials["MAT_Fur"], "arm_L", segments, rings, (0, math.radians(-15), math.radians(-16)))
    add_sphere(parts, prefix + "ArmR", (0.46, -0.10, 0.58), (0.155, 0.16, 0.23), materials["MAT_Fur"], "arm_R", segments, rings, (0, math.radians(15), math.radians(16)))
    add_sphere(parts, prefix + "FootL", (-0.22, -0.09, 0.14), (0.19, 0.24, 0.12), materials["MAT_Muzzle"], "foot_L", segments, rings)
    add_sphere(parts, prefix + "FootR", (0.22, -0.09, 0.14), (0.19, 0.24, 0.12), materials["MAT_Muzzle"], "foot_R", segments, rings)

    add_sphere(parts, prefix + "Head", (0, 0, 1.17), (0.54, 0.405, 0.44), materials["MAT_Fur"], "head", segments, rings)
    for x in (-0.105, 0.105):
        add_sphere(parts, prefix + f"MuzzleLobe_{x}", (x, -0.395, 1.10), (0.19, 0.17, 0.17), materials["MAT_Muzzle"], "head", segments, rings)
    add_sphere(parts, prefix + "Nose", (0, -0.56, 1.18), (0.115, 0.05, 0.08), materials["MAT_InnerEar"], "head", max(12, segments // 2), max(6, rings // 2))
    if lod_index < 2:
        for x in (-0.04, 0.04):
            add_sphere(parts, prefix + f"Nostril_{x}", (x, -0.608, 1.195), (0.014, 0.008, 0.016), materials["MAT_Black"], "head", 8, 4)
    if lod_index < 2:
        for x in (-0.045, 0.045):
            smile_tilt = math.radians(12 if x < 0 else -12)
            add_cube(parts, prefix + f"ClosedSmile_{x}", (x, -0.568, 0.985), (0.055, 0.012, 0.012), materials["MAT_InnerEar"], "head", bevel * 0.35, (0, smile_tilt, 0))
    for x in (-0.22, 0.22):
        add_sphere(parts, prefix + f"Eye_{x}", (x, -0.385, 1.29), (0.10, 0.042, 0.14), materials["MAT_Black"], "head", max(12, segments // 2), max(6, rings // 2))
        if lod_index < 2:
            glint_x = x + (0.028 if x < 0 else -0.028)
            add_sphere(parts, prefix + f"EyeGlint_{x}", (glint_x, -0.426, 1.345), (0.025, 0.01, 0.032), materials["MAT_Ivory"], "head", 8, 4)
    for x in (-0.43, 0.43):
        add_sphere(parts, prefix + f"Ear_{x}", (x, -0.025, 1.38), (0.14, 0.08, 0.145), materials["MAT_Fur"], "head", max(12, segments // 2), max(6, rings // 2))
        add_sphere(parts, prefix + f"InnerEar_{x}", (x, -0.095, 1.38), (0.075, 0.028, 0.085), materials["MAT_InnerEar"], "head", 8, 4)
    add_cube(parts, prefix + "BrowL", (-0.22, -0.423, 1.43), (0.105, 0.02, 0.026), materials["MAT_Plum"], "brow_L", bevel * 0.4, (0, 0, math.radians(-10)))
    add_cube(parts, prefix + "BrowR", (0.22, -0.423, 1.43), (0.105, 0.02, 0.026), materials["MAT_Plum"], "brow_R", bevel * 0.4, (0, 0, math.radians(10)))

    add_cylinder(parts, prefix + "CapBand", (0, 0, 1.52), 0.44, 0.11, materials["MAT_Ochre"], "cap", max(12, segments))
    add_sphere(parts, prefix + "CapCrown", (-0.10, 0, 1.68), (0.44, 0.32, 0.18), materials["MAT_Ivory"], "cap", segments, rings, (0, math.radians(-8), 0))
    add_sphere(parts, prefix + "CapFold", (0.225, 0.035, 1.61), (0.21, 0.23, 0.11), materials["MAT_Ivory"], "cap", max(12, segments // 2), max(6, rings // 2))

    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    mesh = bpy.context.object
    mesh.name = f"CapybaraChef_LOD{lod_index}"
    armature_modifier = mesh.modifiers.new("CapybaraChef Armature", "ARMATURE")
    armature_modifier.object = armature
    mesh.parent = armature
    mesh.hide_render = lod_index != 0
    mesh.display_type = "TEXTURED"
    mesh.select_set(False)
    return mesh


def point_camera(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_preview(lod0, preview_path):
    bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, 0))
    floor = bpy.context.object
    floor.name = "Preview Floor"
    floor_material = bpy.data.materials.new("Preview Floor Material")
    floor_material.diffuse_color = (0.55, 0.38, 0.24, 1)
    floor.data.materials.append(floor_material)

    bpy.ops.object.camera_add(location=(2.8, -4.3, 2.45))
    camera = bpy.context.object
    point_camera(camera, (0, 0, 0.90))
    camera.data.lens = 58
    bpy.context.scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.0, 5.2))
    key = bpy.context.object
    key.data.energy = 600
    key.data.shape = "DISK"
    key.data.size = 4.0
    point_camera(key, (0, 0, 1.0))
    bpy.ops.object.light_add(type="AREA", location=(3.0, -1.0, 2.5))
    fill = bpy.context.object
    fill.data.energy = 300
    fill.data.size = 3.0
    point_camera(fill, (0, 0, 1.1))

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(preview_path)
    scene.world.color = (0.06, 0.045, 0.035)
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.film_transparent = False
    lod0.hide_render = False
    bpy.ops.render.render(write_still=True)


def export_fbx(armature, meshes, fbx_path):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_armature_deform_only=True,
        bake_anim=False,
        mesh_smooth_type="FACE",
        path_mode="AUTO",
    )


def main():
    args = parse_args()
    blend_path = Path(args.blend).resolve()
    fbx_path = Path(args.fbx).resolve()
    preview_path = Path(args.preview).resolve()
    for path in (blend_path, fbx_path, preview_path):
        path.parent.mkdir(parents=True, exist_ok=True)

    reset_scene()
    materials = {name: make_material(name, color) for name, color in COLORS.items()}
    armature = create_armature()
    lod0 = create_lod(0, armature, materials, 24, 12, 0.035)
    lod1 = create_lod(1, armature, materials, 16, 8, 0.025)
    lod2 = create_lod(2, armature, materials, 8, 4, 0.018)
    setup_preview(lod0, preview_path)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    export_fbx(armature, [lod0, lod1, lod2], fbx_path)
    print(f"Saved Blender source: {blend_path}")
    print(f"Saved FBX: {fbx_path}")
    print(f"Saved preview: {preview_path}")


if __name__ == "__main__":
    main()
