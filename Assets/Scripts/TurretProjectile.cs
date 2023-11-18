using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TurretProjectile : Projectile {
    [SerializeField] private float speed;
    [SerializeField] private Vector3 knockback;

    public void FixedUpdate() {
        if (!IsServer) return;
        if (target == null) {
            //GameObject.Destroy(this.gameObject);
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        transform.position += (Vector3)((Vector2)target.position - (Vector2)transform.position).normalized * speed * Time.fixedDeltaTime;
    }

    protected override void OnEnemyCollision(Collider2D other) {
        Debug.Log("Turret trigger enter " + other.name + ' ' + gameObject.layer + ' ' + other.gameObject.layer);

        if (IsSpawned) GetComponent<NetworkObject>().Despawn();

        float dir = 0.0f;
        if (source != null) dir = Mathf.Sign(transform.position.x - source.position.x);
        var deb = other.GetComponent<ITakesDebuff>();
        var dam = other.GetComponent<ITakesDamage>();


        deb?.TakeDebuffServerRpc(Debuff.Knockback, 0, knockback / Time.fixedDeltaTime * dir);
        dam?.TakeDamageServerRpc(baseDamage);
    }
}
