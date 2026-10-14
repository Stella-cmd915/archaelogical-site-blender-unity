# -*- coding: utf-8 -*-
# =====================================================================
#  02_render_views.py
#  Βγάζει αυτόματα 6 renders του χώρου (αυτά που θα βάλεις στο κείμενο).
#
#  ΠΡΟΫΠΟΘΕΣΗ: να έχεις ήδη τρέξει το 01_build_temple.py στην ίδια σκηνή.
#
#  ΠΩΣ ΤΡΕΧΕΙ: Scripting -> Open -> αυτό το αρχείο -> Run Script
#
#  Τα PNG αποθηκεύονται δίπλα στο .blend, στον φάκελο "renders".
#  Αν δεν έχεις σώσει ακόμα το .blend, πάνε στο C:\TempleRenders (Windows)
#  ή ~/TempleRenders (Mac/Linux).
# =====================================================================

import bpy
import os
import math

# ---------------------------------------------------------------------
ENGINE      = 'EEVEE'   # 'EEVEE' = γρήγορο (δευτερόλεπτα) | 'CYCLES' = πιο όμορφο (λεπτά)
SAMPLES     = 64
RES_X       = 1920
RES_Y       = 1080
FILE_FORMAT = 'PNG'
# ---------------------------------------------------------------------


def output_dir():
    if bpy.data.filepath:
        d = os.path.join(os.path.dirname(bpy.data.filepath), "renders")
    else:
        d = os.path.join(os.path.expanduser("~"), "TempleRenders")
    os.makedirs(d, exist_ok=True)
    return d


def set_engine():
    sc = bpy.context.scene
    if ENGINE.upper() == 'CYCLES':
        sc.render.engine = 'CYCLES'
        sc.cycles.samples = SAMPLES
        sc.cycles.use_denoising = True
    else:
        # Το EEVEE έχει διαφορετικό όνομα ανά έκδοση Blender
        for name in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE'):
            try:
                sc.render.engine = name
                break
            except Exception:
                continue
        try:
            sc.eevee.taa_render_samples = SAMPLES
        except Exception:
            pass
    sc.render.resolution_x = RES_X
    sc.render.resolution_y = RES_Y
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = FILE_FORMAT
    try:
        sc.view_settings.look = 'AgX - Medium High Contrast'
    except Exception:
        pass


def get_camera():
    cam = bpy.context.scene.camera
    if cam is None:
        raise RuntimeError("Δεν υπάρχει κάμερα - τρέξε πρώτα το 01_build_temple.py")
    return cam


# (όνομα αρχείου, θέση κάμερας, περιστροφή σε μοίρες, εστιακή απόσταση)
VIEWS = [
    ("01_geniki_apopsi",      (46.0, -40.0, 17.0), (72, 0, 52),  32),
    ("02_prosopsi",           (0.0, -52.0, 9.0),   (85, 0, 0),   35),
    ("03_plagia_opsi",        (58.0, 2.0, 10.0),   (84, 0, 90),  40),
    ("04_leptomereia_kionon", (0.0, -22.0, 2.2),   (78, 0, 8),   50),
    ("05_eisodos_sikou",      (0.0, -14.5, 2.0),   (88, 0, 0),   28),
    ("06_ereipia",            (34.0, -26.0, 2.4),  (86, 0, 40),  35),
]


def main():
    set_engine()
    cam = get_camera()
    out = output_dir()
    saved = []

    old_loc = tuple(cam.location)
    old_rot = tuple(cam.rotation_euler)
    old_lens = cam.data.lens

    for name, loc, rot_deg, lens in VIEWS:
        cam.location = loc
        cam.rotation_euler = tuple(math.radians(a) for a in rot_deg)
        cam.data.lens = lens
        path = os.path.join(out, name + ".png")
        bpy.context.scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        saved.append(path)
        print("  -> " + path)

    cam.location = old_loc
    cam.rotation_euler = old_rot
    cam.data.lens = old_lens

    print("=" * 60)
    print("OK - %d renders αποθηκεύτηκαν στον φάκελο:" % len(saved))
    print("   " + out)
    print("Αυτά τα βάζεις στο .docx/.pdf της τεκμηρίωσης.")
    print("=" * 60)


main()
