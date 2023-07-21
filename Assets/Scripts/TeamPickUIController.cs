using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class TeamPickUIController : MonoBehaviour {
    [SerializeField] private GameObject nextUI;

    [SerializeField] private Button redButton;
    [SerializeField] private Button blueButton;
    [SerializeField] private TMP_Text playerCount;
    [SerializeField] private TMP_Text redCount;
    [SerializeField] private TMP_Text blueCount;

    void Start() {
        redButton.onClick.AddListener(() => {
            GameManager.Singleton.localPlayer.SetTeamServerRpc(TeamColor.red);
            nextUI.gameObject.SetActive(true);
            gameObject.SetActive(false);
        });

        blueButton.onClick.AddListener(() => {
            GameManager.Singleton.localPlayer.SetTeamServerRpc(TeamColor.blue);
            nextUI.gameObject.SetActive(true);
            gameObject.SetActive(false);
        });

    }


    public void SetRed(int value) {
        redCount.text = value.ToString();
    }

    public void SetBlue(int value) {
        blueCount.text = value.ToString();
    }

    public void SetPlayers(int value) {
        playerCount.text = value.ToString();
    }
}
