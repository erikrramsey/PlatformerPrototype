using System.Collections.Generic;
using UnityEngine;

public enum Debuff {
    None = 0,

    Stun,
    JumpSlow,
}

public class DebuffList : MonoBehaviour {
    public static DebuffList Singleton { get; private set; } = null;

    [System.Serializable]
    struct KeyValuePair {
        [SerializeField] public Debuff key;
        [SerializeField] public GameObject prefab;
    }

    [SerializeField] private List<KeyValuePair> debuffs;
    Dictionary<Debuff, GameObject> debuffDict = new Dictionary<Debuff, GameObject>();

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }

        foreach (var dbf in debuffs) {
            debuffDict.Add(dbf.key, dbf.prefab);
        }
    }

    public GameObject Get(Debuff _dbf) {
        return debuffDict[_dbf];
    }
}
