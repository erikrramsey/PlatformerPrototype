using UnityEngine;
using Unity.Netcode;

public class FattyAttack2Projectile : Projectile {
    protected override void OnEnemyCollision(Collider2D other) {
        base.OnEnemyCollision(other);
        var damage = other.GetComponent<ITakesDamage>();
        var debuff = other.GetComponent<ITakesDebuff>();
        //damage.TakeDamageServerRpc(baseDamage);

        if (debuff != null) {
            debuff.TakeDebuffServerRpc(Debuff.JumpSlow, 4.0f, -0.5f, true);
        }
    }

}