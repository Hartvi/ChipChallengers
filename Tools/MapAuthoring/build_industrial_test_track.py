"""Build, export and render a basic industrial driving circuit with Blender 3.6."""
import bpy
import math
import json
import os
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = Path('C:/Users/Acer/BlenderTests/industrial-test-track')
MAPS = ROOT / 'Assets/StreamingAssets/Maps'
OUT.mkdir(parents=True, exist_ok=True)
MAPS.mkdir(parents=True, exist_ok=True)
REVISION = int(os.environ.get('TRACK_REVISION', '3'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
geometry = []

def material(name, color, roughness=0.8):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = roughness
    return mat

asphalt = material('Asphalt', (0.055, 0.075, 0.09))
concrete = material('Concrete', (0.39, 0.42, 0.40))
shoulder = material('Runoff', (0.23, 0.25, 0.22))
grass = material('Dry grass', (0.24, 0.30, 0.18))
yellow = material('Safety yellow', (0.95, 0.64, 0.06))
white = material('Road white', (0.83, 0.85, 0.79))
orange = material('Signal orange', (0.76, 0.22, 0.055))
steel = material('Dark steel', (0.11, 0.16, 0.19), 0.55)
roof = material('Blue grey roofs', (0.17, 0.27, 0.32))
window = material('Blue windows', (0.12, 0.28, 0.34), 0.25)

def box(name, location, size, mat, angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    obj.rotation_euler.z = angle
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    geometry.append(obj)
    return obj

def cylinder(name, location, radius, depth, mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    geometry.append(obj)
    return obj

# The origin lies on the flat start straight; Blender -Y becomes game +Z.
controls = [Vector(p) for p in [
    (0,0,0.35), (0,-75,0.35), (35,-135,0.35), (105,-145,2),
    (160,-100,7), (155,-30,5), (120,5,0.35), (125,65,0.35),
    (95,120,0.35), (25,170,0.35), (-55,155,0.35), (-70,100,0.35), (-25,70,0.35), (0,40,0.35)]]

def sample(segment, t):
    p0,p1,p2,p3 = [controls[(segment+i) % len(controls)] for i in (-1,0,1,2)]
    return 0.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)

# Resample near-uniformly rather than putting many tiny faces in slow curves.
raw = [sample(i, j/100) for i in range(len(controls)) for j in range(100)]
for p in raw: p.z = max(0.35, p.z)
points = [raw[0]]
for p in raw[1:]:
    if (p-points[-1]).length >= 1.2:
        points.append(p)
if (points[-1]-points[0]).length < 0.6:
    points.pop()
N = len(points)
bands = [-18,-10,-8,-6,-5.7,-0.12,0.12,5.7,6,8,10,18]
verts, faces, indices = [], [], []
road_mats = [asphalt, concrete, shoulder, grass, yellow, white]
lengths = [0.0]
for i in range(1,N):
    lengths.append(lengths[-1] + (points[i]-points[i-1]).length)
for i,p in enumerate(points):
    tangent = points[(i+1)%N]-points[(i-1)%N]
    right = Vector((tangent.y, -tangent.x, 0)).normalized()
    for offset in bands:
        v = p + right*offset
        v.z = p.z if abs(offset) <= 8 else max(0, p.z)*(18-abs(offset))/10
        verts.append(tuple(v))
for i in range(N):
    for j in range(len(bands)-1):
        a,b = i*len(bands)+j, ((i+1)%N)*len(bands)+j
        faces.append((a,a+1,b+1,b))
        mid = abs((bands[j]+bands[j+1])*0.5)
        if mid > 10: mat_index = 3
        elif mid > 8: mat_index = 2
        elif mid > 6: mat_index = 1
        elif mid > 5.7: mat_index = 4
        elif mid < 0.13 and int(lengths[i]/4)%2 == 0: mat_index = 5
        else: mat_index = 0
        if i == 0 and mid < 5.7: mat_index = 5
        indices.append(mat_index)
mesh = bpy.data.meshes.new('Continuous driving surface')
mesh.from_pydata(verts, [], faces)
mesh.materials.clear()
for mat in road_mats: mesh.materials.append(mat)
mesh.update()
road = bpy.data.objects.new('Circuit and graded runoff', mesh)
bpy.context.collection.objects.link(road)
geometry.append(road)
for poly,idx in zip(mesh.polygons,indices): poly.material_index = idx
if any(p.normal.z < 0 for p in mesh.polygons):
    raise RuntimeError('Folded runoff: ' + str([tuple(round(v, 1) for v in p.center) for p in mesh.polygons if p.normal.z < 0][:6]))
# All paint is assigned to road faces: no raised markings or overlapping collision surfaces.
box('Site foundation', (50,15,-1), (350,400,2), grass)

# Pit buildings stay well clear of the start straight and recovery space.
for y in [-22,-43,-64]:
    box('Pit garage', (-31,y,4.0), (17,15,8), concrete)
    box('Garage roof', (-31,y,8.25), (18,16,0.5), roof)
    box('Garage door', (-22.43,y,3.0), (0.12,9,5.2), steel)
    box('Garage header', (-22.3,y,6.2), (0.2,10,0.35), yellow)
# A distinctive observation tower anchors the circuit visually.
box('Tower base', (62,-62,6), (9,9,12), concrete)
box('Observation room', (62,-62,14), (13,13,4), window)
box('Tower cap', (62,-62,16.4), (15,15,0.8), roof)
for x,y in [(65,55),(79,55),(93,55)]:
    cylinder('Storage tank', (x,y,5), 5, 10, concrete)
    cylinder('Tank top', (x,y,10.25), 5.2, 0.5, steel)
# Open start marker: nothing crosses above the spawn ray at world origin.
for x in [-9,9]:
    box('Start beacon', (x,-5,3.5), (0.7,0.7,7), orange)
    box('Beacon cap', (x,-5,7.3), (1.1,1.1,0.6), yellow)

# Sparse outside barriers on the elevated section only; smooth runoff elsewhere.
for i in range(N):
    if 225 < lengths[i] < 450 and i % 5 == 0:
        p = points[i]
        tangent = (points[(i+1)%N]-points[(i-1)%N]).normalized()
        right = Vector((tangent.y,-tangent.x,0)).normalized()
        pos = p + right*8.5
        pos.z = p.z+0.65
        box('Hill safety barrier', pos, (4.2,0.45,1.3), concrete, math.atan2(tangent.y,tangent.x))

# Revision two adds readable corner markers, following the first render review.
if REVISION >= 2:
    for segment in [2,4,6,7,9,10]:
        p = sample(segment,0.5)
        direction = sample(segment,0.51)-sample(segment,0.49)
        right = Vector((direction.y,-direction.x,0)).normalized()
        for sign in [-1,1]:
            location = p + right*11*sign
            location.z = p.z+1.1
            box('Corner marker', location, (0.5,0.5,2.2), orange)
            location.z += 0.7
            box('Marker band', location, (0.52,0.52,0.25), white)

# Export only gameplay meshes, with transforms applied and triangulated faces.
bpy.ops.object.select_all(action='DESELECT')
for obj in geometry: obj.select_set(True)
bpy.context.view_layer.objects.active = road
bpy.ops.export_scene.obj(filepath=str(MAPS/'industrial_test_track.obj'), use_selection=True,
    axis_forward='-Z', axis_up='Y', use_materials=True, use_triangles=True,
    use_uvs=False, use_normals=True, path_mode='STRIP')

# Lighting and cameras are authoring-only and do not enter the exported map.
world = bpy.context.scene.world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.55,0.68,0.82,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.55
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(24), math.radians(-18), math.radians(-30)))
bpy.context.object.data.energy = 2.3
bpy.context.object.data.angle = math.radians(12)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.render.resolution_x = 1100
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'Medium High Contrast'
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1

def render(name, location, target, ortho=None):
    bpy.ops.object.camera_add(location=location)
    camera = bpy.context.object
    camera.name = name
    camera.rotation_euler = (Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    if ortho:
        camera.data.type = 'ORTHO'
        camera.data.ortho_scale = ortho
    else:
        camera.data.lens = 26
    camera.data.clip_end = 2000
    scene.camera = camera
    scene.render.filepath = str(OUT/(name+f'_v{REVISION}.png'))
    bpy.ops.render.render(write_still=True)

render('overview', (345,-395,365), (50,10,0), 465)
render('layout', (50,10,500), (50,10,0), 450)
render('start_straight', (0,35,3), (0,-60,2))
scene.camera = bpy.data.objects['overview']
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'industrial_test_track.blend'))
triangles = sum(len(p.vertices)-2 for obj in geometry for p in obj.data.polygons)
road.data.update()
folded_faces = sum(p.normal.z < 0 for p in road.data.polygons)
max_grade = max(abs((points[(i+1)%N].z-points[i].z)/max(1e-6,(points[(i+1)%N].xy-points[i].xy).length)) for i in range(N))
(OUT/'summary.json').write_text(json.dumps({'revision':REVISION, 'lap_length_metres':round(lengths[-1]+(points[-1]-points[0]).length,1), 'road_width_metres':12, 'maximum_elevation_metres':round(max(p.z for p in points),2), 'triangles':triangles, 'folded_road_faces':folded_faces, 'maximum_grade_percent':round(max_grade*100,2), 'mesh_objects':len(geometry), 'map_path':str(MAPS/'industrial_test_track.obj')}, indent=2))
print('MAP_OUTPUT: '+str(MAPS/'industrial_test_track.obj'))
