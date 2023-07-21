using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TurretProjectile : Projectile {
    [SerializeField] private float ttl;
    [SerializeField] private float speed;
    [SerializeField] private float damage;


    public override void Setup(TeamColor _teamColor, Transform _target, Transform _turret) {
        base.Setup(_teamColor, _target, _turret);
        GameObject.Destroy(this.gameObject, ttl);
    }

    public void FixedUpdate() {
        if (!IsServer) return;
        if (target == null) {
            GameObject.Destroy(this.gameObject);
            return;
        }

        transform.position += (Vector3)((Vector2)target.position - (Vector2)transform.position).normalized * speed * Time.fixedDeltaTime;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!IsOwner) return;
        if (!IsSpawned) return;
        if (other.transform != target.transform) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("RedHitbox")) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("BlueHitbox")) return;


        Debug.Log("Turret trigger enter " + other.name + ' ' + gameObject.layer + ' ' + other.gameObject.layer);
        float dir = Mathf.Sign(transform.position.x - source.position.x);
        var d = other.GetComponent<ITakesDamage>();

        if (IsSpawned) GetComponent<NetworkObject>().Despawn();

        d.TakeDamageServerRpc(new Vector2(80.0f * dir, 80.0f), damage);
    }
}
