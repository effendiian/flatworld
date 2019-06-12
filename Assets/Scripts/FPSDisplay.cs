using UnityEngine;

public class FPSDisplay : MonoBehaviour {
    public TextAnchor anch = TextAnchor.UpperRight;
    public int fontSize = 14;
    public bool showInStandalone = true, showInEditor = true;

    float deltaTime = 0;
    GUIStyle style = new GUIStyle(), shad;
    Rect rShad, rTxt;
    void Start() {
        if (!(Application.isEditor ? showInEditor : showInStandalone)) {
            enabled = false;
            return;
        }

        style.alignment = anch;
        style.fontSize = fontSize;
        shad = new GUIStyle(style);
        shad.normal.textColor = new Color(0, 0, 0, .6f);

        rShad = new Rect(0, 1, Screen.width, Screen.height);
        rTxt = new Rect(-1, 0, Screen.width, Screen.height);
    }

    void Update() {
        if (Time.timeScale > 0) {
            deltaTime += (Time.deltaTime - deltaTime) * .1f;
            //deltaTime = Time.deltaTime;
        }
    }

    void OnGUI() {
        if (Time.timeScale > 0) {
            float fps = 1f / deltaTime;
            style.normal.textColor = ((fps < 20) ? Color.red : (fps < 30) ? Color.yellow : Color.green);
            string txt = Mathf.Round(fps) + " FPS";

            rShad.width = rTxt.width = Screen.width;
            rShad.height = rTxt.height = Screen.height;
            GUI.Label(rShad, txt, shad);
            GUI.Label(rTxt, txt, style);
        }
    }
}