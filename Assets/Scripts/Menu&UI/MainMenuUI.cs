using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Steamworks.Data;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour {
    [SerializeField] string GameScene;

    [SerializeField] public GameObject MainMenu;
    [SerializeField] public GameObject LobbyMenu;

    private Stack<GameObject> menuStack = new Stack<GameObject>();

    public static MainMenuUI Singleton { get; private set; } = null;

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    void Start() {
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        menuStack.Push(MainMenu);
        menuStack.Peek().gameObject.SetActive(true);
    }

    public void PushMenu(GameObject menu) {
        menuStack.Peek().gameObject.SetActive(false);
        menuStack.Push(menu); 
        menuStack.Peek().gameObject.SetActive(true);
    }

    public void PopMenu() {
        if (menuStack.Count == 1) {
            Debug.LogError("Trying to pop main menu! Don't do that!");
            return;
        }

        menuStack.Peek().gameObject.SetActive(false);
        menuStack.Pop(); 
        menuStack.Peek().gameObject.SetActive(true);
    }

}
