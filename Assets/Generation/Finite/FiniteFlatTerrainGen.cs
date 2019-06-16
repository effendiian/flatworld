using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FiniteFlatTerrainGen {
    public enum BlockType : byte { Air = 0, Grass, Dirt, Stone, Bedrock }

    [System.Serializable]
    public struct Voxel {
        public byte id;
        public GameObject o;

        public Voxel(BlockType ty, Vector3 pos, FiniteFlatTerrainGen gen) {
            id = (byte)ty;

            o = null;
            CreateGameObject(pos, gen);
        }

        public void CreateGameObject(Vector3 pos, FiniteFlatTerrainGen gen) {
            BlockType ty = (BlockType)id;
            if (ty != BlockType.Air) {
                o = Object.Instantiate(gen.cube, pos, Quaternion.identity, gen.transform);
                o.name = ty.ToString();
                o.hideFlags = HideFlags.HideInHierarchy;
                o.isStatic = true;
                gen.objs[gen.objC++] = o;
            }
        }

        //public override string ToString() => $"{(BlockType)id}";
    }

    [System.Serializable]
    public struct FlatLayer {
        public BlockType type;
        public int height;
    }

    public class FiniteFlatTerrainGen : MonoBehaviour {
        [InspectorButton("Regenerate", ButtonWidth = 100)]
        public bool genInEditor = false;

        [InspectorButton("Clear", ButtonWidth = 100)]
        public bool clear = false;

        public Vector3Int dimensions = Vector3Int.one;
        public FlatLayer[] layers;
        public GameObject cube;

        public Voxel[,,] world;

        [SerializeField, HideInInspector]
        public GameObject[] objs;
        [SerializeField, HideInInspector]
        public int objC = 0;

        //void Start() => Regenerate();

        void OnEnable() => Regenerate();
        void OnDisable() => Clear();

        void Regenerate() {
            Clear();
            Generate();
        }
        void Generate() {
            //if (world != null) return;

            int height = 0;
            foreach (FlatLayer lay in layers)
                height += lay.height;
            dimensions.y = height;

            world = new Voxel[dimensions.x, dimensions.y, dimensions.z];
            objs = new GameObject[dimensions.x * dimensions.y * dimensions.z];

            int y_1 = 0, i = 0;
            foreach (FlatLayer lay in layers) {
                for (int x = 0; x < dimensions.x; x++) {
                    for (int z = 0; z < dimensions.z; z++) {
                        for (int k = 0; k < lay.height; k++) {
                            int y = y_1 + k;
                            world[x, y, z] = new Voxel(lay.type, new Vector3(x, y, z), this);
                        }
                    }
                }
                y_1 += lay.height;
            }
        }

        void Clear() {
            if (objs != null) {
                for (int i = 0; i < objs.Length; i++)
                    if (objs[i] != null) DestroyImmediate(objs[i]);
                objs = null;
                objC = 0;
            }
            if (world != null)
                world = null;
        }

        public Vector3 CoordToWorld(Vector3Int coord) => transform.TransformPoint(coord);
        public Vector3Int WorldToCoord(Vector3 pos) {
            Vector3 v = transform.InverseTransformPoint(pos);
            return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }
    }
}