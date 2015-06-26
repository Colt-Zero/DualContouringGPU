using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class ThreadedJob
{
	private bool m_IsDone = false;
	protected object m_Handle = new object();
	protected Thread m_Thread = null;


	public bool isDone
	{
		get
		{
			bool tmp;
			lock(m_Handle)
			{
				tmp = m_IsDone;
			}
			return tmp;
		}
		set
		{
			lock(m_Handle)
			{
				m_IsDone = value;
			}
		}
	}


	public virtual void Start()
	{
		m_Thread = new Thread(Run);
		m_Thread.Start();
	}

	public virtual void Abort()
	{
		m_Thread.Abort();
	}

	protected virtual void ThreadFunction() { }

	protected virtual void OnFinished() { }

	public virtual bool Update()
	{

		if (isDone)
		{
			OnFinished();
			return true;
		}

		return false;
	}

	private void Run()
	{
		ThreadFunction();
		isDone = true;
	}
}

public class ThreadedChunkLoader : ThreadedJob
{
	private int i_Count = 0;
	private Vector3[] i_VoxMins = null;
	private uint[] i_VoxMaterials = null;
	private Octree.GPUVOX[] i_Voxs = null;
	private Octree i_Tree = null;
	private Vector3 i_Min = Vector3.zero;
	private int i_Size = 0;

	private Test m_Test;

	public OctreeNode m_Root = null;
	public List<Vector3> m_Vertices;
	public List<Vector3> m_Normals;
	public List<int> m_Indices;

	private bool updatingChunk;
	private Chunk chunkToUpdate;


	public ThreadedChunkLoader(Test test)
	{
		chunkToUpdate = null;
		updatingChunk = false;
		m_Test = test;
		m_Vertices = new List<Vector3>();
		m_Normals = new List<Vector3>();
		m_Indices = new List<int>();
	}

	public void setData(Octree tree, int count, Vector3[] voxMins, uint[] voxMats, Octree.GPUVOX[] voxs, Vector3 min, int octreeSize)
	{
		i_Tree = tree;
		i_Count = count;
		i_VoxMins = voxMins;
		i_VoxMaterials = voxMats;
		i_Voxs = voxs;
		i_Min = min;
		i_Size = octreeSize;
	}

	public void setChunkForUpdate(Chunk chunk)
	{
		chunkToUpdate = chunk;
		updatingChunk = true;
	}

	protected override void ThreadFunction()
	{
		if (i_Tree != null && i_Count > 0 && i_VoxMins.Length > 0 && i_VoxMaterials.Length > 0 && i_Voxs.Length > 0 && i_Size != 0)
		{

			List<OctreeNode> computedVoxels = new List<OctreeNode>();


			int HIGHEST_VOXEL_RES = 64;
			int voxelSize = HIGHEST_VOXEL_RES / i_Size;
			for (int i = 0; i < i_Count; i++)
			{
				if (i_Voxs[i].numPoints != 0)
				{
					OctreeNode leaf = new OctreeNode();
					leaf.type = OctreeNodeType.Node_Leaf;
					leaf.size = voxelSize;
					OctreeDrawInfo drawInfo = new OctreeDrawInfo();
					drawInfo.position = i_Voxs[i].vertPoint;
					drawInfo.averageNormal = i_Voxs[i].avgNormal;
					drawInfo.corners = (int) i_VoxMaterials[i];
					leaf.drawInfo = drawInfo;
					leaf.min = i_VoxMins[i];
					computedVoxels.Add(leaf);
				}
			}

			//Debug.Log(computedVoxels.Count);

			if (computedVoxels.Count > 0)
			{
				if (updatingChunk && chunkToUpdate != null)
					chunkToUpdate.DestroyOctree();
				m_Root = i_Tree.ConstructUpwards(computedVoxels, i_Min, HIGHEST_VOXEL_RES);
				if (m_Root != null)
				{
					i_Tree.GenerateMeshFromOctree(m_Root, m_Vertices, m_Normals, m_Indices, voxelSize);
				}
			}

			//m_Vertices.TrimExcess();
			//m_Normals.TrimExcess();
			//m_Indices.TrimExcess();

			computedVoxels.Clear();
			computedVoxels = null;

			i_Min = Vector3.zero;
			i_Size = 0;
			i_Count = 0;
			i_Tree = null;
			//Array.Clear(i_VoxMins, 0, i_VoxMins.Length);
			i_VoxMins = null;
			//Array.Clear(i_VoxMaterials, 0, i_VoxMaterials.Length);
			i_VoxMaterials = null;
			//Array.Clear(i_Voxs, 0, i_Voxs.Length);
			i_Voxs = null;

			//Debug.Log ("Finished loading chunk");

			if (!updatingChunk)
				m_Test.informGame();

			if (updatingChunk)
			{
				m_Test.informGame(chunkToUpdate);
				chunkToUpdate = null;
				updatingChunk = false;
			}

		}
	}

	protected override void OnFinished()
	{
		Debug.Log ("Finished");
	}
}