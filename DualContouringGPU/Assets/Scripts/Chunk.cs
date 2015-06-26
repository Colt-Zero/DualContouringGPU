using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chunk
{
	List<Vector3> vertices;
	List<Vector3> normals;
	List<int> indices;

	OctreeNode root;
	public Octree tree;

	Mesh voxelMesh;
	public GameObject meshObject;

	public Vector3 min;

	public int LOD;

	public DensityPrimitive[] primitiveMods;
	public int modCount = 0;

	public bool containsNothing = true;

	public Chunk seamChunk = null;

	public Chunk()
	{
		int primModCount = 300;
		primitiveMods = new DensityPrimitive[primModCount];
		
		//primitiveMods[0] = new DensityPrimitive(1, 0, new Vector3 (20, 20, 0), new Vector3(10, 10, 10));
		//primitiveMods[1] = new DensityPrimitive(0, 1, new Vector3 (20, 25, 0), new Vector3(5, 3, 5));

		vertices = new List<Vector3>();
		normals = new List<Vector3>();
		indices = new List<int>();

		tree = new Octree();
		meshObject = null;
		voxelMesh = null;
		meshObject = (GameObject) GameObject.Instantiate(Resources.Load("ChunkMesh"), Vector3.zero, Quaternion.identity);
	}

	public void CreateMeshObject()
	{
		meshObject = (GameObject) GameObject.Instantiate(Resources.Load("ChunkMesh"), Vector3.zero, Quaternion.identity);
	}

	private void Octree_FindNodes(OctreeNode node, selectionDel func, List<OctreeNode> nodes, Chunk primaryChunk)
	{
		if (node == null)
			return;

		Vector3 max = node.min + new Vector3(node.size, node.size, node.size);
		//if (this != primaryChunk)
		//{
		    if(!func(node.min, max))
				return;
		//}

		if (node.type == OctreeNodeType.Node_Leaf || node.type == OctreeNodeType.Node_Psuedo)
		{
			//*
			OctreeNode newNode = new OctreeNode();
			newNode.children = node.children;
			newNode.min = node.min;
			newNode.size = node.size;
			newNode.type = node.type;
			newNode.drawInfo = node.drawInfo;
			//*/
			nodes.Add(newNode);
		}
		else
		{
			for (int i = 0; i < 8; i++)
				Octree_FindNodes(node.children[i], func, nodes, primaryChunk);
		}
	}

	public List<OctreeNode> findNodes(selectionDel filterfunc, Chunk primaryChunk)
	{
		List<OctreeNode> nodes = new List<OctreeNode>();
		Octree_FindNodes(root, filterfunc, nodes, primaryChunk);
		return nodes;
	}

	public OctreeNode BuildSeamTree(List<OctreeNode> seamNodes, Vector3 eMin, int size)
	{
		return tree.ConstructUpwards(seamNodes, eMin, size);
	}

	public void DestroyOctree()
	{
		if (root != null)
			tree.DestroyOctree(root);
		root = null;
	}

	/*
	public bool CreateChunk(int octreeSize, Vector3 position)
	{
		min = position - new Vector3(octreeSize / 2, octreeSize / 2, octreeSize / 2);
		Vector3 octreePos = new Vector3(position.x - (octreeSize / 2), position.y - (octreeSize / 2), position.z - (octreeSize / 2));
		root = tree.BuildOctree(octreePos, octreeSize);

		if (root == null && meshObject != null)
		{
			DestroyMesh();
		}

		return root != null;
	}
	*/

	public bool CreateChunk(ComputeShader computeShader, int lod, Vector3 position)
	{
		LOD = lod;
		if (LOD < 1)
			LOD = 1;
		else if (LOD > 6)
			LOD = 6;
		int octreeSize = (int) Mathf.Pow(2, LOD);

		min = position - new Vector3(octreeSize / 2, octreeSize / 2, octreeSize / 2);
		Vector3 octreePos = new Vector3(position.x - (octreeSize / 2), position.y - (octreeSize / 2), position.z - (octreeSize / 2));
		root = tree.BuildOctree(computeShader, octreePos, octreeSize);
		
		if (root == null && meshObject != null)
		{
			DestroyMesh();
		}
		
		return root != null;
	}

	public bool CreateChunk(ComputeShader computeShader, ThreadedChunkLoader thread, int lod, Vector3 position)
	{
		LOD = lod;
		if (LOD < 1)
			LOD = 1;
		else if (LOD > 6)
			LOD = 6;
		int octreeSize = (int) Mathf.Pow(2, LOD);
		min = position - new Vector3(64 / 2, 64 / 2, 64 / 2);
		Vector3 octreePos = new Vector3(position.x - (64 / 2), position.y - (64 / 2), position.z - (64 / 2));
		bool created = tree.Voxelize(computeShader, thread, this, octreePos, octreeSize);

		if (meshObject != null)
		{
			meshObject.GetComponent<MeshINfo>().chunkMin = min;
		}

		if (!created && meshObject != null)
		{
			DestroyMesh();
		}
		
		return created;
	}

	public bool UpdateChunk(ComputeShader computeShader, ThreadedChunkLoader thread, int updateSize, Vector3 position)
	{
		int octreeSize = (int) Mathf.Pow(2, LOD);
		bool updated = tree.VoxelizeUpdate(computeShader, thread, this, position, octreeSize);

		return updated;
	}

	/*
	public void SimpilifyChunk(float threshold)
	{
		tree.Simplify(root, threshold);
	}
	*/

	public int GenerateMesh()
	{
		tree.GenerateMeshFromOctree(root, vertices, normals, indices, (64 / ((int) Mathf.Pow(2, LOD))));
		
		if (vertices.Count == 0)
		{
			tree.DestroyOctree(root);
			root = null;
			if (meshObject != null)
				DestroyMesh();
			return vertices.Count;
		}

		if (meshObject != null)
		{
			meshObject.GetComponent<MeshINfo>().chunkMin = min;
			meshObject.GetComponent<MeshINfo>().vertexCount = vertices.Count;
		}

		if (voxelMesh == null)
			voxelMesh = new Mesh();
		voxelMesh.vertices = vertices.ToArray();
		voxelMesh.normals = normals.ToArray();
		voxelMesh.triangles = indices.ToArray();
		voxelMesh.RecalculateBounds();
		voxelMesh.RecalculateNormals();
		if (meshObject != null)
		{
			meshObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = null;
			meshObject.GetComponent<MeshCollider>().sharedMesh = null;
			meshObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = voxelMesh;
			meshObject.GetComponent<MeshCollider>().sharedMesh = voxelMesh;
		}
			
		return vertices.Count;
	}

	public void clearMesh()
	{
		if (voxelMesh != null)
			voxelMesh.Clear();

		if (seamChunk != null)
			seamChunk.clearMesh();
	}

	public int GenerateMesh(List<Vector3> verts, List<Vector3> norms, List<int> inds)
	{
		if (meshObject != null)
		{
			meshObject.GetComponent<MeshINfo>().chunkMin = min;
			meshObject.GetComponent<MeshINfo>().vertexCount = verts.Count;
		}

		int vertCount = verts.Count;

		if (vertCount <= 0)
		{
			if (root != null)
				tree.DestroyOctree(root);
			root = null;
			if (meshObject != null)
				DestroyMesh();
			return vertCount;
		}


		if (voxelMesh == null)
			voxelMesh = new Mesh();
		voxelMesh.vertices = verts.ToArray();
		voxelMesh.normals = norms.ToArray();
		voxelMesh.triangles = inds.ToArray();
		voxelMesh.RecalculateBounds();
		voxelMesh.RecalculateNormals();
		if (meshObject != null)
		{
			meshObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = null;
			meshObject.GetComponent<MeshCollider>().sharedMesh = null;
			meshObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = voxelMesh;
			meshObject.GetComponent<MeshCollider>().sharedMesh = voxelMesh;
		}
		
		return vertCount;
	}

	public List<OctreeNode> FindDrawnVoxels()
	{
		List<OctreeNode> drawn = new List<OctreeNode>();
		tree.FindDrawnVoxels(root, drawn);
		return drawn;
	}

	public void DestroyMesh()
	{
		if (meshObject != null)
		{
			//meshObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.Clear();
			//meshObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = null;
			//meshObject.GetComponent<MeshCollider>().sharedMesh.Clear();
			//meshObject.GetComponent<MeshCollider>().sharedMesh = null;
			//GameObject.Destroy(meshObject.GetComponent<SkinnedMeshRenderer>());
			//GameObject.Destroy(meshObject.GetComponent<MeshCollider>());
			GameObject.Destroy(meshObject);
			if (voxelMesh != null)
			{
				voxelMesh.Clear();
				voxelMesh = null;
				meshObject = null;
			}
		}

		if (seamChunk != null)
		{
			seamChunk.DestroyMesh();
		}
	}

	public void CreateSeamChunk(OctreeNode seamRoot, Vector3 position)
	{
		min = position;
		root = seamRoot;
	}
}
