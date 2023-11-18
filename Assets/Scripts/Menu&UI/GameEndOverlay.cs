using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;

public class GameEndOverlay : MonoBehaviour {
    [SerializeField] TMP_Text winnerText;
    [SerializeField] Button returnToMenuButton;

    void Start() {
        gameObject.SetActive(false);
        GameplayManager.Singleton.OnGameEndEvent += (loser) => {
            gameObject.SetActive(true);
            if (loser == TeamColor.red) {
                winnerText.text = "Blue Team Wins";
            } else if (loser == TeamColor.blue) {
                winnerText.text = "Red Team Wins";
            }
        };

        returnToMenuButton.onClick.AddListener(() => {
            PlayerManager.Singleton.Shutdown();
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenuScene");
        });
    }
}