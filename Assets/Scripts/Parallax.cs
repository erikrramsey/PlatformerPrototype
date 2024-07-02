using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Parallax : MonoBehaviour
{
    [SerializeField] public float parallaxValue = 0.0f;
    private Camera _camera;

    void Awake() {
        _camera = Camera.main;
    }

    void LateUpdate() {
        transform.position = new Vector3(_camera.transform.position.x * parallaxValue, _camera.transform.position.y * parallaxValue, 0.0f);
    } 
}
