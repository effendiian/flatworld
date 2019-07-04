using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedVoxelMesh {
	[CreateAssetMenu(fileName = "Chunk", menuName = "Voxel/Chunk Settings", order = 1)]
	public class ChunkSettings : ScriptableObject {
		/// <summary> Dimensions of chunk in blocks. </summary>
		public Vector3Int size = Vector3Int.one;
		/// <summary> 1D array of voxels for chunk. You can use conversion functions to calculate a voxel's index. </summary>

		public BlockLayer[] layers;
		/// <summary> Cube mesh used to convert boxes to a chunk mesh. </summary>
		public Mesh cubeMesh;
		/// <summary> Gameobject used hold all chunk colliders </summary>
		public GameObject colliderPrefab;
		/// <summary> Hide colliders in inspector/scene view </summary>
		public bool hideColliders = true;

		public Vector3 noiseScale = new Vector3(1f, 0.5f, 1f);
		public int noiseLayers = 1;
		public float noiseHeightScale = 0.5f;
		public AnimationCurve noiseCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f, 0f, 0f));

		public event Action Validated;

		void OnValidate() {
			noiseLayers = Mathf.Clamp(noiseLayers, 1, 16);

			if (noiseScale.x == 0 || noiseScale.y == 0 || noiseScale.z == 0) return;

			Validated?.Invoke();

			if (CombinedVoxelMesh.instances.Count > 0) {
				CombinedVoxelMesh inst = CombinedVoxelMesh.instances[0];
				if (coro != null) {
					inst.StopCoroutine(coro);
					coro = null;
				}
				coro = inst.StartCoroutine(InspectorRegen());
			}
		}

		static Coroutine coro;
		IEnumerator InspectorRegen() {
			List<CombinedVoxelMesh> insts = CombinedVoxelMesh.instances;
			Debug.Log($"Regenerate {insts.Count} Chunks");

			for (int i = 0; i < insts.Count; i++) {
				CombinedVoxelMesh chunk = insts[i];

				for (int j = 0; j < chunk.voxels.Length; j++)
					chunk.voxels[j].ty = BlockType.Air;

				chunk.FillVoxels();
				yield return null;
				chunk.Regenerate();
			}

			Debug.Log($"{insts.Count} Chunks Regenerated");
		}
	}
}