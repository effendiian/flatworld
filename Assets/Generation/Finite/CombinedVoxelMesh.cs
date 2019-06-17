using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace CombinedVoxelMesh {
    public enum BlockType : byte { Air = 0, Grass, Dirt, Stone, Bedrock }

    [System.Serializable]
    public struct Voxel {
        public BlockType id;

        public Voxel(BlockType ty) => this.id = ty;
    }

    [System.Serializable]
    public struct FlatLayer {
        public BlockType type;
        public int height;
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class CombinedVoxelMesh : MonoBehaviour {
        public Vector3Int size = Vector3Int.one;
        [HideInInspector]
        public Voxel[] voxels;

        public FlatLayer[] layers;
        public Mesh baseMesh;

        void OnValidate() {
            size.y = 0;
            foreach (FlatLayer l in layers)
                size.y += l.height;
            sx = size.x;
            sxz = size.x * size.z;
        }

        void OnEnable() {

        }
        void OnDisable() {

        }

        MeshFilter MF;
        MeshRenderer MR;
        MeshCollider MC;
        Mesh msh;

        int sx, sxz;

        void Start() {
            MF = GetComponent<MeshFilter>();
            MR = GetComponent<MeshRenderer>();
            MC = GetComponent<MeshCollider>();

            msh = new Mesh();
            msh.name = "Combined Voxel Mesh";
            msh.MarkDynamic();

            Generate();

            MC.sharedMesh = msh;
        }

        void Clear() {
            voxels = null;
        }
        void Generate() {
            //if (voxels != null || voxels.Length != 0) return;

            voxels = new Voxel[size.x * size.y * size.z];

            int wh = size.x * size.z, i = 0;
            foreach (FlatLayer lay in layers)
                for (int j = 0; j < wh * lay.height; j++)
                    voxels[i++] = new Voxel(lay.type);

            GenerateMesh();
        }
        public void GenerateMesh() {
            msh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            List<CombineInstance> instances = new List<CombineInstance>(voxels.Length);

            Vector3Int[] sides = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };

            for (int i = 0; i < voxels.Length; i++) {
                Voxel v = voxels[i];
                if (v.id == BlockType.Air) continue;

                Vector3Int p = IndexToXYZ(i);

                /*Profiler.BeginSample("Internal Culling");
                bool bordersAir = false;
                foreach (Vector3Int off in sides) {
                    Vector3Int xyz = p + off;
                    if (xyz.x < 0 || xyz.x >= size.x || xyz.y < 0 || xyz.y >= size.y || xyz.z < 0 || xyz.z >= size.z ||
                            voxels[XYZtoIndex(xyz)].id == BlockType.Air) {
                        bordersAir = true;
                        break;
                    }
                }
                if (!bordersAir) continue;
                Profiler.EndSample();*/

                CombineInstance inst = new CombineInstance {
                    mesh = baseMesh,
                    transform = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one)
                };
                instances.Add(inst);
            }

            msh.CombineMeshes(instances.ToArray(), true, true);

            MF.mesh = msh;
            MC.sharedMesh = msh;
        }
        void GenerateColliders() {

        }

        public Vector3Int IndexToXYZ(int i) => new Vector3Int(i % size.x, i / (size.x * size.z), (i / size.x) % size.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int XYZtoIndex(Vector3Int xyz) => xyz.x + (xyz.z * sx) + (xyz.y * sxz);

        public Vector3 XYZtoWorld(Vector3Int xtz) => transform.TransformPoint(xtz);
        public Vector3Int WorldToXYZ(Vector3 world) {
            Vector3 v = transform.InverseTransformPoint(world);
            return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }
    }
}