using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Unity.Netcode.Transports.UTP;

public class BaseMenu : MonoBehaviour {
    [SerializeField] private Button HostSteamButton;
    [SerializeField] private Button HostUnityButton;
    [SerializeField] private Button JoinUnityButton;

    private NetworkManager _networkManager;

    void Start() {
        _networkManager = NetworkManager.Singleton;

        SteamMatchmaking.OnLobbyEntered += (lobby) => {
            MainMenuUI.Singleton.PushMenu(MainMenuUI.Singleton.LobbyMenu);
        };

        HostSteamButton.onClick.AddListener(() => {
            SteamNetworkManager.Singleton.StartHost(4);
        });

        HostUnityButton.onClick.AddListener(() => {
            _networkManager.NetworkConfig.NetworkTransport = _networkManager.GetComponent<UnityTransport>();
            _networkManager.StartHost();
            MainMenuUI.Singleton.PushMenu(MainMenuUI.Singleton.LobbyMenu);
        });

        JoinUnityButton.onClick.AddListener(() => {
            _networkManager.NetworkConfig.NetworkTransport = _networkManager.GetComponent<UnityTransport>();
            _networkManager.StartClient();
            MainMenuUI.Singleton.PushMenu(MainMenuUI.Singleton.LobbyMenu);
        });
    }
}
