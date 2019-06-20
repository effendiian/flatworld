using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiniteFlatTerrainGen {
    [RequireComponent(typeof(Camera))]
    public class FFTG_player : MonoBehaviour {
        public FiniteFlatTerrainGen world;

        public KeyCode add, remove;
        Camera cam;

        void Start() {
            cam = GetComponent<Camera>();
        }

        bool GetPosUnderCrosshair(out Vector3Int pos, bool adjacent = false) {
            Ray ray;
            if (Cursor.visible)
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            else
                ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                pos = world.WorldToCoord(hit.point + hit.normal * (adjacent ? 0.5f : -0.5f));
                return true;
            }

            pos = new Vector3Int();
            return false;
        }

        Vector3 hitPos, voxelPos;

        void Update() {
            if (Input.anyKeyDown) {
                if (Input.GetKeyDown(add)) {
                    Vector3Int pos;
                    if (GetPosUnderCrosshair(out pos, true)) {
                        Voxel voxel = world.world[pos.x, pos.y, pos.z];
                        voxel.id = (byte)BlockType.Stone;
                        voxel.CreateGameObject(world.CoordToWorld(pos), world);
                        world.world[pos.x, pos.y, pos.z] = voxel;
                    }
                }
                else if (Input.GetKeyDown(remove)) {
                    Vector3Int pos;
                    if (GetPosUnderCrosshair(out pos)) {
                        Voxel voxel = world.world[pos.x, pos.y, pos.z];
                        voxel.id = (byte)BlockType.Air;
                        Destroy(voxel.o);
                        world.world[pos.x, pos.y, pos.z] = voxel;
                    }
                }
            }
        }
    }
}