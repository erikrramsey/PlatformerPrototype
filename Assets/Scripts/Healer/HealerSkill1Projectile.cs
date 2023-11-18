using UnityEngine;
using Unity.Netcode;

public class HealerSkill1Projectile : Projectile {
    protected override void OnEnemyCollision(Collider2D other) {
        if (other.transform == source) return;
        if (!other.GetComponent<PlayerCharacter>()) return;

        var dam = other.GetComponent<ITakesDamage>();
        GetComponent<NetworkObject>().Despawn();
        dam.TakeDamageServerRpc(baseDamage);
    }

}