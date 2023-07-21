using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private float speed;
    void FixedUpdate() {
       transform.rotation = Quaternion.Euler(0.0f, 0.0f, speed * Time.deltaTime) * transform.rotation; 
    }
}
