using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Noise;

public delegate bool selectionDel(Vector3 min, Vector3 max);

public struct DensityPrimitive
{
	public int type;
	public int csg;
	public Vector3 position;
	public Vector3 size;

	public DensityPrimitive(int t, int c, Vector3 p, Vector3 s)
	{
		type = t;
		csg = c;
		position = p;
		size = s;
	}
};

public class Test : MonoBehaviour
{
	const int MAX_THRESHOLDS = 5;
	float[] THRESHOLDS = new float[MAX_THRESHOLDS]
	{
		-1.0f, 0.1f, 1.0f, 10.0f, 50.0f
	};
	int thresholdIndex = 0;

	const int octreeSize = 64;

	Dictionary<Vector3, Chunk> chunks;

	public ComputeShader computeShader;

	Dictionary<Vector3, Vector3> chunkQueue;
	List<Chunk> changedChunks;
	Dictionary<Vector3, Chunk> reloadingChunks;

	ThreadedChunkLoader thread;

	Vector3 prevCameraPos;
	List<Vector3> chunkGrid;


	// Use this for initialization
	void Start()
	{
		//Octree.InitPermutations(133925);
		Octree.InitPermutations(1200);

		thread = new ThreadedChunkLoader(this);

		chunkQueue = new Dictionary<Vector3, Vector3>();
		changedChunks = new List<Chunk>();
		reloadingChunks = new Dictionary<Vector3, Chunk>();
		chunkGrid = new List<Vector3>();

		prevCameraPos = Camera.main.transform.position;

		chunks = new Dictionary<Vector3, Chunk>();

		/*
		chunkQueue.Sort(delegate(Vector3 c1, Vector3 c2){
			return Mathf.Abs((Camera.main.transform.position - c1).sqrMagnitude).CompareTo
				(Mathf.Abs(((Camera.main.transform.position - c2).sqrMagnitude))); 
		});


		List<Vector3> sortQueue = chunkQueue.Keys.ToList();
		
		sortQueue.Sort(delegate(Vector3 c1, Vector3 c2) {
			return Mathf.Abs((Camera.main.transform.position - c1).sqrMagnitude).CompareTo
				(Mathf.Abs(((Camera.main.transform.position - c2).sqrMagnitude)));   
		});
		chunkQueue.Clear();
		chunkQueue = sortQueue.ToDictionary(x => x, x => x);
		*/

		int mapMult = 16;
		int yMult = 16;


		for (int x = 0; x < mapMult; x++)
		{
			for (int y = 0; y < yMult; y++)
			{
				for (int z = 0; z < mapMult; z++)
				{
					chunkGrid.Add(new Vector3(x, y, z));
				}
			}
		}


	}

	float gridScanCalls = 0;
	float averageGridScanTime = 0;

	void OnApplicationQuit()
	{
		Debug.Log (((averageGridScanTime / gridScanCalls) * 1000.0f) + " ms");
		for (int i = 0; i < chunks.Values.Count; i++)
		{
			chunks.Values.ElementAt(i).DestroyMesh();
			chunks.Values.ElementAt(i).DestroyOctree();
		}

		chunkQueue.Clear();
		chunks.Clear();
		changedChunks.Clear();
		reloadingChunks.Clear();
	}

	bool busy = false;
	float loadTimer = 0;
	float modifyTimer = 0;
	float reloadTimer = 0;
	Chunk loadingChunk = null;
	bool finishedChunk = false;
	Chunk changingChunk = null;

	public void informGame()
	{
		finishedChunk = true;
	}

	public void informGame(Chunk chunk)
	{
		if (chunk != null)
		{
			changingChunk = chunk;
			finishedChunk = true;
		}
	}

	void ModifyChunks()
	{
		if (modifyTimer >= 0.0f && !busy)
		{
			if (changedChunks.Count > 0)
			{
				busy = true;
				Chunk chunk = changedChunks[0];
				changedChunks.RemoveAt(0);
				float dist = Mathf.Abs((chunk.min - Camera.main.transform.position).magnitude);
				if (dist <= 96.0f)
					chunk.LOD = 6;
				else if (dist > 96.0f && dist <= 192.0f)
					chunk.LOD = 5;
				else if (dist > 192.0f && dist <= 288.0f)
					chunk.LOD = 4;
				else if (dist > 288.0f && dist <= 384.0f)
					chunk.LOD = 3;
				else if (dist > 384.0f && dist <= 512.0f)
					chunk.LOD = 2;

				if (!chunk.UpdateChunk(computeShader, thread, chunk.LOD, chunk.min))
				{
					busy = false;
				}
			}
			modifyTimer = 0;
		}
		modifyTimer += Time.deltaTime;
	}

	float cpuStartTime = 0;

	void LoadChunks()
	{
		if (loadTimer >= 0.0f && !busy)
		{
			if (chunkQueue.Keys.Count > 0)
			{
				busy = true;
				Vector3 info = chunkQueue.Keys.First();
				chunkQueue.Remove(chunkQueue.Keys.First());
				Chunk chunk = new Chunk();
				loadingChunk = chunk;
				Vector3 position = new Vector3((int) info.x, (int) info.y, (int) info.z);
				int lod = 2;
				Vector3 min = position - new Vector3(octreeSize / 2.0f, octreeSize / 2.0f, octreeSize / 2.0f);
				float dist = Mathf.Abs((min - Camera.main.transform.position).magnitude);
				if (dist <= 96.0f)
					lod = 6;
				else if (dist > 96.0f && dist <= 192.0f)
					lod = 5;
				else if (dist > 192.0f && dist <= 288.0f)
					lod = 4;
				else if (dist > 288.0f && dist <= 384.0f)
					lod = 3;
				else if (dist > 384.0f && dist <= 512.0f)
					lod = 2;



				if(chunk.CreateChunk(computeShader, thread, lod, position))
				{
					cpuStartTime = Time.realtimeSinceStartup;
				}
				else
				{
					chunk.containsNothing = true;
					loadingChunk.DestroyMesh();
					chunks.Add(min, loadingChunk);
					loadingChunk = null;
					busy = false;
				}
			}
			loadTimer = 0;
		}
		loadTimer += Time.deltaTime;
	}

	void ReloadChunks()
	{
		if (reloadTimer >= 0.0f && !busy)
		{
			if (reloadingChunks.Values.Count > 0)
			{
				busy = true;
				Chunk chunk = reloadingChunks.Values.First();
				reloadingChunks.Remove(reloadingChunks.Keys.First());

				if (chunk.meshObject == null)
					chunk.CreateMeshObject();
				float dist = Mathf.Abs((chunk.min - Camera.main.transform.position).magnitude);
				if (dist <= 96.0f)
					chunk.LOD = 6;
				else if (dist > 96.0f && dist <= 192.0f)
					chunk.LOD = 5;
				else if (dist > 192.0f && dist <= 288.0f)
					chunk.LOD = 4;
				else if (dist > 288.0f && dist <= 384.0f)
					chunk.LOD = 3;
				else if (dist > 384.0f && dist <= 512.0f)
					chunk.LOD = 2;

				if (!chunk.UpdateChunk(computeShader, thread, chunk.LOD, chunk.min))
				{
					chunk.containsNothing = true;
					chunk.DestroyMesh();
					busy = false;
				}
			}
			reloadTimer = 0;
		}
		reloadTimer += Time.deltaTime;
	}

	int csgMod = 0;
	int shape = 0;
	float modSize = 1.0f;

	void HandleModifications()
	{
		if (Input.GetMouseButtonDown(0) && !busy)
		{
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			Chunk hitChunk = null;
			if (Physics.Raycast(ray, out hit))
			{
				Bounds mBounds = new Bounds(hit.point, new Vector3(modSize, modSize, modSize));

				Dictionary<Vector3, Chunk> effectedChunks = chunks.Where(x => (new Bounds(x.Value.min + new Vector3(octreeSize / 2, octreeSize / 2, octreeSize / 2), new Vector3(octreeSize + 1, octreeSize + 1, octreeSize + 1)).Intersects(mBounds))).ToDictionary(x => x.Key, x => x.Value);
				
				for (int i = 0; i < effectedChunks.Values.Count; i++)
				{
					Chunk ch = effectedChunks.Values.ElementAt(i);
					if (changedChunks.Contains(ch))
						continue;
					int dip = 0;
					if (csgMod == 0)
						dip = 0;

					ch.primitiveMods[ch.modCount] = new DensityPrimitive(shape, csgMod, hit.point + (hit.normal * modSize * dip), new Vector3(modSize, modSize, modSize));
					ch.modCount++;
					ch.containsNothing = false;
					changedChunks.Add(ch);
					
					
					//Debug.Log ("Chunk " + i + " Intersects");
					//Debug.Log (cBounds.center);
				}
			}
		}
		
		float wheelInput = Input.GetAxis("Mouse ScrollWheel");
		if (wheelInput != 0)
		{
			modSize += wheelInput;
			if (modSize > 10.0f)
				modSize = 10.0f;
			else if (modSize < 0.5f)
				modSize = 0.5f;
		}
		
		if (Input.GetKeyDown(KeyCode.C))
		{
			if (csgMod == 0)
				csgMod = 1;
			else if (csgMod == 1)
				csgMod = 0;
		}
		
		if (Input.GetKeyDown(KeyCode.V))
		{
			shape++;
			if (shape == 3)
				shape = 0;
		}
	}

	void FinishUpChunkLoading()
	{
		if (finishedChunk)
		{
			finishedChunk = false;
			busy = false;
			
			if (changingChunk == null)
			{
				if (thread.m_Root != null && loadingChunk != null)
				{
					//if (loadingChunk.LOD == 6)
					//Debug.Log ("Chunk time on CPU: " + (Time.realtimeSinceStartup - cpuStartTime));
					loadingChunk.CreateSeamChunk(thread.m_Root, thread.m_Root.min);
					if (loadingChunk.GenerateMesh(thread.m_Vertices, thread.m_Normals, thread.m_Indices) > 0)
						loadingChunk.containsNothing = false;


					List<Chunk> seamChunks = new List<Chunk>();
					bool foundAllEight = FindSeamChunks(loadingChunk, seamChunks);

					if (foundAllEight)
					{
						List<OctreeNode> seams = FindSeamNodes(loadingChunk, seamChunks);
						OctreeNode seamRoot = loadingChunk.BuildSeamTree(seams, loadingChunk.min, octreeSize * 2);
						if (seamRoot != null)
						{
							Chunk seamChunk = new Chunk();
							seamChunk.LOD = 6;
							seamChunk.CreateSeamChunk(seamRoot, seamRoot.min);
							seamChunk.GenerateMesh();
							//Debug.Log (seamChunk.GenerateMesh());
							seamChunk.DestroyOctree();
							loadingChunk.seamChunk = seamChunk;
						}
					}

					
					
					
					//loadingChunk.DestroyOctree();
				}
				
				if (loadingChunk != null && thread.m_Root == null)
					loadingChunk.DestroyMesh();
				
				if (loadingChunk != null)
					chunks.Add(loadingChunk.min, loadingChunk);
			}
			else
			{
				if (thread.m_Root != null && changingChunk != null)
				{
					changingChunk.clearMesh();
					changingChunk.DestroyOctree();
					changingChunk.CreateSeamChunk(thread.m_Root, thread.m_Root.min);
					//float meshReloadStart = Time.realtimeSinceStartup;
					changingChunk.GenerateMesh(thread.m_Vertices, thread.m_Normals, thread.m_Indices);
					//Debug.Log (Time.realtimeSinceStartup - meshReloadStart);


					List<Chunk> seamChunks = new List<Chunk>();
					bool foundAllEight = FindSeamChunks(changingChunk, seamChunks);
					
					if (foundAllEight)
					{
						List<OctreeNode> seams = FindSeamNodes(changingChunk, seamChunks);
						OctreeNode seamRoot = changingChunk.BuildSeamTree(seams, changingChunk.min, octreeSize * 2);
						if (seamRoot != null)
						{
							Chunk seamChunk = changingChunk.seamChunk;
							if (seamChunk == null)
							{
								seamChunk = new Chunk();
								changingChunk.seamChunk = seamChunk;
							}

							if (seamChunk != null)
							{
								seamChunk.LOD = 6;
								seamChunk.CreateSeamChunk(seamRoot, seamRoot.min);
								seamChunk.GenerateMesh();
								//Debug.Log (seamChunk.GenerateMesh());
								seamChunk.DestroyOctree();
							}
						}
					}


					//changingChunk.DestroyOctree();
					changingChunk = null;
				}
				
			}
			
			thread.m_Vertices.Clear();
			thread.m_Normals.Clear();
			thread.m_Indices.Clear();
			loadingChunk = null;
			thread.m_Root = null;
		}
	}

	float queueTimer = 0;

	// Update is called once per frame
	void Update()
	{
		HandleModifications();


		FinishUpChunkLoading();

		ModifyChunks();
		LoadChunks();
		ReloadChunks();

		if (reloadingChunks.Count > 0)
		{
			Dictionary<Vector3, Chunk> reloadRemove = reloadingChunks.Where(x => Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) > 512.0f).ToDictionary(x => x.Key, x => x.Value);

			for (int i = reloadRemove.Values.Count - 1; i >= 0; i--)
			{
				Chunk chunk = reloadRemove.Values.ElementAt(i);
				chunk.DestroyOctree();
				chunk.DestroyMesh();
				
				reloadingChunks.Remove(reloadRemove.Keys.ElementAt(i));
				reloadRemove.Remove(reloadRemove.Keys.ElementAt(i));
				chunk = null;
			}
		}

		if (chunkQueue.Keys.Count > 0)
		{
			Vector3 queueStart = chunkQueue.Keys.First();

			Dictionary<Vector3, Vector3> queueRemove = chunkQueue.Where(x => Mathf.Abs(((x.Key - new Vector3(octreeSize / 2.0f, octreeSize / 2.0f, octreeSize / 2.0f)) - Camera.main.transform.position).magnitude) > 512.0f).ToDictionary(x => x.Key, x => x.Value);

			for (int i = queueRemove.Count - 1; i >= 0; i--)
			{
				Vector3 chInfo = queueRemove.Keys.ElementAt(i);
				chunkQueue.Remove(chInfo);
				queueRemove.Remove(chInfo);
			}

			if (!chunkQueue.ContainsKey(queueStart))
				chunkQueue.Add(queueStart, queueStart);
		}

		if (queueTimer >= 0.5f)
		{
			Vector3 camPos = Camera.main.transform.position;
			Vector3 camOff = new Vector3((int) (camPos.x / 64), (int) (camPos.y / 64), (int) (camPos.z / 64));

			if (/*chunkQueue.Keys.Count <= 1000 && reloadingChunks.Values.Count <= 1000 && */camOff != prevCameraPos)
			{
				float gridScanStart = Time.realtimeSinceStartup;
				prevCameraPos = camOff;

				Dictionary<Vector3, Chunk> removeScan = chunks.Where(x => (x.Value.meshObject != null && Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) > 512.0f)).ToDictionary(x => x.Key, x => x.Value);

				for (int i = removeScan.Values.Count - 1; i >= 0; i--)
				{
					Chunk chunk = removeScan.Values.ElementAt(i);
					chunk.DestroyOctree();
					chunk.DestroyMesh();
					if (chunk.modCount == 0)
					{
						chunks.Remove(chunk.min);
					}
					removeScan.Remove(chunk.min);
					chunk = null;
				}

				Dictionary<Vector3, Chunk> reloadScan = chunks.Where(x => !x.Value.containsNothing).ToDictionary(x => x.Key, x => x.Value);
				reloadScan = reloadScan.Where(x => (x.Value.meshObject == null ||
				                                    (Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) <= 96.0f && x.Value.LOD != 6) ||
				                                    (Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) > 96.0f && Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) <= 192.0f && x.Value.LOD != 5) ||
				                                    (Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) > 192.0f && Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) <= 288.0f && x.Value.LOD != 4) ||
				                                    (Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) > 288.0f && Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) <= 384.0f && x.Value.LOD != 3) ||
				                                    (Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) > 384.0f && Mathf.Abs((x.Value.min - Camera.main.transform.position).magnitude) <= 512.0f && x.Value.LOD != 2))).ToDictionary(x => x.Key, x => x.Value);
				reloadScan = reloadScan.Where(x => !reloadingChunks.ContainsValue(x.Value)).ToDictionary(x => x.Key, x => x.Value);

				for (int i = 0; i < reloadScan.Values.Count; i++)
					reloadingChunks.Add(reloadScan.Values.ElementAt(i).min, reloadScan.Values.ElementAt(i));



				List<Vector3> chunkPositions = new List<Vector3>();
				for (int i = 0; i < chunkGrid.Count; i++)
				{
					int x = (int) chunkGrid[i].x; int y = (int) chunkGrid[i].y; int z = (int) chunkGrid[i].z;
					Vector3 position = new Vector3((x + ((int) camOff.x) - 8) * octreeSize, (y + ((int) camOff.y) - 8) * octreeSize, (z + ((int) camOff.z) - 8) * octreeSize);
					Vector3 min = position - new Vector3(octreeSize / 2.0f, octreeSize / 2.0f, octreeSize / 2.0f);
					
					if (!(loadingChunk != null && loadingChunk.min == min))
					{
						float dist = Mathf.Abs((min - Camera.main.transform.position).magnitude);
						if (dist <= 512.0f)
							chunkPositions.Add(position);
					}
				}

				chunkPositions = chunkPositions.Where(x => !reloadingChunks.ContainsKey(x - new Vector3(octreeSize / 2.0f, octreeSize / 2.0f, octreeSize / 2.0f))).ToList();
				chunkPositions = chunkPositions.Where(x => !chunkQueue.ContainsKey(x)).ToList();
				chunkPositions = chunkPositions.Where(x => !chunks.ContainsKey(x - new Vector3(octreeSize / 2.0f, octreeSize / 2.0f, octreeSize / 2.0f))).ToList();

				for (int i = 0; i < chunkPositions.Count; i++)
					chunkQueue.Add(chunkPositions[i], chunkPositions[i]);
				//chunkQueue.AddRange(chunkPositions);



				//chunkPositions.Clear();

				if (chunkQueue.Count > 1)
				{
					List<Vector3> sortQueue = chunkQueue.Keys.ToList();
					
					sortQueue.Sort(delegate(Vector3 c1, Vector3 c2) {
						return Mathf.Abs((Camera.main.transform.position - c1).sqrMagnitude).CompareTo
							(Mathf.Abs(((Camera.main.transform.position - c2).sqrMagnitude)));   
					});
					chunkQueue.Clear();
					chunkQueue = sortQueue.ToDictionary(x => x, x => x);
				}

				if (reloadingChunks.Count > 1)
				{
					List<Chunk> sortReload = reloadingChunks.Values.ToList();

					sortReload.Sort(delegate(Chunk c1, Chunk c2) {
						return Mathf.Abs((Camera.main.transform.position - c1.min).sqrMagnitude).CompareTo
							(Mathf.Abs(((Camera.main.transform.position - c2.min).sqrMagnitude)));   
					});
					reloadingChunks.Clear();
					reloadingChunks = sortReload.ToDictionary(x => x.min, x => x);
				}

				averageGridScanTime += Time.realtimeSinceStartup - gridScanStart;
				gridScanCalls++;

			}

			queueTimer = 0;
		}

		queueTimer += Time.deltaTime;


	}

	void OnGUI()
	{
		//GUILayout.Label("Meshes " + Resources.FindObjectsOfTypeAll(typeof(Mesh)).Length);
	}

	public bool FindSeamChunks(Chunk chunk, List<Chunk> result)
	{
		Vector3 baseChunkMin = chunk.min;

		Vector3[] OFFSETS = new Vector3[8]
		{
			new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1),
			new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1)
		};

		bool foundAllEight = true;
		for (int i = 0; i < 8; i++)
		{
			Vector3 offsetMin = OFFSETS[i] * octreeSize;
			Vector3 chunkMin = baseChunkMin + offsetMin;
			Chunk c = GetChunk(chunkMin);
			if (c == null) foundAllEight = false;
			result.Add(c);
		}

		return foundAllEight;
	}

	public List<OctreeNode> FindSeamNodes(Chunk chunk, List<Chunk> seamChunks)
	{
		Vector3 baseChunkMin = chunk.min;
		Vector3 seamValues = baseChunkMin + new Vector3(octreeSize, octreeSize, octreeSize);

		selectionDel[] selectionFuncs = new selectionDel[8]
		{
			delegate(Vector3 min, Vector3 max)
			{
				return max.x == seamValues.x || max.y == seamValues.y || max.z == seamValues.z;

				//return seamValues.x <= max.x ||
					   //seamValues.y <= max.y ||
					   //seamValues.z <= max.z;
			},

			delegate(Vector3 min, Vector3 max)
			{
				return min.x == seamValues.x;

				//return seamValues.x >= min.x;
			},

			delegate(Vector3 min, Vector3 max)
			{
				return min.z == seamValues.z;

				//return seamValues.z >= min.z;
			},

			delegate(Vector3 min, Vector3 max)
			{
				return min.x == seamValues.x && min.z == seamValues.z;

				//return seamValues.x >= min.x &&
					   //seamValues.z >= min.z;
			},

			delegate(Vector3 min, Vector3 max)
			{
				return min.y == seamValues.y;

				//return seamValues.y >= min.y;
			},
			
			delegate(Vector3 min, Vector3 max)
			{
				return min.x == seamValues.x && min.y == seamValues.y;

				//return seamValues.x <= min.x &&
					   //seamValues.y <= min.y;
			},
			
			delegate(Vector3 min, Vector3 max)
			{
				return min.y == seamValues.y && min.z == seamValues.z;

				//return seamValues.y <= min.y &&
					   //seamValues.z <= min.z;
			},
			
			delegate(Vector3 min, Vector3 max)
			{
				return min.x == seamValues.x && min.y == seamValues.y && min.z == seamValues.z;

				//return seamValues.x >= min.x &&
					   //seamValues.y >= min.y &&
					   //seamValues.z >= min.z;
			}
		};

		List<OctreeNode> seamNodes = new List<OctreeNode>();

		for (int i = 0; i < seamChunks.Count; i++)
		{
			Chunk c = seamChunks[i];
			
			if (c != null)
			{
				List<OctreeNode> chunkNodes = c.findNodes(selectionFuncs[i], chunk);
				for (int j = 0; j < chunkNodes.Count; j++)
					seamNodes.Add(chunkNodes[j]);
			}
		}

		return seamNodes;
	}


	Chunk GetChunk(Vector3 min)
	{
		Chunk res;
		if (chunks.TryGetValue(min, out res))
			return res;

		return null;
	}
}
