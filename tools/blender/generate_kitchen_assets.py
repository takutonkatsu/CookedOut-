import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


COLORS = {
    "MAT_FloorA": (0.40, 0.44, 0.45, 1.0),
    "MAT_FloorB": (0.47, 0.51, 0.52, 1.0),
    "MAT_Wall": (0.68, 0.72, 0.72, 1.0),
    "MAT_Plum": (0.16, 0.075, 0.18, 1.0),
    "MAT_Cream": (0.74, 0.76, 0.75, 1.0),
    "MAT_Teal": (0.02, 0.59, 0.64, 1.0),
    "MAT_Ochre": (0.92, 0.50, 0.08, 1.0),
    "MAT_FlameBlue": (0.08, 0.48, 1.00, 1.0),
    "MAT_DeepInset": (0.07, 0.045, 0.08, 1.0),
    "MAT_Metal": (0.70, 0.72, 0.73, 1.0),
    "MAT_Steel": (0.63, 0.65, 0.65, 1.0),
    "MAT_SteelLight": (0.82, 0.84, 0.83, 1.0),
    "MAT_SteelDark": (0.36, 0.39, 0.40, 1.0),
    "MAT_Glass": (0.70, 0.91, 0.96, 0.28),
    "MAT_Wood": (0.56, 0.31, 0.13, 1.0),
    "MAT_WoodDark": (0.31, 0.15, 0.07, 1.0),
    "MAT_Container": (0.84, 0.75, 0.58, 1.0),
    "MAT_BoardIvory": (0.90, 0.89, 0.84, 1.0),
    "MAT_Lettuce": (0.33, 0.84, 0.16, 1.0),
    "MAT_LettuceDark": (0.16, 0.56, 0.12, 1.0),
    "MAT_LettuceLight": (0.76, 0.96, 0.28, 1.0),
    "MAT_Carrot": (1.00, 0.35, 0.035, 1.0),
    "MAT_CarrotGroove": (0.86, 0.16, 0.018, 1.0),
    "MAT_Leaf": (0.16, 0.56, 0.12, 1.0),
    "MAT_OnionSkin": (0.91, 0.54, 0.17, 1.0),
    "MAT_OnionFlesh": (1.00, 0.88, 0.59, 1.0),
    "MAT_OnionHighlight": (1.00, 0.72, 0.29, 1.0),
    "MAT_BeefRaw": (0.76, 0.16, 0.15, 1.0),
    "MAT_BeefFat": (1.00, 0.64, 0.58, 1.0),
    "MAT_BeefCooked": (0.34, 0.12, 0.045, 1.0),
    "MAT_Sauce": (0.55, 0.055, 0.025, 1.0),
    "MAT_Delivery": (0.14, 0.58, 0.29, 1.0),
    "MAT_Red": (0.88, 0.09, 0.055, 1.0),
    "MAT_Water": (0.25, 0.78, 0.95, 1.0),
}

CELL_SIZE = 1.6


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--blend", required=True)
    parser.add_argument("--model-dir", required=True)
    parser.add_argument("--preview", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def make_material(name, color):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = color
    is_metal = name == "MAT_Metal" or name.startswith("MAT_Steel")
    principled.inputs["Roughness"].default_value = 0.10 if name == "MAT_Glass" else (0.42 if is_metal else 0.63)
    principled.inputs["Metallic"].default_value = 0.48 if is_metal else 0.0
    if name == "MAT_Glass":
        principled.inputs["Alpha"].default_value = color[3]
    return material


def apply_scale(obj, smooth=False):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if smooth and hasattr(obj.data, "polygons"):
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    obj.select_set(False)


def parent_to(obj, root):
    obj.parent = root
    return obj


def add_box(root, name, location, size, material, bevel=0.06, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
    apply_scale(obj)
    if bevel > 0:
        modifier = obj.modifiers.new("Rounded Edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    obj.data.materials.append(material)
    return parent_to(obj, root)


def add_cylinder(root, name, location, radius, depth, material, vertices=24, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    apply_scale(obj, smooth=True)
    obj.data.materials.append(material)
    return parent_to(obj, root)


def add_cone(root, name, location, radius1, radius2, depth, material, vertices=28, rotation=(0, 0, 0), bevel=0.025):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius1,
        radius2=radius2,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    apply_scale(obj, smooth=True)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft Carrot Edge", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    obj.data.materials.append(material)
    return parent_to(obj, root)


def add_sphere(root, name, location, scale, material, rotation=(0, 0, 0), segments=20, rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale(obj, smooth=True)
    obj.data.materials.append(material)
    return parent_to(obj, root)


def add_torus(root, name, location, major_radius, minor_radius, material, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=24,
        minor_segments=8,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    apply_scale(obj, smooth=True)
    obj.data.materials.append(material)
    return parent_to(obj, root)


def add_tube(root, name, points, radius, material):
    """Create a soft raised line for the large veins, grooves and marbling seen on the source cards."""
    curve = bpy.data.curves.new(name + " Curve", type="CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for bezier_point, point in zip(spline.bezier_points, points):
        bezier_point.co = point
        bezier_point.handle_left_type = "AUTO"
        bezier_point.handle_right_type = "AUTO"

    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    apply_scale(obj, smooth=True)
    return parent_to(obj, root)


def add_triangle_prism(root, name, points, y, thickness, material):
    """Create a flat painted-looking triangle on the authored +Y front face."""
    half = thickness * 0.5
    vertices = [(x, y - half, z) for x, z in points] + [(x, y + half, z) for x, z in points]
    faces = [
        (0, 2, 1),
        (3, 4, 5),
        (0, 1, 4, 3),
        (1, 2, 5, 4),
        (2, 0, 3, 5),
    ]
    mesh = bpy.data.meshes.new(name + " Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return parent_to(obj, root)


def add_extruded_polygon(root, name, points, z, thickness, material, bevel=0.0):
    """Create a horizontal toy-like silhouette with real thickness."""
    half = thickness * 0.5
    vertex_count = len(points)
    vertices = [(x, y, z - half) for x, y in points] + [(x, y, z + half) for x, y in points]
    faces = [
        tuple(reversed(range(vertex_count))),
        tuple(range(vertex_count, vertex_count * 2)),
    ]
    for index in range(vertex_count):
        next_index = (index + 1) % vertex_count
        faces.append((index, next_index, next_index + vertex_count, index + vertex_count))

    mesh = bpy.data.meshes.new(name + " Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft Toy Edge", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    obj.data.materials.append(material)
    return parent_to(obj, root)


def create_root(name):
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    root.hide_render = True
    return root


def add_counter_base(root, materials, worktop=True):
    # Clean hotel-kitchen silhouette with toy-scale rounded edges. The large
    # front drawers face the fixed camera after the Blender-to-Unity axis turn.
    # Counter modules share the exact 1.6 m grid footprint. The shallow bevel
    # keeps the toy-like edge while the worktop overlaps by 1 cm on every side,
    # so adjacent modules read as one continuous preparation surface.
    add_box(root, "Gapless Stainless Cabinet", (0, 0, -0.02), (1.58, 1.58, 0.82), materials["MAT_Steel"], 0.035)
    add_box(root, "Dark Toe Kick", (0, 0.735, -0.32), (1.34, 0.10, 0.16), materials["MAT_SteelDark"], 0.025)
    add_box(root, "Large Upper Drawer", (0, 0.795, 0.20), (1.24, 0.055, 0.25), materials["MAT_SteelLight"], 0.025)
    add_box(root, "Large Lower Drawer", (0, 0.795, -0.10), (1.24, 0.055, 0.25), materials["MAT_SteelLight"], 0.025)
    add_box(root, "Upper Drawer Pull", (0, 0.832, 0.26), (0.62, 0.04, 0.055), materials["MAT_SteelDark"], 0.014)
    add_box(root, "Lower Drawer Pull", (0, 0.832, -0.04), (0.62, 0.04, 0.055), materials["MAT_SteelDark"], 0.014)
    if worktop:
        add_box(root, "Gapless Stainless Worktop", (0, 0, 0.45), (1.62, 1.62, 0.14), materials["MAT_SteelLight"], 0.025)
    for index, (x, y) in enumerate(((-0.75, -0.75), (0.75, -0.75), (-0.75, 0.75), (0.75, 0.75)), 1):
        add_box(root, f"Inset Rounded Bumper {index}", (x, y, 0.27), (0.10, 0.10, 0.42), materials["MAT_Teal"], 0.025)


def build_floor(name, material):
    root = create_root(name)
    # Slightly overlap adjacent logical cells. A single foundation prevents the
    # dark world background from showing through decorative tile seams.
    add_box(root, "Gapless Floor Foundation", (0, 0, 0), (1.64, 1.64, 0.20), material, 0.018)
    return root


def build_wall(materials):
    root = create_root("Wall")
    add_box(root, "Wall Body", (0, 0, 0), (1.54, 1.54, 1.10), materials["MAT_Wall"], 0.10)
    add_box(root, "Sanitary Steel Wall Cap", (0, 0, 0.58), (1.58, 1.58, 0.12), materials["MAT_SteelLight"], 0.06)
    return root


def build_counter(materials):
    root = create_root("Counter")
    add_counter_base(root, materials)
    return root


def build_ingredient_crate(materials):
    root = create_root("IngredientCrate")
    # A grounded supermarket ice-cream freezer: insulated lower cabinet,
    # recessed cold well, and two transparent sliding-glass lids on top.
    add_box(root, "Grounded Freezer Cabinet", (0, 0, -0.13), (1.34, 1.24, 0.68), materials["MAT_Steel"], 0.085)
    add_box(root, "Teal Freezer Lower Band", (0, 0, -0.37), (1.38, 1.28, 0.13), materials["MAT_Teal"], 0.045)
    add_box(root, "Dark Frozen Ingredient Well", (0, 0, 0.20), (1.08, 0.96, 0.24), materials["MAT_DeepInset"], 0.055)
    add_box(root, "Left Transparent Sliding Lid", (-0.29, 0, 0.365), (0.58, 1.02, 0.035), materials["MAT_Glass"], 0.018)
    add_box(root, "Right Transparent Sliding Lid", (0.29, 0, 0.375), (0.58, 1.02, 0.035), materials["MAT_Glass"], 0.018)
    add_box(root, "Freezer Front Rim", (0, 0.555, 0.39), (1.38, 0.11, 0.12), materials["MAT_Teal"], 0.035)
    add_box(root, "Freezer Rear Rim", (0, -0.555, 0.39), (1.38, 0.11, 0.12), materials["MAT_Teal"], 0.035)
    add_box(root, "Freezer Left Rim", (-0.635, 0, 0.39), (0.11, 1.02, 0.12), materials["MAT_Teal"], 0.035)
    add_box(root, "Freezer Right Rim", (0.635, 0, 0.39), (0.11, 1.02, 0.12), materials["MAT_Teal"], 0.035)
    add_box(root, "Sliding Lid Centre Rail", (0, 0, 0.415), (0.055, 1.02, 0.055), materials["MAT_SteelLight"], 0.018)
    for index, x in enumerate((-0.34, 0, 0.34), 1):
        add_box(root, f"Freezer Front Vent {index}", (x, 0.626, -0.15), (0.19, 0.055, 0.26), materials["MAT_SteelDark"], 0.035)
    return root


def build_chopping_station(materials):
    root = create_root("ChoppingBoard")
    add_counter_base(root, materials)
    add_box(root, "Rounded Chopping Board", (0, 0, 0.58), (1.02, 0.80, 0.14), materials["MAT_BoardIvory"], 0.09)
    knife_root = bpy.data.objects.new("Knife Motion Root", None)
    bpy.context.collection.objects.link(knife_root)
    # Rotate the complete knife 90 degrees around the board center. Move the
    # handle-tip pivot with that rotation so the blade remains on the board.
    knife_root.location = (-0.197, -0.45, 0.83)
    knife_root.rotation_euler = (0, 0, math.radians(72))
    parent_to(knife_root, root)
    # Keep the blade and handle below a dedicated empty so Unity can animate the
    # complete knife without touching the board, counter, bumpers or colliders.
    # Original chef-knife design: broad readable blade, bright facet, rounded
    # turquoise handle and a separate bolster. This follows the requested visual
    # vocabulary without tracing or reproducing the reference illustration.
    blade = add_extruded_polygon(
        knife_root,
        "Broad Chef Knife Blade",
        ((0.22, -0.17), (0.68, -0.17), (0.82, -0.10), (0.88, -0.01),
         (0.80, 0.10), (0.62, 0.19), (0.28, 0.18), (0.22, 0.08)),
        0,
        0.070,
        materials["MAT_Metal"],
        0.025,
    )
    blade_highlight = add_extruded_polygon(
        knife_root,
        "Chef Knife Blade Highlight",
        ((0.32, 0.03), (0.69, -0.02), (0.77, 0.02), (0.61, 0.12), (0.34, 0.13)),
        0.043,
        0.014,
        materials["MAT_SteelLight"],
        0.012,
    )
    handle = add_box(
        knife_root, "Rounded Turquoise Knife Handle", (0.06, 0, 0),
        (0.38, 0.22, 0.13), materials["MAT_Teal"], 0.060)
    bolster = add_box(
        knife_root, "Bright Knife Bolster", (0.245, 0, 0),
        (0.11, 0.24, 0.14), materials["MAT_SteelLight"], 0.035)
    bpy.ops.object.select_all(action="DESELECT")
    for part in (blade, blade_highlight, handle, bolster):
        part.select_set(True)
    bpy.context.view_layer.objects.active = blade
    bpy.ops.object.join()
    blade.name = "Knife Geometry"
    blade.parent = knife_root
    # Stand the blade on its cutting edge. The previous horizontal face made the
    # chopping motion look like the player was striking with the flat of the knife.
    blade.rotation_euler = (math.radians(90), 0, 0)
    blade.select_set(False)
    blade_direction = bpy.data.objects.new("Knife Blade Direction", None)
    bpy.context.collection.objects.link(blade_direction)
    blade_direction.location = (0.76, 0, 0)
    parent_to(blade_direction, knife_root)
    return root


def build_pot_heat(materials):
    root = create_root("PotHeatSource")
    add_counter_base(root, materials)
    add_cylinder(root, "Gas Burner Basin", (0, 0, 0.57), 0.54, 0.09, materials["MAT_DeepInset"])
    add_torus(root, "Blue Gas Flame Ring", (0, 0, 0.65), 0.32, 0.035, materials["MAT_FlameBlue"])
    add_cylinder(root, "Gas Burner Cap", (0, 0, 0.66), 0.22, 0.07, materials["MAT_SteelDark"])
    add_box(root, "Gas Grate East West", (0, 0, 0.71), (1.02, 0.13, 0.09), materials["MAT_DeepInset"], 0.025)
    add_box(root, "Gas Grate North South", (0, 0, 0.71), (0.13, 1.02, 0.09), materials["MAT_DeepInset"], 0.025)
    add_torus(root, "Gas Grate Outer Ring", (0, 0, 0.70), 0.48, 0.045, materials["MAT_DeepInset"])
    add_cylinder(root, "Heat Control", (0, -0.70, 0.12), 0.13, 0.10, materials["MAT_Teal"], rotation=(math.radians(90), 0, 0))
    add_box(root, "Heat Control Tick", (0, -0.765, 0.17), (0.035, 0.035, 0.10), materials["MAT_SteelLight"], 0.01)
    return root


def build_assembly(materials):
    root = create_root("AssemblyCounter")
    add_counter_base(root, materials)
    add_box(root, "Assembly Recess", (0, 0, 0.55), (0.82, 0.84, 0.10), materials["MAT_Cream"], 0.07)
    add_box(root, "Left Functional Rail", (-0.52, 0, 0.65), (0.17, 1.00, 0.24), materials["MAT_Teal"], 0.07)
    add_box(root, "Right Functional Rail", (0.52, 0, 0.65), (0.17, 1.00, 0.24), materials["MAT_Teal"], 0.07)
    return root


def build_container_dispenser(materials):
    root = create_root("ContainerDispenser")
    add_counter_base(root, materials)
    # The authored front is +Y (converted to Unity local -Z). Put the rack at
    # the rear and extend a bright pickup lip toward the interaction side.
    add_box(root, "Container Rack Back", (0, -0.43, 0.95), (1.05, 0.15, 0.88), materials["MAT_Teal"], 0.08)
    add_box(root, "Container Rack Opening", (0, -0.33, 0.91), (0.78, 0.09, 0.55), materials["MAT_DeepInset"], 0.07)
    add_box(root, "Left Container Guide", (-0.52, 0, 0.69), (0.17, 1.00, 0.30), materials["MAT_Teal"], 0.07)
    add_box(root, "Right Container Guide", (0.52, 0, 0.69), (0.17, 1.00, 0.30), materials["MAT_Teal"], 0.07)
    add_box(root, "Container Pickup Bay", (0, 0.10, 0.60), (0.92, 0.76, 0.08), materials["MAT_DeepInset"], 0.05)
    add_box(root, "Container Pickup Lip", (0, 0.88, 0.68), (1.00, 0.24, 0.15), materials["MAT_Teal"], 0.05)
    for index in range(3):
        z = 0.58 + index * 0.10
        add_box(root, f"Container Stack {index + 1}", (0, 0.10, z), (0.78, 0.62, 0.075), materials["MAT_Container"], 0.05)
        add_box(root, f"Container Rim Front {index + 1}", (0, 0.39, z + 0.04), (0.72, 0.07, 0.08), materials["MAT_Teal"], 0.025)
        add_box(root, f"Container Rim Back {index + 1}", (0, -0.19, z + 0.04), (0.72, 0.07, 0.08), materials["MAT_Teal"], 0.025)
    # Large open-container pictogram on the rear rack; it is visible above the
    # stack and points toward the same pickup side as the lip.
    add_box(root, "Container Icon Tray", (0, -0.335, 0.86), (0.56, 0.045, 0.12), materials["MAT_Container"], 0.025)
    add_box(root, "Container Icon Lid", (0, -0.325, 1.10), (0.54, 0.045, 0.10), materials["MAT_Container"], 0.025, (0, math.radians(18), 0))
    add_box(root, "Container Icon Hinge", (-0.25, -0.315, 0.99), (0.08, 0.05, 0.24), materials["MAT_Teal"], 0.02, (0, math.radians(18), 0))
    return root


def build_serving_hatch(materials):
    root = create_root("ServingHatch")
    add_counter_base(root, materials)
    add_box(root, "Serving Ledge", (0, -0.42, 0.62), (1.05, 0.25, 0.17), materials["MAT_Teal"], 0.07)
    add_box(root, "Serving Arch Left", (-0.56, 0, 0.92), (0.18, 0.20, 0.80), materials["MAT_Ochre"], 0.07)
    add_box(root, "Serving Arch Right", (0.56, 0, 0.92), (0.18, 0.20, 0.80), materials["MAT_Ochre"], 0.07)
    add_box(root, "Serving Arch Top", (0, 0, 1.29), (1.26, 0.20, 0.18), materials["MAT_Ochre"], 0.07)
    # A plate and cloche silhouette reads as a hand-off point without text.
    add_cylinder(root, "Serving Plate Icon", (0, 0.14, 1.28), 0.25, 0.07, materials["MAT_SteelLight"], rotation=(math.radians(90), 0, 0))
    add_sphere(root, "Serving Cloche Icon", (0, 0.19, 1.31), (0.18, 0.045, 0.11), materials["MAT_Teal"])
    add_box(root, "Serving Tray Icon", (0, 0.20, 1.20), (0.48, 0.05, 0.055), materials["MAT_Teal"], 0.02)
    add_sphere(root, "Serving Cloche Handle", (0, 0.20, 1.43), (0.055, 0.035, 0.055), materials["MAT_Teal"])
    add_box(root, "Serving Direction Shaft", (0, 0.08, 0.58), (0.13, 0.50, 0.055), materials["MAT_Teal"], 0.02)
    add_box(root, "Serving Direction Left", (-0.10, 0.30, 0.59), (0.12, 0.30, 0.055), materials["MAT_Teal"], 0.02, (0, 0, math.radians(-42)))
    add_box(root, "Serving Direction Right", (0.10, 0.30, 0.59), (0.12, 0.30, 0.055), materials["MAT_Teal"], 0.02, (0, 0, math.radians(42)))
    return root


def build_trash(materials):
    root = create_root("TrashBin")
    # A rounded flat-front body lets the cue read as paint on the bin instead of
    # a separate sign hovering in front of a curved cylinder.
    add_box(root, "Flat Front Stainless Trash Body", (0, 0, 0), (1.12, 1.02, 0.84), materials["MAT_Steel"], 0.14)
    add_torus(root, "Trash Opening", (0, 0, 0.47), 0.43, 0.10, materials["MAT_SteelDark"])
    add_cylinder(root, "Trash Inner", (0, 0, 0.49), 0.33, 0.05, materials["MAT_DeepInset"])
    add_box(root, "Trash Accent", (0, -0.505, 0.02), (0.58, 0.04, 0.20), materials["MAT_Ochre"], 0.025)
    # Low-profile fish-skeleton pictogram fixed directly to the body side.
    # Positive Y faces the fixed gameplay camera after the Blender-to-Unity rotation.
    icon_y = 0.507
    icon_z = 0.00
    add_sphere(root, "Fish Bone Head", (-0.27, icon_y, icon_z), (0.17, 0.018, 0.15), materials["MAT_Cream"])
    add_box(root, "Fish Bone Spine", (0.08, icon_y, icon_z), (0.56, 0.022, 0.055), materials["MAT_Cream"], 0.012)
    for index, x in enumerate((-0.05, 0.10, 0.25), 1):
        add_box(root, f"Fish Rib Up {index}", (x, icon_y, icon_z + 0.10), (0.055, 0.022, 0.24), materials["MAT_Cream"], 0.012, (0, math.radians(34), 0))
        add_box(root, f"Fish Rib Down {index}", (x, icon_y, icon_z - 0.10), (0.055, 0.022, 0.24), materials["MAT_Cream"], 0.012, (0, math.radians(-34), 0))
    add_triangle_prism(root, "Fish Tail Upper Fin", ((0.34, 0.0), (0.58, 0.22), (0.58, 0.035)), icon_y, 0.022, materials["MAT_Cream"])
    add_triangle_prism(root, "Fish Tail Lower Fin", ((0.34, 0.0), (0.58, -0.035), (0.58, -0.22)), icon_y, 0.022, materials["MAT_Cream"])
    add_sphere(root, "Fish Bone Eye", (-0.31, icon_y + 0.014, icon_z + 0.05), (0.035, 0.012, 0.035), materials["MAT_Plum"])
    return root


def build_bike_dock(materials):
    root = create_root("BikeDock")
    # Loading happens on the bike itself. Keep only a flush boundary marker and
    # wheel stop; there is no separate cargo workbench.
    add_box(root, "Bike Boundary Pad", (0, 0, -0.43), (1.48, 1.48, 0.05), materials["MAT_Delivery"], 0.025)
    add_box(root, "Bike Wheel Stop", (0, 0.50, -0.36), (0.92, 0.16, 0.14), materials["MAT_Ochre"], 0.035)
    return root


def build_delivery_point(materials):
    root = create_root("DeliveryPoint")
    add_cylinder(root, "Stainless Delivery Pedestal", (0, 0, -0.03), 0.62, 0.80, materials["MAT_Steel"])
    add_torus(root, "Delivery Target Ring", (0, 0, 0.46), 0.40, 0.10, materials["MAT_Ochre"])
    add_cylinder(root, "Delivery Surface", (0, 0, 0.46), 0.31, 0.07, materials["MAT_Delivery"])
    return root


def build_recovery(materials):
    root = create_root("RecoveryBin")
    add_counter_base(root, materials)
    add_box(root, "Recovery Slot", (0, 0, 0.58), (0.72, 0.38, 0.12), materials["MAT_Teal"], 0.06)
    add_box(root, "Recovery Accent Left", (-0.24, 0, 0.69), (0.35, 0.11, 0.06), materials["MAT_Ochre"], 0.025, (0, 0, math.radians(28)))
    add_box(root, "Recovery Accent Right", (0.24, 0, 0.69), (0.35, 0.11, 0.06), materials["MAT_Ochre"], 0.025, (0, 0, math.radians(28)))
    return root


def build_extinguisher(materials):
    root = create_root("FireExtinguisher")
    add_cylinder(root, "Rounded Red Tank", (0, 0, 0.38), 0.22, 0.64, materials["MAT_Red"])
    add_sphere(root, "Tank Shoulder", (0, 0, 0.67), (0.22, 0.22, 0.16), materials["MAT_Red"])
    add_box(root, "Metal Valve", (0, 0, 0.82), (0.15, 0.16, 0.12), materials["MAT_SteelLight"], 0.035)
    add_box(root, "Safety Handle", (0.10, 0, 0.91), (0.30, 0.09, 0.08), materials["MAT_SteelDark"], 0.025, (0, 0, math.radians(-8)))
    add_torus(root, "Short Black Hose", (0.27, 0, 0.62), 0.24, 0.035, materials["MAT_DeepInset"], rotation=(math.radians(90), 0, 0))
    add_cone(root, "Hose Nozzle", (0.43, 0, 0.43), 0.07, 0.035, 0.24, materials["MAT_DeepInset"], rotation=(0, math.radians(90), 0), bevel=0.015)
    add_box(root, "Cream Safety Label", (0, 0.215, 0.40), (0.22, 0.025, 0.22), materials["MAT_Cream"], 0.025)
    return root


def build_extinguisher_cabinet(materials):
    root = create_root("ExtinguisherCabinet")
    add_counter_base(root, materials)
    add_box(root, "Red Safety Backboard", (0, -0.48, 0.93), (0.92, 0.15, 0.84), materials["MAT_Red"], 0.08)
    add_box(root, "Cabinet Recess", (0, -0.37, 0.88), (0.62, 0.09, 0.55), materials["MAT_DeepInset"], 0.055)
    add_cylinder(root, "Cabinet Red Tank", (0, -0.25, 0.76), 0.12, 0.36, materials["MAT_Red"])
    add_box(root, "Cabinet Metal Valve", (0, -0.25, 0.99), (0.10, 0.10, 0.08), materials["MAT_SteelLight"], 0.025)
    add_torus(root, "Cabinet Short Hose", (0.15, -0.25, 0.79), 0.14, 0.025, materials["MAT_DeepInset"], rotation=(math.radians(90), 0, 0))
    return root


def build_washing_sink(materials):
    root = create_root("WashingSink")
    add_counter_base(root, materials)
    add_box(root, "Deep Sink Basin", (0, 0, 0.53), (1.05, 0.90, 0.24), materials["MAT_SteelDark"], 0.10)
    add_box(root, "Visible Water Surface", (0, 0, 0.66), (0.82, 0.67, 0.035), materials["MAT_Water"], 0.025)
    add_torus(root, "Tall Faucet", (0, -0.38, 0.91), 0.25, 0.055, materials["MAT_SteelLight"], rotation=(math.radians(90), 0, 0))
    add_cylinder(root, "Faucet Base", (0, -0.38, 0.74), 0.09, 0.24, materials["MAT_SteelLight"])
    add_sphere(root, "Left Soap Bubble", (-0.28, 0.10, 0.75), (0.10, 0.10, 0.10), materials["MAT_Water"])
    add_sphere(root, "Right Soap Bubble", (0.28, -0.02, 0.78), (0.13, 0.13, 0.13), materials["MAT_Water"])
    return root


def build_plate_dispenser(materials):
    root = create_root("PlateDispenser")
    add_counter_base(root, materials)
    add_box(root, "Plate Rack Back", (0, -0.46, 0.88), (1.02, 0.16, 0.72), materials["MAT_Teal"], 0.08)
    for index in range(4):
        z = 0.58 + index * 0.095
        add_cylinder(root, f"Clean Reusable Plate {index + 1}", (0, 0.02, z), 0.38, 0.055, materials["MAT_Cream"])
        add_torus(root, f"Turquoise Plate Rim {index + 1}", (0, 0.02, z + 0.033), 0.29, 0.035, materials["MAT_Teal"])
    return root


def build_dish_return(materials):
    root = create_root("DishReturn")
    add_counter_base(root, materials)
    add_box(root, "Dirty Dish Return Opening", (0, -0.18, 0.63), (0.95, 0.60, 0.18), materials["MAT_DeepInset"], 0.07)
    add_cylinder(root, "Returned Plate", (0, -0.05, 0.76), 0.38, 0.06, materials["MAT_Cream"])
    add_torus(root, "Returned Plate Rim", (0, -0.05, 0.80), 0.29, 0.035, materials["MAT_Teal"])
    add_sphere(root, "Returned Plate Mark", (0.05, -0.05, 0.835), (0.18, 0.12, 0.025), materials["MAT_DeepInset"], rotation=(0, 0, math.radians(20)))
    return root


def build_courier_shelf(materials):
    root = create_root("CourierShelf")
    add_counter_base(root, materials)
    add_box(root, "Courier Pickup Arch Left", (-0.55, -0.25, 0.96), (0.16, 0.18, 0.86), materials["MAT_Delivery"], 0.07)
    add_box(root, "Courier Pickup Arch Right", (0.55, -0.25, 0.96), (0.16, 0.18, 0.86), materials["MAT_Delivery"], 0.07)
    add_box(root, "Courier Pickup Arch Top", (0, -0.25, 1.34), (1.18, 0.18, 0.16), materials["MAT_Delivery"], 0.07)
    add_box(root, "Insulated Carrier", (0, 0.05, 0.69), (0.72, 0.60, 0.34), materials["MAT_Cream"], 0.10)
    add_box(root, "Carrier Teal Corners", (0, 0.05, 0.89), (0.76, 0.64, 0.10), materials["MAT_Teal"], 0.045)
    return root


def build_dynamic_bridge(materials):
    root = create_root("DynamicBridge")
    add_box(root, "Moving Connector Platform", (0, 0, 0.02), (1.48, 1.22, 0.25), materials["MAT_Steel"], 0.08)
    add_box(root, "Left Warning Rail", (-0.62, 0, 0.22), (0.13, 1.08, 0.22), materials["MAT_Ochre"], 0.05)
    add_box(root, "Right Warning Rail", (0.62, 0, 0.22), (0.13, 1.08, 0.22), materials["MAT_Ochre"], 0.05)
    for index, x in enumerate((-0.36, 0, 0.36), 1):
        add_box(root, f"Connector Direction Mark {index}", (x, 0, 0.16), (0.16, 0.78, 0.035), materials["MAT_Teal"], 0.025, (0, 0, math.radians(25)))
    return root


def build_lettuce_raw(materials):
    root = create_root("LettuceRaw")
    add_sphere(root, "Pale Lettuce Heart", (0, 0, 0.28), (0.34, 0.34, 0.29),
               materials["MAT_LettuceLight"])
    for index in range(10):
        angle = math.radians(index * 36 + 18)
        radius = 0.20 if index % 2 == 0 else 0.24
        position = (math.cos(angle) * radius, math.sin(angle) * radius, 0.25)
        material = materials["MAT_Lettuce"] if index % 3 else materials["MAT_LettuceDark"]
        add_sphere(root, f"Layered Lettuce Leaf {index + 1}", position,
                   (0.26, 0.16, 0.25), material, (0, 0, angle))
        rib_start = (math.cos(angle) * 0.055, math.sin(angle) * 0.055, 0.49)
        rib_mid = (math.cos(angle) * radius * 0.62, math.sin(angle) * radius * 0.62, 0.50)
        rib_end = (math.cos(angle) * radius * 1.28, math.sin(angle) * radius * 1.28, 0.43)
        add_tube(root, f"Lettuce Leaf Rib {index + 1}",
                 (rib_start, rib_mid, rib_end), 0.018, materials["MAT_LettuceLight"])
    add_cylinder(root, "Lettuce Core", (0, -0.18, 0.08), 0.12, 0.13,
                 materials["MAT_LettuceLight"], rotation=(math.radians(90), 0, 0))
    return root


def build_lettuce_chopped(materials):
    root = create_root("LettuceChopped")
    leaves = ((-0.23, -0.10, -18), (0, -0.13, 21), (0.23, -0.04, -34), (-0.12, 0.14, 42), (0.15, 0.14, 12))
    for index, (x, y, angle_degrees) in enumerate(leaves, 1):
        angle = math.radians(angle_degrees)
        material = materials["MAT_Lettuce"] if index % 2 else materials["MAT_LettuceDark"]
        add_sphere(root, f"Chopped Lettuce Leaf {index}", (x, y, 0.10),
                   (0.25, 0.15, 0.065), material, (0, 0, angle))
        rib_offset = 0.025
        rib_center = (x + math.cos(angle) * rib_offset, y + math.sin(angle) * rib_offset, 0.168)
        add_sphere(root, f"Chopped Lettuce Pale Rib {index}", rib_center,
                   (0.13, 0.027, 0.014), materials["MAT_LettuceLight"], (0, 0, angle))
    return root


def build_carrot_raw(materials):
    root = create_root("CarrotRaw")
    add_cone(root, "Tapered Whole Carrot", (0, -0.04, 0.18), 0.22, 0.035, 0.84,
             materials["MAT_Carrot"], rotation=(math.radians(90), 0, 0), bevel=0.035)
    add_sphere(root, "Rounded Carrot Shoulder", (0, 0.37, 0.18), (0.21, 0.13, 0.18), materials["MAT_Carrot"])
    for index, (y, ring_radius) in enumerate(((-0.28, 0.075), (-0.14, 0.105), (0.01, 0.135), (0.16, 0.165)), 1):
        add_torus(root, f"Carrot Growth Groove {index}", (0, y, 0.18), ring_radius, 0.012,
                  materials["MAT_CarrotGroove"], rotation=(math.radians(90), 0, 0))
    for index, (x, angle, scale) in enumerate((
        (-0.15, -33, (0.085, 0.28, 0.060)),
        (-0.08, -14, (0.090, 0.31, 0.065)),
        (0.0, 3, (0.095, 0.33, 0.070)),
        (0.09, 20, (0.090, 0.30, 0.065)),
        (0.16, 36, (0.080, 0.26, 0.055)),
    ), 1):
        add_sphere(root, f"Carrot Leaf {index}", (x, 0.66, 0.25), scale,
                   materials["MAT_Leaf"], (0, 0, math.radians(angle)))
    return root


def build_carrot_chopped(materials):
    root = create_root("CarrotChopped")
    for index, (x, y, angle_degrees) in enumerate(((-0.22, -0.11, 19), (0.03, -0.14, -12), (0.24, 0.03, 34), (-0.06, 0.17, -27)), 1):
        angle = math.radians(angle_degrees)
        add_box(root, f"Rounded Carrot Segment {index}", (x, y, 0.105), (0.26, 0.23, 0.19),
                materials["MAT_Carrot"], 0.075, (0, 0, angle))
        add_box(root, f"Carrot Segment Groove {index}", (x, y, 0.207), (0.17, 0.025, 0.014),
                materials["MAT_CarrotGroove"], 0.006, (0, 0, angle))
    return root


def build_onion_raw(materials):
    # Share the detailed reference model with the focused onion-only build.
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    from onion_reference import build
    return build(materials, create_root, add_sphere)


def build_onion_chopped(materials):
    root = create_root("OnionChopped")
    wedge_points = ((0.0, 0.0), (0.25, 0.0), (0.24, 0.08), (0.20, 0.16), (0.13, 0.22), (0.0, 0.25))
    flesh_points = tuple((x * 0.86, y * 0.86) for x, y in wedge_points)
    for index, (x, y, angle_degrees) in enumerate(((-0.23, -0.10, 12), (0.03, -0.14, 101), (0.23, 0.05, 194), (-0.05, 0.17, 278)), 1):
        angle = math.radians(angle_degrees)
        skin = add_extruded_polygon(root, f"Onion Wedge Skin {index}", wedge_points, 0.08, 0.12,
                                    materials["MAT_OnionSkin"], 0.035)
        skin.location = (x, y, 0)
        skin.rotation_euler.z = angle
        flesh = add_extruded_polygon(root, f"Onion Wedge Flesh {index}", flesh_points, 0.155, 0.06,
                                     materials["MAT_OnionFlesh"], 0.025)
        flesh.location = (x, y, 0)
        flesh.rotation_euler.z = angle
    return root


def build_beef_raw(materials):
    root = create_root("BeefRaw")
    add_sphere(root, "Raw Ground Beef Base", (0, 0, 0.11), (0.44, 0.38, 0.12), materials["MAT_BeefRaw"])
    for index, (x, y, scale) in enumerate((
        (-0.26, -0.10, (0.14, 0.12, 0.065)), (-0.10, -0.19, (0.13, 0.11, 0.060)),
        (0.09, -0.18, (0.15, 0.11, 0.065)), (0.26, -0.08, (0.13, 0.12, 0.060)),
        (-0.28, 0.08, (0.12, 0.13, 0.060)), (-0.10, 0.02, (0.15, 0.14, 0.075)),
        (0.11, 0.02, (0.14, 0.13, 0.070)), (0.28, 0.10, (0.12, 0.12, 0.055)),
        (-0.16, 0.18, (0.14, 0.11, 0.060)), (0.08, 0.19, (0.15, 0.11, 0.060)),
    ), 1):
        add_sphere(root, f"Raw Mince Lobe {index}", (x, y, 0.21), scale, materials["MAT_BeefRaw"])
    marbling_paths = (
        ((-0.31, -0.09, 0.255), (-0.18, -0.02, 0.275), (-0.04, -0.08, 0.275)),
        ((-0.12, 0.15, 0.275), (0.02, 0.09, 0.285), (0.18, 0.16, 0.265)),
        ((0.08, -0.15, 0.265), (0.20, -0.07, 0.275), (0.31, 0.00, 0.245)),
    )
    for index, points in enumerate(marbling_paths, 1):
        add_tube(root, f"Raw Beef Fat Marbling {index}", points, 0.018, materials["MAT_BeefFat"])
    return root


def build_beef_chopped(materials):
    root = create_root("BeefChopped")
    add_sphere(root, "Formed Raw Hamburger Patty", (0, 0, 0.12), (0.45, 0.39, 0.12),
               materials["MAT_BeefRaw"])
    for index in range(12):
        angle = math.radians(index * 30)
        add_sphere(root, f"Patty Mince Edge {index + 1}",
                   (math.cos(angle) * 0.39, math.sin(angle) * 0.34, 0.13),
                   (0.075, 0.065, 0.055), materials["MAT_BeefRaw"])
    for index, points in enumerate((
        ((-0.28, -0.08, 0.245), (-0.13, -0.02, 0.255), (0.02, -0.07, 0.25)),
        ((-0.10, 0.14, 0.25), (0.05, 0.08, 0.26), (0.23, 0.14, 0.245)),
        ((0.10, -0.16, 0.245), (0.19, -0.07, 0.255), (0.31, -0.01, 0.24)),
    ), 1):
        add_tube(root, f"Patty Fat Marbling {index}", points, 0.016, materials["MAT_BeefFat"])
    return root


def build_beef_cooked(materials):
    root = create_root("BeefCooked")
    add_sphere(root, "Browned Hamburger Patty", (0, 0, 0.13), (0.45, 0.39, 0.13),
               materials["MAT_BeefCooked"])
    for index in range(12):
        angle = math.radians(index * 30)
        add_sphere(root, f"Cooked Patty Edge {index + 1}",
                   (math.cos(angle) * 0.39, math.sin(angle) * 0.34, 0.14),
                   (0.075, 0.065, 0.055), materials["MAT_BeefCooked"])
    for index, y in enumerate((-0.13, 0.0, 0.13), 1):
        add_box(root, f"Patty Grill Mark {index}", (0, y, 0.255), (0.54, 0.035, 0.026),
                materials["MAT_Sauce"], 0.010, (0, 0, math.radians(13)))
    return root


def descendants(root):
    result = [root]
    for child in root.children:
        result.extend(descendants(child))
    return result


def join_meshes_by_material(root):
    groups = {}
    for child in list(root.children):
        if child.type != "MESH" or not child.data.materials:
            continue
        groups.setdefault(child.data.materials[0].name, []).append(child)

    for material_name, objects in groups.items():
        if len(objects) == 1:
            objects[0].name = material_name + " Geometry"
            continue
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join()
        joined = bpy.context.object
        joined.name = material_name + " Geometry"
        joined.parent = root
        joined.select_set(False)


def join_all_meshes(root, name):
    objects = [child for child in root.children if child.type == "MESH"]
    if len(objects) <= 1:
        return
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    joined.parent = root
    joined.select_set(False)


def export_root(root, path):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in descendants(root):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=False,
        add_leaf_bones=False,
        mesh_smooth_type="FACE",
        path_mode="AUTO",
    )


def duplicate_hierarchy(source, parent=None):
    duplicate = source.copy()
    if source.data is not None:
        duplicate.data = source.data
    bpy.context.collection.objects.link(duplicate)
    duplicate.parent = parent
    duplicate.hide_render = False
    for child in source.children:
        duplicate_hierarchy(child, duplicate)
    return duplicate


def point_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_preview(roots, materials, preview_path):
    for source_root in roots.values():
        for source_object in descendants(source_root):
            source_object.hide_render = True

    preview_positions = {
        "FloorTileA": (-4.8, 2.5, 0.05),
        "Counter": (-3.2, 2.5, 0.45),
        "IngredientCrate": (-1.6, 2.5, 0.45),
        "ChoppingBoard": (0.0, 2.5, 0.45),
        "PotHeatSource": (1.6, 2.5, 0.45),
        "AssemblyCounter": (3.2, 2.5, 0.45),
        "ContainerDispenser": (4.8, 2.5, 0.45),
        "ServingHatch": (-4.0, 0.55, 0.45),
        "TrashBin": (-2.4, 0.55, 0.45),
        "BikeDock": (-0.8, 0.55, 0.45),
        "DeliveryPoint": (0.8, 0.55, 0.45),
        "RecoveryBin": (2.4, 0.55, 0.45),
        "Wall": (4.0, 0.55, 0.55),
        "ExtinguisherCabinet": (-4.8, -1.05, 0.45),
        "WashingSink": (-3.2, -1.05, 0.45),
        "PlateDispenser": (-1.6, -1.05, 0.45),
        "DishReturn": (0.0, -1.05, 0.45),
        "CourierShelf": (1.6, -1.05, 0.45),
        "DynamicBridge": (3.2, -1.05, 0.45),
        "FireExtinguisher": (4.8, -1.05, 0.02),
        "LettuceRaw": (-4.8, -2.65, 0.02),
        "LettuceChopped": (-3.6, -2.65, 0.02),
        "CarrotRaw": (-2.4, -2.65, 0.02),
        "CarrotChopped": (-1.2, -2.65, 0.02),
        "OnionRaw": (0.0, -2.65, 0.02),
        "OnionChopped": (1.2, -2.65, 0.02),
        "BeefRaw": (2.4, -2.65, 0.02),
        "BeefChopped": (3.6, -2.65, 0.02),
        "BeefCooked": (4.8, -2.65, 0.02),
    }
    for name, location in preview_positions.items():
        duplicate = duplicate_hierarchy(roots[name])
        duplicate.name = "Preview_" + name
        duplicate.location = location

    add_box(create_root("PreviewFloor"), "Preview Floor", (0, -0.05, -0.15), (14.0, 10.2, 0.12), materials["MAT_FloorB"], 0.04)
    bpy.context.object.parent.hide_render = False

    # Match the Unity kitchen camera side after the Blender Z-up conversion.
    bpy.ops.object.camera_add(location=(9.8, 13.6, 15.8))
    camera = bpy.context.object
    point_at(camera, (0, 0.55, 0.42))
    camera.data.lens = 58
    bpy.context.scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-4.0, -6.0, 13.0))
    key = bpy.context.object
    key.data.energy = 1650
    key.data.shape = "DISK"
    key.data.size = 7.0
    point_at(key, (0, 0.5, 0.2))
    bpy.ops.object.light_add(type="AREA", location=(7.0, 3.0, 8.0))
    fill = bpy.context.object
    fill.data.energy = 950
    fill.data.size = 6.0
    point_at(fill, (0, 0.5, 0.4))

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(preview_path)
    scene.world.color = (0.055, 0.043, 0.040)
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


def setup_food_preview(roots, materials, preview_path):
    for source_root in roots.values():
        for source_object in descendants(source_root):
            source_object.hide_render = True

    lineup = (
        ("LettuceRaw", -3.6, 1.0), ("CarrotRaw", -1.2, 1.0),
        ("OnionRaw", 1.2, 1.0), ("BeefRaw", 3.6, 1.0),
        ("LettuceChopped", -3.6, -1.0), ("CarrotChopped", -1.2, -1.0),
        ("OnionChopped", 1.2, -1.0), ("BeefChopped", 3.1, -1.0),
        ("BeefCooked", 4.35, -1.0),
    )
    for name, x, y in lineup:
        duplicate = duplicate_hierarchy(roots[name])
        duplicate.name = "Preview_" + name
        duplicate.location = (x, y, 0.03)
        duplicate.scale = (1.45, 1.45, 1.45)

    add_box(create_root("FoodPreviewFloor"), "Food Preview Floor", (0, 0, -0.14),
            (12.5, 6.0, 0.12), materials["MAT_Cream"], 0.05)
    bpy.context.object.parent.hide_render = False

    bpy.ops.object.camera_add(location=(10.8, -15.0, 14.5))
    camera = bpy.context.object
    point_at(camera, (0, 0, 0.25))
    camera.data.lens = 54
    bpy.context.scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-4.5, -5.0, 12.0))
    key = bpy.context.object
    key.data.energy = 1700
    key.data.shape = "DISK"
    key.data.size = 7.5
    point_at(key, (0, 0, 0.2))
    bpy.ops.object.light_add(type="AREA", location=(6.0, 4.0, 8.0))
    fill = bpy.context.object
    fill.data.energy = 900
    fill.data.size = 6.0
    point_at(fill, (0, 0, 0.3))

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(preview_path)
    scene.world.color = (0.055, 0.043, 0.040)
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


def main():
    args = parse_args()
    blend_path = Path(args.blend).resolve()
    model_dir = Path(args.model_dir).resolve()
    preview_path = Path(args.preview).resolve()
    blend_path.parent.mkdir(parents=True, exist_ok=True)
    model_dir.mkdir(parents=True, exist_ok=True)
    preview_path.parent.mkdir(parents=True, exist_ok=True)

    reset_scene()
    materials = {name: make_material(name, color) for name, color in COLORS.items()}
    roots = {
        "FloorTileA": build_floor("FloorTileA", materials["MAT_FloorA"]),
        "FloorTileB": build_floor("FloorTileB", materials["MAT_FloorB"]),
        "Wall": build_wall(materials),
        "Counter": build_counter(materials),
        "IngredientCrate": build_ingredient_crate(materials),
        "ChoppingBoard": build_chopping_station(materials),
        "PotHeatSource": build_pot_heat(materials),
        "AssemblyCounter": build_assembly(materials),
        "ContainerDispenser": build_container_dispenser(materials),
        "ServingHatch": build_serving_hatch(materials),
        "TrashBin": build_trash(materials),
        "BikeDock": build_bike_dock(materials),
        "DeliveryPoint": build_delivery_point(materials),
        "RecoveryBin": build_recovery(materials),
        "ExtinguisherCabinet": build_extinguisher_cabinet(materials),
        "WashingSink": build_washing_sink(materials),
        "PlateDispenser": build_plate_dispenser(materials),
        "DishReturn": build_dish_return(materials),
        "CourierShelf": build_courier_shelf(materials),
        "DynamicBridge": build_dynamic_bridge(materials),
        "FireExtinguisher": build_extinguisher(materials),
        "LettuceRaw": build_lettuce_raw(materials),
        "LettuceChopped": build_lettuce_chopped(materials),
        "CarrotRaw": build_carrot_raw(materials),
        "CarrotChopped": build_carrot_chopped(materials),
        "OnionRaw": build_onion_raw(materials),
        "OnionChopped": build_onion_chopped(materials),
        "BeefRaw": build_beef_raw(materials),
        "BeefChopped": build_beef_chopped(materials),
        "BeefCooked": build_beef_cooked(materials),
    }

    for name, root in roots.items():
        join_meshes_by_material(root)
        if name == "PotHeatSource":
            # Preserve all material slots in one renderer. The gas ring gains
            # clarity without increasing draw objects beyond the mobile budget.
            join_all_meshes(root, "PotHeatSource Combined Geometry")

    for name, root in roots.items():
        export_root(root, model_dir / f"{name}.fbx")

    if "food_card_matched" in preview_path.stem:
        setup_food_preview(roots, materials, preview_path)
    else:
        setup_preview(roots, materials, preview_path)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    print(f"Saved Blender source: {blend_path}")
    print(f"Saved {len(roots)} FBX models: {model_dir}")
    print(f"Saved preview: {preview_path}")


if __name__ == "__main__":
    main()
