using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class Bass : PlayerCharacter {
    private Action OnEnvironmentCollision;
    private Action<Collider2D> OnSkillHit;
    private HashSet<Transform> _transformsHit = new HashSet<Transform>();

    #region UnityCallbacks

    protected void OnCollisionEnter2D(Collision2D other) {
        if (!IsOwner) return;
        var dot = Vector2.Dot(other.relativeVelocity.normalized, other.contacts[0].normal);
        //Debug.LogError("Dot: " + dot + " Velocity: " + other.relativeVelocity.normalized + "Contact: " + other.contacts[0].normal);
        if (dot <= 0.4f && dot >= -0.4f) {
            return;
        }

        switch (LayerMask.LayerToName(other.gameObject.layer)) {
            case "PTEnvironment":
                if (_rigidbody.velocity.y == 0.0f) {
                    OnEnvironmentCollision?.Invoke();
                }
            break;
            case "Environment":
                OnEnvironmentCollision?.Invoke();
            break;
        }
    }

    protected override void OnEnemySkillHit(Collider2D other) {
        OnSkillHit(other);
    }

    #endregion
    #region PlayerCharacterOverrides

    protected override void Skill1Pressed() {
    }

    [Header("Melee Skill")]
    [SerializeField] AnimationClip MeleeUpAnimation;
    [SerializeField] AnimationClip MeleeDownAnimation;
    [SerializeField] AnimationClip MeleeLeftAnimation;
    [SerializeField] AnimationClip MeleeRightAnimation;
    [SerializeField] float MeleeBaseDamage;
    [SerializeField] float MeleeKnockbackForce;
    private Vector2 MeleeKnockback;

    protected override void Skill1Held() {
        if (!skill1Ready) return;
        CooldownSkill(1);
        _transformsHit.Clear(); 

        var Dir = GetNormalizedAim();
        string aimAnimationName;
        if ( Mathf.Sign(Dir.x) * Dir.x >= (1.0f / Mathf.Sqrt(2))) {
            if (Dir.x > 0.0f) {
                aimAnimationName = MeleeRightAnimation.name;
                if (grounded) {
                    MeleeKnockback = new Vector2(2.0f, 1.0f).normalized * MeleeKnockbackForce;
                } else {
                    MeleeKnockback = new Vector2(1.0f, -2.0f).normalized * MeleeKnockbackForce;
                }
            } else {
                aimAnimationName = MeleeLeftAnimation.name;
                if (grounded) {
                    MeleeKnockback = new Vector2(-2.0f, 1.0f).normalized * MeleeKnockbackForce;
                } else {
                    MeleeKnockback = new Vector2(-1.0f, -2.0f).normalized * MeleeKnockbackForce;
                }
            }
        } else {
            if (Dir.y > 0.0f) {
                aimAnimationName = MeleeUpAnimation.name;
                MeleeKnockback = Vector2.up * MeleeKnockbackForce;
            } else {
                aimAnimationName = MeleeDownAnimation.name;
                MeleeKnockback = Vector2.down * MeleeKnockbackForce;
            }
        }

        StartCoroutine(performActionAfterCondition(() => !_animator.GetCurrentAnimatorStateInfo(2).IsName(IdleAnim.name), () => {
            OnSkillHit += OnMeleeHit;
            _animator.Play(aimAnimationName);
            waitingForAnimationComplete = true;
            //Debug.Log(Animator.StringToHash("Base Layer." + aimAnimationName));
        }, () => {
            _animator.Play(IdleAnim.name);
            OnSkillHit -= OnMeleeHit;
            waitingForAnimationComplete = false;
        }));
    }

    protected void OnMeleeHit(Collider2D other) {
        if (!_transformsHit.Add(other.transform)) return;

        var dmg = MeleeBaseDamage * _stats.GetStat(StatType.DamageMultiplier);
        var number = GameObject.Instantiate(damageNumber, other.transform.position + Vector3.up, Quaternion.identity);
        number.GetComponent<DamageNumber>().SetDamage(dmg);

        if (other.TryGetComponent(out ITakesDebuff db)) {
            db.TakeDebuffServerRpc(Debuff.Knockback, 0.0f, MeleeKnockback / Time.fixedDeltaTime);
            db.TakeDebuffServerRpc(Debuff.Stun, 0.2f, 0.0f);
        }
        
        other.GetComponent<ITakesDamage>().TakeDamageServerRpc(GetComponent<NetworkObject>(), dmg);
    }


    protected override void Skill2Pressed() {
        if (!skill2Ready) return;
        CooldownSkill(2);

        _transformsHit.Clear(); 
        DashSkill();
    }

    protected override void Skill2Held() {
    }

    protected override void Skill2Released() {
    }

    protected override void Skill3Pressed() {
    }
    #endregion

    [Header("Dash Skill")]
    [SerializeField] private float DashDuration;
    [SerializeField] private float DashVelocity;
    [SerializeField] private float DashBaseDamage;
    [SerializeField] private GameObject DashHitbox;
    protected void DashSkill() {
        var aim = GetNormalizedAim();
        var OnDashEnd = new Action(() => {
            EnableInputs();
            _rigidbody.velocity = Vector2.zero;
            DashHitbox.SetActive(false);
            _rigidbody.gravityScale = 1;
            OnEnvironmentCollision = null;
            OnSkillHit -= OnDashHit;
        });

        var timedAction = StartCoroutine(performTimedAction(DashDuration, () => {
            DisableInputs();
            DashHitbox.SetActive(true);
            _animator.Play(IdleAnim.name, _animator.GetLayerIndex("Attack"));
            _rigidbody.velocity = aim * DashVelocity;
            _rigidbody.gravityScale = 0;
            OnSkillHit += OnDashHit;
        }, () => {
            OnDashEnd();
        }));
        
        OnEnvironmentCollision += () => {
            OnDashEnd();
            StopCoroutine(timedAction);
        };
    }

    protected void OnDashHit(Collider2D other) {
        if (_transformsHit.Contains(other.transform)) return;
        _transformsHit.Add(other.transform);

        var dmg = DashBaseDamage * _stats.GetStat(StatType.DamageMultiplier);
        var number = GameObject.Instantiate(damageNumber, other.transform.position + Vector3.up, Quaternion.identity);
        number.GetComponent<DamageNumber>().SetDamage(dmg);
        other.GetComponent<ITakesDamage>().TakeDamageServerRpc(GetComponent<NetworkObject>(), dmg);
    }
}
