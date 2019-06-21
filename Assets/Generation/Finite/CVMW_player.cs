using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
	public class CVMW_player : MonoBehaviour {
		public CVM_chunkGen world;

		public KeyCode add, remove;
		public bool allowHold = false;
		Camera cam;

		void Start() {
			cam = GetComponent<Camera>();
		}

		void SetTargetVoxel(BlockType ty, bool adjacent = false) {
			Ray ray;
			if (Cursor.visible)
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			else
				ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

			RaycastHit hit;
			if (Physics.Raycast(ray, out hit)) {
				Vector3 p = hit.point + hit.normal * (adjacent ? 0.5f : -0.5f);
				CombinedVoxelMesh chunk = world.WorldToChunk(p);
				chunk.voxels[chunk.XYZtoIndex(chunk.WorldToXYZ(p))].id = ty;

				if (ty == BlockType.Air) chunk.solids--;
				else chunk.solids++;

				chunk.UpdateMesh();
			}
		}

		Vector3 hitPos, voxelPos;

		void Update() {
			if (allowHold ? Input.anyKey : Input.anyKeyDown) {
				if (Input.GetKey(add)) {
					SetTargetVoxel(BlockType.Stone, adjacent: true);
				}
				else if (Input.GetKey(remove)) {
					SetTargetVoxel(BlockType.Air);
				}
			}
		}
	}
}