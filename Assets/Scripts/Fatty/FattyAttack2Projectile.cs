using UnityEngine;
using Unity.Netcode;

public class FattyAttack2Projectile : Projectile {
    private Rigidbody2D _rigidbody;
    private float damage;

    public override void Setup(TeamColor _teamColor, Transform _target = null, Transform _source = null) {
        base.Setup(_teamColor, _target, _source);
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public void setInitialForce(Vector2 force) {
        _rigidbody.AddForce(force);
    }

    public void setDamage(float _damage) {
        damage = _damage;
    }

    protected override void OnEnemyCollision(Collider2D other) {
        base.OnEnemyCollision(other);
        var d = other.GetComponent<ITakesDamage>();
        GetComponent<NetworkObject>().Despawn();
        d.TakeDamageServerRpc(Vector2.zero, damage);
    }

}