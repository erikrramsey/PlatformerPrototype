using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public PlayerCharacterController CharacterController { get; private set; }
    public static PlayerInput Singleton { get; private set; }

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }

        CharacterController = new PlayerCharacterController();        
        CharacterController.Enable();
    }

}
