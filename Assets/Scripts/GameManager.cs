using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject Music;
    [SerializeField] private GameObject MeleeCreep;

    public List<Player> players;

    public static GameManager Singleton { get; private set; } = null;

    public Player localPlayer;
    NetworkVariable<int> redPlayers = new NetworkVariable<int>();
    NetworkVariable<int> bluePlayers = new NetworkVariable<int>();

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }

        redPlayers.OnValueChanged += OnRedPlayers_ValueChanged;
        bluePlayers.OnValueChanged += OnBluePlayers_ValueChanged;
    }



    IEnumerator SpawnCreeps() {
        Transform redSpawn = GameObject.Find("RedSpawn").transform;
        Transform blueSpawn = GameObject.Find("BlueSpawn").transform;
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

    public void OnRedPlayers_ValueChanged(int previous, int current) {
        if (UI.GetComponentInChildren<TeamPickUIController>() == null) return;
        UI.GetComponentInChildren<TeamPickUIController>().SetRed(current);
        CheckAllReady();
    }

    public void OnBluePlayers_ValueChanged(int previous, int current) {
        if (UI.GetComponentInChildren<TeamPickUIController>() == null) return;
        UI.GetComponentInChildren<TeamPickUIController>().SetBlue(current);
        CheckAllReady();
    }

    public void CheckAllReady() {
        /*
        if (redPlayers.Value + bluePlayers.Value == players.Count) {
            UI.GetComponentInChildren<CharacterPickUIController>().ReadyToStart(true);
        } else {
            UI.GetComponentInChildren<CharacterPickUIController>().ReadyToStart(false);
        }
        */
    }

    public void AddPlayer(Player player) {
        players.Add(player);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc() {
        StartGameClientRpc();

        if (!IsServer) return;

        StartCoroutine(SpawnCreeps());
    }

    [ServerRpc(RequireOwnership=false)]
    public void AddRedPlayerServerRpc() {
        redPlayers.Value++;
    }

    [ServerRpc(RequireOwnership=false)]
    public void AddBluePlayerServerRpc() {
        bluePlayers.Value++;
    }

    [ServerRpc(RequireOwnership=false)]
    public void RemoveRedPlayerServerRpc() {
        redPlayers.Value--;
    }

    [ServerRpc(RequireOwnership=false)]
    public void RemoveBluePlayerServerRpc() {
        bluePlayers.Value--;
    }

    [ClientRpc]
    public void StartGameClientRpc() {
        Debug.Log("Game started");
        var cui = GameObject.Find("UI").GetComponentInChildren<CharacterPickUIController>();
        if (cui) cui.OnGameStarted();

        localPlayer.SpawnPlayerServerRPC();

        GameObject.Instantiate(Music);
    }
}
