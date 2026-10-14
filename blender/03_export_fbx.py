# -*- coding: utf-8 -*-
# =====================================================================
#  03_export_fbx.py
#  Κάνει UV unwrap σε όλα τα αντικείμενα και εξάγει .fbx έτοιμο για Unity
#  με τις ΣΩΣΤΕΣ ρυθμίσεις αξόνων (αλλιώς το μοντέλο μπαίνει ξαπλωμένο).
#
#  ΠΡΟΫΠΟΘΕΣΗ: να έχεις τρέξει το 01_build_temple.py
#  ΠΩΣ ΤΡΕΧΕΙ:  Scripting -> Open -> αυτό το αρχείο -> Run Script
#
#  Το αρχείο βγαίνει ως:  <φάκελος του .blend>/export/ArchaeologicalSite.fbx
#  (ή ~/TempleExport/ArchaeologicalSite.fbx αν δεν έχεις σώσει το .blend)
# =====================================================================

import bpy
import os

DO_UV_UNWRAP = True          # κάνε False αν αργεί πολύ
FBX_NAME = "ArchaeologicalSite.fbx"


def output_dir():
    if bpy.data.filepath:
        d = os.path.join(os.path.dirname(bpy.data.filepath), "export")
    else:
        d = os.path.join(os.path.expanduser("~"), "TempleExport")
    os.makedirs(d, exist_ok=True)
    return d


def mesh_objects():
    return [o for o in bpy.context.scene.objects if o.type == 'MESH']


def uv_unwrap_all():
    """Smart UV Project σε όλα τα meshes -> ώστε να δέχονται textures στη Unity."""
    objs = mesh_objects()
    if not objs:
        return
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    try:
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=0.02)
        bpy.ops.object.mode_set(mode='OBJECT')
        print("  UV unwrap: OK (%d αντικείμενα)" % len(objs))
    except Exception as e:
        try:
            bpy.ops.object.mode_set(mode='OBJECT')
        except Exception:
            pass
        print("  UV unwrap παραλείφθηκε:", e)


def export_fbx(path):
    bpy.ops.object.select_all(action='SELECT')
    kwargs = dict(
        filepath=path,
        use_selection=False,
        object_types={'MESH', 'EMPTY'},
        # --- ΑΞΟΝΕΣ: αυτό είναι το κρίσιμο για τη Unity ---
        axis_forward='-Z',
        axis_up='Y',
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        use_space_transform=True,
        bake_space_transform=False,
        # --- γεωμετρία ---
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        use_tspace=False,
        use_triangles=True,
        # --- τι ΔΕΝ χρειαζόμαστε ---
        add_leaf_bones=False,
        bake_anim=False,
        path_mode='COPY',
        embed_textures=False,
    )
    try:
        bpy.ops.export_scene.fbx(**kwargs)
    except TypeError:
        # παλιότερες/νεότερες εκδόσεις μπορεί να μη δέχονται κάποιο όρισμα
        for k in ('use_tspace', 'use_triangles', 'embed_textures', 'use_space_transform'):
            kwargs.pop(k, None)
        bpy.ops.export_scene.fbx(**kwargs)


def main():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    if DO_UV_UNWRAP:
        uv_unwrap_all()

    out = os.path.join(output_dir(), FBX_NAME)
    export_fbx(out)

    print("=" * 62)
    print("OK - Το FBX εξήχθη:")
    print("   " + out)
    print()
    print("ΣΤΗ UNITY:")
    print("  1. Σύρε αυτό το .fbx μέσα στον φάκελο Assets/Models/")
    print("  2. Κλικ πάνω του -> Inspector -> Model:")
    print("       Scale Factor = 1")
    print("       Convert Units = OFF")
    print("       Generate Colliders = ON")
    print("       Generate Lightmap UVs = ON")
    print("     -> Apply")
    print("=" * 62)


main()
