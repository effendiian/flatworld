using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace CombinedVoxelMesh {
	public enum BlockType : byte { Air = 0, Grass, Dirt, Stone, Bedrock }

	[Serializable]
	public struct Voxel {
		public BlockType id;

		public Voxel(BlockType ty) => this.id = ty;
	}

	[Serializable]
	public struct FlatLayer {
		public BlockType type;
		public int height;
	}

	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class CombinedVoxelMesh : MonoBehaviour {
		public Vector3Int size = Vector3Int.one;
		[HideInInspector]
		public Voxel[] voxels;

		public FlatLayer[] layers;
		public Mesh baseMesh;
		public GameObject colliderPrefab;
		public bool hideColliders = true;

		void OnValidate() => ValidateSize();
		void ValidateSize() {
			size.y = 0;
			foreach (FlatLayer l in layers)
				size.y += l.height;
			sx = size.x;
			sxz = size.x * size.z;
		}

		//public void OnDisable() => Clear();
		//void OnEnable() {
			//if (Time.frameCount != 0) Start();
		//}


		MeshFilter MF;
		MeshRenderer MR;
		Mesh msh;

		int sx, sxz;
		public int solids;
		List<Vector3> verts, norms;
		List<Vector2> uv2;
		List<int> tris;
		bool[] filled;

		static Vector3[] baseVerts, baseNorm;
		static int[] baseTris;
		static int baseVertC, baseTriC;
		static int[] offsets;
		static readonly Vector3 zero = Vector3.zero;

		void Start() {
			MF = GetComponent<MeshFilter>();
			MR = GetComponent<MeshRenderer>();

			msh = new Mesh();
			msh.name = "Combined Voxel Mesh";
			msh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			msh.MarkDynamic();
			MF.mesh = msh;

			ValidateSize();

			offsets = new[] {
				XYZtoIndex(new Vector3Int(-1, 0, 0)), XYZtoIndex(new Vector3Int(1, 0, 0)),
				XYZtoIndex(new Vector3Int(0, -1, 0)), XYZtoIndex(new Vector3Int(0, 1, 0)),
				XYZtoIndex(new Vector3Int(0, 0, -1)), XYZtoIndex(new Vector3Int(0, 0, 1))
			};

			Generate();
		}

		void Clear() {
			voxels = null;
			if (colliders != null) {
				//for (int i = 0; i < colliders.Length; i++)
				//Destroy(colliders[i]);
				colliders = null;
			}
		}
		void Generate() {
			voxels = new Voxel[size.x * size.y * size.z];
			filled = new bool[voxels.Length];

			FillFlat(voxels, size, layers);

			GenerateColliders();
			GenerateMesh();
			UpdateMesh();
			//UpdateCollidersNew();
		}

		public static void FillFlat(Voxel[] voxels, Vector3Int size, FlatLayer[] layers) {
			int wh = size.x * size.z;

			for (int i_v = 0, i_lay = 0; i_lay < layers.Length; i_lay++) {
				FlatLayer lay = layers[i_lay];
				for (int j = 0; j < wh * lay.height; j++) {
					voxels[i_v++] = new Voxel(lay.type);
					//if (lay.type != BlockType.Air) solids++;
				}
			}
		}

		public void GenerateMesh() {
			Profiler.BeginSample("Copy Base");
			baseVerts = baseMesh.vertices;
			baseNorm = baseMesh.normals;
			baseTris = baseMesh.triangles;
			Profiler.EndSample();

			Profiler.BeginSample("Init Arrays");
			baseVertC = baseVerts.Length;
			baseTriC = baseTris.Length;
			int halfC = voxels.Length / 2;
			int vertC = halfC * baseVertC;

			verts = new List<Vector3>(vertC);
			norms = new List<Vector3>(vertC);
			uv2 = new List<Vector2>(vertC);
			tris = new List<int>(halfC * baseTriC);

			uvArr = new Vector2[baseVertC];
			vertArr = new Vector3[baseVertC];
			Profiler.EndSample();
		}

		Vector2[] uvArr;
		Vector3[] vertArr;

		public void UpdateMesh() {
			UpdateColliders();

			verts.Clear();
			norms.Clear();
			uv2.Clear();
			tris.Clear();

			Profiler.BeginSample("Fill Mesh Data");

			InitFill();
			int i_box = 0;
			for (int i = 0; i < voxels.Length; i++) {
				Voxel v = voxels[i];
				BlockType ty = v.id;
				if (filled[i] || ty == BlockType.Air) continue;

				int dimX, dimY, dimZ;
				Vector3 c_p;
				ExpandBox(i, out dimX, out dimY, out dimZ, out c_p);

				Vector2 uv_id = new Vector2((byte)ty, 0);

				for (int j = 0; j < baseVertC; j++) {
					Vector3 p = baseVerts[j];
					p.x *= dimX;
					p.y *= dimY;
					p.z *= dimZ;
					p.x += c_p.x;
					p.y += c_p.y;
					p.z += c_p.z;
					vertArr[j] = p;
					uvArr[j] = uv_id;
				}
				verts.AddRange(vertArr);
				uv2.AddRange(uvArr);
				norms.AddRange(baseNorm);

				int tri_off = i_box * baseVertC;
				for (int j = 0; j < baseTriC; j++)
					tris.Add(tri_off + baseTris[j]);

				i_box++;
			}
			Profiler.EndSample();

			Profiler.BeginSample("Send Mesh");
			msh.Clear();
			msh.SetVertices(verts);
			msh.SetNormals(norms);
			msh.SetUVs(1, uv2);
			msh.SetTriangles(tris, 0);
			Profiler.EndSample();
		}

		BoxCollider[] colliders;
		int colliderC = 0;
		void GenerateColliders() {
			if (colliders == null || colliders.Length == 0) {
				colliderC = voxels.Length / 2;
				colliders = new BoxCollider[colliderC];

				/*for (int i = 0; i < boxes; i++) {
					GameObject o = Instantiate(colliderPrefab, transform.position, Quaternion.identity, transform);
					if (hideColliders) o.hideFlags = HideFlags.HideInHierarchy;
					colliders[i] = o.AddComponent<BoxCollider>();
					o.SetActive(true);
				}*/

				GameObject o = Instantiate(colliderPrefab, transform.position, Quaternion.identity, transform);
				if (hideColliders) o.hideFlags = HideFlags.HideInHierarchy;

				for (int i = 0; i < colliders.Length; i++)
					colliders[i] = o.AddComponent<BoxCollider>();

				o.SetActive(true);
			}
		}

		void InitFill() {
			for (int i = 0; i < filled.Length; i++) filled[i] = false;
		}
		void ExpandBox(int i, out int dimX, out int dimY, out int dimZ, out Vector3 p, bool onType = true) {
			int pX, pY, pZ;
			IndexToXYZ(i, out pX, out pY, out pZ);
			dimX = size.x - pX;
			dimY = size.y - pY;
			dimZ = size.z - pZ;
			int x = 0, z = 0;
			BlockType ty = voxels[i].id;

			#region OLD
			//for (int y = 0; y < dimY; y++, i += sxz - x * z) {
			//for (z = 0; z < dimZ; z++, i += sx - x) {
			/*for (int y = 0; y < dimY; y++, i += pi.x * pi.z) {
				for (z = 0; z < dimZ; z++, i += pi.x) {
					for (x = 0; x < dimX; x++, i++) {
						if (voxels[i].id != ty || filled[i]) {
							if (y == 0 && z == 0 && x < dimX) dimX = x;
							else if (y == 0 && z < dimZ) dimZ = z;
							else if (y < dimY) {
								dimY = y;
								z = dimZ;
							}
							break;
						}
					}
				}
			}

			i = v_i;
			for (int y = 0; y < dimY; y++, i += sxz - x * z)
				for (z = 0; z < dimZ; z++, i += sx - x)
					for (x = 0; x < dimX; x++, i++)
						filled[i] = true;*/
			#endregion

			for (int y = pY; y < pY + dimY; y++) {
				for (z = pZ; z < pZ + dimZ; z++) {
					for (x = pX; x < pX + dimX; x++) {
						i = XYZtoIndex(x, y, z);
						if ((onType ? voxels[i].id != ty : voxels[i].id == BlockType.Air) || filled[i]) {
							if (y == pY && z == pZ) dimX = x - pX;
							else if (y == pY) dimZ = z - pZ;
							else {
								dimY = y - pY;
								z = pZ + dimZ;
							}
							break;
						}
					}
				}
			}

			for (int y = pY; y < pY + dimY; y++)
				for (z = pZ; z < pZ + dimZ; z++)
					for (x = pX; x < pX + dimX; x++)
						filled[XYZtoIndex(x, y, z)] = true;

			p.x = pX + (dimX - 1) * 0.5f;
			p.y = pY + (dimY - 1) * 0.5f;
			p.z = pZ + (dimZ - 1) * 0.5f;
		}

		void UpdateColliders() {
			InitFill();

			int i_col = 0;
			for (int v_i = 0; v_i < voxels.Length; v_i++) {
				if (filled[v_i] || voxels[v_i].id == BlockType.Air) continue;

				int dimX, dimY, dimZ;
				Vector3 p;
				ExpandBox(v_i, out dimX, out dimY, out dimZ, out p, false);

				BoxCollider bc = colliders[i_col++];
				bc.size = new Vector3(dimX, dimY, dimZ);
				bc.center = p;
				bc.enabled = true;
			}

			for (int i = i_col; i < colliderC; i++) colliders[i].enabled = false;
			colliderC = i_col;
		}

		#region Conversions
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IndexToXYZ(int i, out int x, out int y, out int z) {
			x = i % size.x;
			y = i / (size.x * size.z);
			z = (i / size.x) % size.z;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3Int IndexToXYZ(int i) {
			int x, y, z;
			IndexToXYZ(i, out x, out y, out z);
			return new Vector3Int(x, y, z);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int XYZtoIndex(int x, int y, int z) => x + (z * sx) + (y * sxz);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int XYZtoIndex(Vector3Int xyz) => XYZtoIndex(xyz.x, xyz.y, xyz.z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 XYZtoWorld(Vector3Int xtz) => transform.TransformPoint(xtz);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3Int WorldToXYZ(Vector3 world) {
			Vector3 v = transform.InverseTransformPoint(world);
			return VecToRounded(v);
		}

		static Vector3Int VecToRounded(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
		#endregion
	}
}