using UnityEngine;

[System.Serializable]
public class Stats {
    [SerializeField] public float baseHorizontalSpeed = 0.0f;
    [SerializeField] public float baseHorizontalAccel = 0.0f;
    [SerializeField] public float baseHorizontalDecel = 0.0f;
    [SerializeField] public float baseJumpForce = 0.0f;
    [SerializeField] public float baseMaxHealth = 0.0f;
    [SerializeField] public float baseJumpDampForce = 0.0f;

    [SerializeField] public float horizontalSpeedModifier = 1.0f;
    [SerializeField] public float horizontalAccelModifier = 1.0f;
    [SerializeField] public float horizontalDecelModifier = 1.0f;
    [SerializeField] public float jumpForceModifier = 1.0f;
    [SerializeField] public float maxHealthModifier = 1.0f;
    [SerializeField] public float jumpDampForceModifier = 1.0f;

    public float HorizontalSpeed { get {
        return baseHorizontalSpeed * horizontalSpeedModifier;
    }
    private set {} }

    public float HorizontalAccel { get {
        return baseHorizontalAccel * horizontalAccelModifier;
    }
    private set {} }

    public float HorizontalDecel { get {
        return baseHorizontalDecel * horizontalDecelModifier;
    }
    private set {} }

    public float JumpForce { get {
        return baseJumpForce * jumpForceModifier;
    }
    private set {} }

    public float MaxHealth { get {
        return baseMaxHealth * maxHealthModifier;
    }
    private set {} }

    public float JumpDampForce { get {
        return baseJumpDampForce * jumpDampForceModifier;
    }
    private set {} }
}