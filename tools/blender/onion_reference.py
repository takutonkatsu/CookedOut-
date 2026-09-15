"""Reference-matched onion, shared by the kitchen exporter and standalone authoring.

Run with Blender --background --python tools/blender/onion_reference.py.
The supplied reference is visual guidance; no photograph is projected onto the mesh.
"""
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

PROJECT = Path(__file__).resolve().parents[2]
TEXTURE = PROJECT / "Assets/_CookedOut/Art/KitchenProduction/Textures/onion_reference_skin_sv1.png"

# Grounded roots, full rounded lower body, continuous shoulders and a short pinched neck.
PROFILE = ((.065, .055), (.09, .15), (.135, .235), (.21, .333),
           (.30, .396), (.40, .423), (.49, .416), (.58, .378),
           (.665, .310), (.735, .215), (.79, .132), (.84, .09),
           (.895, .080), (.921, .073), (.935, .059), (.944, .033), (.948, .001))


def radius_at(z):
    for i in range(len(PROFILE) - 1):
        if z <= PROFILE[i + 1][0]:
            z0, r0 = PROFILE[i]
            z1, r1 = PROFILE[i + 1]
            previous = PROFILE[max(0, i - 1)]
            following = PROFILE[min(len(PROFILE) - 1, i + 2)]
            m0 = (r1 - previous[1]) / (z1 - previous[0])
            m1 = (following[1] - r0) / (following[0] - z0)
            t = (z - z0) / (z1 - z0)
            return ((2*t**3 - 3*t*t + 1)*r0 + (t**3 - 2*t*t + t)*(z1-z0)*m0
                    + (-2*t**3 + 3*t*t)*r1 + (t**3 - t*t)*(z1-z0)*m1)
    return PROFILE[-1][1]


def skin_material():
    """Original UV texture: broad golden layers and restrained longitudinal fibres."""
    size = 1024
    u, v = np.meshgrid(np.arange(size)/size, np.arange(size)/(size-1))
    theta = u * math.tau
    warp = theta + .016*np.sin(v*7 + theta*3)
    seam = np.exp(-((np.sin(warp*11))/.14)**2)
    fibre = .003*np.sin(warp*173 + np.sin(v*17)*.6) + .002*np.sin(warp*317 + v*13)
    bands = .012*np.cos(warp*11) + .006*np.sin(warp*7 + .8)
    lower = -.065*np.exp(-v*13)
    variation = bands + fibre - .035*seam + lower
    rgb = np.stack([.965 + variation*.65, .680 + variation, .295 + variation*.7], axis=-1)
    rgb = np.clip(rgb, 0, 1)
    # Byte-backed generated images use their declared sRGB color space. Keep these
    # authored sRGB values intact so Unity and Blender sample the same albedo.
    pixels = np.concatenate([rgb, np.ones((size, size, 1))], axis=-1).astype(np.float32)
    texture = bpy.data.images.new("Onion Reference Skin Albedo", size, size, alpha=False)
    texture.pixels.foreach_set(pixels.ravel())
    TEXTURE.parent.mkdir(parents=True, exist_ok=True)
    texture.filepath_raw = str(TEXTURE)
    texture.file_format = "PNG"
    texture.save()
    texture.pack()
    material = bpy.data.materials.new("MAT_OnionReferenceSkin")
    material.diffuse_color = (.965, .665, .270, 1)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    shader = nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = .34
    shader.inputs["Specular IOR Level"].default_value = .42
    image = nodes.new("ShaderNodeTexImage")
    image.image = texture
    material.node_tree.links.new(image.outputs["Color"], shader.inputs["Base Color"])
    return material


def build(materials, create_root, add_sphere):
    root = create_root("OnionRaw")
    material = skin_material()
    radial, rows = 112, 42
    vertices, faces, face_uvs = [], [], []
    for j in range(rows):
        # Reserve close rings for the rounded crown instead of a flat triangle fan.
        z = (.065 + (j/33)*(.84-.065)) if j <= 33 else (.84 + ((j-33)/8)*(.948-.84))
        t = (z-PROFILE[0][0])/(PROFILE[-1][0]-PROFILE[0][0])
        radius = radius_at(z)
        neck = max(0, (z-.75)/.181)**2
        for i in range(radial):
            theta = i/radial*math.tau
            warp = theta + .016*math.sin(t*7 + theta*3)
            groove = math.exp(-(math.sin(warp*11)/.20)**2)
            r = radius*(1 + .008*math.cos(theta*7) + neck*.10*math.cos(theta*5))
            r -= .0022*groove*min(1, radius/.15)
            # Tiny irregularity in the crown gives the pinched, rounded cut stem.
            height = z + neck*min(1,radius/.06)*(.006*math.sin(theta*3+.4) + .003*math.sin(theta*5))
            vertices.append((r*math.cos(theta), r*math.sin(theta), height))
    for j in range(rows-1):
        for i in range(radial):
            n = (i+1) % radial
            faces.append((j*radial+i, j*radial+n, (j+1)*radial+n, (j+1)*radial+i))
            v0 = (vertices[j*radial][2]-.065)/(.948-.065)
            v1 = (vertices[(j+1)*radial][2]-.065)/(.948-.065)
            face_uvs.append(((i/radial,v0),((i+1)/radial,v0),((i+1)/radial,v1),(i/radial,v1)))
    for top in (False, True):
        start = (rows-1)*radial if top else 0
        center = len(vertices)
        vertices.append((0, 0, .948 if top else .065))
        for i in range(radial):
            a, b = start+i, start+(i+1)%radial
            faces.append((a,b,center) if top else (b,a,center))
            face_uvs.append(((i/radial,1 if top else 0),((i+1)/radial,1 if top else 0),
                             ((i+.5)/radial,1 if top else 0)))
    mesh = bpy.data.meshes.new("Continuous Onion Bulb Neck and Skin")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="Onion Skin UV")
    for polygon, coords in zip(mesh.polygons, face_uvs):
        polygon.use_smooth = True
        for loop, coord in zip(polygon.loop_indices, coords):
            uv.data[loop].uv = coord
    bulb = bpy.data.objects.new("MAT_OnionReferenceSkin Geometry", mesh)
    bpy.context.collection.objects.link(bulb)
    bulb.parent = root
    bulb.data.materials.append(material)
    root_material = bpy.data.materials.new("MAT_OnionReferenceRoot")
    root_material.use_nodes = True
    root_material.diffuse_color = (.73,.42,.14,1)
    root_shader = root_material.node_tree.nodes.get("Principled BSDF")
    root_shader.inputs["Base Color"].default_value = (.49,.22,.054,1)
    root_shader.inputs["Roughness"].default_value = .45
    for i in range(9):
        angle = i*math.tau/9
        # Small rounded, downward-folded roots, not long stringy fibres.
        add_sphere(root, f"Onion Folded Root {i+1}",
                   (.083*math.cos(angle), .083*math.sin(angle), .054 + .013*math.sin(i*2)),
                   (.029, .043, .040), root_material,
                   (math.radians(22), 0, angle), segments=16, rings=8)
    add_sphere(root, "Onion Root Heart", (0,0,.041), (.07,.065,.037),
               root_material, segments=16, rings=8)
    return root


def main():
    sys.path.insert(0, str(Path(__file__).parent))
    import generate_kitchen_assets as kitchen
    kitchen.reset_scene()
    materials = {name: kitchen.make_material(name,color) for name,color in kitchen.COLORS.items()}
    # Golden roots in the studio, matching their shared Unity material.
    root = build(materials, kitchen.create_root, kitchen.add_sphere)
    kitchen.join_meshes_by_material(root)
    model = PROJECT / "Assets/_CookedOut/Art/KitchenProduction/Models/OnionRaw.fbx"
    kitchen.export_root(root, model)
    for frame, scale, angle in ((1,(1,1,1),0),(5,(1.04,1.04,.92),-2),
                                (11,(.98,.98,1.04),1),(19,(1,1,1),0)):
        root.scale = scale
        root.rotation_euler.y = math.radians(angle)
        root.keyframe_insert("scale", frame=frame)
        root.keyframe_insert("rotation_euler", frame=frame)
    root.animation_data.action.name = "Onion Settle Authoring"
    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, 19
    scene.render.fps = 30
    scene.frame_set(19)
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.004))
    ground = bpy.context.object
    ground.name = "Studio Ground - not exported"
    ground.data.materials.append(kitchen.make_material("Studio Warm White", (.83,.80,.75,1)))
    bpy.ops.object.camera_add(location=(1.5,-4,1.6))
    camera = bpy.context.object
    kitchen.point_at(camera,(0,0,.47))
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 1.28
    scene.camera = camera
    for location, energy, size in (((-2,-3,4),400,2.3),((3,-1,2),30,3),((0,3,3),60,2.5)):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.data.energy, light.data.shape, light.data.size = energy,"DISK",size
        kitchen.point_at(light,(0,0,.4))
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (.78,.75,.70,1)
    background.inputs["Strength"].default_value = .3
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.resolution_x = scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    output = PROJECT / "docs/art/concepts/onion_reference_sv1_blender_front_v1.png"
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    blend = PROJECT / "art-source/kitchen/OnionReference.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    for name, angle in (("side",90),("back",180)):
        root.animation_data_clear()
        root.rotation_euler.z = math.radians(angle)
        scene.render.filepath = str(output.with_name(f"onion_reference_sv1_blender_{name}_v1.png"))
        bpy.ops.render.render(write_still=True)
    print("ONION_REFERENCE_EXPORTED", model)
    print("TRIANGLES", sum(len(p.vertices)-2 for obj in root.children if obj.type=="MESH" for p in obj.data.polygons))


if __name__ == "__main__":
    main()
