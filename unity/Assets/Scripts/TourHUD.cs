// =====================================================================
//  TourHUD.cs
//  ΟΛΟΚΛΗΡΟ το περιβάλλον χρήστη της περιήγησης, χωρίς Canvas, χωρίς
//  TextMeshPro, χωρίς prefabs. Ζωγραφίζεται με OnGUI() -> μηδέν στήσιμο.
//
//  ΠΟΥ ΜΠΑΙΝΕΙ: Assets/Scripts/TourHUD.cs
//  Μπαίνει στο ίδιο GameObject με τον FirstPersonController.
//
//  ΤΙ ΚΑΝΕΙ:
//    - τίτλος στην αρχή (fade in/out)
//    - στόχαστρο
//    - μήνυμα "[E] Ανάγνωση" όταν πλησιάζεις σημείο τεκμηρίωσης
//    - πάνελ με το κείμενο της πινακίδας
//    - μετρητής προόδου (πόσα σημεία είδες)
//    - βοήθεια με [H]
//    - μενού παύσης με [Esc] και κουμπί εξόδου
// =====================================================================

using UnityEngine;

public class TourHUD : MonoBehaviour
{
    [Header("Στοιχεία έργου")]
    public string siteTitle = "ΙΕΡΟ ΔΩΡΙΚΟΥ ΝΑΟΥ";
    public string siteSubtitle = "Ψηφιακή τεκμηρίωση & περιήγηση";
    public string author = "";

    [Header("Χρώματα")]
    public Color gold = new Color(0.94f, 0.80f, 0.42f);
    public Color panelBg = new Color(0.06f, 0.06f, 0.07f, 0.90f);

    InfoPoint[] points;
    InfoPoint current;      // το κοντινότερο ενεργό σημείο
    InfoPoint openPoint;    // αυτό που διαβάζουμε τώρα

    bool showHelp;
    bool paused;
    float introTimer;
    Vector2 scroll;

    Texture2D texPanel, texGold, texWhite, texDim;
    GUIStyle sTitle, sSub, sBody, sPrompt, sSmall, sButton, sHeader;
    bool stylesReady;

    const float INTRO_DURATION = 6.0f;

    // -----------------------------------------------------------------
    void Start()
    {
#if UNITY_2023_1_OR_NEWER
        points = Object.FindObjectsByType<InfoPoint>(FindObjectsSortMode.None);
#else
        points = FindObjectsOfType<InfoPoint>();
#endif
        introTimer = 0f;
    }

    void Update()
    {
        introTimer += Time.unscaledDeltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (openPoint != null) { openPoint = null; }
            else paused = !paused;
        }
        if (Input.GetKeyDown(KeyCode.H) && openPoint == null && !paused)
            showHelp = !showHelp;

        current = FindNearest();

        if (openPoint == null && !paused && current != null &&
            (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
        {
            openPoint = current;
            openPoint.visited = true;
            scroll = Vector2.zero;
        }

        FirstPersonController.InputBlocked = (openPoint != null) || paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    InfoPoint FindNearest()
    {
        if (points == null) return null;
        InfoPoint best = null;
        float bestD = float.MaxValue;
        Vector3 p = transform.position;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            float d = Vector3.Distance(p, points[i].transform.position);
            if (d <= points[i].activationRadius && d < bestD) { bestD = d; best = points[i]; }
        }
        return best;
    }

    int VisitedCount()
    {
        int n = 0;
        if (points != null)
            for (int i = 0; i < points.Length; i++)
                if (points[i] != null && points[i].visited) n++;
        return n;
    }

    // -----------------------------------------------------------------
    //  ΓΡΑΦΙΚΑ
    // -----------------------------------------------------------------
    Texture2D Solid(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    void BuildStyles()
    {
        texPanel = Solid(panelBg);
        texGold = Solid(gold);
        texWhite = Solid(Color.white);
        texDim = Solid(new Color(0f, 0f, 0f, 0.72f));

        int baseSize = Mathf.RoundToInt(Screen.height / 46f);

        sTitle = new GUIStyle(GUI.skin.label);
        sTitle.fontSize = Mathf.RoundToInt(Screen.height / 14f);
        sTitle.fontStyle = FontStyle.Bold;
        sTitle.alignment = TextAnchor.MiddleCenter;

        sSub = new GUIStyle(GUI.skin.label);
        sSub.fontSize = Mathf.RoundToInt(Screen.height / 36f);
        sSub.alignment = TextAnchor.MiddleCenter;

        sHeader = new GUIStyle(GUI.skin.label);
        sHeader.fontSize = Mathf.RoundToInt(baseSize * 1.75f);
        sHeader.fontStyle = FontStyle.Bold;
        sHeader.wordWrap = true;

        sBody = new GUIStyle(GUI.skin.label);
        sBody.fontSize = baseSize;
        sBody.wordWrap = true;
        sBody.richText = true;
        sBody.normal.textColor = new Color(0.90f, 0.89f, 0.86f);

        sPrompt = new GUIStyle(GUI.skin.label);
        sPrompt.fontSize = Mathf.RoundToInt(baseSize * 1.15f);
        sPrompt.alignment = TextAnchor.MiddleCenter;
        sPrompt.fontStyle = FontStyle.Bold;

        sSmall = new GUIStyle(GUI.skin.label);
        sSmall.fontSize = Mathf.RoundToInt(baseSize * 0.88f);
        sSmall.normal.textColor = new Color(0.78f, 0.76f, 0.72f);

        sButton = new GUIStyle(GUI.skin.button);
        sButton.fontSize = Mathf.RoundToInt(baseSize * 1.15f);

        stylesReady = true;
    }

    void OnGUI()
    {
        if (!stylesReady) BuildStyles();

        if (paused) { DrawPause(); return; }
        if (openPoint != null) { DrawInfoPanel(); return; }

        DrawCrosshair();
        DrawProgress();
        DrawPrompt();
        DrawIntro();
        if (showHelp) DrawHelp();
        else DrawHelpHint();
    }

    void DrawCrosshair()
    {
        float s = Mathf.Max(2f, Screen.height / 340f);
        float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
        Color c = (current != null) ? gold : new Color(1f, 1f, 1f, 0.55f);
        GUI.color = c;
        GUI.DrawTexture(new Rect(cx - s * 4f, cy - s * 0.5f, s * 3f, s), texWhite);
        GUI.DrawTexture(new Rect(cx + s * 1f, cy - s * 0.5f, s * 3f, s), texWhite);
        GUI.DrawTexture(new Rect(cx - s * 0.5f, cy - s * 4f, s, s * 3f), texWhite);
        GUI.DrawTexture(new Rect(cx - s * 0.5f, cy + s * 1f, s, s * 3f), texWhite);
        GUI.color = Color.white;
    }

    void DrawProgress()
    {
        int v = VisitedCount();
        int total = (points != null) ? points.Length : 0;
        string txt = "Σημεία τεκμηρίωσης:  " + v + " / " + total;
        float w = Screen.width * 0.26f, h = Screen.height * 0.052f;
        Rect r = new Rect(Screen.width - w - 18f, 18f, w, h);
        GUI.DrawTexture(r, texPanel);
        GUI.color = gold;
        GUI.DrawTexture(new Rect(r.x, r.y, 4f, r.height), texWhite);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 14f, r.y, r.width - 18f, r.height), txt, sSmall);
    }

    void DrawPrompt()
    {
        if (current == null) return;
        string t = "[ E ]   " + current.title;
        float w = Screen.width * 0.5f, h = Screen.height * 0.062f;
        Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.70f, w, h);
        GUI.DrawTexture(r, texPanel);
        GUI.color = gold;
        GUI.Label(r, t, sPrompt);
        GUI.color = Color.white;
    }

    void DrawInfoPanel()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texDim);

        float w = Mathf.Min(Screen.width * 0.62f, 900f);
        float h = Screen.height * 0.66f;
        Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.DrawTexture(r, texPanel);

        GUI.color = gold;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4f), texWhite);
        GUI.color = Color.white;

        float pad = 30f;
        GUI.color = gold;
        GUI.Label(new Rect(r.x + pad, r.y + pad * 0.7f, r.width - pad * 2f, 34f),
                  "ΣΗΜΕΙΟ " + openPoint.code, sSmall);
        GUI.Label(new Rect(r.x + pad, r.y + pad * 1.7f, r.width - pad * 2f, 60f),
                  openPoint.title, sHeader);
        GUI.color = Color.white;

        Rect body = new Rect(r.x + pad, r.y + pad * 3.6f,
                             r.width - pad * 2f, r.height - pad * 5.4f);
        float contentH = sBody.CalcHeight(new GUIContent(openPoint.body), body.width - 22f);
        scroll = GUI.BeginScrollView(body, scroll,
                                     new Rect(0, 0, body.width - 22f, contentH));
        GUI.Label(new Rect(0, 0, body.width - 22f, contentH), openPoint.body, sBody);
        GUI.EndScrollView();

        GUI.Label(new Rect(r.x + pad, r.yMax - pad * 1.35f, r.width - pad * 2f, 30f),
                  "[ Esc ] ή [ E ]  κλείσιμο", sSmall);

        if (Input.GetKeyDown(KeyCode.E)) openPoint = null;
    }

    void DrawHelpHint()
    {
        GUI.Label(new Rect(18f, Screen.height - 34f, 400f, 26f),
                  "[H] οδηγίες   ·   [Esc] μενού", sSmall);
    }

    void DrawHelp()
    {
        string[,] keys = {
            {"W A S D",      "Κίνηση"},
            {"Ποντίκι",      "Περιστροφή βλέμματος"},
            {"Shift",        "Τρέξιμο"},
            {"Ctrl / C",     "Σκύψιμο"},
            {"Space",        "Άλμα"},
            {"F",            "Φακός on/off"},
            {"E",            "Ανάγνωση πινακίδας"},
            {"H",            "Εμφάνιση/απόκρυψη οδηγιών"},
            {"Esc",          "Μενού παύσης"},
        };
        float w = Mathf.Min(Screen.width * 0.40f, 560f);
        float rowH = Screen.height * 0.042f;
        float h = rowH * (keys.GetLength(0) + 1.6f);
        Rect r = new Rect(18f, Screen.height - h - 46f, w, h);
        GUI.DrawTexture(r, texPanel);
        GUI.color = gold;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3f), texWhite);
        GUI.Label(new Rect(r.x + 18f, r.y + 6f, r.width, rowH), "ΧΕΙΡΙΣΜΟΣ", sSmall);
        GUI.color = Color.white;
        for (int i = 0; i < keys.GetLength(0); i++)
        {
            float y = r.y + rowH * (i + 1.15f);
            GUI.color = gold;
            GUI.Label(new Rect(r.x + 18f, y, w * 0.34f, rowH), keys[i, 0], sSmall);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + w * 0.40f, y, w * 0.58f, rowH), keys[i, 1], sSmall);
        }
    }

    void DrawIntro()
    {
        if (introTimer > INTRO_DURATION) return;
        float a = 1f;
        if (introTimer < 0.8f) a = introTimer / 0.8f;
        else if (introTimer > INTRO_DURATION - 1.6f)
            a = (INTRO_DURATION - introTimer) / 1.6f;
        a = Mathf.Clamp01(a);

        GUI.color = new Color(0f, 0f, 0f, a * 0.55f);
        GUI.DrawTexture(new Rect(0, Screen.height * 0.30f, Screen.width, Screen.height * 0.30f),
                        texWhite);
        GUI.color = new Color(gold.r, gold.g, gold.b, a);
        GUI.Label(new Rect(0, Screen.height * 0.33f, Screen.width, Screen.height * 0.14f),
                  siteTitle, sTitle);
        GUI.color = new Color(1f, 1f, 1f, a * 0.9f);
        GUI.Label(new Rect(0, Screen.height * 0.465f, Screen.width, Screen.height * 0.06f),
                  siteSubtitle + (string.IsNullOrEmpty(author) ? "" : "   ·   " + author),
                  sSub);
        GUI.color = Color.white;
    }

    void DrawPause()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texDim);
        float w = Mathf.Min(Screen.width * 0.34f, 420f);
        float h = Screen.height * 0.42f;
        Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.DrawTexture(r, texPanel);
        GUI.color = gold;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4f), texWhite);
        GUI.Label(new Rect(r.x, r.y + 22f, r.width, 46f), "ΠΑΥΣΗ", sSub);
        GUI.color = Color.white;

        float bw = r.width - 60f, bh = Screen.height * 0.070f;
        float y = r.y + h * 0.34f;

        if (GUI.Button(new Rect(r.x + 30f, y, bw, bh), "Συνέχεια", sButton))
            paused = false;

        y += bh + 14f;
        if (GUI.Button(new Rect(r.x + 30f, y, bw, bh), "Επανεκκίνηση περιήγησης", sButton))
        {
            paused = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        y += bh + 14f;
        if (GUI.Button(new Rect(r.x + 30f, y, bw, bh), "Έξοδος", sButton))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GUI.Label(new Rect(r.x + 30f, r.yMax - 34f, r.width, 26f),
                  "Σημεία που είδατε: " + VisitedCount() + " / " +
                  ((points != null) ? points.Length : 0), sSmall);
    }
}
