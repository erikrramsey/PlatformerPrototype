using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CharacterPickUIController : MonoBehaviour {
    [SerializeField] private GameObject nextUI;

    [SerializeField] private Transform characterLayoutGroup;
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private Button startGame;

    void Start() {
        foreach (Character i in Enum.GetValues(typeof(Character))) {
            string name = Enum.GetName(typeof(Character), i);
            if (name.Equals("none")) continue;

            GameObject panel = GameObject.Instantiate(characterSelectPanel, characterLayoutGroup.transform);
            panel.GetComponentInChildren<TMP_Text>().text = name;
            var button = panel.GetComponent<Button>();
            button.onClick.AddListener(() => {
                GameManager.Singleton.localPlayer.SetCharacterServerRpc(i);
            });
        } 

        startGame.onClick.AddListener(() => {
            nextUI.gameObject.SetActive(true);

            GameManager.Singleton.localPlayer.OnGameStart();

            gameObject.SetActive(false);
        });
    }

    public void OnGameStarted() {
        nextUI.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }


    public void ReadyToStart(bool val) {
        startGame.gameObject.SetActive(val);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
