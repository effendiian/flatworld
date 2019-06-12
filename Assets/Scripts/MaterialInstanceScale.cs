using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MaterialInstanceScale : MonoBehaviour {
    [SerializeField]
    Vector2 scale;
    public Vector2 Scale {
        get => scale;
        set {
            scale = value;
            ApplyProperties();
        }
    }

    MeshRenderer mr;
    public MeshRenderer MR {
        get {
            if (mr == null) mr = GetComponent<MeshRenderer>();
            return mr;
        }
        set => mr = value;
    }

    MaterialPropertyBlock props;
    public MaterialPropertyBlock Props {
        get {
            if (props == null) props = new MaterialPropertyBlock();
            return props;
        }
        set => props = value;
    }

    void OnValidate() {
        if (scale.x < 0) scale.x = 0;
        if (scale.y < 0) scale.y = 0;
        Scale = scale;
    }
    void Start() {
        OnValidate();
    }

    void ApplyProperties() {
        Props.SetVector("_Scale", scale);
        MR.SetPropertyBlock(Props);
    }
}
