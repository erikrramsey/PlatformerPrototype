using UnityEngine;

public interface ITakesDamage {
    public virtual void TakeDamageServerRpc(Vector3 direction, float damage) {
        Debug.LogError("Calling base take damage func, don't do that.");
    }
}