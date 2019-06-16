using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FiniteFlatTerrainGen {
    public enum BlockType : byte { Air = 0, Grass, Dirt, Stone, Bedrock }

    public struct Voxel {
        public byte id;

        public Voxel(BlockType ty) => id = (byte)ty;

        //public override string ToString() => $"{(BlockType)id}";
    }

    [System.Serializable]
    public struct FlatLayer {
        public BlockType type;
        public int height;
    }

    public class FiniteFlatTerrainGen : MonoBehaviour {
        public Vector3Int dimensions = Vector3Int.one;
        public FlatLayer[] layers;
        public GameObject cube;

        public Voxel[,,] world;

        void Start() {
            int height = 0;
            foreach (FlatLayer lay in layers)
                height += lay.height;
            dimensions.y = height;

            world = new Voxel[dimensions.x, dimensions.y, dimensions.z];

            int y_1 = 0;
            foreach (FlatLayer lay in layers) {
                for (int x = 0; x < dimensions.x; x++) {
                    for (int z = 0; z < dimensions.z; z++) {
                        for (int k = 0; k < lay.height; k++) {
                            int y = y_1 + k;
                            world[x, y, z] = new Voxel(lay.type);

                            if (lay.type != BlockType.Air) {
                                GameObject o = Instantiate(cube, new Vector3(x, y, z), Quaternion.identity, transform);
                                o.name = lay.type.ToString();
                            }
                        }
                    }
                }
                y_1 += lay.height;
            }
        }
    }
}