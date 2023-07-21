using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;
using System;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Singleton { get; private set; } = null;
    
    private FacepunchTransport transport = null;

    public Lobby? currentLobby { get; private set; } = null;

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    void Start() {
        transport = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += SteamMatchmaking_OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += SteamMatchmaking_OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += SteamMatchmaking_OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += SteamMatchmaking_OnLobbyGameCreated;

        SteamFriends.OnGameLobbyJoinRequested += SteamMatchmaking_OnGameLobbyJoinRequested;

    }

    void OnDestroy() {

        SteamMatchmaking.OnLobbyCreated -= SteamMatchmaking_OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= SteamMatchmaking_OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= SteamMatchmaking_OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= SteamMatchmaking_OnLobbyGameCreated;

        SteamFriends.OnGameLobbyJoinRequested -= SteamMatchmaking_OnGameLobbyJoinRequested;

        if (NetworkManager.Singleton == null) {
            return;
        }

        NetworkManager.Singleton.OnServerStarted -= Singleton_OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
    }

    private void OnApplicationQuit() {
        Disconnected();
    }

    private async void SteamMatchmaking_OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        RoomEnter joinedLobby = await lobby.Join();
        if (joinedLobby != RoomEnter.Success) {
            Debug.Log("Failed to join lobby");
        } else {
            currentLobby = lobby;
            Debug.Log("Joined steam lobby");
        }
    }

    private void SteamMatchmaking_OnLobbyGameCreated(Lobby lobby, uint arg2, ushort arg3, SteamId id)
    {
        Debug.Log("Lobby was created");
    }

    private void SteamMatchmaking_OnLobbyInvite(Friend friend, Lobby lobby)
    {
        Debug.Log($"Invite from {friend.Name}");
    }

    private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        Debug.Log("Member left");
    }

    private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log("Member joined");
    }

    private void SteamMatchmaking_OnLobbyEntered(Lobby lobby)
    {
        if (NetworkManager.Singleton.IsHost) {
            return;
        }

        StartClient(currentLobby.Value.Owner.Id);
    }

    private void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK) {
            Debug.Log("Lobby not created");
            return;
        }

        lobby.SetPublic();
        lobby.SetJoinable(true);
        lobby.SetGameServer(lobby.Owner.Id);

        SteamFriends.OpenGameInviteOverlay(currentLobby.Value.Id);
    }

    public async void StartHost(int maxMembers) {
        NetworkManager.Singleton.OnServerStarted += Singleton_OnServerStarted;
        NetworkManager.Singleton.StartHost();
        currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    private void Singleton_OnServerStarted() {
        Debug.Log("Hosting started");
    }

    public void StartClient(SteamId sId) {
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
        transport.targetSteamId = sId;
        if (NetworkManager.Singleton.StartClient()) {
            Debug.Log("Client has started");
        }
    }

    public void Disconnected() {
        currentLobby?.Leave();
        if (NetworkManager.Singleton ==  null) {
            return;
        }

        if (NetworkManager.Singleton.IsHost) {
            NetworkManager.Singleton.OnServerStarted -= Singleton_OnServerStarted;
        } else {
            NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
        }

        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Disconnected");
    }

    private void Singleton_OnClientConnectedCallback(ulong obj) {
        GameObject.FindObjectOfType<NetworkManagerUI>().OnSteamClientJoined();
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj) {

    }


}
