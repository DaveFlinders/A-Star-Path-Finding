using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class PathRequestManager : MonoBehaviour {

	Queue<PathResult> results = new Queue<PathResult>();

	static PathRequestManager instance;
	Pathfinding pathfinding;

    public Queue<PathResult> Results
    {
        get
        {
            return results;
        }
    }

    public static PathRequestManager Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<PathRequestManager>();
            }

            return instance;
        }
    }

    void Awake()
    {
		pathfinding = GetComponent<Pathfinding>();
	}

	void Update()
    {
		if (Results.Count > 0)
        {
			int itemsInQueue = Results.Count;
			lock (Results)
            {
				for (int i = 0; i < itemsInQueue; i++)
                {
					PathResult result = Results.Dequeue ();
					result.callback (result.path, result.success);
				}
			}
		}
	}

	public static void RequestPath(PathRequest request)
    {
		ThreadStart threadStart = delegate 
        {
			Instance.pathfinding.FindPath (request, Instance.FinishedProcessingPath);
		};

		threadStart.Invoke ();
	}

	public void FinishedProcessingPath(PathResult result)
    {
		lock (Results)
        {
			Results.Enqueue (result);
		}
	}
}

public struct PathResult
{
	public Vector3[] path;
	public bool success;
	public Action<Vector3[], bool> callback;

	public PathResult (Vector3[] path, bool success, Action<Vector3[], bool> callback)
	{
		this.path = path;
		this.success = success;
		this.callback = callback;
	}

}

public struct PathRequest
{
	public Vector3 pathStart;
	public Vector3 pathEnd;
	public Action<Vector3[], bool> callback;

	public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback)
    {
		pathStart = _start;
		pathEnd = _end;
		callback = _callback;
	}

}
