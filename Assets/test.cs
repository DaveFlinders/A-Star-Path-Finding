using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour {

    Collider m_Collider;

    void Start()
    {
        //Fetch the Collider from the GameObject
        m_Collider = GetComponent<Collider>();
        GameObject g1 = new GameObject();
        g1.transform.position = m_Collider.bounds.max;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * Time.deltaTime);
        Grid.Instance.UpdatePartialGrid(m_Collider.bounds);
    }

}
