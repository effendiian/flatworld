using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace CombinedVoxelMesh {
	public struct CVM_Chunk {
		public Vector2Int pos, center;
		public readonly CombinedVoxelMesh CVM;

		public CVM_Chunk(Vector2Int pos, Vector2Int center, CombinedVoxelMesh cVM) {
			this.pos = pos;
			this.center = center;
			CVM = cVM;
		}
	}

	/// <summary> Generate Infinite Chunks </summary>
	public class CVM_InfChunkGen : MonoBehaviour {
		public int radius = 1;
		/// <summary> Size of world in chunks </summary>
		//public Vector2Int size = Vector2Int.one;
		public GameObject chunkPrefab, cam;

		public CVM_Chunk[] chunks;
		Dictionary<Vector2Int, CombinedVoxelMesh> instMap;
		Dictionary<Vector2Int, Voxel[]> voxelMap;

		int csx, csy, csz;
		bool generated = false;

		Vector2Int viewPos = new Vector2Int(int.MinValue, int.MinValue);
		Stopwatch sw = new Stopwatch();

		void Start() => StartCoroutine(GenChunks());

		Vector2Int GetViewpos() => WorldToXZ(cam.transform.position + new Vector3(csx / 2, 0f, csz / 2));

		static readonly float bias = -0.15f;
		public static int RadCount(float r) {
			int c = 0;
			for (float y = 0.5f - r, rsq = (r + bias) * (r + bias); y < r; y++)
				for (float x = 0.5f - r; x < r; x++)
					if (x * x + y * y <= rsq) c++;
			return c;
		}

		IEnumerator GenChunks() {
			chunks = new CVM_Chunk[RadCount(radius)];
			instMap = new Dictionary<Vector2Int, CombinedVoxelMesh>(chunks.Length);
			voxelMap = new Dictionary<Vector2Int, Voxel[]>(chunks.Length);

			Vector3Int chunkSize = chunkPrefab.GetComponent<CombinedVoxelMesh>().settings.size;
			csx = chunkSize.x;
			csy = chunkSize.y;
			csz = chunkSize.z;

			viewPos = GetViewpos();

			posStack = new Stack<Vector2Int>(chunks.Length);

			int i = 0;
			for (float y = 0.5f - radius, rsq = (radius + bias) * (radius + bias); y < radius; y++) {
				for (float x = 0.5f - radius; x < radius; x++) {
					if (x * x + y * y <= rsq) {
						Vector2Int xz = viewPos + new Vector2Int((int)(x - 0.5f), (int)(y - 0.5f));
						GameObject o = Instantiate(chunkPrefab, new Vector3(xz.x * chunkSize.x, 0f, xz.y * chunkSize.z), Quaternion.identity, transform);

						CombinedVoxelMesh CVM = o.GetComponent<CombinedVoxelMesh>();
						chunks[i++] = new CVM_Chunk(xz, viewPos, CVM);
						instMap[xz] = CVM;
						voxelMap[xz] = CVM.voxels;

						o.name = $"Chunk {i}";
						if (i % 256 == 0) print($"{i} Chunks Generated");
						yield return null;
					}
				}
			}

			print($"{i} Chunks Generated");
			generated = true;
		}

		void Update() {
			Vector2Int pos = GetViewpos();
			if (generated && pos != viewPos) {
				coro = StartCoroutine(UpdateChunks(pos));
			}
		}

		Coroutine coro;
		Stack<Vector2Int> posStack;

		IEnumerator UpdateChunks(Vector2Int vpos, int msLimit = 5) {
			viewPos = vpos;

			if (coro != null) {
				StopCoroutine(coro);
				coro = null;
			}

			sw.Restart();

			for (float y = 0.5f - radius, rsq = (radius + bias) * (radius + bias); y < radius; y++) {
				for (float x = 0.5f - radius; x < radius; x++) {
					if (x * x + y * y <= rsq) {
						Vector2Int p = vpos + new Vector2Int((int)(x - 0.5f), (int)(y - 0.5f));
						if (!instMap.ContainsKey(p))
							posStack.Push(p);
					}
				}
			}

			for (int i = 0; i < chunks.Length; i++) {
				CVM_Chunk c = chunks[i];
				Vector2 dif = c.pos + new Vector2(0.5f, 0.5f) - vpos;

				if (dif.x * dif.x + dif.y * dif.y > (radius + bias) * (radius + bias)) {
					Vector2Int nPos = posStack.Pop();

					if (instMap.ContainsKey(nPos)) print("taken!");

					CombinedVoxelMesh cvm = c.CVM;
					instMap.Remove(c.pos);
					instMap[nPos] = cvm;
					chunks[i].pos = nPos;

					cvm.gameObject.transform.position = new Vector3(nPos.x * csx, 0f, nPos.y * csz);

					if (voxelMap.ContainsKey(nPos))
						cvm.voxels = voxelMap[nPos];
					else {
						cvm.voxels = voxelMap[nPos] = new Voxel[cvm.voxels.Length];
						cvm.FillVoxels();
					}
					cvm.Regenerate();

					if (sw.ElapsedMilliseconds >= msLimit) {
						sw.Restart();
						yield return null;
					}
				}

				chunks[i].center = vpos;
			}

			posStack.Clear();

			/*for (int i = 0; i < chunks.Length; i++) {
				CVM_Chunk c = chunks[i];
				Vector2 dif = c.pos + new Vector2(0.5f, 0.5f) - vpos;

				if (dif.x * dif.x + dif.y * dif.y > (radius + bias) * (radius + bias)) {
					Vector2Int nPos = c.center + vpos - c.pos - new Vector2Int(1, 1);

					if (instMap.ContainsKey(nPos)) print("taken!");

					CombinedVoxelMesh cvm = c.CVM;
					instMap.Remove(c.pos);
					instMap[nPos] = cvm;
					chunks[i].pos = nPos;

					cvm.gameObject.transform.position = new Vector3(nPos.x * csx, 0f, nPos.y * csz);

					if (voxelMap.ContainsKey(nPos))
						cvm.voxels = voxelMap[nPos];
					else {
						cvm.voxels = voxelMap[nPos] = new Voxel[cvm.voxels.Length];
						cvm.FillVoxels();
					}
					cvm.Regenerate();

					if (sw.ElapsedMilliseconds >= msLimit) {
						sw.Restart();
						yield return null;
					}
				}

				chunks[i].center = vpos;
			}*/
		}

		#region Conversions
		public void WorldToXZ(Vector3 world, out int x, out int z) {
			x = Mathf.FloorToInt(Mathf.Round(world.x) / csx);
			z = Mathf.FloorToInt(Mathf.Round(world.z) / csz);
		}
		public Vector2Int WorldToXZ(Vector3 world) {
			WorldToXZ(world, out int x, out int z);
			return new Vector2Int(x, z);
		}
		//public int XZtoIndex(int x, int z) => x + z * size.x;
		/*public void IndexToXZ(int i, out int x, out int z) {
			x = i % size.x;
			z = (i / size.x) % size.y;
		}*/

		public CombinedVoxelMesh WorldToChunk(Vector3 world) {
			//int x, z;
			//WorldToXZ(world, out x, out z);
			//return XZtoChunk(x, z);
			return instMap[WorldToXZ(world)];
		}
		//public CombinedVoxelMesh XZtoChunk(int x, int z) => chunks[XZtoIndex(x, z)].CVM;
		#endregion
	}
}