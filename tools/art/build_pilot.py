"""Original Phase 5.1 art. Blender 4.5 LTS, no downloaded assets.
Run: blender --background --factory-startup --python tools/art/build_pilot.py
Coordinates passed to helpers are Unity metres (+Y up, +Z forward).
Export loop normals/UVs explicitly to an intermediate JSON; Unity owns .asset serialization.
FBX and .blend are retained as editable interchange/source files, not imported into Unity.
"""
import bpy, bmesh, math, json, random, os
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ART_ROOT = Path(os.environ.get('ECHOES_ART_OUTPUT_ROOT', str(ROOT)))
OUT = ART_ROOT / 'game/Assets/Echoes/Art/PilotSource'
SOURCE = ART_ROOT / 'tools/art/source'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
random.seed(5101)

def cv(p): return (p[0], -p[2], p[1])
def mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    return m
def mesh(name, verts, faces, material, rig='Body', smooth=True):
    me = bpy.data.meshes.new(name)
    me.from_pydata([cv(v) for v in verts], [], faces)
    me.update()
    # A watertight surface must face outside. This also covers small leaf ridges
    # whose plane can point either way depending on the direction of their stem.
    bm=bmesh.new();bm.from_mesh(me)
    if bm.edges and all(edge.is_manifold for edge in bm.edges) and bm.calc_volume(signed=True)<0:
        bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(me);me.update()
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat(material))
    ob['rig'] = rig
    for p in me.polygons: p.use_smooth = smooth
    # Cylindrical/projected per-loop UVs; deterministic, usable in Blender and Unity.
    uv = me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        for li in poly.loop_indices:
            v = me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv = (math.atan2(-v.y,v.x)/(2*math.pi)+0.5, v.z)
    return ob
def loft(name, rings, material, rig='Body', n=20, fold=0):
    # Each ring: (centre x, height, centre z, halfwidth, halfdepth).
    vs=[]; fs=[]
    for j,(x,y,z,rx,rz) in enumerate(rings):
        for i in range(n):
            a=i*2*math.pi/n
            d=1+fold*math.sin(a*7+j*.7)
            vs.append((x+rx*math.cos(a)*d,y,z+rz*math.sin(a)*d))
    for j in range(len(rings)-1):
        for i in range(n):
            k=j*n+i; q=j*n+(i+1)%n
            fs.append((k,q,q+n,k+n))
    fs += [tuple(reversed(range(n))),tuple((len(rings)-1)*n+i for i in range(n))]
    # Ring direction matters: sleeves/forearms/trousers run DOWN, towers and heads UP.
    # Keep each closed loft outward, rather than merely making its normals agree with its faces.
    if rings[-1][1] > rings[0][1]: fs=[tuple(reversed(f)) for f in fs]
    ob=mesh(name,vs,fs,material,rig)
    ob.data.calc_loop_triangles()
    volume=sum(ob.data.vertices[t.vertices[0]].co.dot(ob.data.vertices[t.vertices[1]].co.cross(ob.data.vertices[t.vertices[2]].co)) for t in ob.data.loop_triangles)/6
    if volume <= 0: raise ValueError('Inward/degenerate closed loft: '+name+' volume='+str(volume))
    return ob
def box(name,p,s,material,rig='Body',bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=cv(p))
    ob=bpy.context.object; ob.name=name
    ob.scale=(s[0],s[2],s[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new('Worn edges','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.data.materials.append(mat(material));ob['rig']=rig
    # Planar unwrap every face after bevel, retaining object position.
    uv=ob.data.uv_layers.new(name='UVMap')
    ob.data.uv_layers.active = uv  # use projected UVs, not the cube primitive's bevel-interpolated UV layer
    for poly in ob.data.polygons:
        axis=max(range(3),key=lambda i:abs(poly.normal[i])); axes=[i for i in range(3) if i!=axis]
        for li in poly.loop_indices:
            v=ob.data.vertices[ob.data.loops[li].vertex_index].co
            uv.data[li].uv=(v[axes[0]],v[axes[1]])
    return ob
def tube(name,points,radii,material,rig='Body',n=8):
    vs=[];fs=[]
    for j,p in enumerate(points):
        tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        tangent.normalize(); u=tangent.cross(Vector((0,0,1)))
        if u.length<.01: u=tangent.cross(Vector((1,0,0)))
        u.normalize();v=tangent.cross(u).normalized()
        for k in range(n): vs.append(tuple(Vector(p)+(u*math.cos(k*2*math.pi/n)+v*math.sin(k*2*math.pi/n))*radii[j]))
    for j in range(len(points)-1):
        for k in range(n):
            a=j*n+k;b=j*n+(k+1)%n;fs.append((a,b,b+n,a+n))
    fs += [tuple(reversed(range(n))),tuple((len(points)-1)*n+k for k in range(n))]
    return mesh(name,vs,fs,material,rig)
def leaf(name,base,tip,width,material,rig='Static'):
    b=Vector(base);t=Vector(tip);d=t-b;side=d.cross(Vector((0,0,1))).normalized()*width
    if side.length<.01: side=Vector((width,0,0))
    c=b+d*.52+Vector((0,min(width*.20,.045),0))
    verts=[tuple(b),tuple(c+side),tuple(t),tuple(c-side),tuple(c+Vector((0,width*.25,width*.15)))]
    # Solid leaf with a ridge, no alpha or planes seen edge-on.
    return mesh(name,verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(0,3,2,1)],material,rig,False)

def hero():
    # A seven-head adult silhouette; no sphere/capsule primitive anatomy.
    loft('Blouse',[(0,1.02,0,.145,.096),(0,1.12,0,.17,.11),(0,1.25,0,.20,.104),(0,1.32,0,.235,.086),(0,1.37,0,.13,.075),(0,1.40,0,.065,.056)],'ClothCream',fold=.018)
    loft('FieldTunic',[(0,.74,0,.235,.137),(0,.80,0,.221,.131),(0,.92,0,.19,.125),(0,1.04,0,.167,.118),(0,1.17,0,.204,.131),(0,1.25,0,.224,.129),(0,1.29,0,.230,.123)],'ClothTeal',fold=.012)
    loft('Neck',[(0,1.35,0,.055,.05),(0,1.43,0,.052,.052),(0,1.465,0,.065,.06)],'Skin')
    # Jaw, cheeks, brow, crown shaped independently, then smooth normals.
    loft('Face',[(0,1.405,.038,.025,.035),(0,1.426,.018,.059,.070),(0,1.458,.004,.083,.080),(0,1.50,0,.096,.086),(0,1.545,-.004,.101,.087),(0,1.591,-.01,.094,.084),(0,1.625,-.014,.069,.067),(0,1.65,-.018,.015,.018)],'Skin','Head',32)
    for side in [-1,1]:
        s=side
        loft('Ear',[(s*.097,1.485,-.008,.010,.016),(s*.108,1.51,-.007,.015,.022),(s*.104,1.538,-.012,.009,.012)],'Skin','Head',12)
        # Almond eye, iris, highlight and upper lash, all flush with the face.
        x=s*.043
        vs=[(x-.026,1.53,.076),(x-.013,1.540,.081),(x+.012,1.540,.082),(x+.028,1.531,.077),(x+.012,1.520,.082),(x-.012,1.520,.081),(x,1.530,.087)]
        mesh('EyeWhite',vs,[(6,(i+1)%6,i) for i in range(6)],'EyeWhite','Head')
        loft('Iris',[(x,1.521,.086,.008,.002),(x,1.53,.089,.010,.003),(x,1.54,.086,.007,.002)],'Iris','Eye'+str(side),16)
        tube('Lash',[(x-.026,1.532,.080),(x,1.542,.086),(x+.027,1.532,.081)],[.002,.003,.002],'Hair','Head',6)
        tube('Brow',[(x-.024,1.561,.078),(x,1.566,.083),(x+.023,1.560,.079)],[.002,.0035,.002],'Hair','Head',6)
    mesh('Nose',[(-.011,1.509,.082),(.011,1.509,.082),(0,1.534,.081),(0,1.505,.106),(0,1.499,.088)],[(3,2,0),(3,1,2),(3,4,1),(3,0,4),(0,2,1,4)],'Skin','Head')
    tube('Lips',[(-.019,1.467,.083),(0,1.465,.091),(.019,1.467,.083)],[.001,.0025,.001],'Coral','Head',8)
    # Open hair cap: leaves the face visible; irregular fringe and tied back silhouette.
    vs=[];fs=[]
    for j in range(9):
        for i in range(40):
            a=2*math.pi*i/40
            # Front fringe shorter, nape longer.
            end=1.70-.40*max(0,math.sin(a))+.50*max(0,-math.sin(a))
            theta=.04+(end-.04)*j/8
            vs.append((.109*math.sin(theta)*math.cos(a),1.548+.114*math.cos(theta),-.022+.102*math.sin(theta)*math.sin(a)))
    for j in range(8):
        for i in range(40):
            a=j*40+i;b=j*40+(i+1)%40;fs.append((a,b,b+40,a+40))
    mesh('HairCap',vs,fs,'Hair','Head')
    for i in range(7):
        x=-.081+i*.027
        tube('Fringe',[(x*.8,1.641,.03),(x,1.605,.068),(x+.012,1.57-(i%3)*.008,.086)],[.019,.021,.001],'Hair','Head',8)
    for s in [-1,1]:
        tube('Temple',[(s*.092,1.60,0),(s*.104,1.54,.021),(s*.092,1.45,.01)],[.026,.023,.002],'Hair','Head',9)
    tube('Ponytail',[(0,1.57,-.11),(.02,1.50,-.15),(.015,1.39,-.16),(-.02,1.31,-.14)],[.038,.048,.036,.003],'Hair','Ponytail',12)
    for s in [-1,1]:
        tag='ArmL' if s<0 else 'ArmR'
        loft('Sleeve',[(s*.220,1.31,0,.065,.068),(s*.248,1.24,0,.068,.071),(s*.260,1.14,.003,.056,.059),(s*.265,1.08,.006,.046,.049)],'ClothCream',tag,n=20,fold=.035)
        loft('Cuff',[(s*.266,1.088,.005,.048,.051),(s*.266,1.056,.005,.048,.051)],'Leather',tag)
        loft('Forearm',[(s*.266,1.063,.005,.040,.042),(s*.27,.98,.016,.035,.038),(s*.273,.91,.022,.026,.029)],'Skin',tag)
        loft('Palm',[(s*.273,.92,.022,.027,.029),(s*.275,.877,.028,.034,.021),(s*.275,.849,.027,.030,.019)],'Skin',tag,n=16)
        for k in range(4):
            xx=s*.275+(k-1.5)*.015
            tube('Finger',[(xx,.86,.032),(xx,.82-abs(k-1.5)*.007,.038),(xx,.802+abs(k-1.5)*.004,.034)],[.009,.008,.004],'Skin',tag,6)
        tube('Thumb',[(s*.249,.894,.032),(s*.234,.862,.051),(s*.240,.842,.058)],[.012,.010,.004],'Skin',tag,7)
        tag='LegL' if s<0 else 'LegR'
        loft('Trousers',[(s*.105,.83,0,.088,.088),(s*.105,.68,0,.080,.081),(s*.11,.50,.014,.060,.066),(s*.11,.43,.015,.057,.062),(s*.11,.31,-.002,.048,.052)],'Trousers',tag,fold=.018)
        loft('Boot',[(s*.11,.035,.048,.061,.13),(s*.11,.075,.051,.067,.135),(s*.11,.135,.023,.062,.103),(s*.11,.22,0,.052,.055),(s*.11,.34,-.002,.060,.06)],'Leather',tag)
        loft('Sole',[(s*.11,.013,.052,.067,.137),(s*.11,.04,.052,.069,.14)],'Iron',tag)
        loft('BootCuff',[(s*.11,.31,0,.061,.063),(s*.11,.35,0,.063,.065)],'LeatherLight',tag)
        for k in range(3): tube('BootLace',[(s*.11-.027,.16+k*.038,.066-k*.006),(s*.11+.027,.18+k*.038,.066-k*.006)],[.002,.002],'ClothCream',tag,5)
    loft('Belt',[(0,.975,0,.185,.135),(0,1.009,0,.184,.133)],'Leather')
    box('Buckle',(0,.993,.140),(.043,.035,.013),'Brass',bevel=.003)
    # Open front seams and tailored hem trim.
    for s in [-1,1]:
        tube('TunicSeam',[(s*.023,.75,.141),(s*.020,.92,.132),(s*.019,1.16,.140),(s*.052,1.255,.135)],[.003]*4,'ClothCream',n=6)
    loft('Hem',[(0,.745,0,.237,.139),(0,.765,0,.232,.137)],'ClothCream',fold=.02)
    loft('Scarf',[(0,1.347,0,.078,.070),(0,1.375,0,.088,.078),(0,1.397,0,.073,.068)],'Coral',fold=.04)
    # Scarf ribbon is a folded mesh, not a billboard image.
    mesh('ScarfTail',[(.03,1.37,-.073),(.09,1.37,-.06),(.11,1.21,-.10),(.08,1.04,-.12),(.03,1.07,-.10),(.06,1.22,-.08)],[(0,1,2,5),(5,2,3,4),(5,2,1,0),(4,3,2,5)],'Coral','ScarfTail',False)
    tube('SatchelStrap',[(-.18,1.30,.074),(-.10,1.23,.12),(.03,1.09,.126),(.18,.91,.12),(.24,.88,.02)],[.016]*5,'Leather',n=4)
    box('Satchel',(.235,.857,.004),(.17,.19,.10),'Leather',bevel=.023)
    box('SatchelFlap',(.235,.918,.048),(.18,.10,.024),'LeatherLight',bevel=.012)
    box('SatchelClasp',(.235,.884,.064),(.020,.033,.010),'Brass',bevel=.003)
    # Small enamel leaf pin gives a distinct original caretaker motif.
    leaf('Pin',(-.056,1.38,.07),(-.01,1.38,.10),.009,'Brass','Body')

def windmill():
    # Tower height/pivot/collision envelope matches existing gameplay landmark.
    loft('Foundation',[(0,0,0,2.77,2.77),(0,.18,0,2.82,2.82),(0,.55,0,2.65,2.65)],'Stone','Static',48)
    loft('MasonryPlinth',[(0,.34,0,2.28,2.28),(0,1.52,0,2.16,2.16)],'Stone','Static',48,fold=.004)
    loft('TaperedPlaster',[(0,1.35,0,2.18,2.18),(0,3.2,0,1.99,1.99),(0,5.3,0,1.77,1.77),(0,7.12,0,1.58,1.58)],'Plaster','Static',48,fold=.005)
    for y,r in [(1.48,2.19),(3.5,1.98),(6.90,1.62)]:
        loft('TimberRing',[(0,y-.055,0,r,r),(0,y+.055,0,r,r)],'Timber','Static',48)
    # Individually laid fieldstone blocks with actual relief.
    for row in range(4):
        for i in range(28):
            a=(i+(row%2)*.5)*2*math.pi/28;r=2.27-row*.027
            ob=box('Fieldstone',(math.sin(a)*r,.53+row*.245,math.cos(a)*r),(.44,.22,.12),'Stone' if (i+row)%4 else 'StoneDark','Static',.025)
            ob.rotation_euler.z=-a
    # Arched oak doorway, raised threshold, strap hinges and handle.
    box('DoorFrame',(0,1.47,2.158),(1.30,1.90,.17),'TimberDark','Static',.05)
    for i in range(7): box('DoorPlank',(-.48+i*.16,1.44,2.255),(.148,1.70,.055),'Timber','Static',.008)
    for y in [.91,1.83]:
        box('IronHinge',(0,y,2.298),(1.04,.07,.035),'Iron','Static',.008)
        for x in [-.40,.40]: box('HingeRivet',(x,y,2.32),(.025,.025,.018),'Brass','Static',.005)
    box('Latch',(.32,1.40,2.32),(.055,.14,.045),'Iron','Static',.008)
    box('Threshold',(0,.56,2.44),(1.55,.16,.67),'Stone','Static',.04)
    # Frames on three sides; mullions make the windows read from the path.
    for yaw,y in [(0,4.85),(90,4.85),(-90,4.85),(0,6.15)]:
        a=math.radians(yaw);r=2.18-(y-1.35)*.103
        for name,p,s,m in [('Frame',(0,y,r+.02),(.79,1.0,.12),'TimberDark'),('Glass',(0,y,r+.09),(.61,.80,.035),'Glass'),('MullionV',(0,y,r+.12),(.045,.85,.025),'Timber'),('MullionH',(0,y,r+.125),(.65,.045,.025),'Timber'),('Sill',(0,y-.52,r+.14),(.94,.10,.30),'Stone')]:
            pp=(p[0]*math.cos(a)+p[2]*math.sin(a),p[1],-p[0]*math.sin(a)+p[2]*math.cos(a))
            ob=box(name,pp,s,m,'Static',.012);ob.rotation_euler.z=-a
    # Steep faceted roof; eight bands of overlapping shingles.
    loft('RoofUnderlay',[(0,6.96,0,1.90,1.90),(0,7.28,0,1.92,1.92),(0,8.96,0,.14,.14),(0,9.12,0,.035,.035)],'RoofDark','Static',32)
    for row in range(8):
        y=7.22+row*.215;r=1.92-row*.223
        count=max(10,32-row*3)
        for i in range(count):
            a=(i+.5*(row%2))*2*math.pi/count; da=math.pi/count*.97
            ro=r+.055;ri=max(.07,r-.26)
            vs=[(math.sin(a-da)*ro,y,math.cos(a-da)*ro),(math.sin(a+da)*ro,y,math.cos(a+da)*ro),(math.sin(a+da)*ri,y+.29,math.cos(a+da)*ri),(math.sin(a-da)*ri,y+.29,math.cos(a-da)*ri)]
            mesh('Shingle',vs,[(0,1,2,3)],'Roof' if (i+row)%4 else 'RoofLight','Static',False)
    tube('Finial',[(0,8.93,0),(0,9.31,0)],[.05,.016],'Brass','Static',12)
    # Timber pilasters and diagonal framing avoid blank vertical expanses.
    for i in range(8):
        a=i*math.pi/4
        tube('Upright',[(math.sin(a)*2.15,1.45,math.cos(a)*2.15),(math.sin(a)*1.61,6.93,math.cos(a)*1.61)],[.065,.05],'Timber','Static',6)

def prop_library():
    # Barrel at origin, metal hoops and individual bulging staves.
    for i in range(14):
        a=i*2*math.pi/14
        ob=loft('BarrelStave',[(0,.03,0,.26,.26),(0,.14,0,.29,.29),(0,.40,0,.32,.32),(0,.68,0,.29,.29),(0,.77,0,.26,.26)],'Timber','Barrel',28)
        # One whole barrel skin is enough, stave definition added as thin grain grooves.
        if i==0: pass
        else: bpy.data.objects.remove(ob,do_unlink=True)
        tube('StaveJoint',[(math.sin(a)*.262,.04,math.cos(a)*.262),(math.sin(a)*.322,.40,math.cos(a)*.322),(math.sin(a)*.262,.76,math.cos(a)*.262)],[.003]*3,'TimberDark','Barrel',5)
    for y,r in [(.10,.289),(.29,.323),(.57,.314),(.70,.285)]: loft('Hoop',[(0,y,0,r,r),(0,y+.035,0,r,r)],'Iron','Barrel',32)
    for i in range(5): box('Lid',(-.20+i*.10,.77,0),(.095,.025,.38),'Timber','Barrel',.005)
    box('CrateBody',(0,.31,0),(.67,.62,.61),'TimberDark','Crate',.01)
    for i in range(5):
        for s in [-1,1]: box('CrateSlat',(-.27+i*.135,.31,s*.316),(.122,.58,.035),'Timber','Crate',.008)
    for y in [.07,.56]:
        for s in [-1,1]: box('CrateBand',(0,y,s*.34),(.71,.07,.045),'TimberLight','Crate',.008)
    for i in range(5):box('CrateLid',(-.27+i*.135,.63,0),(.12,.035,.63),'Timber','Crate',.008)
    loft('Planter',[(0,0,0,.20,.20),(0,.04,0,.23,.23),(0,.34,0,.29,.29),(0,.39,0,.31,.31)],'Clay','Planter',24)
    loft('PlanterRim',[(0,.35,0,.315,.315),(0,.40,0,.315,.315)],'ClayLight','Planter',24)
    loft('Soil',[(0,.385,0,.275,.275),(0,.387,0,.275,.275)],'Soil','Planter',24)

def vegetation():
    # Twisting main trunk, tapered exposed roots and branching scaffold.
    tube('Trunk',[(0,0,0),(.12,1.1,.06),(-.04,2.35,.03),(.20,3.50,-.05),(.3,4.55,.08)],[.28,.20,.16,.09,.025],'Bark','Tree',12)
    for i in range(5):
        a=i*2*math.pi/5
        tube('Root',[(math.sin(a)*.70,.02,math.cos(a)*.70),(math.sin(a)*.32,.13,math.cos(a)*.32),(0,.40,0)],[.018,.10,.15],'Bark','Tree',8)
    rng=random.Random(88)
    for i in range(11):
        a=i*2.399;y=2.0+i*.19;end=(math.sin(a)*(1.0+(i%3)*.25),y+.95,math.cos(a)*(1.0+(i%3)*.25))
        tube('Branch',[(.05,y,0),(end[0]*.55,y+.35,end[2]*.55),end],[.08,.05,.012],'Bark','Tree',7)
        # Hundreds of solid pointed leaves in loose tiered clusters, no lollipop crown.
        for k in range(45):
            x=end[0]+rng.uniform(-.77,.77); yy=end[1]+rng.uniform(-.22,.59); z=end[2]+rng.uniform(-.77,.77)
            angle=rng.random()*math.tau;length=rng.uniform(.18,.34)
            leaf('CanopyLeaf',(x,yy,z),(x+math.sin(angle)*length,yy+.09,z+math.cos(angle)*length),length*.47,'Leaf'+str(k%3),'Tree')
    for i in range(9):
        a=i*2.399;h=.32+(i%3)*.07;x=math.sin(a)*.19;z=math.cos(a)*.19
        tube('FlowerStem',[(x,0,z),(x+.02,h,z)],[.008,.005],'Leaf0','Flowers',6)
        leaf('BasalLeaf',(x,.05,z),(x+.15,.19,z+.08),.05,'Leaf1','Flowers')
        for p in range(6):
            aa=p*math.tau/6
            leaf('Petal',(x+.02,h,z),(x+.02+math.sin(aa)*.10,h+.025,z+math.cos(aa)*.10),.043,'Petal'+str(i%3),'Flowers')
        loft('Pollen',[(x+.02,h,z,.022,.022),(x+.02,h+.025,z,.013,.013)],'Brass','Flowers',10)
    for i in range(16):
        a=i*2.399;length=.18+(i%4)*.07
        leaf('Broadleaf',(0,.01,0),(math.sin(a)*length,.08+(i%3)*.08,math.cos(a)*length),length*.23,'Leaf'+str(i%3),'Broadleaf')

def villagers(originals):
    for role,cloth,hair in [('Mira','ClothSage','HairGolden'),('Wren','ClothOchre','HairDark'),('Alder','ClothSlate','HairSilver')]:
        for src in originals:
            if src.name.startswith(('Satchel','Pin','Ponytail','ScarfTail')): continue
            ob=src.copy();ob.data=src.data.copy();bpy.context.collection.objects.link(ob)
            ob.name=role+'_'+src.name;ob['rig']=role+'_'+src['rig']
            old=ob.data.materials[0].name
            replacement=cloth if old=='ClothTeal' else hair if old=='Hair' else old
            ob.data.materials.clear();ob.data.materials.append(mat(replacement))
        body=role+'_Body';head=role+'_Head';arm=role+'_ArmR'
        if role=='Mira':
            loft('MiraDress',[(0,.20,0,.28,.22),(0,.30,0,.285,.22),(0,.72,0,.235,.15),(0,1.00,0,.18,.13)],cloth,body,n=24,fold=.018)
            mesh('MiraApron',[(-.17,.28,.23),(.17,.28,.23),(.13,.93,.15),(-.13,.93,.15)],[(0,1,2,3)],'ClothCream',body,False)
            tube('MiraBraid',[(.055,1.57,-.12),(.055,1.42,-.16),(.055,1.23,-.15),(.055,1.12,-.13)],[.04,.035,.025,.008],hair,head,12)
            for j in range(5):
                leaf('MiraHerb',(.27,.82,.02),(.31+j*.025,.99+j*.018,.035),.025,'Leaf1',arm)
            loft('MiraBasket',[(.29,.74,.03,.11,.08),(.29,.83,.03,.14,.10)],'Timber',arm,20)
        elif role=='Wren':
            loft('WrenHatBrim',[(0,1.65,-.01,.29,.26),(0,1.67,-.01,.30,.27)],'Straw',head,40)
            loft('WrenHatCrown',[(0,1.67,-.01,.145,.14),(0,1.75,-.01,.13,.12),(0,1.80,-.01,.09,.08)],'Straw',head,28)
            loft('WrenHatBand',[(0,1.68,-.01,.147,.142),(0,1.71,-.01,.145,.14)],cloth,head,28)
            mesh('WrenApron',[(-.17,.76,.14),(.17,.76,.14),(.12,1.20,.145),(-.12,1.20,.145)],[(0,1,2,3)],'ClothCream',body,False)
            box('SeedPocket',(.055,.93,.154),(.13,.10,.022),'LeatherLight',body,.008)
            tube('TrowelHandle',[(.28,.84,.05),(.28,.84,.24)],[.016,.016],'Timber',arm,8)
            leaf('TrowelBlade',(.28,.84,.23),(.28,.82,.38),.044,'Iron',arm)
        else:
            loft('AlderCap',[(0,1.61,-.01,.118,.11),(0,1.66,-.01,.12,.115),(0,1.70,-.02,.08,.075)],'Leather',head,24)
            box('AlderCapBill',(0,1.62,.105),(.18,.018,.10),'Leather',head,.012)
            for s in [-1,1]:
                tube('AlderMoustache',[(0,1.484,.087),(s*.035,1.478,.091),(s*.06,1.486,.076)],[.006,.009,.002],hair,head,8)
            loft('AlderBeard',[(0,1.391,.055,.025,.025),(0,1.425,.055,.058,.037),(0,1.46,.055,.075,.041)],hair,head,24)
            box('AlderToolPouch',(-.20,.89,.045),(.13,.17,.09),'LeatherLight',body,.018)
            tube('MalletHandle',[(.28,.84,.06),(.28,.84,.28)],[.015,.015],'Timber',arm,8)
            box('MalletHead',(.28,.84,.29),(.20,.09,.09),'TimberDark',arm,.014)

def chicken():
    r='Clover'
    loft('CloverBody',[(0,.17,-.03,.08,.17),(0,.23,-.04,.19,.28),(0,.37,-.035,.255,.32),(0,.49,0,.21,.25),(0,.56,.12,.11,.13)],'Feather',r,28)
    loft('CloverNeck',[(0,.40,.18,.10,.11),(0,.55,.23,.072,.08),(0,.64,.25,.072,.075)],'Feather',r,24)
    loft('CloverHead',[(0,.56,.26,.04,.05),(0,.61,.28,.095,.085),(0,.69,.27,.089,.077),(0,.73,.26,.025,.03)],'Feather',r,24)
    mesh('CloverBeak',[(-.045,.63,.34),(.045,.63,.34),(0,.59,.35),(0,.615,.45),(0,.66,.34)],[(0,2,3),(2,1,3),(1,4,3),(4,0,3),(0,4,1,2)],'Brass',r,False)
    for s in [-1,1]:
        loft('CloverEye',[(s*.083,.642,.306,.014,.014),(s*.085,.663,.307,.014,.014)],'Iris',r,14)
        loft('CloverWing',[(s*.19,.24,-.04,.035,.15),(s*.23,.35,-.04,.065,.20),(s*.20,.45,-.02,.040,.16)],'FeatherShade',r,24)
        tube('CloverLeg',[(s*.09,.20,.03),(s*.09,.055,.04)],[.018,.013],'Brass',r,8)
        for j in [-1,0,1]:tube('CloverToe',[(s*.09,.06,.04),(s*.09+j*.055,.025,.15)],[.011,.004],'Brass',r,7)
        for j in range(4):
            leaf('WingFeather',(s*.24,.38,-.02+j*.04),(s*.245,.24,-.14+j*.04),.027,'Feather',r)
    for i in range(5):
        leaf('TailFeather',((i-2)*.025,.38,-.24),((i-2)*.062,.66-abs(i-2)*.025,-.45),.045,'FeatherShade',r)
    for j in range(3):
        loft('Comb',[(0,.70,.24+j*.035,.025,.02),(0,.79+(j%2)*.025,.24+j*.035,.022,.025),(0,.81+(j%2)*.025,.24+j*.035,.004,.006)],'Coral',r,12)
    loft('Wattle',[(0,.51,.34,.012,.015),(0,.58,.35,.025,.025),(0,.60,.34,.014,.015)],'Coral',r,16)

hero();hero_objects=list(bpy.context.scene.objects)
villagers(hero_objects);chicken();windmill();prop_library();vegetation()
# Split by rig/material, merging meshes without forcing game-visible primitive joins.
groups={}
for ob in list(bpy.context.scene.objects):
    if ob.type=='MESH': groups.setdefault((ob['rig'],ob.data.materials[0].name),[]).append(ob)
parts=[]
for (rig,material),obs in sorted(groups.items()):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs: ob.select_set(True)
    bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join()
    ob=bpy.context.object;ob.name=rig+'_'+material
    me=ob.data;me.calc_loop_triangles();vs=[];ns=[];uvs=[];ts=[]
    normalmatrix=ob.matrix_world.to_3x3().inverted().transposed()
    for tri in me.loop_triangles:
        # This coordinate map is a rotation (determinant +1), not a handedness flip.
        # Keep winding aligned with the exported outward normals, as Unity's native meshes do.
        for li in tri.loops:
            v=ob.matrix_world@me.vertices[me.loops[li].vertex_index].co
            normal=normalmatrix@me.corner_normals[li].vector
            vs.extend([round(v.x,6),round(v.z,6),round(-v.y,6)])
            ns.extend([round(normal.x,6),round(normal.z,6),round(-normal.y,6)])
            uv=me.uv_layers.active.data[li].uv if me.uv_layers.active else (v.x,v.z)
            # Bevel loop UVs vary at ~1e-6 between Blender runs. A 1e-4 UV grid is
            # finer than 1/32 of a 256px texel and removes this irrelevant jitter.
            uvs.extend([round(uv[0],4),round(uv[1],4)]);ts.append(len(ts))
    parts.append(dict(name=ob.name,rig=rig,material=material,vertices=vs,normals=ns,uv=uvs,triangles=ts))
(OUT/'pilot-meshes.json').write_text(json.dumps(dict(parts=parts),separators=(',',':')),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'echoes-pilot.blend'))
bpy.ops.export_scene.fbx(filepath=str(SOURCE/'echoes-pilot.fbx'),use_selection=False,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
print('ECHOES_PILOT_PARTS='+str(len(parts)))
print('ECHOES_PILOT_TRIANGLES='+str(sum(len(p['triangles'])//3 for p in parts)))
