using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParallaxLockY : MonoBehaviour
{
    [SerializeField] public float parallaxValue = 0.0f;
    private Camera _camera;

    void Awake() {
        _camera = Camera.main;
    }

    void LateUpdate() {
        transform.position = new Vector3(_camera.transform.position.x * parallaxValue, transform.position.y, 0.0f);
    } 
}
