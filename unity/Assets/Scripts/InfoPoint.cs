// =====================================================================
//  InfoPoint.cs
//  Σημείο τεκμηρίωσης: όταν το κοιτάς και πατήσεις E, εμφανίζεται
//  το κείμενο για το συγκεκριμένο αρχιτεκτονικό μέλος.
//
//  ΠΟΥ ΜΠΑΙΝΕΙ: Assets/Scripts/InfoPoint.cs
// =====================================================================

using UnityEngine;

public class InfoPoint : MonoBehaviour
{
    [Header("Περιεχόμενο πινακίδας")]
    public string code = "01";
    public string title = "Τίτλος";

    [TextArea(4, 14)]
    public string body = "Κείμενο τεκμηρίωσης...";

    [Header("Εμφάνιση")]
    public float activationRadius = 4.5f;
    public Color markerColor = new Color(1f, 0.82f, 0.35f);
    public bool showBeacon = true;

    [HideInInspector] public bool visited;

    Transform beacon;
    Light glow;

    void Start()
    {
        // κάνε το collider trigger ώστε να μη σε μπλοκάρει
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = activationRadius;

        gameObject.layer = 0;

        if (showBeacon) BuildBeacon();
    }

    void BuildBeacon()
    {
        // μικρός φωτεινός δείκτης ώστε να ξεχωρίζει το σημείο μέσα στον χώρο
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Beacon";
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 0.34f;
        Destroy(go.GetComponent<Collider>());

        Renderer r = go.GetComponent<Renderer>();
        Material m = new Material(r.sharedMaterial);
        m.color = markerColor;
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", markerColor * 2.4f);
        }
        r.material = m;
        beacon = go.transform;

        GameObject lg = new GameObject("BeaconLight");
        lg.transform.SetParent(transform, false);
        glow = lg.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 5.5f;
        glow.intensity = 1.4f;
        glow.color = markerColor;
    }

    void Update()
    {
        if (beacon == null) return;
        // απαλή αιώρηση + παλμός ώστε να τραβάει το μάτι
        float t = Time.time;
        beacon.localPosition = new Vector3(0f, Mathf.Sin(t * 1.6f) * 0.16f, 0f);
        beacon.Rotate(Vector3.up, 45f * Time.deltaTime, Space.Self);
        if (glow != null)
            glow.intensity = visited ? 0.5f : 1.1f + Mathf.Sin(t * 2.4f) * 0.45f;
        if (visited) beacon.localScale = Vector3.one * 0.22f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(markerColor.r, markerColor.g, markerColor.b, 0.35f);
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
