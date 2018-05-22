using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour {

    Collider m_Collider;
    float speed = 2f;

    void Start()
    {
        //Fetch the Collider from the GameObject
        m_Collider = GetComponent<Collider>();
        GameObject g1 = new GameObject();
        g1.transform.position = m_Collider.bounds.max;
    }

    private void Update()
    {
        var v3 = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));
        transform.Translate(speed * v3.normalized * Time.deltaTime);
        Grid.Instance.UpdatePartialGrid(m_Collider.bounds);
    }

}
