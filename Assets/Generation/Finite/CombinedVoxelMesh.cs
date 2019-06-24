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

		public void OnDisable() => Clear();
		void OnEnable() {
			if (Time.frameCount != 0) Start();
		}


		MeshFilter MF;
		MeshRenderer MR;
		Mesh msh;

		int sx, sxz;
		public int solids;
		List<Vector3> verts, norms;
		List<Vector2> uv2;
		List<int> tris;

		static Vector3[] baseVerts, baseNorm;
		//static Vector2[] baseUVs;
		static int[] baseTris;
		static int baseVertC, baseTriC;
		static int[] offsets;
		static Vector3 zero = Vector3.zero;

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
				for (int i = 0; i < colliders.Length; i++)
					Destroy(colliders[i]);
				colliders = null;
			}
		}
		void Generate() {
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

			GenerateColliders();
			GenerateMesh();
			UpdateMesh();
		}

		public void GenerateMesh() {
			Profiler.BeginSample("Copy Base");
			baseVerts = baseMesh.vertices;
			baseNorm = baseMesh.normals;
			baseTris = baseMesh.triangles;
			//baseUVs = baseMesh.uv;
			Profiler.EndSample();

			Profiler.BeginSample("Init Arrays");
			baseVertC = baseVerts.Length;
			baseTriC = baseTris.Length;
			int vertC = voxels.Length * baseVertC;

			/*verts = new Vector3[vertC];
			norms = new Vector3[vertC];
			uv2 = new Vector2[vertC];
			tris = new int[voxels.Length * baseTriC];*/
			verts = new List<Vector3>(vertC);
			norms = new List<Vector3>(vertC);
			uv2 = new List<Vector2>(vertC);
			tris = new List<int>(voxels.Length * baseTriC);

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
			/*int i_vert = 0, i_tri = 0;
			for (int i = 0; i < voxels.Length; i++) {
				Voxel v = voxels[i];

				Profiler.BeginSample("Culling");
				bool skip = v.id == BlockType.Air;
				if (!skip) {
					skip = true;
					for (int j = 0; j < 6; j++) {
						try {
							if (voxels[i + offsets[j]].id == BlockType.Air) {
								skip = false;
								break;
							}
						} catch { }
					}
				}
				Profiler.EndSample();
				if (skip) {
					for (int j = 0; j < baseVertC; j++, i_vert++)
						verts[i_vert] = zero;
					i_tri += baseTriC;
					continue;
				}

				Vector3 p = IndexToXYZ(i);
				for (int j = 0; j < baseVertC; j++, i_vert++) {
					verts[i_vert] = p + baseVerts[j];
					norms[i_vert] = baseNorm[j];
					uv2[i_vert] = new Vector2((byte)v.id, 0);
				}

				int tri_off = i * baseVertC;
				for (int j = 0; j < baseTriC; j++)
					tris[i_tri++] = tri_off + baseTris[j];
			}*/

			for (int i = 0; i < boxes; i++) {
				BoxCollider c = colliders[i];
				Vector3 cent = c.center;
				Vector3 sc = c.size;

				Voxel v = voxels[XYZtoIndex(VecToRounded(cent))];
				Vector2 uv_id = new Vector2((byte)v.id, 0);

				for (int j = 0; j < baseVertC; j++) {
					Vector3 p = baseVerts[j];
					p.x *= sc.x;
					p.y *= sc.y;
					p.z *= sc.z;
					p.x += cent.x;
					p.y += cent.y;
					p.z += cent.z;
					vertArr[j] = p;
					uvArr[j] = uv_id;
				}
				verts.AddRange(vertArr);
				uv2.AddRange(uvArr);
				norms.AddRange(baseNorm);

				int tri_off = i * baseVertC;
				for (int j = 0; j < baseTriC; j++)
					tris.Add(tri_off + baseTris[j]);
			}
			Profiler.EndSample();

			Profiler.BeginSample("Send Mesh");
			/*msh.vertices = verts;
			msh.triangles = tris;
			msh.normals = norms;
			msh.uv2 = uv2;*/
			msh.Clear();
			msh.SetVertices(verts);
			msh.SetNormals(norms);
			msh.SetUVs(1, uv2);
			msh.SetTriangles(tris, 0);
			Profiler.EndSample();
		}

		BoxCollider[] colliders;
		int boxes = 0;
		void GenerateColliders() {
			if (colliders == null || colliders.Length == 0) {
				colliders = new BoxCollider[voxels.Length / 2];

				Vector3 p = transform.position;
				for (int i = 0; i < colliders.Length; i++) {
					GameObject o = Instantiate(colliderPrefab, p, Quaternion.identity, transform);
					if (hideColliders) o.hideFlags = HideFlags.HideInHierarchy;
					colliders[i] = o.GetComponent<BoxCollider>();
				}
			}
		}

		void UpdateColliders() {
			int i_col = 0, h = 0;
			Vector3Int p = Vector3Int.zero;
			Vector3 s = new Vector3(1f, 0f, 1f), c_p = Vector3.zero;

			BlockType last = (BlockType)(255);

			for (int x = 0; x < size.x; x++) {
				p.x = x;
				for (int z = 0; z < size.z; z++) {
					p.z = z;
					for (int y = 0; y < size.y; y++) {
						p.y = y;
						Voxel v = voxels[XYZtoIndex(p)];

						if (v.id != last) {
							if (h > 0) {
								BoxCollider bc = colliders[i_col++];

								c_p.x = x;
								c_p.z = z;
								c_p.y = y - (h * 0.5f) - 0.5f;
								bc.center = c_p;

								s.y = h;
								bc.size = s;

								bc.gameObject.SetActive(true);
								h = 0;
							}

							last = v.id;
						}

						if (v.id == BlockType.Air) h = 0;
						else h++;
					}
				}
			}

			for (; i_col < boxes; i_col++) colliders[i_col].gameObject.SetActive(false);
			boxes = i_col;
		}

		/*IEnumerator UpdateColliders() {
            Transform camT = Camera.main.transform;

            while (true) {
                Vector3 camPos = camT.position;
                float r = 8f;
                Vector3Int xyz = WorldToXYZ(camPos);
                for (int i = 0; i < colliders.Length; i++) {
                    GameObject c = colliders[i];
                    bool newState = voxels[i].id != BlockType.Air && Vector3.Distance(c.transform.position, camPos) <= r;
                    if (c.activeSelf != newState) c.SetActive(newState);
                    if (i % 16 == 0) yield return null;
                }
                yield return null;
            }
        }*/

		#region Conversions
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3Int IndexToXYZ(int i) => new Vector3Int(i % size.x, i / (size.x * size.z), (i / size.x) % size.z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int XYZtoIndex(Vector3Int xyz) => xyz.x + (xyz.z * sx) + (xyz.y * sxz);

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