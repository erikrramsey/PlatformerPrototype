using UnityEngine;
using Unity.Netcode;

public abstract class Projectile : NetworkBehaviour {
    protected Transform target;
    protected Transform source;
    protected TeamColor teamColor;

    #region UnityCallbacks

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (!IsOwner) return;
        if (!IsSpawned) return;

        switch (LayerMask.LayerToName(other.gameObject.layer)) {
            case "RedHurtbox":
            case "BlueHurtbox":
                OnEnemyCollision(other);
                return;
            case "Environment":
                GetComponent<NetworkObject>().Despawn();
                return;

            case "RedHitbox":
            case "BlueHitbox":
            case "PTEnvironment":
                return;
            default:
                Debug.LogError("Unhandled trigger enter layer");
                return;
        }
    }

    #endregion

    public virtual void Setup(TeamColor _teamColor, Transform _target = null, Transform _source = null) {
        teamColor = _teamColor;

        if (teamColor == TeamColor.red) {
            gameObject.layer = LayerMask.NameToLayer("RedHitbox");
        } else if (teamColor == TeamColor.blue) {
            gameObject.layer = LayerMask.NameToLayer("BlueHitbox");
        } else {
            Debug.LogError("No team color assigned to projectile: " + gameObject.name);
        }

        target = _target;
        source = _source;
    }

    protected virtual void OnEnemyCollision(Collider2D other) {}


}