using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public PlayerCharacterController CharacterController { get; private set; }

    void Awake() {
        CharacterController = new PlayerCharacterController();        
        CharacterController.Enable();
    }

}
