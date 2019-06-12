using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetFramerate : MonoBehaviour {
    [SerializeField]
    int framerate = -1;

    public int Framerate {
        get { return Application.targetFrameRate; }
        set {
            if (value != Framerate) {
                Application.targetFrameRate = framerate = value;
            }
        }
    }

    void SetFramerate() => Framerate = framerate;

    void OnValidate() => SetFramerate();
    void Start() => SetFramerate();
    void OnDisable() => Application.targetFrameRate = -1;
    void OnEnable() => SetFramerate();

    #region XR Framerate Workaround
    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

    void OnPreCull() {
        if (UnityEngine.XR.XRSettings.enabled && framerate > 0) {
            sw.Restart();
            int msDelay = (int)((1f / framerate) * 1000);

            while (sw.ElapsedMilliseconds < msDelay) { }
        }
    }
    #endregion
}
