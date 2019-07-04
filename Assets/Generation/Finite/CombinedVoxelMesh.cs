using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

	/// <summary> Describes block type layer. </summary>
	[Serializable]
	public struct BlockLayer {
		/// <summary> Block to generate in this layer </summary>
		public BlockType type;
		/// <summary> Height of block layer in blocks </summary>
		public int height;
		[Range(0f, 1f)]
		public float percentage;

		public BlockLayer(BlockType type) {
			this.type = type;
			this.height = 1;
			this.percentage = -1f;
		}
		public BlockLayer(BlockType type, int height, float percentage) {
			this.type = type;
			this.height = height;
			this.percentage = percentage;
		}
	}
	#endregion

	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class CombinedVoxelMesh : MonoBehaviour {
		#region Public/Inspector
		public ChunkSettings settings;
				
		public static List<CombinedVoxelMesh> instances = new List<CombinedVoxelMesh>();
		#endregion

		[HideInInspector]
		public Voxel[] voxels;

		// size.x, size.x * size.z
		int sx, sxz;
		void UpdateSize() {
			sx = settings.size.x;
			sxz = settings.size.x * settings.size.z;
		}

		#region Hot Compiling
		//public void OnDisable() => Clear();
		//void OnEnable() {
		//if (Time.frameCount != 0) Start();
		//}
		#endregion

		void Awake() {
			if (name.EndsWith("(Clone)")) name = name.Remove(name.IndexOf("(Clone)"));
			
			instances.Add(this);

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
			voxels = new Voxel[settings.size.x * settings.size.y * settings.size.z];
			filled = new bool[voxels.Length];

			FillVoxels();
			InitColliders();
			InitMesh();
			Regenerate();
		}

		#region Voxels
		/// <summary> Populate voxels with terrain data. </summary>
		public void FillVoxels() {
			//FillFlat(voxels, size, layers);
			//Stopwatch sw = new Stopwatch();
			//sw.Start();
			FillPerlin(settings.noiseScale);
			//print(sw.Elapsed.TotalSeconds);
		}

		static float FractalPerlin(float x, float y, int layers, float scale) {
			float z = 0f, scXY = 1f / scale;

			for (int i = 0; i < layers; i++) {
				float sc = Mathf.Pow(scXY, i), scH = Mathf.Pow(scale, i);
				z += Mathf.PerlinNoise(x * sc, y * sc) * scH - (i == 0 ? 0f : scH * 0.5f);
			}

			return z;
		}

		/// <summary> Populate voxels with flat terrain using given layers. </summary>
		public static void FillFlat(Voxel[] voxels, Vector3Int size, BlockLayer[] layers) {
			int wh = size.x * size.z;

			for (int i_v = 0, i_lay = 0; i_lay < layers.Length; i_lay++) {
				BlockLayer lay = layers[i_lay];
				for (int j = 0; j < wh * lay.height; j++) {
					voxels[i_v++] = new Voxel(lay.type);
					//if (lay.type != BlockType.Air) solids++;
				}
			}
		}
		/// <summary> Populate voxels using perlin noise terrain at given position and scale. </summary>
		public static void FillPerlin(Voxel[] voxels, Vector2Int pos, Vector3Int size, Vector3 scale, int noiseLayers, float noiseScale, AnimationCurve noiseCurve, BlockLayer[] layers) {
			const int mirrOff = 1024;
			int pX = pos.x - mirrOff, pY = pos.y - mirrOff;
			int sx = size.x, sy = size.y, sz = size.z, sxz = sx * size.z;

			float scx = 1f / sx * scale.x;
			float scz = 1f / sz * scale.z;
			float scy = sy * scale.y;
			float hbais = (sy - scy) / 2 - 1;

			for (int z = 0; z < sz; z++) {
				for (int x = 0; x < sx; x++) {
					float noise = FractalPerlin((pX + x) * scx, (pY + z) * scz, noiseLayers, noiseScale);
					int h = Mathf.Clamp(Mathf.RoundToInt(noiseCurve.Evaluate(noise) * scy + hbais), 0, sy - 1);
					/*for (int y = 0; y < sy; y++) {
						BlockType ty;
						if (y == 0) ty = BlockType.Bedrock;
						else if (y < h * 0.66f) ty = BlockType.Stone;
						else if (y < h) ty = BlockType.Dirt;
						else if (y == h) ty = BlockType.Grass;
						else ty = BlockType.Air;

						voxels[XYZtoIndex(x, y, z, sx, sxz)].ty = ty;
					}*/

					int y = 0;

					/*voxels[XYZtoIndex(x, y++, z, sx, sxz)].ty = BlockType.Bedrock;

					for (; y < h * 0.66f;)
						voxels[XYZtoIndex(x, y++, z, sx, sxz)].ty = BlockType.Stone;
					for (; y < h;)
						voxels[XYZtoIndex(x, y++, z, sx, sxz)].ty = BlockType.Dirt;
					for (; y == h;)
						voxels[XYZtoIndex(x, y++, z, sx, sxz)].ty = BlockType.Grass;
					for (; y < sy;)
						voxels[XYZtoIndex(x, y++, z, sx, sxz)].ty = BlockType.Air;*/

					for (int l = 0; l < layers.Length; l++) {
						BlockLayer lay = layers[l];
						float lh = (lay.height == 0 ? h * lay.percentage : y + lay.height);
						for (; y < lh;)
							voxels[XYZtoIndex(x, y++, z, sx, sxz)].ty = lay.type;
					}
				}
			}
		}
		/// <summary> Populate voxels using perlin noise using current position and size. </summary>
		public void FillPerlin(Vector3 scale) => FillPerlin(voxels, new Vector2Int((int)transform.position.x, (int)transform.position.z), settings.size, scale, settings.noiseLayers, settings.noiseHeightScale, settings.noiseCurve, settings.layers);
		#endregion

		/// <summary> Regenerate mesh and colliders. Used after voxel(s) is changed. </summary>
		public void Regenerate() {
			UpdateMesh();
			UpdateColliders();
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
		static int[] offsets;

		// Temporary arrays for copying mesh data
		Vector2[] uvBuff;
		Vector3[] vertBuff;
		int[] triBuff;
		#endregion

		/// <summary> Init variables and allocate space for mesh. </summary>
		public void InitMesh() {
			MF = GetComponent<MeshFilter>();
			MR = GetComponent<MeshRenderer>();

			msh = new Mesh {
				name = $"{name} Voxel Mesh",
				indexFormat = UnityEngine.Rendering.IndexFormat.UInt32// allow up to 4,294,967,295 vertices
			};
			msh.MarkDynamic();
			MF.mesh = msh;

			// Copy cube mesh data
			cubeVerts = settings.cubeMesh.vertices;
			cubeNorm = settings.cubeMesh.normals;
			cubeTris = settings.cubeMesh.triangles;

			Profiler.BeginSample("Init Arrays");
			cubeVertC = cubeVerts.Length;
			cubeTriC = cubeTris.Length;
			int halfC = voxels.Length / 32;
			int vertC = halfC * cubeVertC;

			verts = new List<Vector3>(vertC);
			norms = new List<Vector3>(vertC);
			uv2 = new List<Vector2>(vertC);
			tris = new List<int>(halfC * cubeTriC);

			uvBuff = new Vector2[cubeVertC];
			vertBuff = new Vector3[cubeVertC];
			triBuff = new int[cubeTriC];

			offsets = new[] { 0,
				XYZtoIndex(-1, 0, 0), XYZtoIndex(1, 0, 0),
				XYZtoIndex(0, -1, 0), XYZtoIndex(0, 1, 0),
				XYZtoIndex(0, 0, -1), XYZtoIndex(0, 0, 1)
			};
			Profiler.EndSample();
		}


		/// <summary> Generate mesh from voxels. </summary>
		void UpdateMesh() {
			// Clear mesh data to be overwritten
			verts.Clear();
			norms.Clear();
			uv2.Clear();
			tris.Clear();
			msh.Clear();

			Profiler.BeginSample("Fill Mesh Data");
			//InitFill();

			for (int i = 0; i < voxels.Length; i++) {
				bool culled = true;
				for (int j = 0; j < offsets.Length; j++) {
					IndexToXYZ(i, out int x, out int y, out int z);

					if (x == 0 || x == settings.size.x - 1 || y == 0 || y == settings.size.y - 1 || z == 0 || z == settings.size.z - 1 ||
							voxels[i + offsets[j]].ty == BlockType.Air) {
						culled = false;
						break;
					}
				}
				filled[i] = culled;
			}

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
					vertBuff[j] = p;
					uvBuff[j] = uv_id;
				}
				verts.AddRange(vertBuff);
				uv2.AddRange(uvBuff);
				norms.AddRange(cubeNorm);

				int ind_off = i_box * cubeVertC;// vertex index offset to match cube just created
				for (int j = 0; j < cubeTriC; j++)// Add cube triangle vertex indices
					triBuff[j] = ind_off + cubeTris[j];
				tris.AddRange(triBuff);

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
		static Stack<BoxCollider> pool;
		List<BoxCollider> colliders;
		int colliderC = 0;

		GameObject colliderHolder;
		/*public GameObject ColliderHolder {
			get {
				if (colliderHolder == null) {
					colliderHolder = Instantiate(colliderPrefab, Vector3.zero, Quaternion.identity);
					if (hideColliders) colliderHolder.hideFlags = HideFlags.HideInHierarchy;
					colliderHolder.SetActive(true);
				}
				return colliderHolder;
			}
		}*/


		void InitColliders() {
			colliders = new List<BoxCollider>(voxels.Length / 64);
			if (pool == null) pool = new Stack<BoxCollider>(voxels.Length / 64);

			colliderHolder = Instantiate(settings.colliderPrefab, Vector3.zero, Quaternion.identity);
			colliderHolder.name = $"{name} Colliders";
			if (settings.hideColliders) colliderHolder.hideFlags = HideFlags.HideInHierarchy;
			colliderHolder.SetActive(true);
		}

		/// <summary> Regenerate colliders </summary>
		void UpdateColliders() {
			colliderHolder.SetActive(false);
			InitFill();

			Vector3 tp = transform.position;

			int i_col = 0;
			for (int v_i = 0; v_i < voxels.Length; v_i++) {
				if (filled[v_i] || voxels[v_i].ty == BlockType.Air) continue;

				// Generate collider box, ignoring block type boundaries (less colliders)
				ExpandBox(v_i, out int dimX, out int dimY, out int dimZ, out Vector3 p, false);

				// Fit collider to box
				BoxCollider bc;
				if (i_col == colliders.Count) {
					if (pool.Count > 0)
						bc = pool.Pop();
					else {
						bc = colliderHolder.AddComponent<BoxCollider>();
						bc.hideFlags = HideFlags.HideInInspector;
					}
					colliders.Add(bc);
				}
				else bc = colliders[i_col];
				i_col++;
				bc.size = new Vector3(dimX, dimY, dimZ);
				bc.center = tp + p;
				if (!bc.enabled) bc.enabled = true;
			}

			for (int i = i_col; i < colliderC; i++) {// Cleanup unused colliders
				BoxCollider bc = colliders[i];
				bc.enabled = false;
				pool.Push(bc);
			}
			if (i_col < colliderC)
				colliders.RemoveRange(i_col, colliderC - i_col);
			colliderC = i_col;// Set active collider count
			colliderHolder.SetActive(true);
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
			dimX = settings.size.x - pX;
			dimY = settings.size.y - pY;
			dimZ = settings.size.z - pZ;
			int x = 0, z = 0;
			BlockType ty = voxels[i].ty;

			Profiler.BeginSample("Find Limits");
			// Expand box in X direction until obstancle: box X size.
			// Then expand box in Z direction until obstacle: box Z size.
			// Then expand box in Y direction until obstacle: box Y size.
			for (int y = pY; y < pY + dimY; y++) {
				for (z = pZ; z < pZ + dimZ; z++) {
					i = XYZtoIndex(pX, y, z);
					for (x = pX; x < pX + dimX; x++, i++) {
						if ((onType ? voxels[i].ty != ty : voxels[i].ty == BlockType.Air) || filled[i]) {// Air or block type boundary: reached obstacle
							if (y == pY && z == pZ) dimX = x - pX;// X limit found
							else if (y == pY) dimZ = z - pZ;// Z limit found
							else {// Y limit found
								dimY = y - pY;
								z = pZ + dimZ;// End loop
							}
							break;
						}
					}
				}
			}
			Profiler.EndSample();

			Profiler.BeginSample("Mark Filled");
			// Mark voxels as filled to avoid refillling them
			for (int y = pY; y < pY + dimY; y++) {
				for (z = pZ; z < pZ + dimZ; z++) {
					i = XYZtoIndex(pX, y, z);
					for (x = pX; x < pX + dimX; x++, i++)
						filled[i] = true;
				}
			}
			Profiler.EndSample();

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
			x = i % settings.size.x;
			y = i / (settings.size.x * settings.size.z);
			z = (i / settings.size.x) % settings.size.z;
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