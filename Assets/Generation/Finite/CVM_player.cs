using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
	/// <summary> (Depreciated) add/remove blocks under cursor from chunk. </summary>
	[RequireComponent(typeof(Camera))]
	public class CVM_player : MonoBehaviour {
		public CombinedVoxelMesh chunk;
		public KeyCode add = KeyCode.Alpha1, remove = KeyCode.Alpha2, replace = KeyCode.Alpha3;
		public bool allowHold = false, destroyBedrock = false;

		Camera cam;
		BlockType setType;

		void Start() {
			cam = GetComponent<Camera>();
			setType = BlockType.Stone;
		}

		void SetTargetVoxel(BlockType ty, bool adjacent = false) {
			Ray ray;
			if (Cursor.visible)
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			else
				ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

			if (Physics.Raycast(ray, out RaycastHit hit)) {
				Vector3 p = hit.point + hit.normal * (adjacent ? 0.5f : -0.5f);

				Vector3Int xyz = chunk.WorldToXYZ(p), size = chunk.settings.size;
				if (xyz.x < 0 || xyz.y < 0 || xyz.z < 0 || xyz.x >= size.x || xyz.y >= size.y || xyz.z >= size.z)
					return;
				if (!destroyBedrock && chunk.voxels[chunk.XYZtoIndex(xyz)].ty == BlockType.Bedrock)
					return;

				chunk.voxels[chunk.XYZtoIndex(xyz)].ty = ty;
				chunk.Regenerate();
			}
		}

		void Update() {
			if (allowHold ? Input.anyKey : Input.anyKeyDown) {
				if (Input.GetKey(add))// Add block
					SetTargetVoxel(setType, adjacent: true);
				else if (Input.GetKey(remove))// Remove block
					SetTargetVoxel(BlockType.Air);
				else if (Input.GetKey(replace))
					SetTargetVoxel(setType);
			}
		}
	}
}