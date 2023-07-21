using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour {
    [SerializeField] private CharacterList _characterList;

    private GameObject UI;
    private Vector3 redSpawn;
    private Vector3 blueSpawn;

    public NetworkVariable<TeamColor> team = new NetworkVariable<TeamColor>();
    public NetworkVariable<Character> characterSelect = new NetworkVariable<Character>();

    public override void OnNetworkSpawn() {
        
        redSpawn = GameObject.Find("RedSpawn").transform.position;
        blueSpawn = GameObject.Find("BlueSpawn").transform.position;
        GameManager.Singleton.AddPlayer(this);

        if (!IsOwner) return;
        _characterList.Init();
        UI = GameObject.Find("UI");
        GameManager.Singleton.localPlayer = this;

        team.OnValueChanged += OnTeamValueChanged;
        
        base.OnNetworkSpawn();
    }

    public void OnTeamValueChanged(TeamColor previous, TeamColor current) {
        Debug.Log("Team color changed on " + OwnerClientId);
        if (previous == TeamColor.red) {
            GameManager.Singleton.RemoveRedPlayerServerRpc();
        } else if (previous == TeamColor.blue) {
            GameManager.Singleton.RemoveBluePlayerServerRpc();
        }

        if (current == TeamColor.red) {
            GameManager.Singleton.AddRedPlayerServerRpc();
        } else if (current == TeamColor.blue) {
            GameManager.Singleton.AddBluePlayerServerRpc();
        }
    }

    public void OnGameStart() {
        Debug.Log("Game starting on player" + OwnerClientId + " Character is: " + characterSelect.Value.ToString());
        GameManager.Singleton.StartGameServerRpc();
    }

    [ClientRpc]
    public void SpawnPlayerClientRpc() {
        if (!IsOwner) return;
        Debug.Log("Spawn player client rpc on client" + OwnerClientId);
        SpawnPlayerServerRPC();
    }

    [ServerRpc]
    public void SpawnPlayerServerRPC() {
        Debug.Log("Spawning Player server side" + OwnerClientId + " Team: " + team.Value);
        var character = GameObject.Instantiate(_characterList.Get(characterSelect.Value)).GetComponent<PlayerCharacter>();
        character.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);


        if (team.Value == TeamColor.red) {
            character.SetPositionClientRpc(redSpawn);
            character.teamColor.Value = TeamColor.red;
        } else if (team.Value == TeamColor.blue) {
            character.SetPositionClientRpc(blueSpawn);
            character.teamColor.Value = TeamColor.blue;
        } else {
            Debug.LogError("invalid team value");
        }
    }

    [ServerRpc]
    public void SetTeamServerRpc(TeamColor _team) {
        team.Value = _team;
    }

    [ServerRpc]
    public void SetCharacterServerRpc(Character _character) {
        characterSelect.Value = _character;
    }
}
