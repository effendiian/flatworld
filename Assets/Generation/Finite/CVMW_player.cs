using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
	/// <summary> Add/remove blocks under custom from voxel world. </summary>
	public class CVMW_player : MonoBehaviour {
		public CVM_chunkGen world;
		public CVM_InfChunkGen world2;
		public KeyCode add, remove;
		public bool allowHold = false;

		Camera cam;

		void Start() => cam = GetComponent<Camera>();

		/// <summary> Set block under cursor </summary>
		/// <param name="adjacent"> Use block adjacent to surface (adding a block) </param>
		void SetTargetVoxel(BlockType ty, bool adjacent = false) {
			Ray ray;
			if (Cursor.visible)
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			else
				ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

			if (Physics.Raycast(ray, out RaycastHit hit)) {
				Vector3 p = hit.point + hit.normal * (adjacent ? 0.5f : -0.5f);
				CombinedVoxelMesh chunk = (world2 == null) ? world.WorldToChunk(p) : world2.WorldToChunk(p);

				Vector3Int xyz = chunk.WorldToXYZ(p);
				if (xyz.x < 0 || xyz.y < 0 || xyz.z < 0 || xyz.x >= chunk.size.x || xyz.y >= chunk.size.y || xyz.z >= chunk.size.z)
					return;

				chunk.voxels[chunk.XYZtoIndex(xyz)].ty = ty;
				chunk.Regenerate();
			}
		}
		
		void Update() {
			if (allowHold ? Input.anyKey : Input.anyKeyDown) {
				if (Input.GetKey(add))// Add block
					SetTargetVoxel(BlockType.Stone, adjacent: true);
				else if (Input.GetKey(remove))// Remove block
					SetTargetVoxel(BlockType.Air);
			}
		}
	}
}