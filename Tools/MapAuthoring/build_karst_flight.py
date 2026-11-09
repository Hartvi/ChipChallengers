"""Build a sea-karst flight map, Lua hoop race and render previews with Blender 3.6."""
import bpy
import math
import random
import json
import uuid
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
OUT = Path('C:/Users/Acer/BlenderTests/karst-flight')
MAPS = ROOT/'Assets/StreamingAssets/Maps'
SCENARIOS = ROOT/'Assets/StreamingAssets/Scenarios'
for directory in (OUT,MAPS,SCENARIOS): directory.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
rng=random.Random(629)
geometry=[]

# material creates a texture-free material that also survives OBJ/MTL export.
def material(name,color,roughness=0.8,metallic=0):
    mat=bpy.data.materials.new(name)
    mat.diffuse_color=(*color,1)
    mat.use_nodes=True
    shader=mat.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Roughness'].default_value=roughness
    shader.inputs['Metallic'].default_value=metallic
    return mat

water=material('Deep turquoise sea',(0.025,0.22,0.28),0.3,0.15)
shallows=material('Lagoon turquoise',(0.06,0.36,0.36),0.42)
rock=material('Pale limestone',(0.46,0.49,0.43))
rock_dark=material('Limestone fissures',(0.28,0.34,0.30))
rock_light=material('Sunlit limestone',(0.64,0.64,0.52))
wet=material('Wet cliff base',(0.13,0.22,0.21))
foliage=material('Karst forest',(0.10,0.23,0.10))
foliage_light=material('Karst crown',(0.19,0.30,0.12))
asphalt=material('Runway charcoal',(0.065,0.085,0.095))
concrete=material('Airfield concrete',(0.43,0.47,0.44))
white=material('Ivory markings',(0.95,0.91,0.74))
orange=material('Hoop orange',(0.97,0.31,0.055))
steel=material('Dark steel',(0.11,0.17,0.19),0.5)
roof=material('Hangar blue grey',(0.16,0.29,0.33))

# box adds one simple solid to the exported collision geometry.
def box(name,location,size,mat,angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=location)
    obj=bpy.context.object
    obj.name=name
    obj.dimensions=size
    obj.rotation_euler.z=angle
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(mat)
    geometry.append(obj)
    return obj

# mesh_object adds an indexed mesh with explicit face material assignments.
def mesh_object(name,vertices,faces,materials,indices):
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    for mat in materials: mesh.materials.append(mat)
    for face,index in zip(mesh.polygons,indices): face.material_index=index
    geometry.append(obj)
    return obj

# karst builds steep, uneven limestone walls and a vegetated crown from radial rings.
def karst(name,x,y,rx,ry,height,flat=False):
    count=32
    phase=rng.random()*math.tau
    angles=[math.tau*i/count for i in range(count)]
    profile=[(-4,1.03),(0,1.0),(4,0.96),(height*0.34,0.87),(height*0.69,0.79),(height*0.90,0.73),(height,0.60)]
    if flat: profile=[(-4,1.08),(0,1.04),(height*0.50,1.0),(height,0.94)]
    vertices=[]
    noise=[1+rng.uniform(-0.08,0.08) for _ in angles]
    for ring,(z,scale) in enumerate(profile):
        for i,a in enumerate(angles):
            wobble=1+0.055*math.sin(3*a+phase+ring*0.5)+0.035*math.cos(7*a-phase)
            offset=0 if flat else ring*0.75
            top_z=z if flat else z+(rng.uniform(-0.025,0.025)*height if ring>=2 else 0)
            vertices.append((x+rx*scale*noise[i]*wobble*math.cos(a)+offset,
                             y+ry*scale*noise[i]*wobble*math.sin(a),top_z))
    faces=[]; indices=[]
    for ring in range(len(profile)-1):
        for i in range(count):
            a=ring*count+i; b=ring*count+(i+1)%count
            faces.append((a,b,b+count,a+count))
            if ring==0: index=3
            elif ring==len(profile)-2: index=4 if i%3 else 5
            else: index=[0,0,0,1,2][rng.randrange(5)]
            indices.append(index)
    faces.append(tuple(range((len(profile)-1)*count,len(profile)*count))); indices.append(5 if not flat else 0)
    faces.append(tuple(reversed(range(count)))); indices.append(3)
    return mesh_object(name,vertices,faces,[rock,rock_dark,rock_light,wet,foliage,foliage_light],indices)

# Start and all coordinates are authored in Blender Z-up, with forward down -Y.
runway_height=8.15
start=Vector((0,0,runway_height+2))
spawn=Vector((0,35,runway_height+2.5))
checkpoints=[Vector(p) for p in [
    (0,-190,32),(20,-310,60),(150,-400,95),(310,-325,110),
    (395,-150,90),(350,50,75),(230,225,65),(40,300,50),
    (-140,240,45),(-190,70,55),(-150,-100,70),(-65,-145,55),(0,-65,35)]]
finish=Vector((0,35,26))
flight_path=[start]+checkpoints+[finish]
assert (spawn-start).length > 14, 'Spawn must stay outside the countdown trigger'
assert all((b-a).length > 28 for a,b in zip(flight_path,flight_path[1:])), 'Race gates overlap'
inner_radius=12.0

# segment_distance measures the horizontal corridor clearance conservatively.
def segment_distance(point,a,b):
    delta=b.xy-a.xy
    factor=max(0,min(1,(point-a.xy).dot(delta)/delta.length_squared))
    return (point-(a.xy+delta*factor)).length

box('Sea surface',(60,-50,-2),(6000,6000,4),water)
karst('Airfield island',0,-50,62,170,8,True)
# A continuous runway with painted faces instead of raised collision markings.
bands=[-16,-12,-11.6,-0.18,0.18,11.6,12,16]
vertices=[];faces=[];indices=[]
for row in range(241):
    y=70-row
    for x in bands: vertices.append((x,y,runway_height))
for row in range(240):
    y=70-row
    for j in range(len(bands)-1):
        a=row*len(bands)+j;b=(row+1)*len(bands)+j
        faces.append((a,b,b+1,a+1))
        mid=abs((bands[j]+bands[j+1])*0.5)
        index=1 if mid>12 else 0
        if 11.6<mid<12 or (mid<0.2 and row%12<6): index=2
        if y==0 and mid<12: index=2
        if (y>57 or y<-158) and mid<11.6 and j in (2,4): index=2
        indices.append(index)
runway=mesh_object('Runway with flush markings',vertices,faces,[asphalt,concrete,white],indices)
assert all(face.normal.z > 0 for face in runway.data.polygons), 'Runway faces must point up for Unity raycasts'
for y in (28,-12,-52):
    box('Hangar',(-36,y,runway_height+6),(22,28,12),concrete)
    box('Hangar roof',(-36,y,runway_height+12.5),(24,30,1),roof)
    box('Hangar door',(-24.94,y,runway_height+4.5),(0.12,17,8),steel)
    box('Hangar lintel',(-24.7,y,runway_height+9),(0.25,18,0.6),orange)
for x in (-16,16):
    box('Start pillar',(x,0,runway_height+4.5),(1,1,9),orange)
    box('Pillar cap',(x,0,runway_height+9.5),(1.5,1.5,1),white)
box('Operations hut',(33,40,runway_height+4),(14,18,8),concrete)
box('Operations roof',(33,40,runway_height+8.5),(16,20,1),roof)

# Reject towers that would obstruct any straight connection between successive hoops.
tower_specs=[
    (85,-170,27,34,112),(120,-285,34,39,165),(240,-200,43,49,190),
    (230,10,50,43,155),(140,125,36,32,175),(45,180,32,37,130),
    (-65,105,23,27,100),(-90,-25,24,22,135),(-105,170,22,25,105),
    (-260,-280,48,55,170),(-325,-100,36,43,135),(-320,135,47,55,175),
    (-235,370,34,39,115),(-40,450,52,60,190),(170,430,42,48,140),
    (400,350,51,57,180),(540,105,62,68,205),(525,-175,46,53,180),
    (405,-470,51,58,150),(140,-550,63,67,195),(-60,-425,38,43,140),
    (315,-60,20,24,95),(70,-60,20,25,80),(65,60,22,24,110)]
accepted=[]
for x,y,rx,ry,height in tower_specs:
    clearance=min(segment_distance(Vector((x,y)),a,b) for a,b in zip(flight_path,flight_path[1:]))
    bound=max(rx,ry)*1.18+6
    if clearance < bound+inner_radius+16: continue
    karst(f'Sea karst {len(accepted)+1:02d}',x,y,rx,ry,height)
    accepted.append({'center':[x,y],'height':height,'footprint_bound':bound,'flight_corridor_clearance':round(clearance-bound,2)})
    # Small outcrops cluster around the foot of the main pillars.
    for offset in (-1,1):
        sx=x+offset*rx*0.55;sy=y+ry*0.45
        karst('Cliff outcrop',sx,sy,rx*0.30,ry*0.30,height*0.26)

# Hoop normals follow the incoming/outgoing course direction and have a 24 m clear opening.
for index,point in enumerate(checkpoints+[finish]):
    previous=start if index==0 else checkpoints[index-1]
    following=(checkpoints+[finish])[index+1] if index<len(checkpoints) else point+(point-previous)
    normal=(following-previous).normalized()
    bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=8,location=point,
        major_radius=inner_radius+0.8,minor_radius=0.8)
    hoop=bpy.context.object
    hoop.name=f'Flight hoop {index+1:02d}'
    hoop.rotation_euler=normal.to_track_quat('Z','Y').to_euler()
    hoop.data.materials.append(orange);hoop.data.materials.append(white)
    for face in hoop.data.polygons: face.material_index=(face.index//(8*6))%2
    geometry.append(hoop)

# game_point matches OBJ axis conversion and the importer's mirrored X axis.
def game_point(point): return [-point.x,point.z,-point.y]
# lua_vector writes three coordinates in the existing Lua API's indexed format.
def lua_vector(point): return '{'+', '.join(f'{v:.3f}' for v in game_point(point))+'}'
checkpoint_lua='\n'.join('        '+lua_vector(p)+',' for p in checkpoints)
(SCENARIOS/'karst_flight_race.lua').write_text('''-- Generated by Tools/MapAuthoring/build_karst_flight.py; hoop centres match the map geometry.
Scenario({
    id = "%s",
    title = "Karst Coast Flight",
    recordVersion = 1,
    scenarioLogicVersion = 1,
    completionType = "timeTrial",
    rules = {
        forbiddenChipTypes = {}, maxChips = -1,
        physics = { fixedDeltaTime = 0.001, solverIterations = 1, solverVelocityIterations = 3 }
    }
})
LoadMap("karst_coast")
SetPosition("player1", %s)
SetRotation("player1", {0, 0, 0})
Race({
    radius = 12,
    color = {80, 210, 130, 35},
    start = %s,
    checkpoints = {
%s
    },
    finish = %s
})
'''%(uuid.uuid5(uuid.NAMESPACE_URL,'ChipChallengers/karst_coast/flight/v1'),lua_vector(spawn),lua_vector(start),checkpoint_lua,lua_vector(finish)),encoding='utf-8')

# Validate each flight leg against actual transformed collision meshes before exporting.
bpy.context.view_layer.update()
collision_vertices=[]; collision_faces=[]
for obj in geometry:
    offset=len(collision_vertices)
    collision_vertices.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
    collision_faces.extend(tuple(offset+i for i in p.vertices) for p in obj.data.polygons)
collision_tree=BVHTree.FromPolygons(collision_vertices,collision_faces)
for a,b in zip(flight_path,flight_path[1:]):
    delta=b-a
    assert collision_tree.ray_cast(a,delta.normalized(),delta.length)[0] is None, 'Flight centreline intersects map geometry'

bpy.ops.object.select_all(action='DESELECT')
for obj in geometry: obj.select_set(True)
bpy.context.view_layer.objects.active=geometry[0]
bpy.ops.export_scene.obj(filepath=str(MAPS/'karst_coast.obj'),use_selection=True,
    axis_forward='-Z',axis_up='Y',use_materials=True,use_triangles=True,use_uvs=False,use_normals=True,path_mode='STRIP')

world=bpy.context.scene.world
world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.56,0.72,0.78,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=0.65
bpy.ops.object.light_add(type='SUN',rotation=(math.radians(28),math.radians(-22),math.radians(-25)))
bpy.context.object.data.energy=2.5
bpy.context.object.data.angle=math.radians(10)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.device='CPU'
scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'

# camera creates an authoring camera looking toward a chosen landmark or flight corridor.
def camera(name,location,target,ortho=None):
    bpy.ops.object.camera_add(location=location)
    obj=bpy.context.object;obj.name=name
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    obj.data.clip_end=5000;obj.data.lens=28
    if ortho: obj.data.type='ORTHO';obj.data.ortho_scale=ortho
    return obj

cameras=[camera('overview',(1050,-1250,1050),(90,-55,35),1550),
    camera('runway',(0,100,20),(0,-170,18)),
    camera('cliff_flight',(210,-375,118),(330,-300,105))]
for cam in cameras:
    scene.camera=cam;scene.render.filepath=str(OUT/(cam.name+'_v2.png'))
    bpy.ops.render.render(write_still=True)
scene.camera=cameras[0]
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'karst_coast.blend'))
# Numbered route markers are authoring-only and are added after the map export and clean blend save.
labelmat=material('Preview route labels',(0.015,0.025,0.025))
for index,point in enumerate(flight_path):
    bpy.ops.object.text_add(location=point+Vector((0,0,18)))
    text=bpy.context.object
    text.name=f'PREVIEW gate {index:02d}'
    text.data.body='START' if index==0 else ('FINISH' if index==len(flight_path)-1 else str(index))
    text.data.size=14 if index in (0,len(flight_path)-1) else 18
    text.data.align_x='CENTER';text.data.align_y='CENTER'
    text.data.materials.append(labelmat)
    text.rotation_euler=cameras[0].rotation_euler
# A thin dashed route is a preview guide, never a collision object in the game.
route_mat=material('Preview route',(1,0.7,0.08))
for a,b in zip(flight_path,flight_path[1:]):
    delta=b-a
    for i in range(0,int(delta.length),16):
        midpoint=a+delta.normalized()*(i+3)
        bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=0.65,depth=min(6,delta.length-i),location=midpoint)
        line=bpy.context.object;line.name='PREVIEW route guide'
        line.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
        line.data.materials.append(route_mat)
scene.camera=cameras[0];scene.render.filepath=str(OUT/'race_overview_v2.png')
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'karst_flight_preview.blend'))
summary={'map':'karst_coast','scenario':'karst_flight_race','road_reference_width':12,
    'hoop_clear_diameter':24,'trigger_diameter':24,'airborne_hoops':14,'checkpoints':13,
    'course_length':round(sum((b-a).length for a,b in zip(flight_path,flight_path[1:])),1),
    'triangles':sum(len(p.vertices)-2 for obj in geometry for p in obj.data.polygons),
    'cliffs':accepted,'spawn':game_point(spawn),'start':game_point(start),
    'checkpoints_game':[game_point(p) for p in checkpoints],'finish':game_point(finish)}
(OUT/'summary.json').write_text(json.dumps(summary,indent=2))
print('KARST_OUTPUT',MAPS/'karst_coast.obj')
print('CLIFFS',len(accepted),'TRIANGLES',summary['triangles'])
