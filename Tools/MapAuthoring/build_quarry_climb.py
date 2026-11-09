"""Build and render a quarry vehicle-testing map with optional technical shortcuts."""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[2]
OUT=Path('C:/Users/Acer/BlenderTests/quarry-climb')
MAPS=ROOT/'Assets/StreamingAssets/Maps'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
geometry=[]

# material creates an OBJ-compatible flat colour without external image dependencies.
def material(name,color):
    mat=bpy.data.materials.new(name);mat.diffuse_color=(*color,1);mat.use_nodes=True
    node=mat.node_tree.nodes['Principled BSDF'];node.inputs['Base Color'].default_value=(*color,1);node.inputs['Roughness'].default_value=0.85
    return mat
soil=material('Warm quarry stone',(0.39,0.30,0.20))
rock=material('Exposed limestone',(0.48,0.43,0.33))
sand=material('Quarry floor',(0.43,0.34,0.23))
gravel=material('Easy route gravel',(0.37,0.33,0.26))
orange=material('Technical route orange',(0.83,0.35,0.065))
yellow=material('Safety markings',(0.98,0.69,0.08))
steel=material('Dark steel',(0.13,0.18,0.19))
concrete=material('Concrete',(0.43,0.46,0.41))
roof=material('Blue grey roof',(0.16,0.28,0.32))
white=material('White markings',(0.88,0.85,0.73))

# mesh_object creates one exported mesh with explicit per-face materials.
def mesh_object(name,vertices,faces,materials,indices=None):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    for mat in materials:mesh.materials.append(mat)
    if indices:
        for poly,index in zip(mesh.polygons,indices):poly.material_index=index
    geometry.append(obj);return obj

# box adds a simple building or landmark with applied scale.
def box(name,location,size,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=location);obj=bpy.context.object;obj.name=name;obj.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);obj.data.materials.append(mat);geometry.append(obj);return obj

# curve samples an open Catmull-Rom route and keeps the endpoint elevations exact.
def curve(controls):
    controls=[Vector(v) for v in controls];result=[]
    for segment in range(len(controls)-1):
        p1,p2=controls[segment:segment+2]
        p0=controls[segment-1] if segment>0 else p1+(p1-p2)
        p3=controls[segment+2] if segment+2<len(controls) else p2+(p2-p1)
        samples=max(16,int((p2-p1).length*2))
        for j in range(samples):
            t=j/samples
            point=0.5*(2*p1+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)
            point.z=max(min(p.z for p in controls),min(max(p.z for p in controls),point.z))
            if not result or (point.xy-result[-1].xy).length>=1.1:result.append(point)
    result.append(controls[-1]);return result

main=curve([(0,30,2.35),(0,0,2.35),(0,-70,2.35),(70,-100,2.35),(140,-80,8),(160,-20,14),(110,30,20),(40,20,20),(-10,75,26),(-90,70,32),(-125,10,38),(-90,-60,38)])
steep=curve([(0,0,2.35),(8,4,2.35),(32,16,20),(40,20,20)])
steps=curve([(110,30,20),(70,70,23),(25,90,25),(-10,75,26)])
side=curve([(-10,75,26),(-55,45,31),(-95,25,35),(-125,10,38)])
road_samples=[]

# road builds a continuous ribbon, with optional true vertical step risers or a banked middle section.
def road(name,points,width,technical=False,stairs=False,bank=False):
    lengths=[0.0]
    for a,b in zip(points,points[1:]):lengths.append(lengths[-1]+(b-a).length)
    rows=[];last_offset=0
    for i,p in enumerate(points):
        distance=lengths[i];fraction=distance/lengths[-1]
        tangent=points[min(i+1,len(points)-1)]-points[max(0,i-1)]
        right=Vector((tangent.y,-tangent.x,0)).normalized()
        roll=math.radians(20)*math.sin(math.pi*fraction)**2 if bank else 0
        step_offset=0
        if stairs:
            if 30<=distance<62:step_offset=math.floor((distance-30)/4)*0.18
            elif 62<=distance<92:step_offset=1.26*(1-(distance-62)/30)
        point=p.copy();point.z+=0.025 if technical else 0
        if stairs and step_offset>last_offset+0.08:
            rows.append((point+Vector((0,0,last_offset)),right,roll))
        rows.append((point+Vector((0,0,step_offset)),right,roll))
        last_offset=step_offset
    offsets=[-width/2-1,-width/2,-width/2+0.2,width/2-0.2,width/2,width/2+1]
    vertices=[];faces=[];indices=[]
    for p,right,roll in rows:
        for offset in offsets:
            vertex=p+right*offset;vertex.z+=math.tan(roll)*offset;vertices.append(tuple(vertex))
        road_samples.append((p,right,roll,width/2+3))
    for i in range(len(rows)-1):
        for j in range(len(offsets)-1):
            a=i*len(offsets)+j;b=(i+1)*len(offsets)+j
            faces.append((a,a+1,b+1,b));indices.append(1 if j in (1,3) else (2 if j in (0,4) else 0))
    obj=mesh_object(name,vertices,faces,[gravel,orange if technical else yellow,sand],indices)
    assert all(p.normal.z>=-0.0001 for p in obj.data.polygons),'Folded road surface: '+name
    return lengths[-1]

main_length=road('Easy climb and return route',main,12)
road('A steep shortcut',steep,7,True)
road('B uneven step shortcut',steps,7,True,stairs=True)
road('C sideways incline shortcut',side,7,True,bank=True)

# Carve the quarry below all road surfaces; nearest-route lookup keeps generation inexpensive.
tree=KDTree(len(road_samples))
for i,(p,right,roll,width) in enumerate(road_samples):tree.insert((p.x,p.y,0),i)
tree.balance()
vertices=[];faces=[];indices=[]
NX,NY=147,131
for j in range(NY):
    y=-220+j*3
    for i in range(NX):
        x=-210+i*3
        radius=math.sqrt((x-25)**2+((y+30)*1.08)**2)
        height=2 if radius<85 else 9 if radius<120 else 19 if radius<155 else 31 if radius<185 else 44
        height+=0.25*math.sin(x*0.07)*math.cos(y*0.06)
        _,index,distance=tree.find((x,y,0));p,right,roll,width=road_samples[index]
        lateral=Vector((x-p.x,y-p.y,0)).dot(right)
        carved=p.z+math.tan(roll)*lateral-0.4
        blend=max(0,min(1,(distance-width)/9))
        height=carved*(1-blend)+height*blend
        if -48<x<-12 and -30<y<30:height=2.1
        vertices.append((x,y,height))
for j in range(NY-1):
    for i in range(NX-1):
        a=j*NX+i;faces.append((a,a+1,a+1+NX,a+NX))
        mean=sum(vertices[k][2] for k in faces[-1])/4
        indices.append(2 if mean<5 else 1 if mean>30 else 0)
terrain=mesh_object('Terraced quarry terrain',vertices,faces,[soil,rock,sand],indices)
# Verify that terraced terrain does not protrude through the centre of a driving lane.
terrain_tree=BVHTree.FromPolygons([v.co for v in terrain.data.vertices],[tuple(p.vertices) for p in terrain.data.polygons])
for point,right,roll,width in road_samples:
    hit=terrain_tree.ray_cast(point+Vector((0,0,100)),Vector((0,0,-1)),200)[0]
    assert hit is None or hit.z <= point.z+0.03, 'Terrain obstructs a route centreline'

for y in (15,-10):
    box('Workshop',(-30,y,6.1),(17,16,8),concrete)
    box('Workshop roof',(-30,y,10.35),(18,17,0.5),roof)
    box('Workshop door',(-21.4,y,5.1),(0.2,10,5.5),steel)
for x in (-9,9):
    box('Start pillar',(x,-12,5.6),(0.7,0.7,6.5),orange)
    box('Pillar cap',(x,-12,9),(1.1,1.1,0.5),yellow)
# Summit posts make the end of the climb identifiable without requiring scenario gates.
for x,y in [(-96,-60),(-84,-60)]:box('Summit goal post',(x,y,41.5),(0.7,0.7,7),orange)
# Spare blocks and boulders sit off the routes as simple quarry landmarks.
for x,y,z in [(185,5,30),(130,125,35),(-160,90,44),(-145,-130,38)]:
    for k in range(3):box('Stone stockpile',(x+3*k,y,z+k*0.7),(5,5,3),rock)

bpy.ops.object.select_all(action='DESELECT')
for obj in geometry:obj.select_set(True)
bpy.context.view_layer.objects.active=geometry[0]
bpy.ops.export_scene.obj(filepath=str(MAPS/'quarry_climb.obj'),use_selection=True,axis_forward='-Z',axis_up='Y',use_materials=True,use_triangles=True,use_uvs=False,use_normals=True,path_mode='STRIP')
world=bpy.context.scene.world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.57,0.67,0.74,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=0.65
bpy.ops.object.light_add(type='SUN',rotation=(math.radians(25),math.radians(-20),math.radians(-25)))
bpy.context.object.data.energy=2.4;bpy.context.object.data.angle=math.radians(10)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'

# camera adds an authoring-only view of the climb or its obstacle sections.
def camera(name,location,target,ortho=None):
    bpy.ops.object.camera_add(location=location);obj=bpy.context.object;obj.name=name
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    obj.data.clip_end=2000;obj.data.lens=28
    if ortho:obj.data.type='ORTHO';obj.data.ortho_scale=ortho
    return obj
cameras=[camera('overview',(350,-460,390),(25,-25,14),535),camera('start',(0,32,7),(5,-65,7)),camera('steps',(95,12,35),(55,75,25))]
for cam in cameras:
    scene.camera=cam;scene.render.filepath=str(OUT/(cam.name+'_v2.png'));bpy.ops.render.render(write_still=True)
scene.camera=cameras[0];bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'quarry_climb.blend'))
# Render-only labels identify alternative routes without adding collision geometry.
for text,position in [('EASY ROUTE',(150,-60,29)),('A: STEEP',(22,10,22)),('B: STEPS',(58,75,32)),('C: SIDE SLOPE',(-60,30,42)),('SUMMIT',(-90,-60,47))]:
    bpy.ops.object.text_add(location=position);obj=bpy.context.object;obj.name='PREVIEW '+text;obj.data.body=text
    obj.data.align_x='CENTER';obj.data.size=5;obj.rotation_euler=cameras[0].rotation_euler;obj.data.materials.append(steel)
scene.render.filepath=str(OUT/'annotated_overview_v2.png');bpy.ops.render.render(write_still=True)
summary={'map':'quarry_climb','easy_route_length':round(main_length,1),'climb_metres':35.65,'easy_road_width':12,'technical_lane_width':7,'step_rise_metres':0.18,'maximum_side_bank_degrees':20,'triangles':sum(len(p.vertices)-2 for obj in geometry for p in obj.data.polygons),'spawn_ground_height':2.35}
(OUT/'summary.json').write_text(json.dumps(summary,indent=2))
print('QUARRY_OUTPUT',MAPS/'quarry_climb.obj');print('SUMMARY',summary)
