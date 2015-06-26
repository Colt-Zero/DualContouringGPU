using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public enum OctreeNodeType
{
	Node_None,
	Node_Internal,
	Node_Psuedo,
	Node_Leaf
}

public class OctreeDrawInfo : IDisposable
{
	public OctreeDrawInfo()
	{
		index = -1;
		corners = 0;
	}

	public void Dispose()
	{
		Dispose(true);
	}
	
	protected virtual void Dispose(bool disposeManagedResources)
	{

	}
	
	~OctreeDrawInfo()
	{
		Dispose(false);
	}

	public int index;
	public int corners;
	public Vector3 position;
	public Vector3 averageNormal;
}

public class OctreeNode : IComparable<OctreeNode>, IDisposable
{
	public OctreeNode()
	{
		type = OctreeNodeType.Node_None;
		min = Vector3.zero;
		size = 0;
		drawInfo = null;

		children = new OctreeNode[8];

		for (int i = 0; i < 8; i++)
		{
			children[i] = null;
		}
	}

	public OctreeNode(OctreeNodeType _type)
	{
		type = _type;
		min = Vector3.zero;
		size = 0;
		drawInfo = null;

		children = new OctreeNode[8];

		for (int i = 0; i < 8; i++)
		{
			children[i] = null;
		}
	}

	public OctreeNode(OctreeNode rhs)
	{
		type = rhs.type;
		min = rhs.min;
		size = rhs.size;
		drawInfo = rhs.drawInfo;
		if (rhs.children != null)
		{
			for (int i = 0; i < 8; i++)
				children[i] = new OctreeNode(rhs.children[i]);
		}
	}

	public void Dispose()
	{
		Dispose(true);
		System.GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposeManagedResources)
	{
		if (disposeManagedResources)
		{
			if (children != null)
			{
				for (int i = 0; i < 8; i++)
					if (children[i] != null)
						children[i].Dispose();
			}

			if (drawInfo != null)
				drawInfo.Dispose();
		}
	}

	~OctreeNode()
	{
		Dispose(false);
	}

	public int CompareTo(OctreeNode compareNode)
	{
		return 1;
	}

	public OctreeNodeType type;
	public Vector3 min;
	public int size;
	public OctreeNode[] children;
	public OctreeDrawInfo drawInfo;
}

public class Octree
{
	static int[] permutations = new int[512];

	public static void InitPermutations(int seed)
	{
		UnityEngine.Random.seed = seed;
		for (int i = 0; i < 256; i++)
			permutations[i] = (int) (256 * (UnityEngine.Random.Range(0.0f, 10000.0f) / 10000.0f));

		for (int i = 256; i < 512; i++)
			permutations[i] = permutations[i - 256];
	}

	const int MATERIAL_AIR = 0;
	const int MATERIAL_SOLID = 1;

	const float QEF_ERROR = 1e-6f;
	const int QEF_SWEEPS = 4;

	Vector3[] CHILD_MIN_OFFSETS = new Vector3[8]
	{
		// needs to match the vertMap from Dual Contouring impl
		new Vector3(0, 0, 0),
		new Vector3(0, 0, 1),
		new Vector3(0, 1, 0),
		new Vector3(0, 1, 1),
		new Vector3(1, 0, 0),
		new Vector3(1, 0, 1),
		new Vector3(1, 1, 0),
		new Vector3(1, 1, 1)
	};

	int[,] edgevmap = new int[,]
	{
		{0,4},{1,5},{2,6},{3,7},	// x-axis 
		{0,2},{1,3},{4,6},{5,7},	// y-axis
		{0,1},{2,3},{4,5},{6,7}		// z-axis
	};

	int[] edgemask = new int[3] { 5, 3, 6 };

	int[,] vertMap = new int[,]
	{
		{0,0,0},
		{0,0,1},
		{0,1,0},
		{0,1,1},
		{1,0,0},
		{1,0,1},
		{1,1,0},
		{1,1,1}
	};

	int[,] faceMap = new int[,] {{4, 8, 5, 9}, {6, 10, 7, 11},{0, 8, 1, 10},{2, 9, 3, 11},{0, 4, 2, 6},{1, 5, 3, 7}};
	int[,] cellProcFaceMask = new int[,] {{0,4,0},{1,5,0},{2,6,0},{3,7,0},{0,2,1},{4,6,1},{1,3,1},{5,7,1},{0,1,2},{2,3,2},{4,5,2},{6,7,2}};
	int[,] cellProcEdgeMask = new int[,] {{0,1,2,3,0},{4,5,6,7,0},{0,4,1,5,1},{2,6,3,7,1},{0,2,4,6,2},{1,3,5,7,2}};

	int[,,] faceProcFaceMask = new int[,,]
	{
		{{4,0,0},{5,1,0},{6,2,0},{7,3,0}},
		{{2,0,1},{6,4,1},{3,1,1},{7,5,1}},
		{{1,0,2},{3,2,2},{5,4,2},{7,6,2}}
	};

	int[,,] faceProcEdgeMask = new int[,,]
	{
		{{1,4,0,5,1,1},{1,6,2,7,3,1},{0,4,6,0,2,2},{0,5,7,1,3,2}},
		{{0,2,3,0,1,0},{0,6,7,4,5,0},{1,2,0,6,4,2},{1,3,1,7,5,2}},
		{{1,1,0,3,2,0},{1,5,4,7,6,0},{0,1,5,0,4,1},{0,3,7,2,6,1}}
	};

	int[,,] edgeProcEdgeMask = new int[,,]
	{
		{{3,2,1,0,0},{7,6,5,4,0}},
		{{5,1,4,0,1},{7,3,6,2,1}},
		{{6,4,2,0,2},{7,5,3,1,2}},
	};

	int[,] processEdgeMask = new int[,] {{3,2,1,0},{7,5,6,4},{11,10,9,8}};

	private void GenerateVertexIndices(OctreeNode node, List<Vector3> vertices, List<Vector3> normals, int nodeSize)
	{
		if (node == null)
			return;

		if (node.size > nodeSize)
		{
			if (node.type != OctreeNodeType.Node_Leaf)
			{
				for (int i = 0; i < 8; i++)
				{
					GenerateVertexIndices(node.children[i], vertices, normals, nodeSize);
				}
			}
		}

		if (node.type != OctreeNodeType.Node_Internal)
		{
			OctreeDrawInfo d = node.drawInfo;
			if (d == null)
			{
				Debug.LogError("Error! Could not add vertex!");
				Application.Quit();
			}

			d.index = vertices.Count;
			vertices.Add(new Vector3(d.position.x, d.position.y, d.position.z));
			normals.Add(new Vector3(d.averageNormal.x, d.averageNormal.y, d.averageNormal.z));
		}
	}

	private void ContourProcessEdge(OctreeNode[] node, int dir, List<int> indices)
	{
		int minSize = 1000000;		// arbitrary big number
		int minIndex = 0;
		int[] indexes = new int[4] { -1, -1, -1, -1 };
		bool flip = false;
		bool[] signChange = new bool[4] { false, false, false, false };

		for (int i = 0; i < 4; i++)
		{
			int edge = processEdgeMask[dir, i];
			int c1 = edgevmap[edge, 0];
			int c2 = edgevmap[edge, 1];

			int m1 = (node[i].drawInfo.corners >> c1) & 1;
			int m2 = (node[i].drawInfo.corners >> c2) & 1;

			if(node[i].size < minSize)
			{
				minSize = node[i].size;
				minIndex = i;
				flip = m1 != MATERIAL_AIR;
			}

			indexes[i] = node[i].drawInfo.index;

			signChange[i] =
				(m1 == MATERIAL_AIR && m2 != MATERIAL_AIR) ||
				(m1 != MATERIAL_AIR && m2 == MATERIAL_AIR);
		}

		if (signChange[minIndex])
		{
			if (!flip)
			{
				indices.Add(indexes[0]);
				indices.Add(indexes[1]);
				indices.Add(indexes[3]);

				indices.Add(indexes[0]);
				indices.Add(indexes[3]);
				indices.Add(indexes[2]);
			}
			else
			{
				indices.Add(indexes[0]);
				indices.Add(indexes[3]);
				indices.Add(indexes[1]);

				indices.Add(indexes[0]);
				indices.Add(indexes[2]);
				indices.Add(indexes[3]);
			}
		}
	}

	private void ContourEdgeProc(OctreeNode[] node, int dir, List<int> indices)
	{
		if (node[0] == null || node[1] == null || node[2] == null || node[3] == null)
			return;

		if (node[0].type != OctreeNodeType.Node_Internal &&
		    node[1].type != OctreeNodeType.Node_Internal &&
		    node[2].type != OctreeNodeType.Node_Internal &&
		    node[3].type != OctreeNodeType.Node_Internal)
		{
			ContourProcessEdge(node, dir, indices);
		}
		else
		{
			for (int i = 0; i < 2; i++)
			{
				OctreeNode[] edgeNodes = new OctreeNode[4];
				int[] c = new int[4]
				{
					edgeProcEdgeMask[dir, i, 0],
					edgeProcEdgeMask[dir, i, 1],
					edgeProcEdgeMask[dir, i, 2],
					edgeProcEdgeMask[dir, i, 3]
				};

				for (int j = 0; j < 4; j++)
				{
					if (node[j].type == OctreeNodeType.Node_Leaf || node[j].type == OctreeNodeType.Node_Psuedo)
					{
						edgeNodes[j] = node[j];
					}
					else
					{
						edgeNodes[j] = node[j].children[c[j]];
					}
				}

				ContourEdgeProc(edgeNodes, edgeProcEdgeMask[dir, i, 4], indices);
			}
		}
	}

	private void ContourFaceProc(OctreeNode[] node, int dir, List<int> indices)
	{
		if (node[0] == null || node[1] == null)
			return;

		if (node[0].type == OctreeNodeType.Node_Internal ||
		    node[1].type == OctreeNodeType.Node_Internal)
		{
			for (int i = 0; i < 4; i++)
			{
				OctreeNode[] faceNodes = new OctreeNode[2];

				int[] c = new int[2]
				{
					faceProcFaceMask[dir, i, 0],
					faceProcFaceMask[dir, i, 1]
				};

				for (int j = 0; j < 2; j++)
				{
					if (node[j].type != OctreeNodeType.Node_Internal)
					{
						faceNodes[j] = node[j];
					}
					else
					{
						faceNodes[j] = node[j].children[c[j]];
					}
				}

				ContourFaceProc(faceNodes, faceProcFaceMask[dir, i, 2], indices);
			}

			int[,] orders = new int[,]
			{
				{ 0, 0, 1, 1 },
				{ 0, 1, 0, 1 }
			};

			for (int i = 0; i < 4; i++)
			{
				OctreeNode[] edgeNodes = new OctreeNode[4];
				int[] c = new int[4]
				{
					faceProcEdgeMask[dir, i, 1],
					faceProcEdgeMask[dir, i, 2],
					faceProcEdgeMask[dir, i, 3],
					faceProcEdgeMask[dir, i, 4]
				};

				int[] order = new int[4]
				{
					orders[faceProcEdgeMask[dir, i, 0], 0],
					orders[faceProcEdgeMask[dir, i, 0], 1],
					orders[faceProcEdgeMask[dir, i, 0], 2],
					orders[faceProcEdgeMask[dir, i, 0], 3]
				};

				for (int j = 0; j < 4; j++)
				{
					if (node[order[j]].type == OctreeNodeType.Node_Leaf ||
					    node[order[j]].type == OctreeNodeType.Node_Psuedo)
					{
						edgeNodes[j] = node[order[j]];
					}
					else
					{
						edgeNodes[j] = node[order[j]].children[c[j]];
					}
				}

				ContourEdgeProc(edgeNodes, faceProcEdgeMask[dir, i, 5], indices);
			}
		}
	}

	private void ContourCellProc(OctreeNode node, List<int> indices)
	{
		if (node == null)
			return;

		if (node.type == OctreeNodeType.Node_Internal)
		{
			for (int i = 0; i < 8; i++)
			{
				ContourCellProc(node.children[i], indices);
			}

			for (int i = 0; i < 12; i++)
			{
				OctreeNode[] faceNodes = new OctreeNode[2];
				int[] c = new int[2] { cellProcFaceMask[i, 0], cellProcFaceMask[i, 1] };

				faceNodes[0] = node.children[c[0]];
				faceNodes[1] = node.children[c[1]];

				ContourFaceProc(faceNodes, cellProcFaceMask[i, 2], indices);
			}

			for (int i = 0; i < 6; i++)
			{
				OctreeNode[] edgeNodes = new OctreeNode[4];
				int[] c = new int[4]
				{
					cellProcEdgeMask[i, 0],
					cellProcEdgeMask[i, 1],
					cellProcEdgeMask[i, 2],
					cellProcEdgeMask[i, 3]
				};

				for (int j = 0; j < 4; j++)
				{
					edgeNodes[j] = node.children[c[j]];
				}

				ContourEdgeProc(edgeNodes, cellProcEdgeMask[i, 4], indices);
			}
		}
	}

	Vector3 ApproximateZeroCrossingPosition(Vector3 p0, Vector3 p1)
	{
		// approximate the zero crossing by finding the min value along the edge
		float minValue = 100000.0f;
		float t = 0.0f;
		float currentT = 0.0f;
		int steps = 8;
		float increment = 1.0f / (float) steps;
		while (currentT <= 1.0f)
		{
			Vector3 p = p0 + ((p1 - p0) * currentT);
			float density = Mathf.Abs(DensityFunctions.Density_Func(p));
			if (density < minValue)
			{
				minValue = density;
				t = currentT;
			}

			currentT += increment;
		}

		return p0 + ((p1 - p0) * t);
	}

	Vector3 CalculateSurfaceNormal(Vector3 p)
	{
		float H = 0.001f;
		float dx = DensityFunctions.Density_Func(p + new Vector3(H, 0.0f, 0.0f)) - DensityFunctions.Density_Func(p - new Vector3(H, 0.0f, 0.0f));
		float dy = DensityFunctions.Density_Func(p + new Vector3(0.0f, H, 0.0f)) - DensityFunctions.Density_Func(p - new Vector3(0.0f, H, 0.0f));
		float dz = DensityFunctions.Density_Func(p + new Vector3(0.0f, 0.0f, H)) - DensityFunctions.Density_Func(p - new Vector3(0.0f, 0.0f, H));

		return Vector3.Normalize(new Vector3(dx, dy, dz));
	}

	public struct GPUVOX
	{
		public Vector3 vertPoint;
		public Vector3 avgNormal;
		public int numPoints;
	};

	private List<OctreeNode> ComputeVoxels(ComputeShader shader, Vector3 min, int octreeSize)
	{
		float gpuStart = Time.realtimeSinceStartup;

		float[] chunkPos = new float[3] { min.x, min.y, min.z };
		shader.SetFloats("chunkPosition", chunkPos);

		shader.SetInt("resolution", octreeSize);
		shader.SetInt("octreeSize", octreeSize);

		float sqRTRC = Mathf.Sqrt(octreeSize * octreeSize * octreeSize);
		int sqRTRes = (int) sqRTRC;
		if (sqRTRC > sqRTRes)
			sqRTRes++;

		ComputeBuffer Perm = new ComputeBuffer(512, sizeof(int));
		Perm.SetData(permutations);
		
		ComputeBuffer cornCount = new ComputeBuffer(sqRTRes, sizeof(int));
		ComputeBuffer finalCount = new ComputeBuffer(1, sizeof(float));
		ComputeBuffer voxMatBuffer = new ComputeBuffer(octreeSize * octreeSize * octreeSize, sizeof(uint));
		
		float rD8 = octreeSize / 8.0f;
		int rD8I = (int) rD8;
		if (rD8 > rD8I)
			rD8I++;
		
		int kernel = shader.FindKernel("ComputeCorners");
		shader.SetBuffer(kernel, "Perm", Perm);
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.Dispatch(kernel, rD8I, rD8I, rD8I);

		/*kernel = shader.FindKernel("ComputeLength");
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.Dispatch(kernel, 1, 1, 1);*/

		kernel = shader.FindKernel("AddLength");
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "finalCount", finalCount);
		shader.Dispatch(kernel, 1, 1, 1);

		float[] voxelCount = new float[1];
		finalCount.GetData(voxelCount);
		finalCount.SetData(voxelCount);
		int count = (int) voxelCount[0];
		//Debug.Log (count);

		if (count <= 0)
		{
			voxMatBuffer.Dispose();
			cornCount.Dispose();
			finalCount.Dispose();
			Perm.Dispose();
			return null;
		}

		ComputeBuffer cornerIndexes = new ComputeBuffer(count, sizeof(uint));

		kernel = shader.FindKernel("ComputePositions");
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "cornerIndexes", cornerIndexes);
		shader.Dispatch(kernel, 1, 1, 1);

		ComputeBuffer voxBuffer = new ComputeBuffer(count, (sizeof(float) * 6) + sizeof(int));
		ComputeBuffer positionBuffer = new ComputeBuffer(count, sizeof(float) * 3);
		
		kernel = shader.FindKernel("ComputeVoxels");
		shader.SetBuffer(kernel, "Perm", Perm);
		shader.SetBuffer(kernel, "voxMins", positionBuffer);
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "finalCount", finalCount);
		shader.SetBuffer(kernel, "cornerIndexes", cornerIndexes);
		shader.SetBuffer(kernel, "voxels", voxBuffer);
		//int dispatchCount = count / 10;
		shader.Dispatch(kernel, (count / 128) + 1, 1, 1);

		List<OctreeNode> computedVoxels = new List<OctreeNode>();

		Vector3[] voxelMins = new Vector3[count];
		positionBuffer.GetData(voxelMins);
		positionBuffer.Dispose();

		uint[] voxelMaterials = new uint[count];
		cornerIndexes.GetData(voxelMaterials);
		cornerIndexes.Dispose();

		GPUVOX[] voxs = new GPUVOX[count];
		voxBuffer.GetData(voxs);
		voxBuffer.Dispose();

		voxMatBuffer.Dispose();
		cornCount.Dispose();
		finalCount.Dispose();
		Perm.Dispose();
		
		float gpuEnd = Time.realtimeSinceStartup;
		//Debug.Log ("GPU time on chunk: " + (gpuEnd - gpuStart));

		int HIGHEST_VOXEL_RES = 64;
		int voxelSize = HIGHEST_VOXEL_RES / octreeSize;


		for (int i = 0; i < count; i++)
		{
			if (voxs[i].numPoints != 0)
			{
				OctreeNode leaf = new OctreeNode();
				leaf.type = OctreeNodeType.Node_Leaf;
				leaf.size = voxelSize;
				OctreeDrawInfo drawInfo = new OctreeDrawInfo();
				drawInfo.position = voxs[i].vertPoint;
				drawInfo.averageNormal = voxs[i].avgNormal;
				drawInfo.corners = (int) voxelMaterials[i];
				leaf.drawInfo = drawInfo;
				leaf.min = voxelMins[i];
				computedVoxels.Add(leaf);
			}
		}

		//Debug.Log ("CPU Leaf generation time on chunk: " + (Time.realtimeSinceStartup - gpuEnd));
		//Debug.Log (computedVoxels.Count);
		
		return computedVoxels;
	}

	private bool ComputeVoxels(ComputeShader shader, ThreadedChunkLoader thread, Chunk chunk, Vector3 min, int octreeSize)
	{
		float gpuStart = Time.realtimeSinceStartup;

		float[] chunkPos = new float[3] { min.x, min.y, min.z };
		shader.SetFloats("chunkPosition", chunkPos);

		shader.SetInt("resolution", octreeSize);
		shader.SetInt("octreeSize", octreeSize);

		int[] zero = new int[1] { 0 };

		float sqRTRC = Mathf.Sqrt(octreeSize * octreeSize * octreeSize);
		int sqRTRes = (int) sqRTRC;
		if (sqRTRC > sqRTRes)
			sqRTRes++;

		int[] zeroSq = new int[sqRTRes];
		for (int i = 0; i < sqRTRes; i++)
			zeroSq[i] = 0;

		ComputeBuffer Perm = new ComputeBuffer(512, sizeof(int));
		Perm.SetData(permutations);

		ComputeBuffer cornCount = new ComputeBuffer(sqRTRes, sizeof(int));
		ComputeBuffer finalCount = new ComputeBuffer(1, sizeof(int));
		ComputeBuffer voxMatBuffer = new ComputeBuffer(octreeSize * octreeSize * octreeSize, sizeof(uint));
		ComputeBuffer cornerMaterials = new ComputeBuffer((octreeSize + 1) * (octreeSize + 1) * (octreeSize + 1), sizeof(uint));

		shader.SetInt("primitiveModCount", chunk.modCount);
		ComputeBuffer PrimMods = null;
		if (chunk.modCount > 0)
		{
			PrimMods = new ComputeBuffer(chunk.modCount, (sizeof(int) * 2) + (sizeof(float) * 6));
			PrimMods.SetData(chunk.primitiveMods);
		}

		int kernel = shader.FindKernel("ComputeMaterials");
		if (chunk.modCount > 0)
			shader.SetBuffer(kernel, "primitiveMods", PrimMods);
		shader.SetBuffer(kernel, "Perm", Perm);
		shader.SetBuffer(kernel, "cornerMaterials", cornerMaterials);
		shader.Dispatch(kernel, 1, 1, 1);

		kernel = shader.FindKernel("ComputeCorners");
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "cornerMaterials", cornerMaterials);
		cornCount.SetData(zeroSq);
		shader.Dispatch(kernel, 1, 1, 1);
		
		kernel = shader.FindKernel("AddLength");
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "finalCount", finalCount);
		finalCount.SetData(zero);
		shader.Dispatch(kernel, 1, 1, 1);
		
		int[] voxelCount = new int[1];
		finalCount.GetData(voxelCount);
		int count = voxelCount[0];

		if (count <= 0)
		{
			voxMatBuffer.Dispose();
			cornerMaterials.Dispose();
			cornCount.Dispose();
			finalCount.Dispose();
			Perm.Dispose();
			if (chunk.modCount > 0)
			{
				PrimMods.Dispose();
				PrimMods = null;
			}
			Perm = null;
			voxMatBuffer = null;
			cornerMaterials = null;
			cornCount = null;
			finalCount = null;
			return false;
		}

		//Debug.Log (count);
		
		ComputeBuffer cornerIndexes = new ComputeBuffer(count, sizeof(uint));
		
		kernel = shader.FindKernel("ComputePositions");
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "cornerIndexes", cornerIndexes);
		shader.Dispatch(kernel, 1, 1, 1);

		ComputeBuffer voxBuffer = new ComputeBuffer(count, (sizeof(float) * 6) + sizeof(int));
		ComputeBuffer positionBuffer = new ComputeBuffer(count, sizeof(float) * 3);
		
		kernel = shader.FindKernel("ComputeVoxels");
		if (chunk.modCount > 0)
			shader.SetBuffer(kernel, "primitiveMods", PrimMods);
		shader.SetBuffer(kernel, "Perm", Perm);
		shader.SetBuffer(kernel, "voxMins", positionBuffer);
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "finalCount", finalCount);
		shader.SetBuffer(kernel, "cornerIndexes", cornerIndexes);
		shader.SetBuffer(kernel, "voxels", voxBuffer);
		shader.Dispatch(kernel, (count / 128) + 1, 1, 1);
		
		Vector3[] voxelMins = new Vector3[count];
		positionBuffer.GetData(voxelMins);
		positionBuffer.Dispose();
		positionBuffer = null;
		
		uint[] voxelMaterials = new uint[count];
		cornerIndexes.GetData(voxelMaterials);
		cornerIndexes.Dispose();
		cornerIndexes = null;

		GPUVOX[] voxs = new GPUVOX[count];
		voxBuffer.GetData(voxs);
		voxBuffer.Dispose();
		voxBuffer = null;
		
		voxMatBuffer.Dispose();
		cornerMaterials.Dispose();
		cornCount.Dispose();
		finalCount.Dispose();
		Perm.Dispose();
		if (chunk.modCount > 0)
		{
			PrimMods.Dispose();
			PrimMods = null;
		}
		Perm = null;
		voxMatBuffer = null;
		cornerMaterials = null;
		cornCount = null;
		finalCount = null;
		
		float gpuEnd = Time.realtimeSinceStartup;
		//Debug.Log ("GPU time on chunk: " + (gpuEnd - gpuStart));

		thread.setData(this, count, voxelMins, voxelMaterials, voxs, min, octreeSize);
		thread.Start();
		
		return true;
	}

	private bool ComputeNewVoxels(ComputeShader shader, ThreadedChunkLoader thread, Chunk chunk, Vector3 min, int octreeSize)
	{
		float gpuStart = Time.realtimeSinceStartup;


		float[] chunkPos = new float[3] { min.x, min.y, min.z };
		shader.SetFloats("chunkPosition", chunkPos);
		
		shader.SetInt("resolution", octreeSize);
		shader.SetInt("octreeSize", octreeSize);

		int[] zero = new int[1] { 0 };
		
		float sqRTRC = Mathf.Sqrt(octreeSize * octreeSize * octreeSize);
		int sqRTRes = (int) sqRTRC;
		if (sqRTRC > sqRTRes)
			sqRTRes++;

		int[] zeroSq = new int[sqRTRes];
		for (int i = 0; i < sqRTRes; i++)
			zeroSq[i] = 0;

		ComputeBuffer Perm = new ComputeBuffer(512, sizeof(int));
		Perm.SetData(permutations);
		
		ComputeBuffer cornCount = new ComputeBuffer(sqRTRes, sizeof(int));
		ComputeBuffer finalCount = new ComputeBuffer(1, sizeof(int));
		ComputeBuffer voxMatBuffer = new ComputeBuffer(octreeSize * octreeSize * octreeSize, sizeof(uint));
		ComputeBuffer cornerMaterials = new ComputeBuffer((octreeSize + 1) * (octreeSize + 1) * (octreeSize + 1), sizeof(uint));

		shader.SetInt("primitiveModCount", chunk.modCount);
		ComputeBuffer PrimMods = null;
		if (chunk.modCount > 0)
		{
			PrimMods = new ComputeBuffer(chunk.modCount, (sizeof(int) * 2) + (sizeof(float) * 6));
			PrimMods.SetData(chunk.primitiveMods);
		}


		int kernel = shader.FindKernel("ComputeMaterials");
		if (chunk.modCount > 0)
			shader.SetBuffer(kernel, "primitiveMods", PrimMods);
		shader.SetBuffer(kernel, "Perm", Perm);
		shader.SetBuffer(kernel, "cornerMaterials", cornerMaterials);
		shader.Dispatch(kernel, 1, 1, 1);
		
		kernel = shader.FindKernel("ComputeCorners");
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "cornerMaterials", cornerMaterials);
		cornCount.SetData(zeroSq);
		shader.Dispatch(kernel, 1, 1, 1);
		
		kernel = shader.FindKernel("AddLength");
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "finalCount", finalCount);
		finalCount.SetData(zero);
		shader.Dispatch(kernel, 1, 1, 1);
		
		int[] voxelCount = new int[1];
		finalCount.GetData(voxelCount);
		int count = voxelCount[0];

		if (count <= 0)
		{
			voxMatBuffer.Dispose();
			cornerMaterials.Dispose();
			cornCount.Dispose();
			finalCount.Dispose();
			Perm.Dispose();
			if (chunk.modCount > 0)
			{
				PrimMods.Dispose();
				PrimMods = null;
			}
			Perm = null;
			voxMatBuffer = null;
			cornerMaterials = null;
			cornCount = null;
			finalCount = null;
			return false;
		}

		//Debug.Log (count);
		
		ComputeBuffer cornerIndexes = new ComputeBuffer(count, sizeof(uint));
		
		kernel = shader.FindKernel("ComputePositions");
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "cornerCount", cornCount);
		shader.SetBuffer(kernel, "cornerIndexes", cornerIndexes);
		shader.Dispatch(kernel, 1, 1, 1);
		
		ComputeBuffer voxBuffer = new ComputeBuffer(count, (sizeof(float) * 6) + sizeof(int));
		ComputeBuffer positionBuffer = new ComputeBuffer(count, sizeof(float) * 3);
		
		kernel = shader.FindKernel("ComputeVoxels");
		if (chunk.modCount > 0)
			shader.SetBuffer(kernel, "primitiveMods", PrimMods);
		shader.SetBuffer(kernel, "Perm", Perm);
		shader.SetBuffer(kernel, "voxMins", positionBuffer);
		shader.SetBuffer(kernel, "voxelMaterials", voxMatBuffer);
		shader.SetBuffer(kernel, "finalCount", finalCount);
		shader.SetBuffer(kernel, "cornerIndexes", cornerIndexes);
		shader.SetBuffer(kernel, "voxels", voxBuffer);
		shader.Dispatch(kernel, (count / 128) + 1, 1, 1);
		
		Vector3[] voxelMins = new Vector3[count];
		positionBuffer.GetData(voxelMins);
		positionBuffer.Dispose();
		positionBuffer = null;
		
		uint[] voxelMaterials = new uint[count];
		cornerIndexes.GetData(voxelMaterials);
		cornerIndexes.Dispose();
		cornerIndexes = null;
		
		GPUVOX[] voxs = new GPUVOX[count];
		voxBuffer.GetData(voxs);
		voxBuffer.Dispose();
		voxBuffer = null;
		
		voxMatBuffer.Dispose();
		cornerMaterials.Dispose();
		cornCount.Dispose();
		finalCount.Dispose();
		Perm.Dispose();
		if (chunk.modCount > 0)
		{
			PrimMods.Dispose();
			PrimMods = null;
		}
		Perm = null;
		voxMatBuffer = null;
		cornerMaterials = null;
		cornCount = null;
		finalCount = null;
		float gpuEnd = Time.realtimeSinceStartup;
		//Debug.Log ("GPU time on chunk: " + (gpuEnd - gpuStart));

		thread.setData(this, count, voxelMins, voxelMaterials, voxs, min, octreeSize);
		thread.setChunkForUpdate(chunk);
		thread.Start();

		return true;
	}

	/*
	public OctreeNode BuildOctree(Vector3 min, int size)
	{
		OctreeNode root = new OctreeNode();
		root.min = min;
		root.size = size;
		root.type = OctreeNodeType.Node_Internal;
		root = ConstructOctreeNodes(root);

		return root;
	}
	*/

	public OctreeNode BuildOctree(ComputeShader shader, Vector3 min, int size)
	{
		List<OctreeNode> computedVoxels = ComputeVoxels(shader, min, size);
		if (computedVoxels == null || computedVoxels.Count == 0) return null;

		float octreeGenerationStart = Time.realtimeSinceStartup;
		OctreeNode root = ConstructUpwards(computedVoxels, min, size);
		//Debug.Log ("CPU Octree generation time on chunk: " + (Time.realtimeSinceStartup - octreeGenerationStart));
		
		return root;
	}

	public bool Voxelize(ComputeShader shader, ThreadedChunkLoader thread, Chunk chunk, Vector3 min, int size)
	{
		return ComputeVoxels(shader, thread, chunk, min, size);
	}

	public bool VoxelizeUpdate(ComputeShader shader, ThreadedChunkLoader thread, Chunk chunk, Vector3 pos, int size)
	{
		return ComputeNewVoxels(shader, thread, chunk, pos, size);
	}

	public void FindDrawnVoxels(OctreeNode node, List<OctreeNode> drawn)
	{
		if (node == null)
			return;

		for (int i = 0; i < 8; i++)
		{
			FindDrawnVoxels(node.children[i], drawn);
		}

		if (node.type == OctreeNodeType.Node_Leaf || node.type == OctreeNodeType.Node_Psuedo)
		{
			drawn.Add(node);
		}
	}

	/*
	public OctreeNode Simplify(OctreeNode root, float threshold)
	{
		root = SimplifyOctree(root, threshold);
		return root;
	}
	*/

	public void DestroyOctree(OctreeNode node)
	{
		if (node == null)
			return;

		/*for (int i = 0; i < 8; i++)
		{
			DestroyOctree(node.children[i]);
		}

		if (node.drawInfo != null)
		{
			node.drawInfo = null;
		}
		*/
		node.Dispose();

		node = null;
	}

	private List<OctreeNode> ConstructParents(List<OctreeNode> children, Vector3 position, int parentSize)
	{
		Dictionary<Vector3, OctreeNode> parentsHash = new Dictionary<Vector3, OctreeNode>();
		for (int i = 0; i < children.Count; i++)
		{
			OctreeNode node = children[i];
			Vector3 parentPos = node.min - (new Vector3((node.min.x - position.x) % parentSize,
			                                            (node.min.y - position.y) % parentSize,
			                                            (node.min.z - position.z) % parentSize)); 

			OctreeNode parent;
			if (!parentsHash.TryGetValue(parentPos, out parent))
			{
				parent = new OctreeNode();
				parent.min = parentPos;
				parent.size = parentSize;
				parent.type = OctreeNodeType.Node_Internal;
				parentsHash.Add(parent.min, parent);
			}

			for (int j = 0; j < 8; j++)
			{
				Vector3 childMin = parentPos + (node.size * CHILD_MIN_OFFSETS[j]);
				
				if (childMin == node.min)
				{
					parent.children[j] = node;
					break;
				}
			}
		}

		children.Clear();
		List<OctreeNode> tempValues = parentsHash.Values.ToList();
		parentsHash.Clear();
		parentsHash = null;

		return tempValues;
	}

	public OctreeNode ConstructUpwards(List<OctreeNode> inputNodes, Vector3 rootMin, int rootNodeSize)
	{
		if (inputNodes.Count == 0)
		{
			return null;
		}

		int baseNodeSize = inputNodes[0].size;

		List<OctreeNode> nodes = new List<OctreeNode>();
		for (int i = 0; i < inputNodes.Count; i++)
			nodes.Add(inputNodes[i]);
		nodes.Sort(delegate(OctreeNode lhs, OctreeNode rhs)
		           {
						return lhs.size - rhs.size;
				   });

		// the input nodes may be different sizes if a seam octree is being constructed
		// in that case we need to process the input nodes in stages along with the newly
		// constructed parent nodes until all the nodes have the same size
		while (nodes[0].size != nodes[nodes.Count - 1].size)
		{
			// find the end of this run
			int iter = 0;
			int size = nodes[iter].size;
			do
			{
				++iter;
			} while (nodes[iter].size == size);

			// construct the new parent nodes for this run
			List<OctreeNode> newNodes = new List<OctreeNode>();
			for (int i = 0; i < iter; i++)
				newNodes.Add(nodes[i]);
			newNodes = ConstructParents(newNodes, rootMin, size * 2);

			// set up for the next iteration: the parents produced plus any remaining input nodes
			for (int i = iter; i < nodes.Count; i++)
				newNodes.Add(nodes[i]);

			nodes.Clear();
			for (int i = 0; i < newNodes.Count; i++)
				nodes.Add(newNodes[i]);
			newNodes.Clear();
			newNodes = null;
		}

		int parentSize = nodes[0].size * 2;
		while (parentSize <= rootNodeSize)
		{
			nodes = ConstructParents(nodes, rootMin, parentSize);
			parentSize *= 2;
		}

		if (nodes.Count != 1)
		{
			Debug.Log (baseNodeSize);
			Debug.Log(rootMin);
			Debug.Log(rootNodeSize);
			Debug.LogError("There can only be one root node!");
			Application.Quit();
		}

		OctreeNode root = nodes[0];
		nodes.Clear ();
		nodes = null;
		return root;
	}

	public void GenerateMeshFromOctree(OctreeNode node, List<Vector3> vertices, List<Vector3> normals, List<int> indices, int nodeSize)
	{
		if (node == null)
			return;

		vertices.Clear();
		normals.Clear();
		indices.Clear();

		GenerateVertexIndices(node, vertices, normals, nodeSize);
		ContourCellProc(node, indices);
	}
}
