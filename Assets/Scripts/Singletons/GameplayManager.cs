using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameplayManager : NetworkBehaviour
{
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject MeleeCreep;
    [SerializeField] private GameObject PlayerCharacterPrefab;

    [SerializeField] private Transform redSpawn;
    [SerializeField] private Transform blueSpawn;

    [SerializeField] private float GoldTickRate;

    [SerializeField] private GameObject StageHazardPrefab;
    [SerializeField] private Transform StageHazardLocationParent;

    public static GameplayManager Singleton { get; private set; } = null;

    public event Action<TeamColor> OnGameEndEvent;
    private int clientsLoaded;
    private int clientsSpawned;

    private Dictionary<ulong, PlayerCharacter> playerCharacters = new Dictionary<ulong, PlayerCharacter>();

    [ServerRpc]
    public void FinishGameServerRpc(TeamColor loser) {
        FinishGameClientRpc(loser);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnClientLoadedServerRpc(ServerRpcParams serverRpcParams = default) {
        clientsSpawned++;
        if (clientsSpawned == PlayerManager.Singleton.PlayerList.Count) {
            StartSystems();
        }
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

        clientsLoaded = 0;
        clientsSpawned = 0;
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    void OnSceneEvent(SceneEvent sceneEvent) {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete) {
            clientsLoaded++;
            Debug.Log("Clients loaded " + clientsLoaded);
            Debug.Log("Count " + PlayerManager.Singleton.PlayerList.Count);
            if (clientsLoaded == PlayerManager.Singleton.PlayerList.Count) {
                foreach (var player in PlayerManager.Singleton.PlayerList) {
                    SpawnPlayerCharacter(player.id);
                }
            }
        }
    }

    private void StartSystems() {
        StartCoroutine(SpawnCreeps());
        StartCoroutine(GenerateGold());
        StartCoroutine(SpawnStageHazards());
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

    IEnumerator GenerateGold() {
        while (true) {
            foreach (var player in playerCharacters) {
                player.Value.currentGold.Value++;
                player.Value.OnClientsSpawnedServerRpc();
            }

            yield return new WaitForSeconds(GoldTickRate);
        }
    }

    IEnumerator SpawnStageHazards() {
        while (true) {
            foreach (Transform tr in StageHazardLocationParent.transform) {
                if (tr.childCount == 0) {
                    var sh = GameObject.Instantiate(StageHazardPrefab);
                    sh.GetComponent<NetworkObject>().Spawn();
                    sh.GetComponent<NetworkObject>().TrySetParent(tr, false);
                }
            }
            yield return new WaitForSeconds(60.0f);
        }
    }

    void SpawnPlayerCharacter(ulong clientId) {
        var player = PlayerManager.Singleton.PlayerList[PlayerManager.Singleton.GetPlayerIndex(clientId)];
        var ch = GameObject.Instantiate(PlayerCharacterPrefab).GetComponent<PlayerCharacter>();
        ch.GetComponent<NetworkObject>().SpawnWithOwnership(player.id);
        
        if (player.teamColor == TeamColor.red) {
            ch.teamColor.Value = TeamColor.red;
        } else if (player.teamColor == TeamColor.blue) {
            ch.teamColor.Value = TeamColor.blue;
        } else {
            Debug.LogError("invalid team value");
        }

        playerCharacters.Add(clientId, ch);
    }

    public Transform GetSpawn(TeamColor color) {
        return color == TeamColor.red? redSpawn : blueSpawn;
    }
}
