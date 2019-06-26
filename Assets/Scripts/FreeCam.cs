using System;
using UnityEngine;

public class FreeCam : MonoBehaviour {
    public bool translation = true, rotation = true;
    public float vel = 1, minVel = .1f;
    public bool doClipPlane = false;
    public Color velColor = Color.black;
    public string[] axesX = new string[] { "Mouse X" }, axesY = new string[] { "Mouse Y" };

    #region InputFocus
    bool focused = true;
    public bool Focused {
        get { return focused; }
        set {
            focused = enabled = cam.enabled = AL.enabled = value;
            if (focused) ShowCursor = ShowCursor;
        }
    }

    static FreeCam focusedInstance;
    public static FreeCam FocusedInstance {
        get { return focusedInstance; }
        set {
            if (focusedInstance != value) {
                if (focusedInstance != null) focusedInstance.Focused = false;
                focusedInstance = value;
                focusedInstance.Focused = true;
            }
        }
    }
    #endregion

    #region Cursor
    [SerializeField]
    bool showCursor;
    public bool ShowCursor {
        get { return showCursor; }
        set {
            showCursor = value;
            Cursor.visible = showCursor;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
    void OnApplicationFocus(bool focus) {
        if (focus && Focused) {
            ShowCursor = ShowCursor;
            /*if (ShowCursor) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.lockState = CursorLockMode.None;
            }*/
        }
    }
    #endregion

    [Serializable]
    public struct Keybinds {
        public KeyCode forward, back, left, right, up, down, mouse;
        public Keybinds(KeyCode forward, KeyCode back, KeyCode left, KeyCode right, KeyCode up, KeyCode down, KeyCode mouse) {
            this.forward = forward;
            this.back = back;
            this.left = left;
            this.right = right;
            this.up = up;
            this.down = down;
            this.mouse = mouse;
        }
    }
    public Keybinds keys = new Keybinds(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.E, KeyCode.Q, KeyCode.LeftControl);

    Camera cam;
    AudioListener AL;
    float defNear, defFar;
    void Start() {
        #region VarInit
        cam = GetComponent<Camera>();
        AL = GetComponent<AudioListener>();
        if (cam != null) {
            defNear = cam.nearClipPlane;
            defFar = cam.farClipPlane;
        }

		st.normal.textColor = velColor;
		st.fontSize = 14;
		#endregion

		ShowCursor = showCursor;
    }

    void OnDestroy() {
        if (Focused) ShowCursor = true;
    }


	GUIStyle st = new GUIStyle();
	Rect rect = new Rect(10, 10, 100, 100);
	void OnGUI() {
        if (Focused) {
            string txt = $"Velocity: {vel:.00}m/s";
            if (doClipPlane) txt += "\nNear: " + cam.nearClipPlane + " Far: " + cam.farClipPlane;
            GUI.Label(rect, txt, st);
        }
    }
    
    void Update() {
        if (Focused) {
            #region Rotation
            if (rotation) {
                float mX = 0f, mY = 0f;

                foreach (string axis in axesX) {
                    float val = Input.GetAxis(axis);
                    if (Mathf.Abs(val) > Mathf.Abs(mX)) mX = val;
                }
                foreach (string axis in axesY) {
                    float val = Input.GetAxis(axis);
                    if (Mathf.Abs(val) > Mathf.Abs(mY)) mY = val;
                }

                if ((mX != 0 || mY != 0) && !Cursor.visible) {
                    Vector3 angs = transform.localEulerAngles;
                    angs.y += mX;
                    angs.x = Mathf.Clamp((angs.x + 180f) % 360f - mY, 90f, 270f) - 180f;
                    transform.localEulerAngles = angs;
                }
            }
            #endregion

            #region Set Vel
            float scroll = Input.mouseScrollDelta.y;
            if (Input.anyKey && scroll == 0) {
                scroll = Input.GetKey(KeyCode.Equals) ? .1f : Input.GetKey(KeyCode.Minus) ? -.1f : 0;
            }
            if (scroll != 0) {
                //vel = Mathf.Round(Mathf.Max(vel + (.1f * vel * scroll), 0.1f) * 10) / 10;
                vel = Mathf.Max(vel + (.1f * vel * scroll), minVel);
            }
            #endregion

            #region Keys
            if (Input.anyKey) {
                //Quaternion dir = transform.rotation;

                if (translation) {
                    Vector3 mov = new Vector3(
                        Input.GetKey(keys.right) ? 1 : Input.GetKey(keys.left) ? -1 : 0,
                        Input.GetKey(keys.up) ? 1 : Input.GetKey(keys.down) ? -1 : 0,
                        Input.GetKey(keys.forward) ? 1 : Input.GetKey(keys.back) ? -1 : 0
                    );

                    if (mov != Vector3.zero) {
                        Vector3 p = transform.TransformPoint(Vector3.ClampMagnitude(mov, 1) * vel * Time.deltaTime);
                        transform.position = p;
                    }
                }

                if (Input.anyKeyDown) {
                    if (Input.GetKeyDown(keys.mouse) || Input.GetKeyDown(KeyCode.Escape))
                        ShowCursor = !ShowCursor;

                    #region clipPlane
                    if (doClipPlane && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R)) {
                        cam.nearClipPlane = defNear;
                        cam.farClipPlane = defFar;
                    }
                    #endregion
                }

                #region clipPlane
                if (doClipPlane) {
                    if (Input.GetKey(KeyCode.U))
                        cam.nearClipPlane += cam.nearClipPlane * .01f;
                    else if (Input.GetKey(KeyCode.J))
                        cam.nearClipPlane = Mathf.Max(cam.nearClipPlane - .01f * cam.nearClipPlane, .1f);

                    if (Input.GetKey(KeyCode.I))
                        cam.farClipPlane += cam.farClipPlane * .01f;
                    else if (Input.GetKey(KeyCode.K))
                        cam.farClipPlane = Mathf.Max(cam.farClipPlane - .01f * cam.farClipPlane, .1f);
                }
                #endregion
            }
            #endregion
        }
    }
}
