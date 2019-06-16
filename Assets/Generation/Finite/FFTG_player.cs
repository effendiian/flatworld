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

        Vector3Int GetPosUnderCrosshair() {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
                print("I'm looking at " + hit.transform.name);

            return new Vector3Int();
        }

        void Update() {
            if (Input.anyKeyDown) {
                if (Input.GetKeyDown(add)) {

                }
                else if (Input.GetKeyDown(remove)) {
                    //Vector3Int pos = GetPosUnderCrosshair();

                    Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit)) {
                        var v = world.transform.InverseTransformPoint(hit.point - (hit.normal * 0.5f));
                        Vector3Int pos = new Vector3Int((int)v.x, (int)v.y, (int)v.z);

                        world.world[pos.x, pos.y, pos.z].id = (byte)BlockType.Air;

                        Destroy(hit.transform.gameObject);
                    }
                }
            }
        }
    }
}