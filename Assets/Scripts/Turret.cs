using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class Turret : NetworkBehaviour {
    [SerializeField] private GameObject projectile;
    [SerializeField] private float range;
    [SerializeField] private float cooldown;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private TeamColor teamColor;
    [SerializeField] private int goldValue;
    [SerializeField] private Transform[] destroyedTransforms;

    private bool OnCooldown;
    private Health _health;

    protected void Awake() {
        _health = GetComponent<Health>();
        _health.OnDeath += OnDeath;
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) return;
        OnCooldown = false;
    }

    void FixedUpdate() {
        if (!IsServer) return;
        if (OnCooldown) return;
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);

        if (targets.Length > 0) {
            Array.Sort(targets, (x, y) =>
                (int)Mathf.Sign(Mathf.Abs(x.transform.position.x - transform.position.x) - Mathf.Abs(y.transform.position.x - transform.position.x))
            );
            var target = targets[0];
            var proj = GameObject.Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<TurretProjectile>();
            proj.GetComponent<NetworkObject>().Spawn();
            proj.Setup(teamColor, target.transform, transform);
            StartCoroutine(ShotCooldown());
        }
    }

    protected void OnDeath() {
        _health.LastDamageSource.GetComponent<IHasGold>()?.AddGoldServerRpc(goldValue);
        GetComponent<NetworkObject>().Despawn();
        foreach (var tr in destroyedTransforms) {
            tr.GetComponent<NetworkObject>().Despawn();
        }
    }

    IEnumerator ShotCooldown() {
        OnCooldown = true;
        yield return new WaitForSeconds(cooldown);
        OnCooldown = false;
    }
}
