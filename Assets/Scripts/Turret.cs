using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class Turret : NetworkBehaviour, ITakesDamage {
    [SerializeField] private GameObject projectile;
    [SerializeField] private float range;
    [SerializeField] private float cooldown;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private TeamColor teamColor;

    [SerializeField] private RawImage healthBar;
    [SerializeField] private float maxHealth;
    public NetworkVariable<float> currentHealth;

    private bool OnCooldown;

    public override void OnNetworkSpawn() {
        currentHealth.OnValueChanged = CurrentHealth_OnValueChanged;
        if (currentHealth.Value > 0) {
            CurrentHealth_OnValueChanged(currentHealth.Value, currentHealth.Value);
        }

        if (!IsServer) return;
        OnCooldown = false;
        currentHealth.Value = maxHealth;
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        if (OnCooldown) return;
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);

        if (targets.Length > 0) {
            Array.Sort(targets, (x, y) => (int)Mathf.Sign(Mathf.Abs(x.transform.position.x - transform.position.x) - Mathf.Abs(y.transform.position.x - transform.position.x)) );
            var target = targets[0];
            var proj = GameObject.Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<TurretProjectile>();
            proj.GetComponent<NetworkObject>().Spawn();
            proj.Setup(teamColor, target.transform, transform);
            StartCoroutine(ShotCooldown());
        }
    }

    void CurrentHealth_OnValueChanged(float previous, float current) {
        if (current <= 0.0f) {
            GetComponent<NetworkObject>().Despawn();
            return;
        }
        healthBar.rectTransform.localScale = new Vector3(currentHealth.Value / maxHealth, 1.0f, 1.0f);
        healthBar.uvRect = new Rect(0.0f, 0.0f, current / 20.0f, 1.0f);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(Vector3 direction, float damage) {
        Debug.Log("Turret taking damage" + OwnerClientId);
        currentHealth.Value -= damage;
    }

    IEnumerator ShotCooldown() {
        OnCooldown = true;
        yield return new WaitForSeconds(cooldown);
        OnCooldown = false;
    }
}
