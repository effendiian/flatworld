using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
	public class CVM_chunkGen : MonoBehaviour {
		public Vector2Int size = Vector2Int.one;
		public GameObject chunkPrefab;

		[HideInInspector]
		public CombinedVoxelMesh[] chunks;

		int csx, csz;

		void Start() {
			chunks = new CombinedVoxelMesh[size.x * size.y];

			CombinedVoxelMesh CVM = chunkPrefab.GetComponent<CombinedVoxelMesh>();
			Vector3Int chunkSize = CVM.size;

			csx = chunkSize.x;
			csz = chunkSize.z;

			int x, z;
			for (int i = 0; i < chunks.Length; i++) {
				IndexToXZ(i, out x, out z);
				chunks[i] = Instantiate(chunkPrefab, new Vector3(x * chunkSize.x, 0f, z * chunkSize.z), Quaternion.identity, transform).GetComponent<CombinedVoxelMesh>();
			}
		}

		public int XZtoIndex(int x, int z) => x + z * size.x;
		public void IndexToXZ(int i, out int x, out int z) {
			x = i % size.x;
			z = (i / size.x) % size.y;
		}

		public CombinedVoxelMesh WorldToChunk(Vector3 world) => XZtoChunk(Mathf.RoundToInt(world.x) / csx, Mathf.RoundToInt(world.z) / csz);
		public CombinedVoxelMesh XZtoChunk(int x, int z) => chunks[XZtoIndex(x, z)];
	}
}