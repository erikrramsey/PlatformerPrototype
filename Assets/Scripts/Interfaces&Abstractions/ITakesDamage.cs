using UnityEngine;

public interface ITakesDamage {
    public void TakeDamageServerRpc(Vector3 direction, float damage);
}