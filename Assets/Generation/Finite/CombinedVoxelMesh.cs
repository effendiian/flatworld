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

		MeshFilter MF;
		MeshRenderer MR;
		Mesh msh;

		int sx, sxz;
		public int solids;

		void Start() {
			MF = GetComponent<MeshFilter>();
			MR = GetComponent<MeshRenderer>();

			msh = new Mesh();
			msh.name = "Combined Voxel Mesh";
			msh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			msh.MarkDynamic();
			MF.mesh = msh;

			ValidateSize();
			Generate();
		}

		void Clear() {
			voxels = null;
			if (colliders != null) {
				for (int i = 0; i < colliders.Length; i++)
					Destroy(colliders[i]);
				colliders = null;
			}
		}
		void Generate() {
			//if (voxels != null || voxels.Length != 0) return;

			voxels = new Voxel[size.x * size.y * size.z];

			int wh = size.x * size.z, i_v = 0;
			solids = 0;
			for (int i_lay = 0; i_lay < layers.Length; i_lay++) {
				FlatLayer lay = layers[i_lay];
				for (int j = 0; j < wh * lay.height; j++) {
					voxels[i_v++] = new Voxel(lay.type);
					if (lay.type != BlockType.Air) solids++;
				}
			}

			GenerateMesh();
		}
		void GenerateMeshOld() {
			List<CombineInstance> instances = new List<CombineInstance>(voxels.Length);

			Vector3Int[] sides = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };

			for (int i = 0; i < voxels.Length; i++) {
				Voxel v = voxels[i];
				if (v.id == BlockType.Air) continue;

				Vector3Int p = IndexToXYZ(i);

				#region InternalCulling
				/*Profiler.BeginSample("Internal Culling");
		  bool bordersAir = false;
		  foreach (Vector3Int off in sides) {
				Vector3Int xyz = p + off;
				if (xyz.x < 0 || xyz.x >= size.x || xyz.y < 0 || xyz.y >= size.y || xyz.z < 0 || xyz.z >= size.z ||
						  voxels[XYZtoIndex(xyz)].id == BlockType.Air) {
					 bordersAir = true;
					 break;
				}
		  }
		  if (!bordersAir) continue;
		  Profiler.EndSample();*/
				#endregion

				CombineInstance inst = new CombineInstance {
					mesh = baseMesh,
					transform = Matrix4x4.TRS(XYZtoWorld(p), Quaternion.identity, Vector3.one)
				};
				instances.Add(inst);
			}

			msh.CombineMeshes(instances.ToArray(), true, true);

			MF.mesh = msh;

			GenerateColliders();
		}

		Vector3[] baseVerts, baseNorm, verts, norms;
		Vector2[] baseUVs, uv2;//uv
		int[] baseTris, tris;
		int baseVertC, baseTriC;

		public void GenerateMesh() {
			Profiler.BeginSample("Copy Base");
			baseVerts = baseMesh.vertices;
			baseNorm = baseMesh.normals;
			baseTris = baseMesh.triangles;
			baseUVs = baseMesh.uv;
			Profiler.EndSample();

			baseVertC = baseVerts.Length;
			baseTriC = baseTris.Length;

			Profiler.BeginSample("Init Arrays");
			int voxelC = voxels.Length;
			int vertC = voxelC * baseVertC;
			//int vertC = solids * baseVertC;

			verts = new Vector3[vertC];
			norms = new Vector3[vertC];
			//uv = new Vector2[vertC];
			uv2 = new Vector2[vertC];
			tris = new int[voxels.Length * baseTriC];

			Profiler.EndSample();

			UpdateMesh();
		}
		public void UpdateMesh() {

			Profiler.BeginSample("Fill Mesh Data");
			int i_vert = 0, i_tri = 0;// i_cube = 0
			for (int i = 0; i < voxels.Length; i++) {
				Voxel v = voxels[i];
				if (v.id == BlockType.Air) {
					for (int j = 0; j < baseVertC; j++, i_vert++)
						verts[i_vert] = Vector3.zero;
					i_tri += baseTriC;
					continue;
				}

				Vector3 p = IndexToXYZ(i);

				for (int j = 0; j < baseVertC; j++, i_vert++) {
					verts[i_vert] = p + baseVerts[j];
					norms[i_vert] = baseNorm[j];
					//uv[i_vert] = baseUVs[j];
					uv2[i_vert] = new Vector2((byte)v.id, 0f);
				}

				int tri_off = i * baseVertC;
				for (int j = 0; j < baseTriC; j++)
					tris[i_tri++] = tri_off + baseTris[j];
			}
			Profiler.EndSample();

			Profiler.BeginSample("Send Mesh");
			msh.vertices = verts;
			msh.triangles = tris;
			msh.normals = norms;
			//msh.uv = uv;
			msh.uv2 = uv2;
			Profiler.EndSample();

			GenerateColliders();
		}

		public void OnDisable() => Clear();
		void OnEnable() {
			if (Time.frameCount != 0) Start();
		}

		//[SerializeField, HideInInspector]
		GameObject[] colliders;
		void GenerateColliders() {
			if (colliders == null || colliders.Length == 0) {
				colliders = new GameObject[voxels.Length];

				for (int i = 0; i < voxels.Length; i++) {
					GameObject o = Instantiate(colliderPrefab, XYZtoWorld(IndexToXYZ(i)), Quaternion.identity, transform);
					if (hideColliders) o.hideFlags = HideFlags.HideInHierarchy;
					colliders[i] = o;
				}
			}

			for (int i = 0; i < voxels.Length; i++)
				colliders[i].SetActive(voxels[i].id != BlockType.Air);
		}

		#region Conversions
		public Vector3Int IndexToXYZ(int i) => new Vector3Int(i % size.x, i / (size.x * size.z), (i / size.x) % size.z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int XYZtoIndex(Vector3Int xyz) => xyz.x + (xyz.z * sx) + (xyz.y * sxz);

		public Vector3 XYZtoWorld(Vector3Int xtz) => transform.TransformPoint(xtz);
		public Vector3Int WorldToXYZ(Vector3 world) {
			Vector3 v = transform.InverseTransformPoint(world);
			return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
		}
		#endregion
	}
}