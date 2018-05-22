using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour {

    Collider m_Collider;
    Vector3 m_Center;
    Vector3 m_Size, m_Min, m_Max;

    void Start()
    {
        //Fetch the Collider from the GameObject
        m_Collider = GetComponent<Collider>();

    }

    private void Update()
    {
        //transform.Translate(transform.right * Time.deltaTime);
        //Grid.Instance.UpdatePartialGrid(m_Collider.bounds);
    }

}
