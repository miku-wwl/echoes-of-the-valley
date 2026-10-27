"""Quick Workbench previews of the parametric model (geometry check only; the real look is judged in the Unity player).

blender --background --factory-startup --python tools/art/phase52b2/blender_preview.py -- <outdir> [view ...]
Views: front side back threeq headfront headside headback headthreeq
"""
import bpy, sys, os, math, importlib
import numpy as np
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import kit, atlas, design
importlib.reload(kit); importlib.reload(atlas); importlib.reload(design)

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
outdir = args[0] if args else os.path.join(HERE, 'preview')
views = args[1:] or ['front', 'side', 'back', 'headfront', 'headside']
os.makedirs(outdir, exist_ok=True)

cv = lambda v: (v[0], -v[2], v[1])
srgb = lambda c: c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
parts = design.build_model()
print('PARTS', len(parts), 'VERTS', sum(len(p.V) for p in parts), 'TRIS', sum(len(p.F) for p in parts))

for p in parts:
    me = bpy.data.meshes.new(p.name)
    me.from_pydata([cv(v) for v in p.V], [], p.F.tolist())
    me.update()
    for poly in me.polygons:
        poly.use_smooth = True
    base = atlas.PALETTE.get(p.material, '888888')
    rgb = [srgb(int(base[i:i + 2], 16) / 255) for i in (0, 2, 4)]
    col = me.color_attributes.new('Col', 'FLOAT_COLOR', 'POINT')
    flat = np.concatenate([np.clip(np.array(rgb)[None, :] * p.C[:, :3], 0, 1), np.ones((len(p.V), 1))], 1).reshape(-1)
    col.data.foreach_set('color', flat.tolist())
    ob = bpy.data.objects.new(p.name, me)
    bpy.context.collection.objects.link(ob)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x, scene.render.resolution_y = 900, 1200
shading = scene.display.shading
shading.light = 'STUDIO'
shading.color_type = 'VERTEX'
shading.show_object_outline = True
shading.object_outline_color = (0.08, 0.04, 0.03)
shading.show_cavity = False
scene.display.render_aa = '8'
scene.view_settings.view_transform = 'Standard'
world = bpy.data.worlds.new('W'); world.color = (0.55, 0.62, 0.68); scene.world = world
shading.background_type = 'WORLD'

cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); bpy.context.collection.objects.link(cam); scene.camera = cam


def shoot(name, at, target, lens=None, ortho=None):
    cam.location = cv(at)
    d = Vector(cv(target)) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    if ortho:
        cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho
    else:
        cam.data.type = 'PERSP'; cam.data.lens = lens or 70
    scene.render.filepath = os.path.join(outdir, name + '.png')
    bpy.ops.render.render(write_still=True)


H = design.HEAD_C
table = {
    'front': ((0, 0.86, 5), (0, 0.86, 0), None, 1.95),
    'side': ((5, 0.86, 0), (0, 0.86, 0), None, 1.95),
    'back': ((0, 0.86, -5), (0, 0.86, 0), None, 1.95),
    'threeq': ((3.2, 1.05, 3.6), (0, 0.84, 0), None, 1.95),
    'headfront': ((0, 1.54, 1.0), (0, 1.52, 0), 85, None),
    'headside': ((1.0, 1.54, 0), (0, 1.52, -0.02), 85, None),
    'headback': ((0, 1.50, -1.0), (0, 1.48, 0), 70, None),
    'headthreeq': ((0.62, 1.58, 0.72), (0, 1.50, 0), 80, None),
}
for v in views:
    at, tg, lens, ortho = table[v]
    shoot(v, at, tg, lens, ortho)
print('PREVIEW_DONE', outdir)
