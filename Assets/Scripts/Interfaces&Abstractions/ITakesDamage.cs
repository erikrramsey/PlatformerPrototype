using UnityEngine;
using Unity.Netcode;

public interface ITakesDamage {
    public abstract void TakeDamageServerRpc(NetworkObjectReference dealer, float damage);
    public void TakeDamageServerRpc(float damage) {
        Debug.LogError("Calling base take damage function.");
    }
}