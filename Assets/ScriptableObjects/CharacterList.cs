using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "characterList", menuName = "ScriptableObjects/CharacterList", order = 1)]
public class CharacterList : ScriptableObject {
    [SerializeField] private List<Character> values;
    [SerializeField] private List<GameObject> characterPrefabs;

    Dictionary<Character, GameObject> list = new Dictionary<Character, GameObject>();

    public void Init() {
        for (int i = 0; i < values.Count; i++) {
            list.Add(values[i], characterPrefabs[i]);
        }
    }

    public GameObject Get(Character _char) {
        return list[_char];
    }
}
