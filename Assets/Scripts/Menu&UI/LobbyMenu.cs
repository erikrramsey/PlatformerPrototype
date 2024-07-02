using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyMenu : MonoBehaviour {
    [SerializeField] private TMP_Text playerText;

    [SerializeField] private GameObject characterIconPrefab;
    [SerializeField] private GameObject playerIconPrefab;

    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button joinRedButton;
    [SerializeField] private Button joinBlueButton;

    /*
    [SerializeField] private RectTransform redCharacterLayout;
    [SerializeField] private RectTransform blueCharacterLayout;
    */
    [SerializeField] private RectTransform redPlayerLayout;
    [SerializeField] private RectTransform bluePlayerLayout;

    
    private PlayerManager _playerManager;
    private Dictionary<Character, Button> redButtonLookup = new Dictionary<Character, Button>();
    private Dictionary<Character, Button> blueButtonLookup = new Dictionary<Character, Button>();
    private List<Button> allButtons = new List<Button>();

    void Start() {
        /*
        foreach (var ch in CharacterList.Singleton.characters) {
            var redPanel = GameObject.Instantiate(characterIconPrefab, redCharacterLayout);

            redPanel.GetComponentInChildren<TMP_Text>().text = ch.key.ToString();

            var rb = redPanel.GetComponentInChildren<Button>();
            redButtonLookup.Add(ch.key, rb);
            rb.onClick.AddListener(() => {
                var info = _playerManager.GetLocalPlayerInfo();
                Debug.Log("On click: " + info.id);
                info.character = ch.key;
                info.teamColor = TeamColor.red;
                _playerManager.SetPlayerInfoServerRPC(info);
            });

            var bluePanel = GameObject.Instantiate(characterIconPrefab, blueCharacterLayout);

            bluePanel.GetComponentInChildren<TMP_Text>().text = ch.key.ToString();

            var bb = bluePanel.GetComponentInChildren<Button>();
            blueButtonLookup.Add(ch.key, bb);

            bb.onClick.AddListener(() => {
                var info = _playerManager.GetLocalPlayerInfo();
                info.character = ch.key;
                info.teamColor = TeamColor.blue;
                _playerManager.SetPlayerInfoServerRPC(info);
            });

            allButtons.Add(rb);
            allButtons.Add(bb);
        }
        */

        joinRedButton.onClick.AddListener(() => {
            var info = _playerManager.GetLocalPlayerInfo();
            info.teamColor = TeamColor.red;
            _playerManager.SetPlayerInfoServerRPC(info);
        });

        joinBlueButton.onClick.AddListener(() => {
            var info = _playerManager.GetLocalPlayerInfo();
            info.teamColor = TeamColor.blue;
            _playerManager.SetPlayerInfoServerRPC(info);
        });

        leaveButton.onClick.AddListener(() => {
            _playerManager.Shutdown();
            NetworkManager.Singleton.Shutdown();
            MainMenuUI.Singleton.PopMenu();
        });

        startButton.onClick.AddListener(() => {
            NetworkManager.Singleton.SceneManager.LoadScene("GameplayScene", LoadSceneMode.Single);
        });

        _playerManager = PlayerManager.Singleton;
        _playerManager.OnPlayerInfoChange += OnPlayerInfoChange;
        startButton.interactable = false;
    }

    void OnEnable() {
        if (_playerManager != null) {
            OnPlayerInfoChange();
        }
    }

    void OnDestroy() {
        Debug.Log("Destroying lobby");
        _playerManager.OnPlayerInfoChange -= OnPlayerInfoChange;
    }

    void OnPlayerInfoChange() {
        playerText.text = "Players: " + _playerManager.PlayerList.Count.ToString();

        foreach (var but in allButtons) {
            but.interactable = true;
        }

        foreach (Transform child in redPlayerLayout.transform) {
            Destroy(child.gameObject);
        }

        foreach (Transform child in bluePlayerLayout.transform) {
            Destroy(child.gameObject);
        }


        bool readyToStart = _playerManager.RedPlayers.Count + _playerManager.BluePlayers.Count == PlayerManager.Singleton.PlayerList.Count;

        foreach (var player in _playerManager.RedPlayers) {
            //redButtonLookup[player.character].interactable = false;
            var picon = GameObject.Instantiate(playerIconPrefab, redPlayerLayout).transform;
            picon.Find("PlayerText").GetComponent<TMP_Text>().text = "P" + player.id.ToString();
            //picon.Find("CharacterPanel/CharacterText").GetComponent<TMP_Text>().text = player.character.ToString();

            readyToStart = readyToStart && player.teamColor != TeamColor.none;
        }

        foreach (var player in _playerManager.BluePlayers) {
            //blueButtonLookup[player.character].interactable = false;
            var picon = GameObject.Instantiate(playerIconPrefab, bluePlayerLayout).transform;
            picon.Find("PlayerText").GetComponent<TMP_Text>().text = "P" + player.id.ToString();
            //picon.Find("CharacterPanel/CharacterText").GetComponent<TMP_Text>().text = player.character.ToString();

            readyToStart = readyToStart && player.teamColor != TeamColor.none;
        }

        if (NetworkManager.Singleton.IsHost) {
            startButton.GetComponentInChildren<TMP_Text>().text = "Start game";
            startButton.interactable = readyToStart;
        } else {
            startButton.GetComponentInChildren<TMP_Text>().text = "Not host :(";
            startButton.interactable = false;
        }
    }
}
