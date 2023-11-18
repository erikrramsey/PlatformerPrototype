using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
public class BassProjectile1 : Projectile {
    private LineRenderer _lineRenderer;
    private Transform _attachedPlayer;
    private bool _wasAttached = false;

    public Action<ClientRpcParams> OnHit;
    public Action<ClientRpcParams> OnDespawn;

    void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;

        if (!IsServer) return;

    }

    // Client side visuals
    void Update() {
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, source.position);
    }

    void FixedUpdate() {
        if (_attachedPlayer == null) {
            if (_wasAttached) 
                GetComponent<NetworkObject>().Despawn();
            else
                return;
        }

        transform.position = _attachedPlayer.transform.position;
    }


    [ClientRpc]
    public void SetupVisualsClientRpc(NetworkObjectReference target) {
        if (target.TryGet(out NetworkObject targetObject)) {
            source = targetObject.transform;   
        }
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
                OnHit(ownerParams);
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
        if (!IsServer) return;
        OnDespawn(ownerParams);
    }

    protected override void OnEnemyCollision(Collider2D other) {
        _attachedPlayer = other.transform; 
        _wasAttached = true;
        _rigidbody.velocity = Vector2.zero;
        source.GetComponent<Bass>().SetProjectile1TargetClientRpc(_attachedPlayer.GetComponent<NetworkObject>(), ownerParams);
        OnHit(ownerParams);
    }


}
