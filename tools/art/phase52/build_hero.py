"""Original weighted caretaker pilot; Blender 4.5 LTS, no acquired geometry.
Builds editable armature/weights/actions and exports explicit native-Unity data.
Input helpers use Unity metres (+Y up, +Z forward), converted by a proper rotation.
Run: blender --background --factory-startup --python tools/art/phase52/build_hero.py
"""
import bpy, bmesh, math, json, os
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'game/Assets/Echoes/Art/Phase52/Source'
SOURCE = ROOT / 'tools/art/phase52/source'
PREVIEW = Path(os.environ.get('ECHOES_MODEL_PREVIEW_DIR', str(ROOT / 'artifacts/visual-qa/phase-5.2/model-preview')))
for p in (OUT, SOURCE, PREVIEW): p.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
cv = lambda v: Vector((v[0], -v[2], v[1]))
uc = lambda v: (v[0], v[2], -v[1])
C = Matrix(((1,0,0,0),(0,0,1,0),(0,-1,0,0),(0,0,0,1)))
PALETTE = {'Skin':'EBC0A6','Face':'EBC0A6','Ear':'D99483','Cream':'F0E0BF','Teal':'285B58','TealLight':'40716A','Burgundy':'78394B','Leather':'624331','LeatherEdge':'916747','Legging':'3D3237','Gold':'BA985C','Hair':'593D31','HairLight':'805A42','Lash':'362827','EyeWhite':'FFF2DE','Iris':'9C6A31','IrisLight':'D3AD60','Pupil':'282620','Glint':'FFF9E7','Lips':'B86762','Glass':'438D86'}
materials = {}
for name, hx in PALETTE.items():
    linear=lambda c:c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
    m=bpy.data.materials.new(name);m.diffuse_color=tuple(linear(int(hx[i:i+2],16)/255) for i in (0,2,4))+(1,)
    m.use_nodes=True; bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=m.diffuse_color;bs.inputs['Roughness'].default_value=.76 if name!='Gold' else .38
    if name=='Gold':bs.inputs['Metallic'].default_value=.52
    materials[name]=m

objects=[]
def surface(name, verts, faces, material, zone, smooth=True):
    me=bpy.data.meshes.new(name);me.from_pydata([cv(v) for v in verts],[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me)
    if bm.edges and all(e.is_manifold for e in bm.edges) and bm.calc_volume(signed=True)<0:bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(me)
    bm.free();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);ob.data.materials.append(materials[material]);ob['zone']=zone
    for p in me.polygons:p.use_smooth=smooth
    uv=me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        for li in poly.loop_indices:
            x,y,z=uc(me.vertices[me.loops[li].vertex_index].co)
            uv.data[li].uv=(math.atan2(z,x)/(2*math.pi)%1,y)
    objects.append(ob);return ob

def loft(name,rings,material,zone,n=24,fold=0,cap=True):
    vs=[];fs=[]
    for j,(x,y,z,rx,rz) in enumerate(rings):
        for i in range(n):
            a=2*math.pi*i/n;f=1+fold*math.cos(a*9+j*.16)
            vs.append((x+rx*math.cos(a)*f,y,z+rz*math.sin(a)*f))
    for j in range(len(rings)-1):
        for i in range(n):
            a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
    if cap:fs.extend([tuple(reversed(range(n))),tuple((len(rings)-1)*n+i for i in range(n))])
    ob=surface(name,vs,fs,material,zone)
    uv=ob.data.uv_layers.active
    for p in ob.data.polygons:
        for li in p.loop_indices:
            vi=ob.data.loops[li].vertex_index;uv.data[li].uv=(vi%n/n,vi//n/max(1,len(rings)-1))
    return ob

def tube(name,points,widths,material,zone,n=10,flat=1):
    vs=[];fs=[]
    for j,pt in enumerate(points):
        t=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized();u=t.cross(Vector((0,0,1)))
        if u.length<.01:u=t.cross(Vector((1,0,0)))
        u.normalize();v=t.cross(u).normalized()
        for i in range(n):
            a=math.tau*i/n;vs.append(Vector(pt)+(u*math.cos(a)+v*math.sin(a)*flat)*widths[j])
    for j in range(len(points)-1):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.extend([tuple(reversed(range(n))),tuple((len(points)-1)*n+i for i in range(n))])
    return surface(name,vs,fs,material,zone)

def line(name,points,thickness,material,zone):return tube(name,points,[thickness]*len(points),material,zone,n=5)
def box(name,p,s,material,zone,bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=cv(p));ob=bpy.context.object;ob.name=name;ob.scale=(s[0],s[2],s[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new('Soft leather edges','BEVEL');mod.width=bevel;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.data.materials.append(materials[material]);ob['zone']=zone
    for p in ob.data.polygons:p.use_smooth=False
    objects.append(ob);return ob

def union(name,parts,material,zone,voxel=.009,ratio=.48):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in parts:ob.select_set(True);objects.remove(ob)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();ob=parts[0];ob.name=name
    ob.data.materials.clear();ob.data.materials.append(materials[material])
    mod=ob.modifiers.new('Continuous shoulder and body topology','REMESH');mod.mode='VOXEL';mod.voxel_size=voxel;mod.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=ob.modifiers.new('Relax anatomy','SMOOTH');mod.factor=.72;mod.iterations=4;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=ob.modifiers.new('Controlled game topology','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
    ob['zone']=zone;objects.append(ob);return ob

# The blouse is a single connected surface, including shoulders and sleeves.
parts=[loft('BlouseTorso',[(0,.96,0,.145,.087),(0,1.04,0,.13,.081),(0,1.14,0,.142,.088),(0,1.23,.008,.174,.108),(0,1.30,.003,.193,.100),(0,1.34,0,.192,.083),(0,1.375,0,.125,.061),(0,1.395,0,.058,.047)],'Cream','Upper')]
for side,tag in [(-1,'L'),(1,'R')]:
    parts.append(loft('Sleeve'+tag,[(side*.168,1.344,0,.087,.09),(side*.218,1.306,0,.089,.09),(side*.265,1.235,0,.081,.083),(side*.307,1.16,.008,.071,.077),(side*.345,1.082,.015,.070,.074),(side*.374,1.005,.023,.082,.081),(side*.39,.965,.028,.088,.083)],'Cream','Upper',n=20,fold=.045))
union('ConnectedBlouse',parts,'Cream','Upper',voxel=.0075,ratio=.16)
loft('Neck',[(0,1.335,0,.064,.050),(0,1.39,0,.050,.043),(0,1.445,.004,.043,.042),(0,1.476,.005,.055,.044)],'Skin','Torso',n=24)

# Sculpted head: jaw/chin/cheek/forehead rings; nose bridge belongs to the face surface.
head=loft('Face',[(0,1.442,.018,.027,.027),(0,1.465,.015,.055,.047),(0,1.493,.008,.073,.063),(0,1.523,0,.089,.076),(0,1.555,-.003,.102,.083),(0,1.587,-.008,.107,.089),(0,1.619,-.013,.108,.090),(0,1.647,-.015,.102,.087),(0,1.674,-.017,.085,.073),(0,1.693,-.017,.050,.047),(0,1.702,-.016,.012,.012)],'Face','Head',n=48)
for v in head.data.vertices:
    x,y,z=uc(v.co)
    if z>0:
        z+=.025*math.exp(-(x/.018)**2-((y-1.546)/.026)**2)+.007*math.exp(-(x/.025)**2-((y-1.578)/.044)**2)
    v.co=cv((x,y,z))
head.data.update()
def face_z(x,y):
    # Elliptic front approximates the above cheek rows; features sit just outside it.
    zz=.082*math.sqrt(max(.05,1-(x/.108)**2))-.004
    if y<1.53:zz-=max(0,1.53-y)*.22
    return zz
for side,tag in [(-1,'L'),(1,'R')]:
    loft('Ear'+tag,[(side*.103,1.521,-.012,.015,.013),(side*.114,1.549,-.008,.018,.017),(side*.111,1.582,-.012,.017,.015),(side*.103,1.60,-.012,.009,.008)],'Skin','Head',n=12)
    line('EarFold'+tag,[(side*.117,1.543,.004),(side*.12,1.568,.005),(side*.111,1.583,.005)],.003,'Ear','Head')
    # Almond whites use a curved filled outline, with their own eyelid contours.
    xc=side*.047;yc=1.58;verts=[(xc,yc,face_z(xc,yc)+.009)];outline=[]
    for i in range(32):
        a=math.tau*i/32;dx=math.cos(a)*.034;dy=math.sin(a)*.019*(.82+.18*abs(math.sin(a)))+side*dx*.12
        x=xc+dx;y=yc+dy;outline.append((x,y,face_z(x,y)+.005));verts.append(outline[-1])
    surface('AlmondWhite'+tag,verts,[(0,i+1,(i+1)%32+1) for i in range(32)],'EyeWhite','Head')
    for label,rx,ry,offset,mat in [('Iris',.014,.017,.012,'Iris'),('Amber',.010,.012,.013,'IrisLight'),('Pupil',.0058,.012,.014,'Pupil')]:
        vs=[(xc,yc,face_z(xc,yc)+offset)]+[(xc+rx*math.cos(a*math.tau/24),yc+ry*math.sin(a*math.tau/24),face_z(xc+rx*math.cos(a*math.tau/24),yc)+offset) for a in range(24)]
        surface(label+tag,vs,[(0,i+1,(i+1)%24+1) for i in range(24)],mat,'Head')
    for j,(dx,dy,r) in enumerate([(-.005,.007,.004),(.005,-.006,.0018)]):
        loft('EyeGlint'+tag+str(j),[(xc+dx,yc+dy-r,face_z(xc,yc)+.016,r*.25,r*.25),(xc+dx,yc+dy,face_z(xc,yc)+.016,r,r*.5),(xc+dx,yc+dy+r,face_z(xc,yc)+.016,r*.25,r*.25)],'Glint','Head',n=8)
    upper=[];lower=[]
    for i in range(13):
        a=math.pi*i/12;x=xc+.034*math.cos(a);y=yc+.019*math.sin(a)+side*(x-xc)*.12;upper.append((x,y,face_z(x,y)+.008))
        y=yc-.016*math.sin(a)+side*(x-xc)*.12;lower.append((x,y,face_z(x,y)+.007))
    line('UpperLash'+tag,upper,.0026,'Lash','Head');line('LowerLid'+tag,lower,.0011,'Ear','Head')
    outer=xc+side*.034;line('LashWing'+tag,[(outer,1.584,face_z(outer,1.584)+.01),(outer+side*.009,1.590,face_z(outer,1.584)+.006)],.0018,'Lash','Head')
    line('Brow'+tag,[(side*.018,1.616,face_z(side*.018,1.616)+.004),(side*.040,1.625,face_z(side*.04,1.625)+.004),(side*.066,1.621,face_z(side*.066,1.621)+.003),(side*.08,1.614,face_z(side*.08,1.614))],.0028,'Hair','Head')
    # Tiny hanging leaf earring, part of the original model rather than a copied motif.
    line('EarringLink'+tag,[(side*.115,1.531,.005),(side*.115,1.516,.006)],.0018,'Gold','Head')
    surface('LeafEarring'+tag,[(side*.115,1.518,.007),(side*.121,1.507,.007),(side*.115,1.493,.008),(side*.109,1.507,.008)],[(0,1,2),(0,2,3)],'Gold','Head',False)
line('Mouth', [(-.020,1.507,.075),(-.010,1.504,.081),(0,1.503,.084),(.010,1.505,.081),(.020,1.510,.075)],.0017,'Lips','Head')
line('LowerLip',[(-.010,1.499,.079),(0,1.497,.082),(.011,1.501,.079)],.0012,'Ear','Head')

# Hair cap has a shaped hairline; wide solid swept locks create connected masses.
rings=[(0,1.574,-.03,.108,.088),(0,1.62,-.017,.116,.102),(0,1.67,-.012,.109,.095),(0,1.706,-.012,.079,.072),(0,1.722,-.014,.032,.031)]
cap=loft('HairCap',rings,'Hair','Head',n=40)
for v in cap.data.vertices:
    x,y,z=uc(v.co)
    if y<1.60 and z>.015:v.co=cv((x,y+.065*(z/.07),z))
for i in range(9):
    x=-.102+i*.024
    tube('Fringe'+str(i),[(x*.5,1.704,.035),(x*.8+.013,1.679,.073),(x+.016,1.645,.089),(x+.004,1.611+.017*math.sin(i*.7),.092)], [.023,.025,.020,.0015],'HairLight' if i in (2,6) else 'Hair','Head',n=10,flat=.28)
for side,tag in [(-1,'L'),(1,'R')]:
    for i in range(3):
        tube('Temple'+tag+str(i),[(side*.088,1.684,-.015+i*.025),(side*.111,1.63,-.012+i*.028),(side*.108,1.563,.006+i*.017),(side*(.091+i*.007),1.499-i*.014,.020)], [.022,.024,.016,.001],'Hair' if i%2 else 'HairLight','Head',n=10,flat=.45)
tube('Ponytail',[(.025,1.689,-.077),(.02,1.64,-.163),(.045,1.51,-.205),(.023,1.36,-.234),(.045,1.20,-.227),(.025,1.11,-.179)], [.043,.069,.075,.065,.045,.003],'Hair','HairTail',n=18,flat=.70)
for i in range(4):
    tube('TailLock'+str(i),[(.02+(i-1.5)*.023,1.65,-.17),(.035+(i-1.5)*.028,1.49,-.245),(.015+(i-1.5)*.025,1.30,-.258),(.04+(i-1.5)*.02,1.14+(i%2)*.03,-.20)], [.019,.023,.019,.001],'HairLight' if i==1 else 'Hair','HairTail',n=8,flat=.45)
tube('HairRibbon',[(.014,1.67,-.14),(-.02,1.53,-.23),(-.035,1.34,-.25),(-.058,1.24,-.24)],[.021,.025,.020,.002],'TealLight','HairTail',n=6,flat=.15)

# Torso vest: two open front panels and an uninterrupted back, all skin weighted.
def panel(name,rows,material,zone,fold=.004):
    vs=[];fs=[];n=8
    for j,(y,left,right,z) in enumerate(rows):
        for i in range(n):
            t=i/(n-1);vs.append((left+(right-left)*t,y,z+fold*math.sin(t*math.tau*2+j*.7)))
    for j in range(len(rows)-1):
        for i in range(n-1):k=j*n+i;fs.append((k,k+1,k+n+1,k+n))
    ob=surface(name,vs,fs,material,zone)
    mod=ob.modifiers.new('Fabric thickness','SOLIDIFY');mod.thickness=.003;bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob
for side,tag in [(-1,'L'),(1,'R')]:
    vest=panel('FittedVest'+tag,[(1.353,side*.05,side*.18,.096),(1.29,side*.065,side*.18,.125),(1.20,side*.055,side*.155,.119),(1.10,side*.047,side*.135,.110),(.98,side*.055,side*.15,.111)],'Teal','Upper')
    line('VestPiping'+tag,[(side*.05,1.353,.068),(side*.065,1.29,.110),(side*.055,1.20,.110),(side*.047,1.10,.094),(side*.055,.98,.091)],.0026,'Gold','Upper')
    loft('ShoulderMantle'+tag,[(side*.174,1.35,0,.097,.116),(side*.224,1.30,0,.110,.114),(side*.268,1.225,.004,.104,.108)],'Teal','Upper',n=20,fold=.020)
    line('SleeveCuffGold'+tag,[(side*.327,.976,.080),(side*.36,.955,.102),(side*.40,.950,.11),(side*.44,.96,.085)],.002,'Gold','Upper')
loft('VestBack',[(0,.97,-.035,.150,.061),(0,1.12,-.033,.135,.071),(0,1.25,-.03,.178,.071),(0,1.34,-.026,.175,.064)],'Teal','Upper',n=24)
loft('LeatherCorset',[(0,.965,.004,.151,.097),(0,1.00,.002,.143,.098),(0,1.08,.002,.136,.094),(0,1.125,.003,.145,.096)],'Leather','Torso',n=32)
for y in [1.0,1.075]:
    loft('WaistStrap'+str(y),[(0,y-.013,.002,.149,.100),(0,y+.013,.002,.149,.100)],'LeatherEdge','Torso',n=32)
    box('BeltBuckle'+str(y),(.042,y,.108),(.035,.032,.009),'Gold','Torso')
for i in range(4):
    y=.985+i*.039
    line('CorsetLace'+str(i),[(-.020,y,.106),(.021,y+.035,.108)],.0018,'Gold','Torso')
    line('CorsetCross'+str(i),[(.020,y,.106),(-.021,y+.035,.108)],.0018,'Gold','Torso')
loft('BurgundyUnderskirt',[(0,.96,0,.15,.11),(0,.86,0,.190,.135),(0,.75,0,.214,.150),(0,.68,0,.228,.162)],'Burgundy','Skirt',n=40,fold=.03)
loft('CreamOverskirt',[(0,.97,0,.168,.123),(0,.89,0,.202,.150),(0,.81,0,.232,.168),(0,.735,0,.253,.186)],'Cream','Skirt',n=40,fold=.035)
for side,tag in [(-1,'L'),(1,'R')]:
    panel('CoatTail'+tag,[(.99,side*.108,side*.163,-.064),(.85,side*.146,side*.217,-.10),(.67,side*.155,side*.272,-.12),(.48,side*.17,side*.285,-.14),(.37,side*.16,side*.246,-.16)],'Teal','Cape'+tag,fold=.011)
    line('CoatTailTrim'+tag,[(side*.163,.99,-.065),(side*.217,.85,-.101),(side*.272,.67,-.121),(side*.285,.48,-.141),(side*.246,.37,-.161)],.003,'Gold','Cape'+tag)
    # Restrained original botanical stitching on the coat.
    for i in range(3):
        y=.47+i*.04;x=side*(.212+(i%2)*.017)
        line('StitchStem'+tag+str(i),[(x,y,-.151),(x+side*.008,y+.038,-.145)],.0014,'Gold','Cape'+tag)
        line('StitchLeaf'+tag+str(i),[(x,y+.018,-.153),(x+side*.016,y+.025,-.15),(x+side*.008,y+.038,-.146)],.0017,'Gold','Cape'+tag)
loft('ScarfCollar',[(0,1.335,0,.101,.071),(0,1.367,0,.100,.068),(0,1.398,0,.077,.059)],'Burgundy','Torso',n=32,fold=.035)
panel('TrailingScarf',[(1.37,-.04,.05,-.078),(1.22,-.03,.074,-.11),(1.03,-.01,.089,-.128),(.86,.015,.107,-.13)],'Burgundy','Scarf',fold=.008)

for side,tag in [(-1,'L'),(1,'R')]:
    # Forearms and hands connect under the sleeve cuff; fingers have tapered segments.
    loft('Forearm'+tag,[(side*.36,1.016,.023,.047,.047),(side*.385,.95,.027,.044,.043),(side*.405,.89,.029,.034,.033),(side*.423,.848,.033,.028,.029)],'Skin','Arm'+tag,n=20)
    palm=loft('Palm'+tag,[(side*.422,.857,.032,.027,.025),(side*.438,.817,.038,.033,.022),(side*.446,.783,.042,.031,.017)],'Skin','Hand'+tag,n=18)
    for i in range(4):
        x=side*(.422+i*.014);base=.793;length=[.058,.071,.067,.052][i]
        tube('Finger'+tag+str(i),[(x,base,.044),(x+side*.005,base-length*.50,.053),(x+side*.004,base-length,.064)], [.008,.007,.0045],'Skin','Hand'+tag,n=8)
    tube('Thumb'+tag,[(side*.420,.83,.050),(side*.404,.812,.066),(side*.399,.792,.073)],[.011,.009,.005],'Skin','Hand'+tag,n=8)
    loft('Legging'+tag,[(side*.102,.91,0,.086,.086),(side*.106,.80,0,.087,.083),(side*.11,.66,.008,.076,.073),(side*.115,.53,.028,.059,.059),(side*.115,.47,.035,.053,.055),(side*.115,.40,.021,.057,.058),(side*.115,.30,.006,.052,.053),(side*.115,.16,0,.037,.043)],'Legging','Leg'+tag,n=24)
    loft('Boot'+tag,[(side*.115,.015,.072,.065,.140),(side*.115,.042,.073,.065,.141),(side*.115,.08,.059,.061,.127),(side*.115,.12,.017,.045,.073),(side*.115,.19,0,.047,.055),(side*.115,.30,0,.056,.060),(side*.115,.335,-.004,.060,.061)],'Leather','Leg'+tag,n=28)
    loft('BootSole'+tag,[(side*.115,.008,.072,.066,.141),(side*.115,.029,.072,.066,.141)],'Legging','Leg'+tag,n=28)
    loft('BootCuff'+tag,[(side*.115,.302,0,.059,.064),(side*.115,.326,-.002,.067,.069),(side*.115,.35,-.003,.064,.065)],'LeatherEdge','Leg'+tag,n=24)
    for i in range(5):
        y=.115+i*.038;z=.071 if i<2 else .061
        line('BootLace'+tag+str(i),[(side*.115-.024,y,z),(side*.115+.024,y+.031,z+.002)],.0019,'Cream','Leg'+tag)
        line('BootLaceX'+tag+str(i),[(side*.115+.024,y,z),(side*.115-.024,y+.031,z+.002)],.0019,'Cream','Leg'+tag)
    box('AnkleBuckle'+tag,(side*.173,.17,.015),(.012,.035,.034),'Gold','Leg'+tag)

# Practical alchemy bag and distinct glass vials, with a fitted cross-body strap.
box('AlchemySatchel',(.242,.83,.0),(.16,.22,.105),'Leather','Torso',.018)
box('SatchelFlap',(.242,.914,.062),(.169,.093,.02),'LeatherEdge','Torso',.009)
box('SatchelClasp',(.242,.876,.08),(.027,.036,.011),'Gold','Torso')
tube('ShoulderStrap',[(-.148,1.346,.109),(-.075,1.245,.146),(.024,1.112,.129),(.129,.986,.117),(.225,.89,.083)],[.014]*5,'LeatherEdge','Upper',n=6,flat=.18)
for i in range(2):
    x=.18+i*.112
    loft('AlchemyVial'+str(i),[(x,.733,.071,.012,.012),(x,.756,.071,.019,.019),(x,.798,.071,.016,.016),(x,.813,.071,.008,.008)],'Glass','Torso',n=12)
    loft('VialStopper'+str(i),[(x,.811,.071,.011,.011),(x,.825,.071,.011,.011)],'Gold','Torso',n=12)

# Anatomical rest skeleton. Bone matrices are converted explicitly for Unity.
spec=[('Root',None,(0,0,0),(0,.12,0)),('Pelvis','Root',(0,.86,0),(0,1.02,0)),('Spine','Pelvis',(0,1.02,0),(0,1.18,0)),('Chest','Spine',(0,1.18,0),(0,1.365,0)),('Neck','Chest',(0,1.365,0),(0,1.44,0)),('Head','Neck',(0,1.44,0),(0,1.70,0)),('Ponytail','Head',(.02,1.67,-.12),(.03,1.42,-.22)),('PonytailTip','Ponytail',(.03,1.42,-.22),(.03,1.12,-.20)),('Scarf','Chest',(0,1.36,-.08),(.05,.91,-.13)),('CapeL','Pelvis',(-.14,.96,-.065),(-.23,.40,-.15)),('CapeR','Pelvis',(.14,.96,-.065),(.23,.40,-.15))]
for side,tag in [(-1,'L'),(1,'R')]:
    spec.extend([('Clavicle'+tag,'Chest',(side*.10,1.34,0),(side*.20,1.32,0)),('UpperArm'+tag,'Clavicle'+tag,(side*.20,1.32,0),(side*.345,1.082,.015)),('Forearm'+tag,'UpperArm'+tag,(side*.345,1.082,.015),(side*.423,.848,.033)),('Hand'+tag,'Forearm'+tag,(side*.423,.848,.033),(side*.448,.775,.047)),('Thigh'+tag,'Pelvis',(side*.105,.86,0),(side*.115,.48,.035)),('Calf'+tag,'Thigh'+tag,(side*.115,.48,.035),(side*.115,.12,0)),('Foot'+tag,'Calf'+tag,(side*.115,.12,0),(side*.115,.04,.17))])
bpy.ops.object.armature_add(enter_editmode=True);rig=bpy.context.object;rig.name='CaretakerArmature';rig.show_in_front=True
for b in list(rig.data.edit_bones):rig.data.edit_bones.remove(b)
for name,parent,headp,tail in spec:
    b=rig.data.edit_bones.new(name);b.head=cv(headp);b.tail=cv(tail)
    if parent:b.parent=rig.data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
bones={b.name:b for b in rig.data.bones};points={n:(Vector(h),Vector(t)) for n,_,h,t in spec};bone_names=[n for n,_,_,_ in spec]
def chain_weights(p,names,soft=.055):
    ds=[];p=Vector(p)
    for name in names:
        a,b=points[name];v=b-a;t=max(0,min(1,(p-a).dot(v)/v.length_squared));d=(p-(a+v*t)).length
        ds.append((name,1/(d*d+soft*soft)**2))
    ds=sorted(ds,key=lambda x:-x[1])[:3];s=sum(w for _,w in ds);return [(n,w/s) for n,w in ds]
def weights(p,zone):
    x,y,z=p;tag='L' if x<0 else 'R'
    if zone=='Head':return [('Head',1)]
    if zone=='HairTail':
        t=max(0,min(1,(1.53-y)/.18));return [('Ponytail',1-t),('PonytailTip',t)]
    if zone in ('CapeL','CapeR'):return [(zone,max(0,min(1,(.99-y)/.23))),('Pelvis',1-max(0,min(1,(.99-y)/.23)))]
    if zone=='Scarf':return [('Scarf',max(0,min(1,(1.36-y)/.2))),('Chest',1-max(0,min(1,(1.36-y)/.2)))]
    if zone=='Skirt':
        leg=max(0,min(.92,(.965-y)/.21));return [('Pelvis',1-leg),('Thigh'+tag,leg)]
    if zone.startswith('Leg'):return chain_weights(p,['Thigh'+tag,'Calf'+tag,'Foot'+tag],.032)
    if zone.startswith('Hand'):return [('Hand'+zone[-1],1)]
    if zone.startswith('Arm'):return chain_weights(p,['UpperArm'+zone[-1],'Forearm'+zone[-1],'Hand'+zone[-1]],.035)
    if zone=='Upper' and abs(x)>.18 and y<1.355:
        return chain_weights(p,['Chest','Clavicle'+tag,'UpperArm'+tag,'Forearm'+tag],.045)
    return chain_weights(p,['Pelvis','Spine','Chest','Neck'],.045)
for ob in objects:
    for n in bone_names:ob.vertex_groups.new(name=n)
    for vert in ob.data.vertices:
        for name,w in weights(uc(ob.matrix_world@vert.co),ob['zone']):
            if w>.00001:ob.vertex_groups[name].add([vert.index],w,'REPLACE')
    modifier=ob.modifiers.new('Weighted continuous deformation','ARMATURE');modifier.object=rig;ob.parent=rig

def segment_matrix(name,headp,tail):
    rest=bones[name];a,b=points[name]
    q=cv(b-a).rotation_difference(cv(Vector(tail)-Vector(headp))) @ rest.matrix_local.to_quaternion()
    return Matrix.Translation(cv(headp)) @ q.to_matrix().to_4x4()
def make_pose(kind,t):
    walk=kind=='Walk';turn=1 if kind=='TurnLeft' else -1 if kind=='TurnRight' else 0
    phase=t*math.tau;bob=(-.035+.014*math.cos(phase*2)) if walk else .003*math.sin(phase)
    body_rot=Matrix.Rotation(math.radians(5 if walk else 0),3,'X') @ Matrix.Rotation(math.radians(turn*5*math.sin(math.pi*t)),3,'Y')
    center=Vector((0,.86+bob,0));P=lambda p:center+body_rot@(Vector(p)-Vector((0,.86,0)))
    result={'Root':bones['Root'].matrix_local.copy()}
    for name,parent,h,tail in spec:
        if name in ('Root',) or any(name.startswith(s) for s in ('Clavicle','UpperArm','Forearm','Hand','Thigh','Calf','Foot')):continue
        a=P(h);b=P(tail)
        if name.startswith('Ponytail'):
            b.z-=.025+(math.sin(phase+(.7 if name.endswith('Tip') else 0))*.018 if walk else math.sin(phase)*.005)
        if name.startswith('Cape'):b.z-=.07 if walk else .01;b.y+=.025*math.sin(phase+(0 if name=='CapeL' else math.pi)) if walk else .002*math.sin(phase)
        if name=='Scarf':b.z-=.06 if walk else 0
        if name=='Head':b.x+=.01*math.sin(phase)* (0.3 if walk else 1)
        result[name]=segment_matrix(name,a,b)
    for side,tag in [(-1,'L'),(1,'R')]:
        ph=(t+(0 if side<0 else .5))%1
        hip=P((side*.105,.86,0));ankle=Vector((side*.115,.12,0))
        if walk:
            stance=.28
            if ph<stance:ankle.z=.336-.672*ph/stance
            else:
                s=(ph-stance)/(1-stance);ankle.z=-.336+.672*(s*s*(3-2*s));ankle.y+=.16*math.sin(math.pi*s)
        elif turn:ankle.y+=.025*max(0,math.sin(phase+(0 if side<0 else math.pi)))
        l1=(points['Thigh'+tag][1]-points['Thigh'+tag][0]).length;l2=(points['Calf'+tag][1]-points['Calf'+tag][0]).length
        delta=ankle-hip;dist=min(delta.length,l1+l2-.001);direction=delta.normalized();a=(l1*l1-l2*l2+dist*dist)/(2*dist);h=math.sqrt(max(.000001,l1*l1-a*a));perp=Vector((0,direction.z,-direction.y)).normalized();knee=hip+direction*a+perp*h
        result['Thigh'+tag]=segment_matrix('Thigh'+tag,hip,knee);result['Calf'+tag]=segment_matrix('Calf'+tag,knee,ankle)
        toe=ankle+Vector((0,-.08,.17));result['Foot'+tag]=segment_matrix('Foot'+tag,ankle,toe)
        shoulder=P((side*.20,1.32,0));swing=-.53*math.sin(phase+(0 if side<0 else math.pi)) if walk else .02*math.sin(phase)
        upper=Vector((side*.12,-math.cos(swing),math.sin(swing))).normalized()*(points['UpperArm'+tag][1]-points['UpperArm'+tag][0]).length
        lower=Vector((side*.05,-math.cos(swing+.34),math.sin(swing+.34))).normalized()*(points['Forearm'+tag][1]-points['Forearm'+tag][0]).length
        elbow=shoulder+upper;wrist=elbow+lower;hand=wrist+lower.normalized()*.080
        result['Clavicle'+tag]=segment_matrix('Clavicle'+tag,P((side*.10,1.34,0)),shoulder)
        result['UpperArm'+tag]=segment_matrix('UpperArm'+tag,shoulder,elbow);result['Forearm'+tag]=segment_matrix('Forearm'+tag,elbow,wrist);result['Hand'+tag]=segment_matrix('Hand'+tag,wrist,hand)
    return result

def assign_pose(poses):
    # Compute bases from desired parent matrices, not lazily evaluated parent poses.
    for n in bone_names:
        pb=rig.pose.bones[n];parent=pb.parent
        local=poses[parent.name].inverted()@poses[n] if parent else poses[n]
        rest=parent.bone.matrix_local.inverted()@pb.bone.matrix_local if parent else pb.bone.matrix_local
        pb.rotation_mode='QUATERNION';pb.matrix_basis=rest.inverted()@local
    bpy.context.view_layer.update()

clips=[]
for kind,seconds in [('Idle',2.4),('Walk',.72),('TurnLeft',.9),('TurnRight',.9)]:
    count=round(seconds*30);clip={'name':kind,'length':seconds,'tracks':[]};tracks={n:[] for n in bone_names}
    rig.animation_data_create();rig.animation_data.action=bpy.data.actions.new(kind);rig.animation_data.action.use_fake_user=True
    for frame in range(count+1):
        bpy.context.scene.frame_set(frame)
        poses=make_pose(kind,frame/count)
        assign_pose(poses)
        for n in bone_names:
            pb=rig.pose.bones[n];pb.keyframe_insert('location',frame=frame);pb.keyframe_insert('rotation_quaternion',frame=frame)
            parent=pb.parent;local=poses[parent.name].inverted()@poses[n] if parent else poses[n];unity=C@local@C.inverted();p,q,s=unity.decompose()
            if tracks[n] and sum(a*b for a,b in zip(tracks[n][-1][4:8],(q.x,q.y,q.z,q.w)))<0:q.negate()
            tracks[n].append([round(frame*seconds/count,6),*[round(v,7) for v in p],round(q.x,7),round(q.y,7),round(q.z,7),round(q.w,7)])
    for n in bone_names:clip['tracks'].append({'bone':n,'keys':[{'time':k[0],'values':k[1:]} for k in tracks[n]]})
    clips.append(clip)
rig.animation_data.action=None
for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update()
export={'version':1,'materials':[{'name':n,'color':hx} for n,hx in PALETTE.items()],'bones':[],'parts':[],'clips':clips,'strideMetres':2.4}
for name,parent,h,t in spec:
    local=bones[parent].matrix_local.inverted()@bones[name].matrix_local if parent else bones[name].matrix_local
    p,q,s=(C@local@C.inverted()).decompose();export['bones'].append({'name':name,'parent':parent or '', 'position':list(p),'rotation':[q.x,q.y,q.z,q.w]})
triangle_count=0
for ob in objects:
    me=ob.data;me.calc_loop_triangles();verts=[];normals=[];uvs=[];indices=[];skin=[];weights_out=[];lookup={}
    normal_matrix=ob.matrix_world.to_3x3().inverted().transposed()
    for tri in me.loop_triangles:
        for li in tri.loops:
            loop=me.loops[li];vert=me.vertices[loop.vertex_index];position=uc(ob.matrix_world@vert.co);normal=uc(normal_matrix@me.corner_normals[li].vector)
            uv=me.uv_layers.active.data[li].uv if me.uv_layers.active else Vector((position[0],position[1]))
            ws=sorted([(bone_names.index(ob.vertex_groups[g.group].name),g.weight) for g in vert.groups if g.weight>.00001],key=lambda x:-x[1])[:4];total=sum(w for _,w in ws);ws=[(n,w/total) for n,w in ws]
            while len(ws)<4:ws.append((0,0))
            key=tuple(round(x,6) for x in (*position,*normal,*uv))+tuple(n for n,w in ws)+tuple(round(w,6) for n,w in ws)
            if key not in lookup:
                lookup[key]=len(verts)//3;verts.extend(key[:3]);normals.extend(key[3:6]);uvs.extend(key[6:8]);skin.extend(n for n,w in ws);weights_out.extend(round(w,7) for n,w in ws)
            indices.append(lookup[key])
    triangle_count+=len(indices)//3
    export['parts'].append({'name':ob.name,'material':ob.data.materials[0].name,'vertices':verts,'normals':normals,'uv':uvs,'triangles':indices,'boneIndices':skin,'weights':weights_out})
(OUT/'hero-skin.json').write_text(json.dumps(export,separators=(',',':')),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'caretaker-pilot.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for ob in objects:ob.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(SOURCE/'caretaker-pilot.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
print('PHASE52_BONES='+str(len(spec))+' PARTS='+str(len(objects))+' TRIANGLES='+str(triangle_count)+' CLIPS='+str(len(clips)),flush=True)

# Studio model-only previews are explicitly separate from game evidence.
if os.environ.get('ECHOES_MODEL_PREVIEW','1')=='1':
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=768;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
    scene.world.color=(.28,.28,.28);scene.view_settings.view_transform='Standard'
    def light(name,p,power,size):
        bpy.ops.object.light_add(type='AREA',location=cv(p));ob=bpy.context.object;ob.name=name;ob.data.energy=power;ob.data.shape='DISK';ob.data.size=size;ob.rotation_euler=(cv((0,1,0))-ob.location).to_track_quat('-Z','Y').to_euler()
    light('Key',(-2,3,4),140,3);light('Fill',(2,2,2),95,3);light('Rim',(0,3,-2),110,2)
    bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=1.9
    poses=make_pose('Idle',0);assign_pose(poses)
    for name,p in [('front',(0,.96,4)),('side',(4,.96,0)),('rear',(0,.96,-4))]:
        cam.location=cv(p);cam.rotation_euler=(cv((0,.88,0))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(PREVIEW/(name+'.png'));bpy.ops.render.render(write_still=True)
    assign_pose(make_pose('Walk',.16));cam.location=cv((2,1.05,3));cam.rotation_euler=(cv((0,.85,0))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(PREVIEW/'walk.png');bpy.ops.render.render(write_still=True)
