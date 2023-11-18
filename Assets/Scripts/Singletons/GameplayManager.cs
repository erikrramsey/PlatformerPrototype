using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameplayManager : NetworkBehaviour
{
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject MeleeCreep;

    [SerializeField] private Transform redSpawn;
    [SerializeField] private Transform blueSpawn;

    public static GameplayManager Singleton { get; private set; } = null;

    public event Action<TeamColor> OnGameEndEvent;

    [ServerRpc]
    public void FinishGameServerRpc(TeamColor loser) {
        FinishGameClientRpc(loser);
    }

    [ClientRpc]
    private void FinishGameClientRpc(TeamColor loser) {
        OnGameEndEvent?.Invoke(loser);
    }

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;

        StartCoroutine(SpawnCreeps());
    }

    void OnSceneEvent(SceneEvent sceneEvent) {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete) {
            SpawnPlayerCharacter(sceneEvent.ClientId);
        }

    }


    IEnumerator SpawnCreeps() {
        while (true) {
            CreepMelee creep;

            creep = GameObject.Instantiate(MeleeCreep, redSpawn.position, Quaternion.identity).GetComponent<CreepMelee>();
            creep.GetComponent<NetworkObject>().Spawn();
            creep.SetTeamServerRpc(TeamColor.red);


            creep = GameObject.Instantiate(MeleeCreep, blueSpawn.position, Quaternion.identity).GetComponent<CreepMelee>();
            creep.GetComponent<NetworkObject>().Spawn();
            creep.SetTeamServerRpc(TeamColor.blue);

            yield return new WaitForSeconds(1.0f);
            
            creep = GameObject.Instantiate(MeleeCreep, redSpawn.position, Quaternion.identity).GetComponent<CreepMelee>();
            creep.GetComponent<NetworkObject>().Spawn();
            creep.SetTeamServerRpc(TeamColor.red);


            creep = GameObject.Instantiate(MeleeCreep, blueSpawn.position, Quaternion.identity).GetComponent<CreepMelee>();
            creep.GetComponent<NetworkObject>().Spawn();
            creep.SetTeamServerRpc(TeamColor.blue);

            yield return new WaitForSeconds(60.0f);
        }
    }

    void SpawnPlayerCharacter(ulong clientId) {
        Debug.Log("Client loaded: " + clientId);

        var player = PlayerManager.Singleton.PlayerList[PlayerManager.Singleton.GetPlayerIndex(clientId)];
        var ch = GameObject.Instantiate(CharacterList.Singleton.Get(player.character)).GetComponent<PlayerCharacter>();
        ch.GetComponent<NetworkObject>().SpawnWithOwnership(player.id);
        
        if (player.teamColor == TeamColor.red) {
            ch.SetPositionClientRpc(redSpawn.position);
            ch.teamColor.Value = TeamColor.red;
        } else if (player.teamColor == TeamColor.blue) {
            ch.SetPositionClientRpc(blueSpawn.position);
            ch.teamColor.Value = TeamColor.blue;
        } else {
            Debug.LogError("invalid team value");
        }
    }
}
