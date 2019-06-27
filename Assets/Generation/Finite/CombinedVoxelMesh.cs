using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace CombinedVoxelMesh {
	#region Data Types
	/// <summary> Block byte id </summary>
	public enum BlockType : byte { Air = 0, Grass, Dirt, Stone, Bedrock }

	[Serializable]
	public struct Voxel {
		public BlockType ty;

		public Voxel(BlockType ty) => this.ty = ty;
	}

	/// <summary> Describes block type layer of a flat chunk. </summary>
	[Serializable]
	public struct FlatLayer {
		/// <summary> Block to generate in this layer </summary>
		public BlockType type;
		/// <summary> Height of block layer in blocks </summary>
		public int height;
	}
	#endregion

	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class CombinedVoxelMesh : MonoBehaviour {
		#region Public/Inspector
		/// <summary> Dimensions of chunk in blocks. </summary>
		public Vector3Int size = Vector3Int.one;
		/// <summary> 1D array of voxels for chunk. You can use conversion functions to calculate a voxel's index. </summary>
		[HideInInspector]
		public Voxel[] voxels;

		public FlatLayer[] layers;
		/// <summary> Cube mesh used to convert boxes to a chunk mesh. </summary>
		public Mesh cubeMesh;
		/// <summary> Gameobject used hold all chunk colliders </summary>
		public GameObject colliderPrefab;
		/// <summary> Hide colliders in inspector/scene view </summary>
		public bool hideColliders = true;

		void OnValidate() => UpdateSize();
		#endregion

		/// <summary> size.x </summary>
		int sx;
		/// <summary> size.x * size.z </summary>
		int sxz;
		/// <summary> Calculate height based on provided layers. </summary>
		void UpdateSize() {
			size.y = 0;
			foreach (FlatLayer l in layers)
				size.y += l.height;
			sx = size.x;
			sxz = size.x * size.z;
		}

		#region Hot Compiling
		//public void OnDisable() => Clear();
		//void OnEnable() {
		//if (Time.frameCount != 0) Start();
		//}
		#endregion

		void Start() {
			UpdateSize();
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

			FillVoxels();
			//InitColliders();
			StartCoroutine(InitCollidersCoro());
			InitMesh();
			Regenerate();
		}

		#region Voxels
		/// <summary> Populate voxels with terrain data. </summary>
		public void FillVoxels() {
			//FillFlat(voxels, size, layers);
			FillPerlin(new Vector3(1f, 0.5f, 1f));
		}

		/// <summary> Populate voxels with flat terrain using given layers. </summary>
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
		/// <summary> Populate voxels using perlin noise terrain at given position and scale. </summary>
		public static void FillPerlin(Voxel[] voxels, Vector2Int pos, Vector3Int size, Vector3 scale) {
			const int mirrOff = 0;
			int pX = pos.x - mirrOff, pY = pos.y - mirrOff;

			int sx = size.x, sxz = sx * size.z;
			float hsy = size.y / 2;

			float scx = 1f / sx * scale.x;
			float scz = 1f / size.z * scale.z;
			float scy = size.y * scale.y;

			for (int z = 0; z < size.z; z++) {
				for (int x = 0; x < size.x; x++) {
					int h = Mathf.RoundToInt(Mathf.PerlinNoise((pX + x) * scx, (pY + z) * scz) * scy + hsy);
					for (int y = 0; y < size.y; y++) {
						BlockType ty;
						if (y == 0) ty = BlockType.Bedrock;
						else if (y < h * 0.66f) ty = BlockType.Stone;
						else if (y < h) ty = BlockType.Dirt;
						else if (y == h) ty = BlockType.Grass;
						else ty = BlockType.Air;

						voxels[XYZtoIndex(x, y, z, sx, sxz)].ty = ty;
					}
				}
			}
		}
		/// <summary> Populate voxels using perlin noise using current position and size. </summary>
		public void FillPerlin(Vector3 scale) => FillPerlin(voxels, new Vector2Int((int)transform.position.x, (int)transform.position.z), size, scale);
		#endregion

		/// <summary> Regenerate mesh and colliders. Used after voxel(s) is changed. </summary>
		public void Regenerate() {
			UpdateMesh();
			if (collidersInited) UpdateColliders();
			else StartCoroutine(InitCollidersWait());// if colliders aren't ready, wait
		}

		#region Mesh
		#region Mesh Data
		MeshFilter MF;
		MeshRenderer MR;
		/// <summary> Chunk mesh </summary>
		Mesh msh;

		/// <summary> Vertex positions </summary>
		List<Vector3> verts;
		/// <summary> Vertex normals </summary>
		List<Vector3> norms;
		/// <summary> Vertex 2nd UV (block id) </summary>
		List<Vector2> uv2;
		/// <summary> Triangles (3 vertex indices per triangle) </summary>
		List<int> tris;

		static Vector3[] cubeVerts, cubeNorm;
		static int[] cubeTris;
		static int cubeVertC, cubeTriC;
		//static int[] offsets;
		static readonly Vector3 zero = Vector3.zero;
		#endregion

		/// <summary> Init variables and allocate space for mesh. </summary>
		public void InitMesh() {
			MF = GetComponent<MeshFilter>();
			MR = GetComponent<MeshRenderer>();

			msh = new Mesh();
			msh.name = "Combined Voxel Mesh";
			msh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;// allow up to 4,294,967,295 vertices
			msh.MarkDynamic();
			MF.mesh = msh;

			/*offsets = new[] {
				XYZtoIndex(-1, 0, 0), XYZtoIndex(1, 0, 0),
				XYZtoIndex(0, -1, 0), XYZtoIndex(0, 1, 0),
				XYZtoIndex(0, 0, -1), XYZtoIndex(0, 0, 1)
			};*/
			
			// Copy cube mesh data
			cubeVerts = cubeMesh.vertices;
			cubeNorm = cubeMesh.normals;
			cubeTris = cubeMesh.triangles;

			Profiler.BeginSample("Init Arrays");
			cubeVertC = cubeVerts.Length;
			cubeTriC = cubeTris.Length;
			int halfC = voxels.Length / 2;
			int vertC = halfC * cubeVertC;

			verts = new List<Vector3>(vertC);
			norms = new List<Vector3>(vertC);
			uv2 = new List<Vector2>(vertC);
			tris = new List<int>(halfC * cubeTriC);

			uvArr = new Vector2[cubeVertC];
			vertArr = new Vector3[cubeVertC];
			Profiler.EndSample();
		}

		/// <summary> Temporary array for copying uv data. </summary>
		Vector2[] uvArr;
		/// <summary> Temporary array for copying vertex data. </summary>
		Vector3[] vertArr;
		/// <summary> Generate mesh from voxels. </summary>
		void UpdateMesh() {
			// Clear mesh data to be overwritten
			verts.Clear();
			norms.Clear();
			uv2.Clear();
			tris.Clear();
			msh.Clear();

			Profiler.BeginSample("Fill Mesh Data");
			InitFill();

			int i_box = 0;
			for (int i = 0; i < voxels.Length; i++) {// For each voxel
				Voxel v = voxels[i];
				BlockType ty = v.ty;
				if (filled[i] || ty == BlockType.Air) continue;// Skip air or boxed voxels

				ExpandBox(i, out int dimX, out int dimY, out int dimZ, out Vector3 c_p);// Get box, using block type boundaries

				Vector2 uv_id = new Vector2((byte)ty, 0);// custom UV, holds block type for shader
				for (int j = 0; j < cubeVertC; j++) {// Scale and reposition cube to take place of the box
					Vector3 p = cubeVerts[j];
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
				norms.AddRange(cubeNorm);

				int ind_off = i_box * cubeVertC;// vertex index offset to match cube just created
				for (int j = 0; j < cubeTriC; j++)// Add cube triangle vertex indices
					tris.Add(ind_off + cubeTris[j]);

				i_box++;// mesh box counter
			}
			Profiler.EndSample();

			// Update mesh with data
			msh.SetVertices(verts);
			msh.SetNormals(norms);
			msh.SetUVs(1, uv2);
			msh.SetTriangles(tris, 0);
		}
		#endregion

		#region Colliders
		/// <summary> Collider pool. </summary>
		BoxCollider[] colliders;
		/// <summary> Active collider count </summary>
		int colliderC = 0;
		/// <summary> Create colliders (depreciated, too slow to do in one frame, InitCollidersCoro instead). </summary>
		void InitColliders() {
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

		/// <summary> Minimum number of colliders ready </summary>
		bool collidersInited = false;
		/// <summary> Spawn colliders over time, to be used in UpdateColliders. </summary>
		IEnumerator InitCollidersCoro() {
			colliderC = voxels.Length / 2;
			colliders = new BoxCollider[colliderC];

			// Collider container GameObject
			GameObject o = Instantiate(colliderPrefab, transform.position, Quaternion.identity, transform);
			if (hideColliders) o.hideFlags = HideFlags.HideInHierarchy;


			// (jank) Run Box Expansion algo to get minimum number of colliders needed

			InitFill();

			int colC = 0;
			for (int v_i = 0; v_i < voxels.Length; v_i++) {
				if (filled[v_i] || voxels[v_i].ty == BlockType.Air) continue;

				ExpandBox(v_i, out int dimX, out int dimY, out int dimZ, out Vector3 p, false);
				colC++;
			}


			int printInt = colliders.Length / 10;
			for (int i = 0; i < colliders.Length; i++) {// Create colliders (1 per frame)
				BoxCollider bc = o.AddComponent<BoxCollider>();
				bc.enabled = false;
				colliders[i] = bc;// Add collider to pool
				if (i == colC) {
					collidersInited = true;
					o.SetActive(true);
				}
				if (i > 0 && i % printInt == 0) print($"{i * 100 / colliders.Length}%");
				yield return null;
			}

			o.SetActive(true);
			collidersInited = true;
		}

		/// <summary> Wait for minimum amount of colliders to be ready. </summary>
		IEnumerator InitCollidersWait() {
			yield return new WaitUntil(() => collidersInited);// Wait for minimum needed colliders
			print($"{name} Colliders Inited");
			UpdateColliders();
		}

		/// <summary> Regenerate colliders </summary>
		void UpdateColliders() {
			InitFill();

			int i_col = 0;
			for (int v_i = 0; v_i < voxels.Length; v_i++) {
				if (filled[v_i] || voxels[v_i].ty == BlockType.Air) continue;

				// Generate collider box, ignoring block type boundaries (less colliders)
				ExpandBox(v_i, out int dimX, out int dimY, out int dimZ, out Vector3 p, false);

				// Fit collider to box
				BoxCollider bc = colliders[i_col++];
				bc.size = new Vector3(dimX, dimY, dimZ);
				bc.center = p;
				bc.enabled = true;
			}

			for (int i = i_col; i < colliderC; i++) {// Disable unused colliders
				if (colliders[i] == null) break;
				colliders[i].enabled = false;
			}
			colliderC = i_col;// Set active collider count
		}
		#endregion

		#region Box Expansion
		/// <summary> Bit array used for box expansion to determine if voxel needs a box. </summary>
		bool[] filled;
		/// <summary> Reset filled array to false </summary>
		void InitFill() {
			for (int i = 0; i < filled.Length; i++) filled[i] = false;
		}
		/// <summary> Convert voxels to a box by expanding in all directions until an obstacle is hit. Fills "filled" array to avoid reboxing a voxel. </summary>
		/// <param name="i"> Starting voxel index </param>
		/// <param name="dimX"> Box X dimension </param>
		/// <param name="dimY"> Box Y dimension </param>
		/// <param name="dimZ"> Box Z dimension </param>
		/// <param name="p"> Box center position </param>
		/// <param name="onType"> Use block type boundaries </param>
		void ExpandBox(int i, out int dimX, out int dimY, out int dimZ, out Vector3 p, bool onType = true) {
			IndexToXYZ(i, out int pX, out int pY, out int pZ);
			// Dimensions default to distance to chunk size boundaries
			dimX = size.x - pX;
			dimY = size.y - pY;
			dimZ = size.z - pZ;
			int x = 0, z = 0;
			BlockType ty = voxels[i].ty;

			// Expand box in X direction until obstancle: box X size.
			// Then expand box in Z direction until obstacle: box Z size.
			// Then expand box in Y direction until obstacle: box Y size.
			for (int y = pY; y < pY + dimY; y++) {
				for (z = pZ; z < pZ + dimZ; z++) {
					for (x = pX; x < pX + dimX; x++) {
						i = XYZtoIndex(x, y, z);
						if ((onType ? voxels[i].ty != ty : voxels[i].ty == BlockType.Air) || filled[i]) {// Air or block type boundary: reached obstacle
							if (y == pY && z == pZ) dimX = x - pX;// X limit found
							else if (y == pY) dimZ = z - pZ;// Z limit found
							else {// Y limit found
								dimY = y - pY;
								z = pZ + dimZ;
							}
							break;
						}
					}
				}
			}

			// Mark voxels as filled to avoid refillling them
			for (int y = pY; y < pY + dimY; y++)
				for (z = pZ; z < pZ + dimZ; z++)
					for (x = pX; x < pX + dimX; x++)
						filled[XYZtoIndex(x, y, z)] = true;
			// Box center position
			p.x = pX + (dimX - 1) * 0.5f;
			p.y = pY + (dimY - 1) * 0.5f;
			p.z = pZ + (dimZ - 1) * 0.5f;
		}
		#endregion

		#region Conversions
		/// <summary> Voxel array index to chunk XYZ. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IndexToXYZ(int i, out int x, out int y, out int z) {
			x = i % size.x;
			y = i / (size.x * size.z);
			z = (i / size.x) % size.z;
		}

		/// <summary> Voxel array index to chunk XYZ vector. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3Int IndexToXYZ(int i) {
			IndexToXYZ(i, out int x, out int y, out int z);
			return new Vector3Int(x, y, z);
		}


		/// <summary> Chunk XYZ to voxel array index. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int XYZtoIndex(int x, int y, int z, int sx, int sxz) => x + (z * sx) + (y * sxz);

		/// <summary> Chunk XYZ to voxel array index. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int XYZtoIndex(int x, int y, int z) => x + (z * sx) + (y * sxz);

		/// <summary> Chunk XYZ vector to voxel array index. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int XYZtoIndex(Vector3Int xyz) => XYZtoIndex(xyz.x, xyz.y, xyz.z);


		/// <summary> Chunk XYZ vector to world position. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 XYZtoWorld(Vector3Int xtz) => transform.TransformPoint(xtz);

		/// <summary> World position to chunk XYZ. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3Int WorldToXYZ(Vector3 world) => Vector3Int.RoundToInt(transform.InverseTransformPoint(world));
		#endregion
	}
}