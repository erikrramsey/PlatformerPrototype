using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum StatType {
    HorizontalSpeed,
    HorizontalAccel,
    HorizontalDecel,
    JumpForce,
    MaxHealth,
    JumpDampForce,
    Skill1Cooldown,
    Skill2Cooldown,
}

[System.Serializable]
public class Stats {
    [System.Serializable]
    private struct statValue {
        public StatType type;
        public float value;
    }

    [SerializeField] private List<statValue> baseStatsSerialized;

    Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
    Dictionary<StatType, float> multiModifiers = new Dictionary<StatType, float>();
    Dictionary<StatType, float> addModifiers = new Dictionary<StatType, float>();

    public void Initialize() {
        foreach (var stat in baseStatsSerialized) {
            baseStats.Add(stat.type, stat.value);
            multiModifiers.Add(stat.type, 1.0f);
            addModifiers.Add(stat.type, 0.0f);
        }
    }

    public float GetStat(StatType type) {
        return (baseStats[type] + addModifiers[type]) * multiModifiers[type];
    }

    public void AddToAddMod(StatType type, float value) {
        addModifiers[type] += value;
    }

    public void AddToMultiMod(StatType type, float value) {
        multiModifiers[type] += value;
    }

    public float GetBaseStat(StatType type) { return baseStats[type]; }
    public float GetMulti(StatType type) { return multiModifiers[type]; }
    public float GetAdd(StatType type) { return addModifiers[type]; }
}