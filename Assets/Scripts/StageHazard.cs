using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class StageHazard : NetworkBehaviour {
    [SerializeField] private float DPS;
    [SerializeField] private float DamageTickRate;
    [SerializeField] private int GoldValue;

    private bool _cooldown;
    private Health health;

    protected void Awake() {
        health = GetComponent<Health>();
        health.OnDeath += OnDeath;
    }

    protected virtual void OnTriggerStay2D(Collider2D other) {
        if (!IsOwner) return;
        if (!(other.gameObject.layer == LayerMask.NameToLayer("RedHurtbox") || 
            other.gameObject.layer == LayerMask.NameToLayer("BlueHurtbox"))) return;
        
        if (!_cooldown) {
            if (other.TryGetComponent<ITakesDamage>(out var oth)) {
                oth.TakeDamageServerRpc(GetComponent<NetworkObject>(), DPS * DamageTickRate);
            }
            StartCoroutine(Cooldown());
        }

    }

    protected IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(DamageTickRate);
        _cooldown = false;
    }

    protected void OnDeath() {
        health.LastDamageSource.GetComponent<IHasGold>()?.AddGoldServerRpc(GoldValue);
    }
}
