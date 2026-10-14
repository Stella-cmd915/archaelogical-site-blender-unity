# -*- coding: utf-8 -*-
# =====================================================================
#  01_build_temple.py
#  Δημιουργεί ΟΛΟΚΛΗΡΟ τον αρχαιολογικό χώρο με τον Δωρικό ναό.
#
#  ΠΩΣ ΤΡΕΧΕΙ:
#    Blender  ->  καρτέλα "Scripting" (πάνω μπάρα)
#             ->  Open  ->  διάλεξε αυτό το αρχείο
#             ->  κουμπί "Run Script" (το ▶ πάνω δεξιά στον editor)
#             ή πάτα Alt + P με τον κέρσορα μέσα στον editor
#
#  Δουλεύει σε Blender 3.0 έως 4.x. Δεν χρειάζεται κανένα addon,
#  καμία εικόνα, κανένα εξωτερικό asset. Όλα φτιάχνονται με κώδικα.
#
#  ΜΟΝΑΔΕΣ: μέτρα (1 Blender unit = 1 m) -> σωστό scale στη Unity.
# =====================================================================

import bpy
import bmesh
import math
import random
from mathutils import Vector

# ---------------------------------------------------------------------
# ΡΥΘΜΙΣΕΙΣ  -  άλλαξε ό,τι θέλεις εδώ και ξανατρέξε το script
# ---------------------------------------------------------------------
SEED              = 7        # αλλάζοντάς το, αλλάζει η διάταξη των ερειπίων

COLS_FRONT        = 6        # κίονες στην πρόσοψη (κλασικός Δωρικός: 6)
COLS_SIDE         = 11       # κίονες στη μακριά πλευρά
COL_SPACING       = 2.80     # απόσταση κέντρου-κέντρου κιόνων (m)

COL_HEIGHT        = 5.60     # ύψος κορμού κίονα (m)
COL_R_BOTTOM      = 0.52     # ακτίνα βάσης κορμού
COL_R_TOP         = 0.40     # ακτίνα κορυφής κορμού (μείωσις)
COL_ENTASIS       = 0.035    # ελαφρύ "φούσκωμα" στη μέση του κορμού
COL_FLUTES        = 20       # ραβδώσεις (ο Δωρικός έχει 20)
COL_FLUTE_DEPTH   = 0.055    # βάθος ραβδώσεων (0 = λείος κίονας)
COL_RING_SEGMENTS = 8        # τμήματα καθ' ύψος (περισσότερα = ομαλότερη εντασις)

STEP_COUNT        = 3        # βαθμίδες κρηπίδας
STEP_HEIGHT       = 0.34
STEP_OVERHANG     = 0.55     # πόσο προεξέχει κάθε βαθμίδα από την από πάνω

ARCHITRAVE_H      = 0.95     # επιστύλιο
FRIEZE_H          = 0.85     # ζωφόρος (τρίγλυφα + μετόπες)
CORNICE_H         = 0.42     # γείσο
CORNICE_OUT       = 0.42     # προεξοχή γείσου
PEDIMENT_H        = 2.30     # ύψος αετώματος

CELLA_INSET       = 1.35 * COL_SPACING   # πόσο μέσα από την κρηπίδα ο σηκός
CELLA_WALL_H      = 5.10
CELLA_WALL_T      = 0.62
CELLA_DOOR_W      = 2.60
CELLA_DOOR_H      = 3.60

GROUND_SIZE       = 140.0    # πλευρά του εδάφους (m)
RUIN_COUNT        = 26       # πεσμένοι σπόνδυλοι / λιθόπλινθοι γύρω
TREE_COUNT        = 18       # κυπαρίσσια περιμετρικά (0 = κανένα)

BUILD_INFO_MARKERS = True    # Empties στα σημεία τεκμηρίωσης (φαίνονται στη Unity)

random.seed(SEED)

# ---------------------------------------------------------------------
# ΒΟΗΘΗΤΙΚΑ
# ---------------------------------------------------------------------

COLLECTION_NAME = "ArchaeologicalSite"


def purge_scene():
    """Σβήνει τα πάντα από τη σκηνή για καθαρό ξεκίνημα."""
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials,
                  bpy.data.curves, bpy.data.images):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def get_collection():
    col = bpy.data.collections.get(COLLECTION_NAME)
    if col is None:
        col = bpy.data.collections.new(COLLECTION_NAME)
        bpy.context.scene.collection.children.link(col)
    return col


def new_object(name, mesh, mat=None, parent=None):
    ob = bpy.data.objects.new(name, mesh)
    get_collection().objects.link(ob)
    if mat is not None and mat.name not in [m.name for m in ob.data.materials if m]:
        # προσοχή: πολλά objects μοιράζονται το ίδιο mesh -> βάζουμε το υλικό
        # μία μόνο φορά, αλλιώς δημιουργούνται διπλά material slots
        ob.data.materials.append(mat)
    if parent is not None:
        ob.parent = parent
    return ob


def mesh_from_pydata(name, verts, faces):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    me.validate()
    return me


def box_mesh(name, sx, sy, sz, centered_z=False):
    """Κουτί sx x sy x sz. Origin: κέντρο βάσης (ή κέντρο αν centered_z)."""
    z0 = -sz / 2.0 if centered_z else 0.0
    z1 = z0 + sz
    hx, hy = sx / 2.0, sy / 2.0
    v = [(-hx, -hy, z0), (hx, -hy, z0), (hx, hy, z0), (-hx, hy, z0),
         (-hx, -hy, z1), (hx, -hy, z1), (hx, hy, z1), (-hx, hy, z1)]
    f = [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
         (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return mesh_from_pydata(name, v, f)


def frame_box(name, out_x, out_y, sz, thickness):
    """Κλειστό ορθογώνιο 'κάδρο' (4 τοίχοι) -> για κρηπίδα/τοιχοβάτη."""
    bm = bmesh.new()
    hx_o, hy_o = out_x / 2.0, out_y / 2.0
    hx_i, hy_i = hx_o - thickness, hy_o - thickness

    def ring(z):
        outer = [bm.verts.new((-hx_o, -hy_o, z)), bm.verts.new((hx_o, -hy_o, z)),
                 bm.verts.new((hx_o, hy_o, z)), bm.verts.new((-hx_o, hy_o, z))]
        inner = [bm.verts.new((-hx_i, -hy_i, z)), bm.verts.new((hx_i, -hy_i, z)),
                 bm.verts.new((hx_i, hy_i, z)), bm.verts.new((-hx_i, hy_i, z))]
        return outer, inner

    o0, i0 = ring(0.0)
    o1, i1 = ring(sz)
    for k in range(4):
        n = (k + 1) % 4
        bm.faces.new((o0[k], o0[n], o1[n], o1[k]))       # εξωτερικό
        bm.faces.new((i1[k], i1[n], i0[n], i0[k]))       # εσωτερικό
        bm.faces.new((o1[k], o1[n], i1[n], i1[k]))       # πάνω
        bm.faces.new((i0[k], i0[n], o0[n], o0[k]))       # κάτω
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.update()
    return me


# ---------------------------------------------------------------------
# ΥΛΙΚΑ  (μόνο για τα renders στο Blender - στη Unity τα ξαναφτιάχνουμε)
# ---------------------------------------------------------------------

def make_stone_material(name, rgb, roughness=0.72, bump=0.28, scale=9.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial"); out.location = (620, 0)
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled"); bsdf.location = (300, 0)
    noise = nt.nodes.new("ShaderNodeTexNoise");      noise.location = (-320, -160)
    musg = nt.nodes.new("ShaderNodeTexMusgrave") if "ShaderNodeTexMusgrave" in dir(bpy.types) else None
    bumpn = nt.nodes.new("ShaderNodeBump");          bumpn.location = (60, -220)
    ramp = nt.nodes.new("ShaderNodeValToRGB");       ramp.location = (-60, 60)

    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = 8.0
    if "Roughness" in noise.inputs:
        noise.inputs["Roughness"].default_value = 0.62

    # ελαφριά διακύμανση χρώματος
    r, g, b = rgb
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (r * 0.82, g * 0.82, b * 0.82, 1)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (min(r * 1.10, 1), min(g * 1.10, 1), min(b * 1.10, 1), 1)

    bumpn.inputs["Strength"].default_value = bump

    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(noise.outputs["Fac"], bumpn.inputs["Height"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(bumpn.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = roughness
    if "Specular IOR Level" in bsdf.inputs:          # Blender 4.x
        bsdf.inputs["Specular IOR Level"].default_value = 0.35
    elif "Specular" in bsdf.inputs:                  # Blender 3.x
        bsdf.inputs["Specular"].default_value = 0.35
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


MAT = {}


def build_materials():
    MAT["marble"] = make_stone_material("M_Marble",      (0.86, 0.83, 0.75), 0.62, 0.22, 12.0)
    MAT["marble_worn"] = make_stone_material("M_MarbleWorn", (0.74, 0.70, 0.61), 0.80, 0.42, 8.0)
    MAT["limestone"] = make_stone_material("M_Limestone", (0.68, 0.64, 0.55), 0.85, 0.45, 6.0)
    MAT["roof"] = make_stone_material("M_RoofTile",   (0.55, 0.36, 0.27), 0.78, 0.30, 14.0)
    MAT["ground"] = make_stone_material("M_Ground",     (0.42, 0.38, 0.28), 0.95, 0.55, 3.0)
    MAT["path"] = make_stone_material("M_Path",       (0.58, 0.54, 0.46), 0.92, 0.50, 5.0)
    MAT["tree"] = make_stone_material("M_Cypress",    (0.10, 0.20, 0.09), 0.90, 0.30, 20.0)


# ---------------------------------------------------------------------
# ΓΕΩΜΕΤΡΙΑ ΝΑΟΥ
# ---------------------------------------------------------------------

def temple_footprint():
    """Επιστρέφει (πλάτος, βάθος) της ανώτερης βαθμίδας (στυλοβάτη)."""
    w = (COLS_FRONT - 1) * COL_SPACING + 2 * COL_SPACING * 0.55
    d = (COLS_SIDE - 1) * COL_SPACING + 2 * COL_SPACING * 0.55
    return w, d


def build_krepis(parent):
    """Οι 3 βαθμίδες (κρηπίδα) - πάνω τους στέκεται ο ναός."""
    w, d = temple_footprint()
    z = 0.0
    tops = []
    for i in range(STEP_COUNT):
        grow = STEP_OVERHANG * (STEP_COUNT - 1 - i)
        sx = w + 2 * grow
        sy = d + 2 * grow
        me = box_mesh("Krepis_%d" % (i + 1), sx, sy, STEP_HEIGHT)
        ob = new_object("Krepis_%02d" % (i + 1), me,
                        MAT["marble_worn"] if i < STEP_COUNT - 1 else MAT["marble"],
                        parent)
        ob.location = (0, 0, z)
        tops.append(z + STEP_HEIGHT)
        z += STEP_HEIGHT
    return z            # ύψος στυλοβάτη


def column_shaft_mesh(name):
    """Ραβδωτός κορμός Δωρικού κίονα με έντασιν."""
    seg = max(COL_FLUTES * 4, 24)
    rings = max(COL_RING_SEGMENTS, 2)
    verts, faces = [], []

    for ri in range(rings + 1):
        t = ri / float(rings)
        z = t * COL_HEIGHT
        # μείωσις (γραμμική) + έντασις (ημιτονοειδές φούσκωμα στη μέση)
        base_r = COL_R_BOTTOM + (COL_R_TOP - COL_R_BOTTOM) * t
        base_r += COL_ENTASIS * math.sin(math.pi * t)
        for si in range(seg):
            a = 2.0 * math.pi * si / seg
            # ραβδώσεις: κοίλες αυλακώσεις
            flute = 0.5 + 0.5 * math.cos(COL_FLUTES * a)
            r = base_r * (1.0 - COL_FLUTE_DEPTH * flute)
            verts.append((r * math.cos(a), r * math.sin(a), z))

    for ri in range(rings):
        for si in range(seg):
            a0 = ri * seg + si
            a1 = ri * seg + (si + 1) % seg
            b0 = a0 + seg
            b1 = a1 + seg
            faces.append((a0, a1, b1, b0))

    # καπάκια
    cap_b = len(verts); verts.append((0, 0, 0))
    cap_t = len(verts); verts.append((0, 0, COL_HEIGHT))
    for si in range(seg):
        faces.append((cap_b, (si + 1) % seg, si))
        top0 = rings * seg + si
        top1 = rings * seg + (si + 1) % seg
        faces.append((cap_t, top0, top1))

    return mesh_from_pydata(name, verts, faces)


def capital_mesh(name):
    """Δωρικό κιονόκρανο: εχίνος (κωνικό) + άβακας (τετράγωνη πλάκα)."""
    bm = bmesh.new()
    seg = 32
    ech_h = 0.30
    ab_h = 0.26
    r0 = COL_R_TOP * 1.02
    r1 = COL_R_TOP * 1.62

    rings = 5
    prev = None
    for ri in range(rings + 1):
        t = ri / float(rings)
        z = t * ech_h
        # καμπύλο προφίλ εχίνου
        r = r0 + (r1 - r0) * (t ** 1.7)
        cur = [bm.verts.new((r * math.cos(2 * math.pi * s / seg),
                             r * math.sin(2 * math.pi * s / seg), z))
               for s in range(seg)]
        if prev:
            for s in range(seg):
                n = (s + 1) % seg
                bm.faces.new((prev[s], prev[n], cur[n], cur[s]))
        prev = cur

    # άβακας
    hx = r1 * 1.03
    z0, z1 = ech_h, ech_h + ab_h
    a = [bm.verts.new(p) for p in [(-hx, -hx, z0), (hx, -hx, z0), (hx, hx, z0), (-hx, hx, z0)]]
    b = [bm.verts.new(p) for p in [(-hx, -hx, z1), (hx, -hx, z1), (hx, hx, z1), (-hx, hx, z1)]]
    for k in range(4):
        n = (k + 1) % 4
        bm.faces.new((a[k], a[n], b[n], b[k]))
    bm.faces.new(tuple(b))
    bm.faces.new(tuple(reversed(a)))
    # κλείσιμο κάτω εχίνου
    bottom = [bm.verts.new((r0 * math.cos(2 * math.pi * s / seg),
                            r0 * math.sin(2 * math.pi * s / seg), 0.0))
              for s in range(seg)]
    ctr = bm.verts.new((0, 0, 0))
    for s in range(seg):
        bm.faces.new((ctr, bottom[(s + 1) % seg], bottom[s]))

    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free(); me.update()
    return me


def column_positions():
    """Θέσεις (x, y) όλων των κιόνων της περίστασης (περιμετρικά)."""
    w, d = temple_footprint()
    x0 = -(COLS_FRONT - 1) * COL_SPACING / 2.0
    y0 = -(COLS_SIDE - 1) * COL_SPACING / 2.0
    pts = []
    for i in range(COLS_FRONT):
        x = x0 + i * COL_SPACING
        pts.append((x, y0))                                   # πρόσοψη (νότια)
        pts.append((x, y0 + (COLS_SIDE - 1) * COL_SPACING))   # οπίσθια
    for j in range(1, COLS_SIDE - 1):
        y = y0 + j * COL_SPACING
        pts.append((x0, y))
        pts.append((x0 + (COLS_FRONT - 1) * COL_SPACING, y))
    return pts


def build_colonnade(parent, z_stylo):
    shaft_me = column_shaft_mesh("Mesh_ColumnShaft")
    cap_me = capital_mesh("Mesh_Capital")
    for idx, (x, y) in enumerate(column_positions()):
        sh = new_object("Column_%02d" % (idx + 1), shaft_me, MAT["marble"], parent)
        sh.location = (x, y, z_stylo)
        sh.rotation_euler[2] = random.uniform(0, math.pi / 10)  # μικρή τυχαία στροφή
        cp = new_object("Capital_%02d" % (idx + 1), cap_me, MAT["marble"], parent)
        cp.location = (x, y, z_stylo + COL_HEIGHT)
    return z_stylo + COL_HEIGHT + 0.56          # κορυφή άβακα


def build_entablature(parent, z_top):
    """Επιστύλιο + ζωφόρος (τρίγλυφα/μετόπες) + γείσο."""
    w, d = temple_footprint()
    z = z_top

    # --- επιστύλιο ---
    me = frame_box("Mesh_Architrave", w, d, ARCHITRAVE_H, COL_R_BOTTOM * 2.0)
    ob = new_object("Architrave", me, MAT["marble"], parent)
    ob.location = (0, 0, z)
    z += ARCHITRAVE_H

    # --- ζωφόρος: συνεχής ταινία + τρίγλυφα μπροστά ---
    me = frame_box("Mesh_FriezeBand", w, d, FRIEZE_H, COL_R_BOTTOM * 2.0)
    ob = new_object("Frieze", me, MAT["marble_worn"], parent)
    ob.location = (0, 0, z)

    tri_me = box_mesh("Mesh_Triglyph", 0.42, 0.14, FRIEZE_H * 0.95)
    n = 0
    for (ax, sign) in (("y", -1), ("y", 1)):
        count = (COLS_FRONT - 1) * 2 + 1
        span = (COLS_FRONT - 1) * COL_SPACING
        for i in range(count):
            x = -span / 2.0 + i * (span / (count - 1))
            t = new_object("Triglyph_%02d" % (n + 1), tri_me, MAT["marble"], parent)
            t.location = (x, sign * (d / 2.0 - 0.02), z + FRIEZE_H * 0.025)
            n += 1
    tri_me_s = box_mesh("Mesh_TriglyphSide", 0.14, 0.42, FRIEZE_H * 0.95)
    for sign in (-1, 1):
        count = (COLS_SIDE - 1) * 2 + 1
        span = (COLS_SIDE - 1) * COL_SPACING
        for i in range(count):
            y = -span / 2.0 + i * (span / (count - 1))
            t = new_object("Triglyph_%02d" % (n + 1), tri_me_s, MAT["marble"], parent)
            t.location = (sign * (w / 2.0 - 0.02), y, z + FRIEZE_H * 0.025)
            n += 1
    z += FRIEZE_H

    # --- γείσο ---
    me = box_mesh("Mesh_Cornice", w + 2 * CORNICE_OUT, d + 2 * CORNICE_OUT, CORNICE_H)
    ob = new_object("Cornice", me, MAT["marble"], parent)
    ob.location = (0, 0, z)
    z += CORNICE_H
    return z


def pediment_mesh(name, width, thickness, height):
    """Τριγωνικό αέτωμα (πρίσμα)."""
    hx = width / 2.0
    hy = thickness / 2.0
    v = [(-hx, -hy, 0), (hx, -hy, 0), (0, -hy, height),
         (-hx, hy, 0), (hx, hy, 0), (0, hy, height)]
    f = [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
    return mesh_from_pydata(name, v, f)


def build_pediments_and_roof(parent, z_cornice):
    w, d = temple_footprint()
    pw = w + 2 * CORNICE_OUT
    ped_me = pediment_mesh("Mesh_Pediment", pw, 0.70, PEDIMENT_H)
    for sign in (-1, 1):
        ob = new_object("Pediment_%s" % ("Front" if sign < 0 else "Back"),
                        ped_me, MAT["marble"], parent)
        ob.location = (0, sign * (d / 2.0 + CORNICE_OUT - 0.35), z_cornice)

    # δίρριχτη στέγη: δύο κεκλιμένες πλάκες
    depth = d + 2 * CORNICE_OUT
    slope_len = math.hypot(pw / 2.0, PEDIMENT_H)
    for sign in (-1, 1):
        me = box_mesh("Mesh_RoofSlab_%d" % (sign > 0), slope_len * 1.02, depth, 0.22,
                      centered_z=True)
        ob = new_object("Roof_%s" % ("West" if sign < 0 else "East"), me, MAT["roof"], parent)
        ang = math.atan2(PEDIMENT_H, pw / 2.0)
        ob.rotation_euler = (0, sign * ang, 0)
        ob.location = (sign * pw / 4.0, 0, z_cornice + PEDIMENT_H / 2.0 + 0.10)
    return z_cornice + PEDIMENT_H


def build_cella(parent, z_stylo):
    """Ο σηκός (εσωτερικός θάλαμος) με άνοιγμα θύρας στην πρόσοψη."""
    w, d = temple_footprint()
    cw = w - 2 * CELLA_INSET
    cd = d - 2 * CELLA_INSET

    bm = bmesh.new()
    hx, hy = cw / 2.0, cd / 2.0
    ix, iy = hx - CELLA_WALL_T, hy - CELLA_WALL_T
    H = CELLA_WALL_H

    def rect(z, a, b):
        return [bm.verts.new((-a, -b, z)), bm.verts.new((a, -b, z)),
                bm.verts.new((a, b, z)), bm.verts.new((-a, b, z))]

    o0, i0 = rect(0, hx, hy), rect(0, ix, iy)
    o1, i1 = rect(H, hx, hy), rect(H, ix, iy)
    for k in range(4):
        n = (k + 1) % 4
        bm.faces.new((o0[k], o0[n], o1[n], o1[k]))
        bm.faces.new((i1[k], i1[n], i0[n], i0[k]))
        bm.faces.new((o1[k], o1[n], i1[n], i1[k]))
        bm.faces.new((i0[k], i0[n], o0[n], o0[k]))
    me = bpy.data.meshes.new("Mesh_CellaWalls")
    bm.to_mesh(me); bm.free(); me.update()
    walls = new_object("Cella_Walls", me, MAT["limestone"], parent)
    walls.location = (0, 0, z_stylo)

    # θύρα: boolean αφαίρεση ενός κουτιού
    cutter = new_object("_DoorCutter",
                        box_mesh("Mesh_DoorCut", CELLA_DOOR_W, CELLA_WALL_T * 4, CELLA_DOOR_H),
                        None, parent)
    cutter.location = (0, -cd / 2.0, z_stylo)
    mod = walls.modifiers.new("Door", 'BOOLEAN')
    mod.object = cutter
    mod.operation = 'DIFFERENCE'
    bpy.context.view_layer.objects.active = walls
    try:
        bpy.ops.object.modifier_apply(modifier="Door")
    except Exception:
        pass
    bpy.data.objects.remove(cutter, do_unlink=True)

    # δάπεδο σηκού
    fl = new_object("Cella_Floor", box_mesh("Mesh_CellaFloor", cw, cd, 0.10),
                    MAT["marble_worn"], parent)
    fl.location = (0, 0, z_stylo + 0.001)

    # βάθρο λατρευτικού αγάλματος (σημείο ενδιαφέροντος)
    ped = new_object("Cella_StatueBase", box_mesh("Mesh_StatueBase", 2.4, 2.4, 1.15),
                     MAT["marble"], parent)
    ped.location = (0, cd / 2.0 - 3.0, z_stylo + 0.10)
    return cw, cd


# ---------------------------------------------------------------------
# ΠΕΡΙΒΑΛΛΟΝ
# ---------------------------------------------------------------------

def build_ground(parent):
    g = new_object("Ground", box_mesh("Mesh_Ground", GROUND_SIZE, GROUND_SIZE, 0.4),
                   MAT["ground"], parent)
    g.location = (0, 0, -0.4)

    # μονοπάτι που οδηγεί στην πρόσοψη
    w, d = temple_footprint()
    p = new_object("Path", box_mesh("Mesh_Path", 4.5, 26.0, 0.06), MAT["path"], parent)
    p.location = (0, -(d / 2.0 + 15.0), 0.005)
    return g


def build_ruins(parent):
    """Πεσμένοι σπόνδυλοι και λιθόπλινθοι διάσπαρτοι γύρω από τον ναό."""
    w, d = temple_footprint()
    drum = None
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=18,
                          radius1=COL_R_BOTTOM, radius2=COL_R_BOTTOM * 0.94,
                          depth=0.75)
    drum = bpy.data.meshes.new("Mesh_ColumnDrum")
    bm.to_mesh(drum); bm.free(); drum.update()

    block = box_mesh("Mesh_RuinBlock", 1.5, 0.8, 0.6, centered_z=True)

    for i in range(RUIN_COUNT):
        ang = random.uniform(0, 2 * math.pi)
        rad = random.uniform(max(w, d) * 0.62, max(w, d) * 1.25)
        x, y = rad * math.cos(ang), rad * math.sin(ang) * 0.75
        if random.random() < 0.55:
            ob = new_object("Ruin_Drum_%02d" % (i + 1), drum, MAT["marble_worn"], parent)
            ob.rotation_euler = (math.radians(90) + random.uniform(-0.25, 0.25),
                                 0, random.uniform(0, math.pi))
            ob.location = (x, y, COL_R_BOTTOM * 0.95)
        else:
            ob = new_object("Ruin_Block_%02d" % (i + 1), block, MAT["limestone"], parent)
            ob.rotation_euler = (random.uniform(-0.12, 0.12),
                                 random.uniform(-0.12, 0.12),
                                 random.uniform(0, math.pi))
            ob.location = (x, y, 0.3)
        s = random.uniform(0.85, 1.25)
        ob.scale = (s, s, s)


def build_trees(parent):
    if TREE_COUNT <= 0:
        return
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=10,
                          radius1=1.05, radius2=0.02, depth=7.5)
    me = bpy.data.meshes.new("Mesh_Cypress")
    bm.to_mesh(me); bm.free(); me.update()
    w, d = temple_footprint()
    for i in range(TREE_COUNT):
        ang = 2 * math.pi * i / TREE_COUNT + random.uniform(-0.15, 0.15)
        rad = max(w, d) * random.uniform(1.15, 1.75)
        ob = new_object("Cypress_%02d" % (i + 1), me, MAT["tree"], parent)
        ob.location = (rad * math.cos(ang), rad * math.sin(ang) * 0.9, 3.75)
        s = random.uniform(0.8, 1.35)
        ob.scale = (s, s, s)


# ---------------------------------------------------------------------
# ΣΗΜΕΙΑ ΤΕΚΜΗΡΙΩΣΗΣ  (τα βλέπεις και στη Unity ως άδεια GameObjects)
# ---------------------------------------------------------------------

INFO_POINTS = []


def build_info_markers(parent, z_stylo, z_cornice):
    global INFO_POINTS
    w, d = temple_footprint()
    pts = [
        ("01_Krepis",     (0.0, -(d / 2.0 + 2.2), 0.6)),
        ("02_Kiones",     (-(w / 2.0) - 2.0, -d * 0.22, 1.6)),
        ("03_Kionokrano", (-(w / 2.0) - 2.0, d * 0.10, 1.6)),
        ("04_Epistylio",  ((w / 2.0) + 2.2, -d * 0.10, 1.6)),
        ("05_Zoforos",    ((w / 2.0) + 2.2, d * 0.18, 1.6)),
        ("06_Aetoma",     (0.0, -(d / 2.0 + 6.5), 1.6)),
        ("07_Sikos",      (0.0, -d * 0.20, z_stylo + 1.6)),
        ("08_Bathro",     (0.0, d * 0.16, z_stylo + 1.6)),
        ("09_Ereipia",    (max(w, d) * 0.80, -max(w, d) * 0.35, 1.2)),
        ("10_Stegi",      (0.0, (d / 2.0) + 8.0, 1.6)),
    ]
    INFO_POINTS = pts
    if not BUILD_INFO_MARKERS:
        return
    for name, loc in pts:
        e = bpy.data.objects.new("InfoPoint_" + name, None)
        e.empty_display_type = 'SPHERE'
        e.empty_display_size = 0.6
        e.location = loc
        e.parent = parent
        get_collection().objects.link(e)


# ---------------------------------------------------------------------
# ΦΩΤΙΣΜΟΣ / ΚΑΜΕΡΑ
# ---------------------------------------------------------------------

def build_lighting_and_camera(z_top):
    sun_data = bpy.data.lights.new("Sun", type='SUN')
    sun_data.energy = 3.4
    sun_data.angle = math.radians(1.5)
    sun = bpy.data.objects.new("Sun", sun_data)
    sun.rotation_euler = (math.radians(52), 0, math.radians(38))
    bpy.context.scene.collection.objects.link(sun)

    w, d = temple_footprint()

    # δύο ζεστά φώτα μέσα στον σηκό (σαν πυρσοί) - χωρίς αυτά είναι κατασκότεινα
    for sx in (-1, 1):
        pl = bpy.data.lights.new("CellaLight_%d" % (sx > 0), type='POINT')
        pl.energy = 900.0
        pl.color = (1.0, 0.76, 0.48)
        pl.shadow_soft_size = 0.6
        lo = bpy.data.objects.new("CellaLight_%d" % (sx > 0), pl)
        lo.location = (sx * (w * 0.14), 0.0, 3.4)
        bpy.context.scene.collection.objects.link(lo)

    cam_data = bpy.data.cameras.new("Camera")
    cam_data.lens = 32
    cam = bpy.data.objects.new("Camera", cam_data)
    cam.location = (w * 1.55, -(d * 0.92), z_top * 0.95)
    cam.rotation_euler = (math.radians(78), 0, math.radians(58))
    bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam

    # ουρανός
    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.42, 0.60, 0.85, 1.0)
        bg.inputs[1].default_value = 1.15


# ---------------------------------------------------------------------
# MAIN
# ---------------------------------------------------------------------

def main():
    purge_scene()
    build_materials()

    root = bpy.data.objects.new("SITE_ROOT", None)
    root.empty_display_size = 2.0
    get_collection().objects.link(root)

    z_stylo = build_krepis(root)
    z_abacus = build_colonnade(root, z_stylo)
    z_cornice = build_entablature(root, z_abacus)
    z_ridge = build_pediments_and_roof(root, z_cornice)
    build_cella(root, z_stylo)
    build_ground(root)
    build_ruins(root)
    build_trees(root)
    build_info_markers(root, z_stylo, z_cornice)
    build_lighting_and_camera(z_ridge)

    # Όλες οι κλίμακες είναι ομοιόμορφες (uniform) -> η Unity τις δέχεται
    # χωρίς πρόβλημα, δεν χρειάζεται transform_apply.
    bpy.ops.object.select_all(action='DESELECT')

    n_obj = len([o for o in get_collection().objects if o.type == 'MESH'])
    n_tri = sum(len(o.data.loop_triangles) for o in get_collection().objects
                if o.type == 'MESH' and o.data.loop_triangles) or "-"
    print("=" * 60)
    print("OK - Ο αρχαιολογικός χώρος δημιουργήθηκε.")
    print("  Αντικείμενα mesh : %d" % n_obj)
    print("  Ύψος κορυφής     : %.2f m" % z_ridge)
    print("  Στυλοβάτης       : %.2f m" % z_stylo)
    print("  Σημεία τεκμηρίωσης: %d" % len(INFO_POINTS))
    print("  Επόμενο βήμα     : τρέξε το 03_export_fbx.py")
    print("=" * 60)


main()
