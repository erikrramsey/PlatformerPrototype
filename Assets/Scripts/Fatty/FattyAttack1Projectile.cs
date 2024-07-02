using UnityEngine;
using Unity.Netcode;

public class FattyAttack1Projectile : Projectile {
    protected override void OnEnemyCollision(Collider2D other) {
        var d = other.GetComponent<ITakesDamage>();
        GetComponent<NetworkObject>().Despawn();
        //d.TakeDamageServerRpc(baseDamage);
    }

}