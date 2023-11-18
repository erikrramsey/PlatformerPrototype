using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class Bass : PlayerCharacter {

    [Header("Skill 1 Stuff")]
    [SerializeField] private AnimationClip Skill1Animation;
    [SerializeField] private float Skill1Damage;

    private HashSet<Transform> _skill1HitSet = new HashSet<Transform>();

    [Header("Skill 2 Stuff")]
    [SerializeField] private GameObject Skill2Projectile;
    [SerializeField] private float Skill2ProjectileVelocity;
    [SerializeField] private float Skill2LeapVelocity;
    [SerializeField] private float Skill2LeapTime;

    private Transform _skill2Transform;
    private Transform _skill2AttachedTransform;
    private bool _skill2Attached;

    [Header("Skill 3 Stuff")]
    [SerializeField] private float Skill3MinDistance;
    [SerializeField] private float Skill3Velocity;
    [SerializeField] private float Skill3Angle;

    protected override void Skill1Pressed() {
    }

    protected override void Skill1Held() {
        if (!skill1Ready) return;

        CooldownSkill(1);
        StartCoroutine(performActionAfterCondition(() => _animator.GetCurrentAnimatorStateInfo(2).IsName(Skill1Animation.name), () => {
            _animator.Play(Skill1Animation.name);
            waitingForAnimationComplete = true;
            Debug.Log(Animator.StringToHash("Base Layer." + Skill1Animation.name));
        }, () => {
            _skill1HitSet.Clear(); 
            waitingForAnimationComplete = false;
        }));
    }

    protected override void OnEnemySkillHit(Collider2D other) {
        if (_skill1HitSet.Contains(other.transform)) return;
        _skill1HitSet.Add(other.transform);
        var number = GameObject.Instantiate(damageNumber, other.transform.position + Vector3.up, Quaternion.identity);
        number.GetComponent<DamageNumber>().SetDamage(Skill1Damage);
        
        other.GetComponent<ITakesDamage>().TakeDamageServerRpc(Skill1Damage);
    }

    protected override void Skill2Pressed() {
        if (_skill2Attached) {
            StartCoroutine(performTimedAction(Skill2LeapTime, () => {
                DisableInputs();
                _rigidbody.velocity = Vector2.zero;
                _rigidbody.gravityScale = 0;
                _stats.AddToMultiMod(StatType.HorizontalSpeed, 1.0f);
            }, () => {
                EnableInputs();
                _rigidbody.gravityScale = 1;
                _stats.AddToMultiMod(StatType.HorizontalSpeed, -1.0f);
            }));
            StartCoroutine(performOnFixedForTime(Skill2LeapTime, () => {
                _rigidbody.AddForce((_skill2Transform.position - transform.position).normalized * Skill2LeapVelocity);
            }));
        } else {
            var aim = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            var direction = (aim - (Vector2)transform.position).normalized;

            SpawnSkill2PrjoectileServerRpc(direction);
        }
    }

    protected override void Skill2Held() {
    }

    protected override void Skill2Released() {
    }

    [ServerRpc]
    void SpawnSkill2PrjoectileServerRpc(Vector2 direction) {
        var projectile = GameObject.Instantiate(Skill2Projectile, transform.position, Quaternion.identity).GetComponent<BassProjectile1>();
        projectile.GetComponent<NetworkObject>().Spawn();
        SetSkill2ProjectileReferenceClientRpc(projectile.GetComponent<NetworkObject>());
        projectile.Setup(teamColor.Value, null, transform, OwnerClientId);
        projectile.SetInitialForce(Skill2ProjectileVelocity * direction / Time.fixedDeltaTime);

        projectile.SetupVisualsClientRpc(GetComponent<NetworkObject>());

        projectile.OnHit += OnProjectile1HitClientRpc;
        projectile.OnDespawn += OnProjectile1DespawnClientRpc;
    }

    [ClientRpc]
    private void OnProjectile1HitClientRpc(ClientRpcParams clientRpcParams = default) {
        _skill2Attached = true;
    }

    [ClientRpc]
    private void OnProjectile1DespawnClientRpc(ClientRpcParams clientRpcParams = default) {
        _skill2Attached = false;
        _skill2AttachedTransform = null;
    }

    [ClientRpc]
    private void SetSkill2ProjectileReferenceClientRpc(NetworkObjectReference target, ClientRpcParams clientRpcParams = default) {
        if (target.TryGet(out NetworkObject targetObject)) {
            _skill2Transform = targetObject.transform;
        }
    }

    [ClientRpc]
    public void SetProjectile1TargetClientRpc(NetworkObjectReference target, ClientRpcParams clientRpcParams = default) {
        if (target.TryGet(out NetworkObject targetObject)) {
            Debug.Log(targetObject);
            _skill2AttachedTransform = targetObject.transform;
        }
    }


    protected override void Skill3Pressed() {
        if (_skill2Attached && _skill2AttachedTransform != null) {
            if (Vector3.Distance(_skill2AttachedTransform.position, transform.position) > Skill3MinDistance) return;

            Vector3 kb = Quaternion.Euler(0.0f, 0.0f, Skill3Angle) * Vector3.right * Skill3Velocity / Time.fixedDeltaTime;
            kb.x *= Mathf.Sign(_skill2AttachedTransform.position.x - transform.position.x);
            var deb = _skill2AttachedTransform.GetComponent<ITakesDebuff>();
            deb?.TakeDebuffServerRpc(Debuff.Knockback, 0, kb);
            deb?.TakeDebuffServerRpc(Debuff.Stun, 0.0f, 0.2f);
        } else if (_skill2Attached) {
            _skill2Transform.GetComponent<Projectile>().DespawnProjectileServerRpc();
        }
    }


}
