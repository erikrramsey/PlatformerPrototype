using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "attack", menuName = "ScriptableObjects/AttackObject", order = 1)]
public class AttackObject : ScriptableObject {
    public AnimationClip animation;
    public GameObject projectile;
    public Vector2 knockback;
    public float hitstun;
    public float damage;
}
