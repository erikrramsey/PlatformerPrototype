using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Health : NetworkBehaviour, ITakesDamage {
    [SerializeField] private RawImage healthBarImage;
    [SerializeField] private float maxHealth;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    public Action OnDeath;

    public Transform LastDamageSource;

    public void Awake() {
        currentHealth.Value = maxHealth;
    }

    public override void OnNetworkSpawn() {
        currentHealth.OnValueChanged += CurrentHealth_OnValueChanged;
        if (currentHealth.Value > 0) {
            CurrentHealth_OnValueChanged(currentHealth.Value, currentHealth.Value);
        }

        if (!IsServer) return;
        currentHealth.Value = maxHealth;
    }

    void CurrentHealth_OnValueChanged(float previous, float current) {
        healthBarImage.rectTransform.localScale = new Vector3(currentHealth.Value / maxHealth, 1.0f, 1.0f);
        healthBarImage.uvRect = new Rect(0.0f, 0.0f, current / 20.0f, 1.0f);

        if (!IsOwner) return;
        if (current <= 0.0f) {
            OnDeath?.Invoke();
            GetComponent<NetworkObject>().Despawn();
            return;
        }
    } 
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(NetworkObjectReference dealer, float damage) {
        if (dealer.TryGet(out NetworkObject dealerObj)) {
            LastDamageSource = dealerObj.transform;
        }

        TakeDamageServerRpc(damage);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage) {
        if (!IsSpawned) return;
        currentHealth.Value -= damage;
        if (currentHealth.Value > maxHealth) currentHealth.Value = maxHealth;
    }
}
