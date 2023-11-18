using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TurretProjectile : Projectile {
    [SerializeField] private float speed;

    public void FixedUpdate() {
        if (!IsServer) return;
        if (target == null) {
            GameObject.Destroy(this.gameObject);
            return;
        }

        transform.position += (Vector3)((Vector2)target.position - (Vector2)transform.position).normalized * speed * Time.fixedDeltaTime;
    }

    protected override void OnEnemyCollision(Collider2D other) {
        Debug.Log("Turret trigger enter " + other.name + ' ' + gameObject.layer + ' ' + other.gameObject.layer);

        float dir = 0.0f;
        if (source != null) dir = Mathf.Sign(transform.position.x - source.position.x);
        var d = other.GetComponent<ITakesDamage>();

        if (IsSpawned) GetComponent<NetworkObject>().Despawn();

        d.TakeDamageServerRpc(new Vector2(80.0f * dir, 80.0f), baseDamage);
    }
}
