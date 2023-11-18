using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
public class BassProjectile1 : Projectile {
    
    private LineRenderer _lineRenderer;
    public Action<Vector3> OnHit;
    public Action OnDespawn;

    void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
    }

    // Client side visuals
    void Update() {
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, source.position);
    }

    protected override void OnTriggerEnter2D(Collider2D other) {
        if (!IsOwner) return;
        if (!IsSpawned) return;

        switch (LayerMask.LayerToName(other.gameObject.layer)) {
            case "RedHurtbox":
            case "BlueHurtbox":
                OnEnemyCollision(other);
                return;
            case "Environment":
                OnHit(transform.position);
                _rigidbody.velocity = Vector2.zero;
                return;

            case "RedHitbox":
            case "BlueHitbox":
            case "PTEnvironment":
                return;
            default:
                Debug.LogError("Unhandled trigger enter layer");
                return;
        }
    }

    public override void OnNetworkDespawn() {
        OnDespawn();
    }


}
