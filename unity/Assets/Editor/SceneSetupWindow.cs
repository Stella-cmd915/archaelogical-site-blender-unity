// =====================================================================
//  SceneSetupWindow.cs      >>> ΤΟ ΠΙΟ ΣΗΜΑΝΤΙΚΟ ΑΡΧΕΙΟ <<<
//
//  ΠΟΥ ΜΠΑΙΝΕΙ: Assets/Editor/SceneSetupWindow.cs   (ΥΠΟΧΡΕΩΤΙΚΑ σε
//               φάκελο με το όνομα "Editor", αλλιώς δεν κάνει compile)
//
//  ΤΙ ΚΑΝΕΙ: με ΕΝΑ κλικ στήνει ολόκληρη τη σκηνή:
//      · βάζει Mesh Colliders σε όλο το μοντέλο (για να μη σε περνάει)
//      · φτιάχνει και αναθέτει υλικά (Standard ή URP - το βρίσκει μόνο του)
//      · δημιουργεί τον παίκτη με κάμερα + χειρισμό + HUD
//      · τοποθετεί τα 10 σημεία τεκμηρίωσης με τα ελληνικά κείμενα
//      · ρυθμίζει φωτισμό, ομίχλη, ουρανό, σκιές
//
//  ΠΩΣ ΤΡΕΧΕΙ: πάνω μπάρα Unity ->  Project  ->  1. Στήσιμο σκηνής
// =====================================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;

public class SceneSetupWindow : EditorWindow
{
    GameObject siteRoot;
    bool addColliders = true;
    bool buildMaterials = true;
    bool createPlayer = true;
    bool createInfoPoints = true;
    bool setupLighting = true;
    string authorName = "";

    const string MAT_FOLDER = "Assets/Materials";

    [MenuItem("Project/1. Στήσιμο σκηνής %#T", false, 1)]
    public static void Open()
    {
        SceneSetupWindow w = GetWindow<SceneSetupWindow>(true, "Στήσιμο περιήγησης");
        w.minSize = new Vector2(430, 470);
        w.AutoFindRoot();
        w.Show();
    }

    void AutoFindRoot()
    {
        if (siteRoot != null) return;
        GameObject g = GameObject.Find("SITE_ROOT");
        if (g == null) g = GameObject.Find("ArchaeologicalSite");
        if (g == null)
        {
            // πάρε το πρώτο αντικείμενο που έχει πολλά mesh παιδιά
            foreach (GameObject root in
                     UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.GetComponentsInChildren<MeshFilter>(true).Length > 20)
                { g = root; break; }
            }
        }
        siteRoot = g;
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Στήσιμο περιήγησης πρώτου προσώπου",
                                   EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1) Σύρε πρώτα το ArchaeologicalSite.fbx μέσα στη σκηνή.\n" +
            "2) Βάλε το εδώ κάτω (ή θα το βρει μόνο του).\n" +
            "3) Πάτα το μεγάλο κουμπί.", MessageType.Info);

        EditorGUILayout.Space(4);
        siteRoot = (GameObject)EditorGUILayout.ObjectField(
            "Μοντέλο στη σκηνή", siteRoot, typeof(GameObject), true);

        if (siteRoot == null && GUILayout.Button("Εντοπισμός μοντέλου"))
            AutoFindRoot();

        EditorGUILayout.Space(8);
        authorName = EditorGUILayout.TextField("Ονοματεπώνυμο (στην intro)", authorName);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Βήματα", EditorStyles.boldLabel);
        addColliders = EditorGUILayout.ToggleLeft("Mesh Colliders σε όλο το μοντέλο", addColliders);
        buildMaterials = EditorGUILayout.ToggleLeft("Δημιουργία & ανάθεση υλικών", buildMaterials);
        createPlayer = EditorGUILayout.ToggleLeft("Δημιουργία παίκτη (κάμερα + χειρισμός + HUD)", createPlayer);
        createInfoPoints = EditorGUILayout.ToggleLeft("Τοποθέτηση 10 σημείων τεκμηρίωσης", createInfoPoints);
        setupLighting = EditorGUILayout.ToggleLeft("Φωτισμός, ομίχλη, ουρανός, σκιές", setupLighting);

        EditorGUILayout.Space(14);
        GUI.enabled = (siteRoot != null);
        GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
        if (GUILayout.Button("ΣΤΗΣΕ ΤΗ ΣΚΗΝΗ", GUILayout.Height(48)))
            RunSetup();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (siteRoot == null)
            EditorGUILayout.HelpBox("Δεν βρέθηκε το μοντέλο στη σκηνή.", MessageType.Warning);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Μετά το στήσιμο", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("· Πάτα Play για δοκιμή", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("· Project → 2. Ψήσιμο φωτισμού", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("· Project → 3. Δημιουργία .exe", EditorStyles.miniLabel);
    }

    // =================================================================
    void RunSetup()
    {
        if (siteRoot == null) return;
        Undo.RegisterFullObjectHierarchyUndo(siteRoot, "Στήσιμο σκηνής");

        int nCol = 0, nMat = 0, nPts = 0;

        if (addColliders) nCol = AddColliders(siteRoot);
        if (buildMaterials) nMat = BuildAndAssignMaterials(siteRoot);
        if (setupLighting) SetupLighting();
        if (createInfoPoints) nPts = CreateInfoPoints(siteRoot);
        if (createPlayer) CreatePlayer();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Έτοιμο",
            "Η σκηνή στήθηκε:\n\n" +
            "· Colliders: " + nCol + "\n" +
            "· Υλικά: " + nMat + "\n" +
            "· Σημεία τεκμηρίωσης: " + nPts + "\n\n" +
            "Πάτα Play για να δοκιμάσεις.\n" +
            "Μην ξεχάσεις Ctrl+S για αποθήκευση σκηνής.",
            "OK");
    }

    // -----------------------------------------------------------------
    int AddColliders(GameObject root)
    {
        int n = 0;
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<Collider>() != null) continue;
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            n++;
        }
        return n;
    }

    // -----------------------------------------------------------------
    static bool IsURP()
    {
        return GraphicsSettings.defaultRenderPipeline != null;
    }

    static Shader LitShader()
    {
        if (IsURP())
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s != null) return s;
        }
        return Shader.Find("Standard");
    }

    // όνομα υλικού (από το Blender) -> χρώμα, γυαλάδα, μεταλλικότητα
    static readonly Dictionary<string, Color> PALETTE = new Dictionary<string, Color>()
    {
        { "M_Marble",     new Color(0.885f, 0.865f, 0.805f) },
        { "M_MarbleWorn", new Color(0.760f, 0.725f, 0.640f) },
        { "M_Limestone",  new Color(0.700f, 0.660f, 0.570f) },
        { "M_RoofTile",   new Color(0.560f, 0.350f, 0.260f) },
        { "M_Ground",     new Color(0.455f, 0.420f, 0.310f) },
        { "M_Path",       new Color(0.605f, 0.565f, 0.485f) },
        { "M_Cypress",    new Color(0.115f, 0.225f, 0.105f) },
    };

    Material MakeMaterial(string name, Color col)
    {
        if (!AssetDatabase.IsValidFolder(MAT_FOLDER))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = MAT_FOLDER + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(LitShader());
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);

        float smooth = name.Contains("Marble") ? 0.24f : 0.08f;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    int BuildAndAssignMaterials(GameObject root)
    {
        Dictionary<string, Material> cache = new Dictionary<string, Material>();
        int n = 0;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                string key = (mats[i] != null) ? mats[i].name.Replace(" (Instance)", "") : "M_Limestone";
                if (!PALETTE.ContainsKey(key))
                {
                    // ταίριασμα με βάση το όνομα του αντικειμένου
                    string gn = r.gameObject.name.ToLower();
                    if (gn.Contains("roof")) key = "M_RoofTile";
                    else if (gn.Contains("ground")) key = "M_Ground";
                    else if (gn.Contains("path")) key = "M_Path";
                    else if (gn.Contains("cypress")) key = "M_Cypress";
                    else if (gn.Contains("cella")) key = "M_Limestone";
                    else if (gn.Contains("ruin") || gn.Contains("krepis")) key = "M_MarbleWorn";
                    else key = "M_Marble";
                }
                if (!cache.ContainsKey(key))
                { cache[key] = MakeMaterial(key, PALETTE[key]); n++; }
                if (mats[i] != cache[key]) { mats[i] = cache[key]; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
        AssetDatabase.SaveAssets();
        return n;
    }

    // -----------------------------------------------------------------
    void SetupLighting()
    {
        Light sun = null;
#if UNITY_2023_1_OR_NEWER
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
#else
        foreach (Light l in Object.FindObjectsOfType<Light>())
#endif
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun == null)
        {
            GameObject g = new GameObject("Ήλιος");
            sun = g.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.transform.rotation = Quaternion.Euler(46f, -36f, 0f);
        sun.color = new Color(1f, 0.955f, 0.875f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.82f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.52f, 0.62f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.44f, 0.44f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.26f, 0.24f, 0.20f);
        RenderSettings.ambientIntensity = 1.0f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.86f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 260f;

        QualitySettings.shadowDistance = 160f;
        QualitySettings.shadows = ShadowQuality.All;
    }

    // -----------------------------------------------------------------
    //  ΤΑ ΚΕΙΜΕΝΑ ΤΩΝ ΠΙΝΑΚΙΔΩΝ
    // -----------------------------------------------------------------
    struct Info
    {
        public string code, key, title, body;
        public Vector3 fallback;   // θέση σε Unity αν δεν βρεθεί Empty από το FBX
        public Info(string c, string k, string t, string b, Vector3 f)
        { code = c; key = k; title = t; body = b; fallback = f; }
    }

    static readonly Info[] INFOS = new Info[]
    {
        new Info("01","01_Krepis","Κρηπίδα και στυλοβάτης",
"Η κρηπίδα είναι η βαθμιδωτή βάση πάνω στην οποία εδράζεται ολόκληρος ο ναός. Αποτελείται εδώ από τρεις βαθμίδες ύψους 0,34 m η καθεμία.\n\nΗ ανώτερη βαθμίδα ονομάζεται <b>στυλοβάτης</b>, γιατί πάνω της πατούν απευθείας οι κίονες (στύλοι). Οι δύο κατώτερες βαθμίδες είναι η <b>κρηπίς</b>, ενώ η θεμελίωση κάτω από το έδαφος λέγεται <b>ευθυντηρία</b>.\n\nΣτους μεγάλους ναούς ο στυλοβάτης δεν είναι επίπεδος αλλά ελαφρώς κυρτός προς τα πάνω. Η σκόπιμη αυτή απόκλιση ονομάζεται <b>καμπυλότητα</b> και εξουδετερώνει την οπτική ψευδαίσθηση που θα έκανε μια απόλυτα ευθεία γραμμή να φαίνεται κοίλη.",
            new Vector3(0f, 0.6f, 17.7f)),

        new Info("02","02_Kiones","Δωρικός κίονας",
"Ο δωρικός κίονας πατά κατευθείαν στον στυλοβάτη, <b>χωρίς βάση</b> - αυτό είναι το βασικό γνώρισμα που τον ξεχωρίζει από τον ιωνικό.\n\nΟ κορμός συντίθεται από κυλινδρικούς <b>σπονδύλους</b> που ενώνονταν μεταξύ τους με ξύλινους ή μεταλλικούς <b>πόλους</b> και <b>εμπόλια</b>, χωρίς κονίαμα.\n\nΓύρω από τον κορμό λαξεύονταν <b>20 ραβδώσεις</b> με οξείες ακμές (αρρίδες). Οι ραβδώσεις τονίζουν την κατακόρυφη κίνηση και δημιουργούν συνεχή εναλλαγή φωτός-σκιάς.\n\nΗ διάμετρος μειώνεται προς τα πάνω (<b>μείωσις</b>) και ο κορμός έχει ένα ανεπαίσθητο φούσκωμα στο μέσο (<b>έντασις</b>), ώστε να μη φαίνεται κοίλος.",
            new Vector3(-10.5f, 1.6f, 6.8f)),

        new Info("03","03_Kionokrano","Δωρικό κιονόκρανο",
"Το κιονόκρανο μεταφέρει το βάρος της ανωδομής στον κορμό και αποτελείται από δύο μέρη.\n\nΟ <b>εχίνος</b> είναι το κυκλικό, καμπύλο στοιχείο που «ανοίγει» προς τα πάνω σαν μαξιλάρι. Στα αρχαϊκά χρόνια είναι πολύ πλατύς και πεπλατυσμένος· όσο προχωρούμε στην κλασική εποχή γίνεται πιο ορθός και αυστηρός. Είναι από τα ασφαλέστερα κριτήρια χρονολόγησης ενός δωρικού ναού.\n\nΟ <b>άβακας</b> είναι η τετράγωνη πλάκα πάνω από τον εχίνο, που δέχεται το επιστύλιο.\n\nΣτη βάση του κιονοκράνου, εκεί που τελειώνει ο κορμός, χαράσσονται οι <b>ιμάντες</b> ή <b>ανάγλυφοι δακτύλιοι</b>.",
            new Vector3(-10.5f, 1.6f, -3.1f)),

        new Info("04","04_Epistylio","Επιστύλιο (αρχιτράβη)",
"Το επιστύλιο είναι η οριζόντια δοκός που γεφυρώνει το κενό ανάμεσα σε δύο διαδοχικούς κίονες και είναι το πρώτο μέλος του <b>θριγκού</b> (της ανωδομής).\n\nΚάθε λίθος του καλύπτει την απόσταση από άξονα σε άξονα κιόνων, ώστε οι αρμοί να πέφτουν πάντα πάνω από κιονόκρανο - ποτέ στο κενό.\n\nΣτον δωρικό ρυθμό το επιστύλιο είναι εντελώς <b>λείο και αδιακόσμητο</b>, σε αντίθεση με το ιωνικό που χωρίζεται σε τρεις οριζόντιες ταινίες.\n\nΣτο πάνω μέρος του τρέχει μια στενή ταινία και κάτω από κάθε τρίγλυφο κρέμονται οι <b>ρεγούλες</b> με έξι <b>σταγόνες</b> - πέτρινη ανάμνηση των ξύλινων προκατόχων.",
            new Vector3(10.5f, 1.6f, 3.1f)),

        new Info("05","05_Zoforos","Ζωφόρος: τρίγλυφα και μετόπες",
"Η δωρική ζωφόρος είναι η ζώνη πάνω από το επιστύλιο και εναλλάσσει ρυθμικά δύο στοιχεία.\n\nΤα <b>τρίγλυφα</b> είναι ορθογώνιες πλάκες με δύο πλήρεις κατακόρυφες αυλακώσεις (γλυφές) και δύο μισές στις άκρες - εξού και το όνομα. Θεωρούνται η λίθινη μεταγραφή των απολήξεων των ξύλινων δοκών της αρχέγονης στέγης.\n\nΟι <b>μετόπες</b> είναι τα τετράγωνα διάκενα ανάμεσά τους, συχνά με ανάγλυφες παραστάσεις.\n\nΚανόνας: ένα τρίγλυφο πάνω από κάθε κίονα και ένα ακριβώς στο μέσο κάθε μετακιονίου. Στις γωνίες όμως ο κανόνας συγκρούεται με τον εαυτό του - το περίφημο <b>γωνιακό πρόβλημα του δωρικού ρυθμού</b>, που λυνόταν με συστολή του ακραίου μετακιονίου.",
            new Vector3(10.5f, 1.6f, -5.6f)),

        new Info("06","06_Aetoma","Αέτωμα και γείσο",
"Πάνω από τη ζωφόρο προεξέχει το <b>γείσο</b>, που προστατεύει τα κατώτερα μέλη από το νερό της βροχής και ρίχνει βαθιά σκιά στην πρόσοψη.\n\nΤο <b>αέτωμα</b> είναι το τριγωνικό μέτωπο που σχηματίζει η δίρριχτη στέγη στις δύο στενές πλευρές. Η εσωτερική τριγωνική επιφάνειά του λέγεται <b>τύμπανο</b> και φιλοξενούσε τα σημαντικότερα γλυπτά του ναού.\n\nΗ σύνθεση προσαρμοζόταν στο σχήμα: όρθιες μορφές στο κέντρο, καθιστές προς τα πλάγια, ξαπλωμένες στις οξείες γωνίες.\n\nΣτην κορυφή και στις δύο κάτω γωνίες στέκονταν τα <b>ακρωτήρια</b> - γλυπτά ανθεμίων, Νικών ή Σφιγγών.",
            new Vector3(0f, 1.6f, 22.0f)),

        new Info("07","07_Sikos","Σηκός (ο κυρίως ναός)",
"Ο σηκός είναι ο κλειστός πυρήνας του κτηρίου - ο καθαυτό «ναός», ενώ η κιονοστοιχία γύρω του λέγεται <b>περίσταση</b> ή <b>πτερόν</b>.\n\nΜπροστά από τον σηκό βρίσκεται ο <b>πρόναος</b> και πίσω ο <b>οπισθόδομος</b>, που χρησίμευε ως θησαυροφυλάκιο για τα αναθήματα.\n\nΟ σηκός δεν ήταν χώρος συγκέντρωσης πιστών: εκεί έμπαιναν μόνο οι ιερείς. Η λατρεία, οι θυσίες και οι τελετές γίνονταν έξω, στον <b>βωμό</b> μπροστά από την ανατολική πρόσοψη.\n\nΤο εσωτερικό φωτιζόταν ελάχιστα - μόνο από τη θύρα - και αυτό ακριβώς το ημίφως ήταν σκόπιμο: έκανε το χρυσελεφάντινο άγαλμα να λάμπει μέσα στο σκοτάδι.",
            new Vector3(0f, 2.6f, 6.2f)),

        new Info("08","08_Bathro","Βάθρο λατρευτικού αγάλματος",
"Στο βάθος του σηκού, απέναντι από τη θύρα, υψωνόταν το βάθρο του <b>λατρευτικού αγάλματος</b> της θεότητας στην οποία ήταν αφιερωμένος ο ναός.\n\nΤα μεγάλα λατρευτικά αγάλματα ήταν συχνά <b>χρυσελεφάντινα</b>: ξύλινος πυρήνας ντυμένος με φύλλα χρυσού για τα ενδύματα και πλάκες ελεφαντόδοντου για το γυμνό δέρμα. Ο χρυσός ήταν αφαιρούμενος και ζυγιζόταν - λειτουργούσε και ως αποθεματικό της πόλης.\n\nΤα ίχνη στο δάπεδο (εγκοπές έδρασης, υποδοχές μεταλλικών συνδέσμων, φθορές από την κυκλοφορία) είναι από τις πολυτιμότερες πηγές: επιτρέπουν στους αρχαιολόγους να αποκαταστήσουν το μέγεθος και τη θέση ενός αγάλματος που δεν σώζεται πια.",
            new Vector3(0f, 2.6f, -5.0f)),

        new Info("09","09_Ereipia","Διάσπαρτο αρχιτεκτονικό υλικό",
"Οι πεσμένοι <b>σπόνδυλοι</b> κιόνων και οι λιθόπλινθοι που βλέπετε γύρω σας δεν είναι απλώς μπάζα: αποτελούν το βασικότερο τεκμήριο για την αποκατάσταση ενός ερειπωμένου μνημείου.\n\nΚάθε λίθος καταγράφεται, αριθμείται και μελετάται ως προς τις διαστάσεις, τους αρμούς, τις υποδοχές συνδέσμων και τα ίχνη εργαλείων. Έτσι προσδιορίζεται η αρχική του θέση στο κτήριο - η διαδικασία ονομάζεται <b>ταύτιση διάσπαρτων μελών</b>.\n\nΌταν το ποσοστό των σωζόμενων αυθεντικών λίθων είναι επαρκές, μπορεί να γίνει <b>αναστήλωση</b>: επανατοποθέτηση των μελών στην αρχική τους θέση, με νέα συμπληρώματα σαφώς διακριτά από το αυθεντικό υλικό, σύμφωνα με τον Χάρτη της Βενετίας (1964).",
            new Vector3(24.9f, 1.2f, 10.9f)),

        new Info("10","10_Stegi","Στέγη και κεράμωση",
"Η στέγη ήταν ξύλινη, δίρριχτη, με σκελετό από δοκούς και επικάλυψη από <b>κεραμίδια</b>.\n\nΤο σύστημα κάλυψης ήταν συνήθως το <b>λακωνικό</b> (καμπύλα κεραμίδια) ή το <b>κορινθιακό</b> (επίπεδοι στρωτήρες με τριγωνικούς καλυπτήρες στους αρμούς). Στα πολυτελέστερα κτήρια, όπως ο Παρθενώνας, χρησιμοποιήθηκαν λεπτές πλάκες μαρμάρου αντί για πηλό.\n\nΣτο κατώτερο άκρο κάθε σειράς τοποθετούνταν οι <b>ηγεμόνες κάλυπτήρες</b> ή <b>ακροκέραμα</b>, διακοσμημένα με ανθέμια, που έκλειναν οπτικά τη σειρά και εμπόδιζαν την είσοδο νερού και πουλιών.\n\nΣτις γωνίες της στέγης οι <b>υδρορρόες</b>, συχνά σε σχήμα λεοντοκεφαλής, απομάκρυναν τα όμβρια από το γείσο.",
            new Vector3(0f, 1.6f, -23.5f)),
    };

    int CreateInfoPoints(GameObject root)
    {
        GameObject parent = GameObject.Find("Σημεία τεκμηρίωσης");
        if (parent == null) parent = new GameObject("Σημεία τεκμηρίωσης");

        // καθάρισε τυχόν προηγούμενα
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(parent.transform.GetChild(i).gameObject);

        // μάζεψε τα Empties που ήρθαν από το Blender (InfoPoint_XX_...)
        Dictionary<string, Transform> markers = new Dictionary<string, Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("InfoPoint_")) continue;
            string key = t.name.Substring("InfoPoint_".Length);
            if (!markers.ContainsKey(key)) markers[key] = t;
        }

        int n = 0;
        foreach (Info inf in INFOS)
        {
            GameObject go = new GameObject("InfoPoint_" + inf.code);
            go.transform.SetParent(parent.transform, false);

            if (markers.ContainsKey(inf.key))
                go.transform.position = markers[inf.key].position;
            else
                go.transform.position = inf.fallback;

            InfoPoint ip = go.AddComponent<InfoPoint>();
            ip.code = inf.code;
            ip.title = inf.title;
            ip.body = inf.body;
            ip.activationRadius = 4.5f;
            n++;
        }

        // κρύψε τα αρχικά empties του μοντέλου για να μην μπερδεύουν
        foreach (KeyValuePair<string, Transform> kv in markers)
            kv.Value.gameObject.SetActive(false);

        return n;
    }

    // -----------------------------------------------------------------
    void CreatePlayer()
    {
        GameObject old = GameObject.Find("Παίκτης");
        if (old != null) DestroyImmediate(old);

        GameObject p = new GameObject("Παίκτης");
        p.tag = "Player";
        // αφετηρία: στο μονοπάτι, κοιτώντας την πρόσοψη
        p.transform.position = new Vector3(0f, 1.2f, 36f);
        p.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        CharacterController cc = p.AddComponent<CharacterController>();
        cc.height = 1.75f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.875f, 0f);

        GameObject camGo = new GameObject("PlayerCamera");
        camGo.transform.SetParent(p.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.59f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 68f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 400f;
        camGo.AddComponent<AudioListener>();

        // σβήσε άλλες κάμερες/AudioListeners για να μην υπάρχει σύγκρουση
#if UNITY_2023_1_OR_NEWER
        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
#else
        foreach (Camera c in Object.FindObjectsOfType<Camera>())
#endif
            if (c != cam) c.gameObject.SetActive(false);

        p.AddComponent<FirstPersonController>();
        TourHUD hud = p.AddComponent<TourHUD>();
        hud.author = authorName;

        Selection.activeGameObject = p;
    }

    // =================================================================
    [MenuItem("Project/2. Ψήσιμο φωτισμού (Bake)", false, 2)]
    public static void BakeLighting()
    {
        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        if (EditorUtility.DisplayDialog("Ψήσιμο φωτισμού",
            "Θα ξεκινήσει το bake. Ανάλογα με τον υπολογιστή μπορεί να πάρει " +
            "από 2 έως 20 λεπτά.\n\nΘέλεις να συνεχίσεις;", "Ναι", "Άκυρο"))
        {
            Lightmapping.BakeAsync();
        }
    }

    [MenuItem("Project/3. Δημιουργία .exe (Build)", false, 3)]
    public static void BuildExe()
    {
        string dir = EditorUtility.SaveFolderPanel(
            "Διάλεξε ΑΔΕΙΟ φάκελο για το build", "", "Build");
        if (string.IsNullOrEmpty(dir)) return;

        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(scene))
        {
            EditorUtility.DisplayDialog("Σφάλμα",
                "Αποθήκευσε πρώτα τη σκηνή (Ctrl+S).", "OK");
            return;
        }

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = new string[] { scene };
        opt.locationPathName = Path.Combine(dir, "Periigisi.exe");
        opt.target = BuildTarget.StandaloneWindows64;
        opt.options = BuildOptions.None;

        PlayerSettings.companyName = "Panepistimio Peiraios";
        PlayerSettings.productName = "Periigisi Doric Temple";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.runInBackground = false;

        BuildPipeline.BuildPlayer(opt);
        EditorUtility.RevealInFinder(opt.locationPathName);
    }

    [MenuItem("Project/Άνοιγμα φακέλου έργου", false, 20)]
    public static void OpenProjectFolder()
    {
        EditorUtility.RevealInFinder(Application.dataPath);
    }
}
