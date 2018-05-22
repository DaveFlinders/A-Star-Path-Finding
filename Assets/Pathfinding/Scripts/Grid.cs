using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid : MonoBehaviour
{
    private static Grid instance;

	public bool displayGridGizmos;
	public LayerMask unwalkableMask;
	public Vector2 gridWorldSize;
	public float nodeRadius;
	public TerrainType[] walkableRegions;
	public int obstacleProximityPenalty = 10;
	Dictionary<int,int> walkableRegionsDictionary = new Dictionary<int, int>();
	LayerMask walkableMask;
    Vector3 worldBottomLeft;

    Node[,] grid;

	float nodeDiameter;
	int gridSizeX, gridSizeY;

	int penaltyMin = int.MaxValue;
	int penaltyMax = int.MinValue;

	void Awake()
    {
		nodeDiameter = nodeRadius*2;
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);

		foreach (TerrainType region in walkableRegions)
        {
			walkableMask.value |= region.terrainMask.value;
			walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value,2),region.terrainPenalty);
		}

        worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        FullGridScan();
	}

	public int MaxSize
    {
		get {
			return gridSizeX * gridSizeY;
		}
	}

    public static Grid Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<Grid>();
            }

            return instance;
        }
    }

    /// <summary>
    /// This is a full grid scan
    /// Use at the game start -- REQUIRED!
    /// </summary>
    void FullGridScan()
    {
		grid = new Node[gridSizeX,gridSizeY];

        lock (PathRequestManager.Instance.Results)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    ScanGridAtCoord(x, y);
                }
            }
        }

		BlurPenaltyMap (3);

    }

    /// <summary>
    /// This function will update the grid using the passed in game object collider bounds
    /// This can be slow if used every frame
    /// </summary>
    /// <param name="objectBounds"></param>
    public void UpdatePartialGrid(Bounds objectBounds)
    {
        int startX = Mathf.FloorToInt(Mathf.Abs(worldBottomLeft.x)) - Mathf.Abs(Mathf.FloorToInt(objectBounds.min.x)) - Mathf.CeilToInt(nodeDiameter + nodeRadius);
        int endX = Mathf.CeilToInt(Mathf.Abs(worldBottomLeft.x)) + Mathf.Abs(Mathf.CeilToInt(objectBounds.max.x)) + Mathf.CeilToInt(nodeDiameter + nodeRadius);

        int startY = Mathf.FloorToInt(Mathf.Abs(worldBottomLeft.z)) + Mathf.FloorToInt(objectBounds.min.z) - Mathf.CeilToInt(nodeDiameter + nodeRadius);
        int endY = Mathf.CeilToInt(Mathf.Abs(worldBottomLeft.z)) + Mathf.CeilToInt(objectBounds.max.z) + Mathf.CeilToInt(nodeDiameter + nodeRadius);

        lock (PathRequestManager.Instance.Results)
        {
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (x > gridSizeX || y > gridSizeY)
                    {
                        Debug.LogWarning("UpdatePartialGrid out of bounds at X: " + x + " " + y + " :: failed.");
                        continue;
                    }

                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);

                    //Reset the node to walkable
                    grid[x, y] = new Node(true, worldPoint, x, y, 0);
                    //Scan the node
                    ScanGridAtCoord(x, y);
                   
                }
            }
        }

        BlurPenaltyMap(3);

    }

    void ScanGridAtCoord(int x, int y)
    {
        Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
        bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));

        int movementPenalty = 0;

        Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, walkableMask))
        {
            walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
        }

        if (!walkable)
        {
            movementPenalty += obstacleProximityPenalty;
        }

        grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
    }

	void BlurPenaltyMap(int blurSize)
    {
		int kernelSize = blurSize * 2 + 1;
		int kernelExtents = (kernelSize - 1) / 2;

		int[,] penaltiesHorizontalPass = new int[gridSizeX,gridSizeY];
		int[,] penaltiesVerticalPass = new int[gridSizeX,gridSizeY];

		for (int y = 0; y < gridSizeY; y++)
        {
			for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
				int sampleX = Mathf.Clamp (x, 0, kernelExtents);
				penaltiesHorizontalPass [0, y] += grid [sampleX, y].movementPenalty;
			}

			for (int x = 1; x < gridSizeX; x++)
            {
				int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
				int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX-1);

				penaltiesHorizontalPass [x, y] = penaltiesHorizontalPass [x - 1, y] - grid [removeIndex, y].movementPenalty + grid [addIndex, y].movementPenalty;
			}
		}
			
		for (int x = 0; x < gridSizeX; x++)
        {
			for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
				int sampleY = Mathf.Clamp (y, 0, kernelExtents);
				penaltiesVerticalPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
			}

			int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, 0] / (kernelSize * kernelSize));
			grid [x, 0].movementPenalty = blurredPenalty;

			for (int y = 1; y < gridSizeY; y++)
            {
				int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
				int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY-1);

				penaltiesVerticalPass [x, y] = penaltiesVerticalPass [x, y-1] - penaltiesHorizontalPass [x,removeIndex] + penaltiesHorizontalPass [x, addIndex];
				blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, y] / (kernelSize * kernelSize));
				grid [x, y].movementPenalty = blurredPenalty;

				if (blurredPenalty > penaltyMax)
                {
					penaltyMax = blurredPenalty;
				}
				if (blurredPenalty < penaltyMin)
                {
					penaltyMin = blurredPenalty;
				}
			}
		}

	}

	public List<Node> GetNeighbours(Node node)
    {
		List<Node> neighbours = new List<Node>();

		for (int x = -1; x <= 1; x++)
        {
			for (int y = -1; y <= 1; y++)
            {
				if (x == 0 && y == 0)
					continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
					neighbours.Add(grid[checkX,checkY]);
				}
			}
		}

		return neighbours;
	}


	public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);

		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
		return grid[x,y];
	}

	void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,1,gridWorldSize.y));
		if (grid != null && displayGridGizmos)
        {
			foreach (Node n in grid)
            {

				Gizmos.color = Color.Lerp (Color.white, Color.black, Mathf.InverseLerp (penaltyMin, penaltyMax, n.movementPenalty));
				Gizmos.color = (n.walkable)?Gizmos.color:Color.red;
				Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
			}
		}
	}

	[System.Serializable]
	public class TerrainType
    {
		public LayerMask terrainMask;
		public int terrainPenalty;
	}


}