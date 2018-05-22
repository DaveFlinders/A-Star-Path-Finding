using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMover : MonoBehaviour {

    Vector3 worldBottomLeft;
    Vector3 worldBottomRight;
    Vector3 worldTopLeft;
    Vector3 worldTopRight;

    void Start () {
        

        worldBottomLeft = transform.position - Vector3.right * Grid.Instance.gridWorldSize.x / 2 - Vector3.forward * Grid.Instance.gridWorldSize.y / 2;
        worldBottomRight = transform.position + Vector3.right * Grid.Instance.gridWorldSize.x / 2 - Vector3.forward * Grid.Instance.gridWorldSize.y / 2;

        worldTopLeft = transform.position - Vector3.right * Grid.Instance.gridWorldSize.x / 2 + Vector3.forward * Grid.Instance.gridWorldSize.y / 2;
        worldTopRight = transform.position + Vector3.right * Grid.Instance.gridWorldSize.x / 2 + Vector3.forward * Grid.Instance.gridWorldSize.y / 2;

        StartCoroutine(MoveTarget());
    }

    IEnumerator MoveTarget()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);

            transform.position = new Vector3(Random.Range(worldBottomLeft.x, worldBottomRight.x), 0, Random.Range(worldTopLeft.x, worldTopRight.x));
        }
    }
}
