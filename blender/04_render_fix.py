# -*- coding: utf-8 -*-
# =====================================================================
#  04_render_fix.py     (αντικαθιστά το 02_render_views.py)
#
#  ΓΙΑΤΙ ΥΠΑΡΧΕΙ: στα renders 04 και 05 η κάμερα ήταν πολύ χαμηλά και πολύ
#  κοντά, και ο φωτισμός έβγαινε "καμένος" (υπερβολικά λευκός). Εδώ:
#     · η κάμερα ΣΤΟΧΕΥΕΙ σε σημείο (look-at) αντί για σταθερές γωνίες
#     · μειώνεται η έκθεση ώστε να μη χάνονται οι λεπτομέρειες
#     · χαμηλώνουν τα φώτα του σηκού που έκαιγαν το εσωτερικό
#
#  ΠΡΟΫΠΟΘΕΣΗ: έχεις ήδη τρέξει το 01_build_temple.py και έχεις σώσει
#              το naos.blend.
#
#  ΠΩΣ ΤΡΕΧΕΙ: Scripting -> Open -> αυτό το αρχείο -> Run Script
#  Ξαναγράφει και τα 6 PNG στον φάκελο renders/ (πάνω στα παλιά).
# =====================================================================

import bpy
import os
import math
from mathutils import Vector

# ---------------------------------------------------------------------
ENGINE   = 'EEVEE'    # 'EEVEE' γρήγορο | 'CYCLES' πιο όμορφο αλλά αργό
SAMPLES  = 96
RES_X    = 1920
RES_Y    = 1080
EXPOSURE = -0.55      # πιο αρνητικό = πιο σκούρο. Δοκίμασε -0.9 αν είναι ακόμη καμένο
CELLA_LIGHT_ENERGY = 260.0   # ήταν 900 -> έκαιγε το εσωτερικό
# ---------------------------------------------------------------------


def output_dir():
    if bpy.data.filepath:
        d = os.path.join(os.path.dirname(bpy.data.filepath), "renders")
    else:
        d = os.path.join(os.path.expanduser("~"), "TempleRenders")
    os.makedirs(d, exist_ok=True)
    return d


def setup_engine():
    sc = bpy.context.scene
    if ENGINE.upper() == 'CYCLES':
        sc.render.engine = 'CYCLES'
        sc.cycles.samples = SAMPLES
        sc.cycles.use_denoising = True
    else:
        for name in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE'):
            try:
                sc.render.engine = name
                break
            except Exception:
                continue
        for attr in ('taa_render_samples',):
            try:
                setattr(sc.eevee, attr, SAMPLES)
            except Exception:
                pass
        # μαλακές σκιές & ambient occlusion όπου υπάρχουν
        for attr, val in (('use_gtao', True), ('use_soft_shadows', True),
                          ('use_shadows', True), ('use_raytracing', True)):
            try:
                setattr(sc.eevee, attr, val)
            except Exception:
                pass

    sc.render.resolution_x = RES_X
    sc.render.resolution_y = RES_Y
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = 'PNG'

    # --- τονική απόδοση: το κλειδί για να μη βγαίνει κάτασπρο ---
    vs = sc.view_settings
    try:
        vs.view_transform = 'AgX'
    except Exception:
        try:
            vs.view_transform = 'Filmic'
        except Exception:
            pass
    for look in ('AgX - Medium High Contrast', 'Medium High Contrast',
                 'Filmic - Medium High Contrast', 'None'):
        try:
            vs.look = look
            break
        except Exception:
            continue
    vs.exposure = EXPOSURE


def tame_cella_lights():
    for ob in bpy.data.objects:
        if ob.type == 'LIGHT' and ob.name.startswith("CellaLight"):
            ob.data.energy = CELLA_LIGHT_ENERGY


def look_at(cam, target):
    """Στρέφει την κάμερα ώστε να κοιτάει το σημείο target."""
    d = Vector(target) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()


# (όνομα, θέση κάμερας, σημείο-στόχος, εστιακή απόσταση)
VIEWS = [
    ("01_geniki_apopsi",      (44.0, -42.0, 18.0),  (0.0,   0.0,  5.5), 34),
    ("02_prosopsi",           (0.0,  -54.0, 10.5),  (0.0,   0.0,  7.0), 38),
    ("03_plagia_opsi",        (60.0,   3.0, 11.0),  (0.0,   0.0,  6.0), 42),
    ("04_leptomereia_kionon", (14.2, -21.5,  1.75), (7.6, -11.5,  8.2), 34),
    ("05_eisodos_sikou",      (0.0,  -20.5,  1.75), (0.0,  -2.0,  3.0), 30),
    ("06_ereipia",            (33.0, -28.0,  2.10), (4.0, -12.0,  6.0), 36),
]


def main():
    cam = bpy.context.scene.camera
    if cam is None:
        raise RuntimeError("Δεν υπάρχει κάμερα. Τρέξε πρώτα το 01_build_temple.py")

    setup_engine()
    tame_cella_lights()

    out = output_dir()
    for name, pos, target, lens in VIEWS:
        cam.location = Vector(pos)
        cam.data.lens = lens
        look_at(cam, target)
        bpy.context.view_layer.update()
        bpy.context.scene.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("  -> " + name + ".png")

    print("=" * 62)
    print("OK - 6 renders ξαναγράφτηκαν στον φάκελο:")
    print("   " + out)
    print()
    print("Αν κάποιο βγαίνει ακόμη πολύ φωτεινό, άλλαξε στην κορυφή:")
    print("   EXPOSURE = -0.9   και ξανατρέξε.")
    print("=" * 62)


main()
