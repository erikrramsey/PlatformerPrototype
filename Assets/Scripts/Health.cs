using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Health : NetworkBehaviour, ITakesDamage {
    [SerializeField] private RawImage healthBarImage;
    [SerializeField] private float maxHealth;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();

    public override void OnNetworkSpawn() {
        currentHealth.OnValueChanged += CurrentHealth_OnValueChanged;
        if (currentHealth.Value > 0) {
            CurrentHealth_OnValueChanged(currentHealth.Value, currentHealth.Value);
        }

        if (!IsServer) return;
        currentHealth.Value = maxHealth;
    }

    void CurrentHealth_OnValueChanged(float previous, float current) {
        if (current <= 0.0f) {
            GetComponent<NetworkObject>().Despawn();
            return;
        }
        healthBarImage.rectTransform.localScale = new Vector3(currentHealth.Value / maxHealth, 1.0f, 1.0f);
        healthBarImage.uvRect = new Rect(0.0f, 0.0f, current / 20.0f, 1.0f);
    } 
    
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(Vector3 force, float damage) {
        currentHealth.Value -= damage;
    }
}
