using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class PlayerManager : NetworkBehaviour {
    public static PlayerManager Singleton { get; private set; } = null;


    public delegate void OnPlayerInfoChangeDelegate();
    public OnPlayerInfoChangeDelegate OnPlayerInfoChange;

    private NetworkManager _networkManager;

    public NetworkList<PlayerInfo> PlayerList;
    public List<PlayerInfo> RedPlayers { get; private set; }
    public List<PlayerInfo> BluePlayers { get; private set; }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerInfoServerRPC(PlayerInfo info) {
        PlayerList[GetPlayerIndex(info.id)] = info;
    }

    public PlayerInfo GetLocalPlayerInfo() {
        return PlayerList[GetPlayerIndex(NetworkManager.Singleton.LocalClientId)];
    }

    public int GetPlayerIndex(ulong id) {
        for (int i = 0; i < PlayerList.Count; i++) {
            if (PlayerList[i].id == id) return i;
        }

        return -1;
    }

    void Awake() {
        Debug.Log("PlayerManager Awake");
        PlayerList = new NetworkList<PlayerInfo>();
        PlayerList.OnListChanged += OnPlayerListChanged;
        RedPlayers = new List<PlayerInfo>();
        BluePlayers = new List<PlayerInfo>();


        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    public override void OnNetworkSpawn() {
        if (!IsOwner) return;
        _networkManager = NetworkManager.Singleton;
        _networkManager.OnClientConnectedCallback += PlayerConnected;
        _networkManager.OnClientDisconnectCallback += PlayerDisconnected;

        if (!IsHost) return;
        Debug.Log("Spawning PlayerManager");

        PlayerList.Clear();
        RedPlayers.Clear();
        BluePlayers.Clear();
    }

    public void Shutdown() {
        if (!IsOwner) return;
        _networkManager.OnClientConnectedCallback -= PlayerConnected;
        _networkManager.OnClientDisconnectCallback -= PlayerDisconnected;

        if (!IsHost) return;
        Debug.Log("Shutting Down PlayerManager");

        PlayerList.Clear();
        RedPlayers.Clear();
        BluePlayers.Clear();
    }

    void PlayerConnected(ulong id) {
        var player = new PlayerInfo(id, TeamColor.none, Character.none);
        PlayerList.Add(player);
    }

    void PlayerDisconnected(ulong id) {
        PlayerList.RemoveAt(GetPlayerIndex(id));
    }

    void OnPlayerListChanged(NetworkListEvent<PlayerInfo> changeEvent) {
        RedPlayers.Clear();
        BluePlayers.Clear();

        foreach (var player in PlayerList) {
            if (player.teamColor == TeamColor.red) RedPlayers.Add(player);
            if (player.teamColor == TeamColor.blue) BluePlayers.Add(player);
        }

        if (OnPlayerInfoChange != null)
            OnPlayerInfoChange();
    }
}