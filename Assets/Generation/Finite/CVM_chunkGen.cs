using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
	/// <summary> Generate Finite Chunks </summary>
	public class CVM_chunkGen : MonoBehaviour {
		/// <summary> Size of world in chunks </summary>
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
			
			StartCoroutine(GenChunks());
		}

		/// <summary> Generate Chunk every frame </summary>
		IEnumerator GenChunks() {
			for (int i = 0; i < chunks.Length; i++) {// Instantiate Chunks and add to array
				IndexToXZ(i, out int x, out int z);
				chunkPrefab.name = $"Chunk {i + 1}";
				GameObject o = Instantiate(chunkPrefab, new Vector3(x * csx, 0f, z * csz), Quaternion.identity, transform);
				print($"{o.name} Generated");
				chunks[i] = o.GetComponent<CombinedVoxelMesh>();
				yield return null;
			}
		}

		/// <summary> Chunk coordinate to array index. </summary>
		public int XZtoIndex(int x, int z) => x + z * size.x;
		/// <summary> Array index to chunk coordinate. </summary>
		public void IndexToXZ(int i, out int x, out int z) {
			x = i % size.x;
			z = (i / size.x) % size.y;
		}

		/// <summary> Get chunk from world position. </summary>
		public CombinedVoxelMesh WorldToChunk(Vector3 world) => XZtoChunk(Mathf.RoundToInt(world.x) / csx, Mathf.RoundToInt(world.z) / csz);
		/// <summary> Get chunk from chunk coordinate. </summary>
		public CombinedVoxelMesh XZtoChunk(int x, int z) => chunks[XZtoIndex(x, z)];
	}
}