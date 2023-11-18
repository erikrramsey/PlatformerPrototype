using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class Base : NetworkBehaviour {
    [SerializeField] TeamColor teamColor;

    public override void OnNetworkDespawn() {
        if (!IsServer) return;
        GameplayManager.Singleton.FinishGameServerRpc(teamColor);
    }
}
