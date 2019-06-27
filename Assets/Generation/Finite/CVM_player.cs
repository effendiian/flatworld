using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
	/// <summary> (Depreciated) add/remove blocks under cursor from chunk. </summary>
	[RequireComponent(typeof(Camera))]
	public class CVM_player : MonoBehaviour {
		public CombinedVoxelMesh chunk;
		public KeyCode add, remove;
		public bool allowHold = false;

		Camera cam;

		void Start() => cam = GetComponent<Camera>();

		void SetTargetVoxel(BlockType ty, bool adjacent = false) {
			Ray ray;
			if (Cursor.visible)
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			else
				ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

			if (Physics.Raycast(ray, out RaycastHit hit)) {
				Vector3 p = hit.point + hit.normal * (adjacent ? 0.5f : -0.5f);

				Vector3Int xyz = chunk.WorldToXYZ(p);
				if (xyz.x < 0 || xyz.y < 0 || xyz.z < 0 || xyz.x >= chunk.size.x || xyz.y >= chunk.size.y || xyz.z >= chunk.size.z)
					return;

				chunk.voxels[chunk.XYZtoIndex(xyz)].ty = ty;
				chunk.Regenerate();
			}
		}

		void Update() {
			if (allowHold ? Input.anyKey : Input.anyKeyDown) {
				if (Input.GetKey(add))
					SetTargetVoxel(BlockType.Stone, adjacent: true);
				else if (Input.GetKey(remove))
					SetTargetVoxel(BlockType.Air);
			}
		}
	}
}