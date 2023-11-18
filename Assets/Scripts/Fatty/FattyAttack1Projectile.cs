using UnityEngine;
using Unity.Netcode;

public class FattyAttack1Projectile : Projectile {
    protected override void OnEnemyCollision(Collider2D other) {
        base.OnEnemyCollision(other);
        var d = other.GetComponent<ITakesDamage>();
        GetComponent<NetworkObject>().Despawn();
        d.TakeDamageServerRpc(Vector2.zero, baseDamage);
    }

}