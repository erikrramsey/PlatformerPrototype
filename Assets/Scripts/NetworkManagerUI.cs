using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Netcode.Transports.Facepunch;
using Unity.Netcode.Transports.UTP;

public class NetworkManagerUI : MonoBehaviour {
    [SerializeField] private GameObject nextUI;

    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostSteamButton;
    [SerializeField] private Button hostUnityButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button steamClientButton;


    private void Start() {
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
        nextUI.SetActive(false);

        serverButton.onClick.AddListener(() => {
            nextUI.SetActive(true);
            gameObject.SetActive(false);
            NetworkManager.Singleton.StartServer();
        });

        hostSteamButton.onClick.AddListener(() => {
            nextUI.SetActive(true);
            gameObject.SetActive(false);
            GameNetworkManager.Singleton.StartHost(4);
        });

        hostUnityButton.onClick.AddListener(() => {
            nextUI.SetActive(true);
            gameObject.SetActive(false);
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            NetworkManager.Singleton.StartHost();
        });

        clientButton.onClick.AddListener(() => {
            nextUI.SetActive(true);
            gameObject.SetActive(false);
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            NetworkManager.Singleton.StartClient();
        });
    }

    public void OnSteamClientJoined() {
        nextUI.SetActive(true);
        gameObject.SetActive(false);
    }
}
