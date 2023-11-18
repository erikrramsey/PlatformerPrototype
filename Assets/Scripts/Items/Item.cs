using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {
    [SerializeField] public string ItemName;
    [SerializeField] public string Description;

    [System.Serializable]
    public struct AffectedStat {
        [SerializeField] public bool IsMultiplier;
        [SerializeField] public StatType Type;
        [SerializeField] public float Value;
    }

    [SerializeField] public AffectedStat[] affectedStats;
}
