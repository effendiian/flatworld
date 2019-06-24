using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
    public struct CVM_Chunk {
        public Vector2Int pos;
        public readonly CombinedVoxelMesh CVM;

        public CVM_Chunk(Vector2Int pos, CombinedVoxelMesh cVM) {
            this.pos = pos;
            CVM = cVM;
        }
    }

    public class CVM_InfChunkGen : MonoBehaviour {
        public Vector2Int size = Vector2Int.one;
        public GameObject chunkPrefab;
        public GameObject cam;

        //[HideInInspector]
        //public CombinedVoxelMesh[] chunks;
        public CVM_Chunk[] chunks;
        Dictionary<Vector2Int, CombinedVoxelMesh> chunkMap;

        int csx, csz;

        Vector2Int viewPos = new Vector2Int(int.MinValue, int.MinValue);

        void Start() {
            chunks = new CVM_Chunk[size.x * size.y];

            CombinedVoxelMesh CVM = chunkPrefab.GetComponent<CombinedVoxelMesh>();
            Vector3Int chunkSize = CVM.size;

            csx = chunkSize.x;
            csz = chunkSize.z;

            chunkMap = new Dictionary<Vector2Int, CombinedVoxelMesh>(chunks.Length);

            viewPos = GetViewpos();
            Vector2Int off = viewPos - new Vector2Int(size.x / 2, size.y / 2);

            int x, z;
            for (int i = 0; i < chunks.Length; i++) {
                IndexToXZ(i, out x, out z);
                Vector2Int xz = new Vector2Int(x, z) + off;
                CombinedVoxelMesh CVM_1 = Instantiate(chunkPrefab, new Vector3(xz.x * chunkSize.x, 0f, xz.y * chunkSize.z), Quaternion.identity, transform).GetComponent<CombinedVoxelMesh>();
                chunks[i] = new CVM_Chunk(xz, CVM_1);
                chunkMap[xz] = CVM_1;
            }
        }


        void Update() {
            Vector2Int pos = GetViewpos();
            if (pos != viewPos) {
                UpdatePositons(pos);
            }
        }

        Vector2Int GetViewpos() => WorldToXZ(cam.transform.position);
        void UpdatePositons(Vector2Int vpos) {
            int r = size.x / 2;

            Vector2Int p2 = viewPos + vpos;

            for (int i = 0; i < chunks.Length; i++) {
                CVM_Chunk c = chunks[i];
                Vector2Int dif = c.pos - vpos;
                if (Mathf.Max(Mathf.Abs(dif.x), Mathf.Abs(dif.y)) > r) {
                    int x = p2.x - c.pos.x;
                    int z = p2.y - c.pos.y;
                    Vector2Int nPos = new Vector2Int(x, z);
                    chunks[i].pos = nPos;
                    c.CVM.gameObject.transform.position = new Vector3(x * csx, 0, z * csz);
                    chunkMap[nPos] = c.CVM;
                }
            }

            viewPos = vpos;
        }

        public void WorldToXZ(Vector3 world, out int x, out int z) {
            x = Mathf.FloorToInt(Mathf.Round(world.x) / csx);
            z = Mathf.FloorToInt(Mathf.Round(world.z) / csz);
        }
        public Vector2Int WorldToXZ(Vector3 world) {
            int x, z;
            WorldToXZ(world, out x, out z);
            return new Vector2Int(x, z);
        }
        public int XZtoIndex(int x, int z) => x + z * size.x;
        public void IndexToXZ(int i, out int x, out int z) {
            x = i % size.x;
            z = (i / size.x) % size.y;
        }

        public CombinedVoxelMesh WorldToChunk(Vector3 world) {
            //int x, z;
            //WorldToXZ(world, out x, out z);
            //return XZtoChunk(x, z);
            return chunkMap[WorldToXZ(world)];
        }
        public CombinedVoxelMesh XZtoChunk(int x, int z) => chunks[XZtoIndex(x, z)].CVM;
    }
}