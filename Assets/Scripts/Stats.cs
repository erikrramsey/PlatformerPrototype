using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public enum StatType {
    HorizontalSpeed,
    HorizontalAccel,
    HorizontalDecel,
    JumpForce,
    MaxHealth,
    JumpDampForce,
    Skill1Cooldown,
    Skill2Cooldown,
    Skill3Cooldown,
    Armor,
    DamageMultiplier,
}

public class Stats : NetworkBehaviour {
    [System.Serializable]
    public struct StatValue {
        public StatType type;
        public float value;
    }

    [SerializeField] StatValue[] statValuesArray;
    Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
    Dictionary<StatType, float> effectiveStats = new Dictionary<StatType, float>();
    public Action OnStatChange;

    public void Awake() {
        foreach (var stat in statValuesArray) {
            baseStats.Add(stat.type, stat.value);
            effectiveStats.Add(stat.type, stat.value);
        }
    }

    public void AddItem(HashSet<Item> items) {
        Dictionary<StatType, float> cummValue = new Dictionary<StatType, float>(baseStats);
        foreach (var item in items) {
            foreach (var af in item.affectedStats) {
                float prevVal = 0;
                cummValue.TryGetValue(af.Type, out prevVal);
                cummValue[af.Type] = prevVal + af.Value;
            }
        }

        foreach(var stat in cummValue) {
            SetStatServerRpc(stat.Key, stat.Value);
        }
    }

    [ServerRpc(RequireOwnership=false)]
    public void SetStatServerRpc(StatType type, float value) {
        SetStatClientRpc(type, value);
    }

    [ClientRpc]
    public void SetStatClientRpc(StatType type, float value) {
        Debug.Log("Stat changed: " + type + " " + value);
        effectiveStats[type] = value;
        OnStatChange();
    }

    public float GetStat(StatType type) {
        return effectiveStats[type];
    }
}
