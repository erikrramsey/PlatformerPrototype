using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelPerfectFollow : MonoBehaviour {
    Camera _camera;

    void Awake() {
        _camera = Camera.main;
    }

    void FixedUpdate() {
        var ppu = 32.0f;
        transform.position = new Vector3(Mathf.Round(transform.position.x *  ppu) / ppu, Mathf.Round(transform.position.y * ppu) / ppu, transform.position.z);
    }
}
