using UnityEngine;

public interface ITakesDamage {
    public abstract void TakeDamageServerRpc(float damage);
}