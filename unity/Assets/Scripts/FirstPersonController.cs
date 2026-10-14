// =====================================================================
//  FirstPersonController.cs
//  Περιήγηση πρώτου προσώπου (WASD + ποντίκι), με CharacterController.
//
//  ΠΟΥ ΜΠΑΙΝΕΙ: Assets/Scripts/FirstPersonController.cs
//  Το προσθέτει αυτόματα το SceneSetupWindow - δεν χρειάζεται να κάνεις
//  τίποτα με το χέρι.
//
//  ΧΕΙΡΙΣΜΟΣ:
//    W A S D  : κίνηση            Ποντίκι : βλέμμα
//    Shift    : τρέξιμο           Space   : άλμα
//    Ctrl     : σκύψιμο           F       : φακός
//    E        : ανάγνωση πινακίδας
//    Esc      : ελευθέρωση ποντικιού
// =====================================================================

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Ταχύτητες (m/s)")]
    public float walkSpeed = 3.2f;
    public float runSpeed = 6.4f;
    public float crouchSpeed = 1.6f;
    public float acceleration = 12f;

    [Header("Βλέμμα")]
    public float mouseSensitivity = 2.2f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    public bool invertY = false;

    [Header("Φυσική")]
    public float gravity = -18f;
    public float jumpHeight = 1.05f;
    public float standHeight = 1.75f;
    public float crouchHeight = 1.05f;

    [Header("Head bob (ταλάντωση κεφαλιού)")]
    public bool headBob = true;
    public float bobFrequency = 1.9f;
    public float bobAmplitude = 0.045f;

    [Header("Φακός")]
    public bool flashlightEnabled = true;

    /// <summary>Το θέτει το TourHUD όταν ανοίγει μενού/πινακίδα.</summary>
    public static bool InputBlocked = false;

    // -- εσωτερικά --
    CharacterController cc;
    Transform cam;
    Light flashlight;

    float pitch;
    float bobTimer;
    float camBaseY;
    Vector3 velocity;          // οριζόντια ταχύτητα
    float verticalVel;
    bool crouching;
    bool cursorLocked = true;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.height = standHeight;
        cc.center = new Vector3(0f, standHeight * 0.5f, 0f);
        cc.radius = 0.32f;
        cc.slopeLimit = 50f;
        cc.stepOffset = 0.45f;
        cc.skinWidth = 0.02f;

        Camera c = GetComponentInChildren<Camera>();
        if (c == null)
        {
            GameObject go = new GameObject("PlayerCamera");
            go.transform.SetParent(transform, false);
            c = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam = c.transform;
        cam.localPosition = new Vector3(0f, standHeight - 0.16f, 0f);
        camBaseY = cam.localPosition.y;

        flashlight = cam.GetComponentInChildren<Light>();
        if (flashlight == null && flashlightEnabled)
        {
            GameObject fl = new GameObject("Flashlight");
            fl.transform.SetParent(cam, false);
            flashlight = fl.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 26f;
            flashlight.spotAngle = 58f;
            flashlight.intensity = 2.4f;
            flashlight.color = new Color(1f, 0.94f, 0.82f);
            flashlight.shadows = LightShadows.Soft;
            flashlight.enabled = false;
        }
    }

    void Start()
    {
        LockCursor(true);
    }

    void Update()
    {
        if (InputBlocked)
        {
            if (cursorLocked) LockCursor(false);
            // εφαρμογή βαρύτητας ώστε να μη "κολλάει" στον αέρα
            verticalVel += gravity * Time.deltaTime;
            cc.Move(new Vector3(0f, verticalVel, 0f) * Time.deltaTime);
            if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
            return;
        }
        if (!cursorLocked) LockCursor(true);

        HandleLook();
        HandleCrouch();
        HandleMove();
        HandleBob();
        HandleFlashlight();
    }

    // -----------------------------------------------------------------
    void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleLook()
    {
        float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * (invertY ? 1f : -1f);

        transform.Rotate(Vector3.up * mx);
        pitch = Mathf.Clamp(pitch + my, minPitch, maxPitch);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleCrouch()
    {
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

        // μη σηκωθείς αν έχεις ταβάνι από πάνω σου
        if (crouching && !wantCrouch)
        {
            Vector3 top = transform.position + Vector3.up * crouchHeight;
            if (Physics.SphereCast(top, cc.radius * 0.9f, Vector3.up, out _,
                                   standHeight - crouchHeight))
                wantCrouch = true;
        }

        crouching = wantCrouch;
        float target = crouching ? crouchHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, target, Time.deltaTime * 11f);
        cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
        camBaseY = cc.height - 0.16f;
    }

    void HandleMove()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 wish = (transform.right * h + transform.forward * v);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        float speed = crouching ? crouchSpeed
                    : (Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed);

        velocity = Vector3.MoveTowards(velocity, wish * speed,
                                       acceleration * Time.deltaTime);

        if (cc.isGrounded)
        {
            if (verticalVel < 0f) verticalVel = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && !crouching)
                verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVel += gravity * Time.deltaTime;

        Vector3 move = velocity;
        move.y = verticalVel;
        cc.Move(move * Time.deltaTime);
    }

    void HandleBob()
    {
        if (!headBob || cam == null) return;
        float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        if (cc.isGrounded && planarSpeed > 0.15f)
        {
            bobTimer += Time.deltaTime * bobFrequency * planarSpeed;
            float y = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude;
            float x = Mathf.Cos(bobTimer * Mathf.PI) * bobAmplitude * 0.45f;
            cam.localPosition = new Vector3(x, camBaseY + y, 0f);
        }
        else
        {
            bobTimer = 0f;
            cam.localPosition = Vector3.Lerp(cam.localPosition,
                                             new Vector3(0f, camBaseY, 0f),
                                             Time.deltaTime * 8f);
        }
    }

    void HandleFlashlight()
    {
        if (flashlight != null && Input.GetKeyDown(KeyCode.F))
            flashlight.enabled = !flashlight.enabled;
    }

    public bool IsRunning
    {
        get { return !crouching && Input.GetKey(KeyCode.LeftShift); }
    }
}
