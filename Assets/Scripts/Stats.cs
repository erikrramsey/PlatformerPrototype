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
}

public class Stats : NetworkBehaviour {
    [System.Serializable]
    public struct StatValue : INetworkSerializable, IEquatable<StatValue> {
        public StatType type;
        public float value;

        public StatValue(StatType _type, float _value) {
            type = _type;
            value = _value;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref value);
        }

        public bool Equals(StatValue other) {
            return type == other.type;
        }
    }

    [SerializeField] private StatValue[] baseStatsSerialized;

    Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
    Dictionary<StatType, float> multiModifiers = new Dictionary<StatType, float>();
    Dictionary<StatType, float> addModifiers = new Dictionary<StatType, float>();

    Dictionary<StatType, int> NetworkIndex = new Dictionary<StatType, int>();
    public NetworkList<StatValue> EffectiveValues;

    public Action OnStatChange;

    public void Awake() {
        EffectiveValues = new NetworkList<StatValue>();
        EffectiveValues.OnListChanged += OnEffectiveValuesChanged;
    }

    public void Initialize() {
        Debug.Log("init stats");
        int index = 0;
        foreach (var stat in baseStatsSerialized) {
            baseStats.Add(stat.type, stat.value);
            multiModifiers.Add(stat.type, 1.0f);
            addModifiers.Add(stat.type, 0.0f);

            AddValueServerRpc(new StatValue(stat.type, stat.value));
            //NetworkIndex.Add(stat.type, index);
            index++;
        }
    }

    public float GetStat(StatType type) {
        if (IsOwner) {
            return (baseStats[type] + addModifiers[type]) * multiModifiers[type];
        } else {
            return EffectiveValues[EffectiveValues.IndexOf(new StatValue(type, 0))].value;
        }
    }

    public void AddToAddMod(StatType type, float value) {
        addModifiers[type] += value;

        var ind = EffectiveValues.IndexOf(new StatValue(type, 0));
        SetValueServerRpc(new StatValue(type, GetStat(type)), ind);
    }

    public void AddToMultiMod(StatType type, float value) {
        multiModifiers[type] += value;
    }

    public float GetBaseStat(StatType type) { return baseStats[type]; }
    public float GetMulti(StatType type) { return multiModifiers[type]; }
    public float GetAdd(StatType type) { return addModifiers[type]; }

    void OnEffectiveValuesChanged(NetworkListEvent<StatValue> changeEvent) {
        if (changeEvent.Value.type == StatType.MaxHealth) {
            Debug.LogError(changeEvent.Value.value);
        }
        
        if (OnStatChange != null) OnStatChange();
    }

    [ServerRpc]
    void AddValueServerRpc(StatValue val) {
        EffectiveValues.Add(val);
    }

    [ServerRpc]
    void SetValueServerRpc(StatValue val, int index) {
        EffectiveValues[index] = val;
    }
}