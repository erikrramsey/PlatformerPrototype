using System.Collections.Generic;
using UnityEngine;

public enum Character {
    none = 0,
    fatty = 1,
    pingpong = 2,
    bass,
    healer,
}

public class CharacterList : MonoBehaviour {
    public static CharacterList Singleton { get; private set; } = null;

    [System.Serializable]
    public struct KeyValuePair {
        [SerializeField] public Character key;
        [SerializeField] public GameObject prefab;
    }

    [SerializeField] public List<KeyValuePair> characters;
    Dictionary<Character, GameObject> characterDict = new Dictionary<Character, GameObject>();

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }

        foreach (var ch in characters) {
            characterDict.Add(ch.key, ch.prefab);
        }
    }

    public GameObject Get(Character _char) {
        return characterDict[_char];
    }
}
