using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class BaseMenu : MonoBehaviour {
    [SerializeField] private Button HostUnityButton;
    [SerializeField] private Button QuitButton;

    private NetworkManager _networkManager;

    void Start() {
        _networkManager = NetworkManager.Singleton;

        HostUnityButton.onClick.AddListener(() => {
            _networkManager.NetworkConfig.NetworkTransport = _networkManager.GetComponent<UnityTransport>();
            _networkManager.StartHost();

            var info = PlayerManager.Singleton.GetLocalPlayerInfo();
            info.teamColor = TeamColor.red;
            PlayerManager.Singleton.SetPlayerInfoServerRPC(info);
            NetworkManager.Singleton.SceneManager.LoadScene("GameplayScene", LoadSceneMode.Single);
        });

        QuitButton.onClick.AddListener(() => {
            Application.Quit();
        });
    }
}
