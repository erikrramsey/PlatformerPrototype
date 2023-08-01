using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class Bass : PlayerCharacter {

    [Header("Skill 1 Stuff")]
    [SerializeField] private GameObject Skill1Projectile;
    [SerializeField] private float Skill1ProjectileVelocity;
    [SerializeField] private float Skill1LeapVelocity;

    private Vector3 _skill1Position;
    private bool _skill1Attached;

    /*
    [Header("Skill 2 Stuff")]

    [Header("Skill 3 Stuff")]
    */


    protected override void Skill1Pressed() {
        if (_skill1Attached) {
            performTimedAction(
            StartCoroutine(performOnFixedForTime(0.3f, () => {
                _rigidbody.AddForce((_skill1Position - transform.position).normalized * Skill1LeapVelocity);
            }));
        } else {
            var aim = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            var direction = (aim - (Vector2)transform.position).normalized;
            var projectile = GameObject.Instantiate(Skill1Projectile, transform.position, Quaternion.identity).GetComponent<BassProjectile1>();
            projectile.OnHit += OnProjectile1Hit;
            projectile.OnDespawn += OnProjectile1Despawn;
            projectile.GetComponent<NetworkObject>().Spawn();
            projectile.Setup(teamColor.Value, null, transform);
            projectile.SetInitialForce(Skill1ProjectileVelocity * direction / Time.fixedDeltaTime);
        }
    }

    protected override void Skill1Held() {
    }

    protected override void Skill1Released() {
    }

    protected override void Skill2Released() {
    }

    protected override void Skill3Pressed() {
    }

    private void OnProjectile1Hit(Vector3 position) {
        _skill1Attached = true;
        _skill1Position = position;
    }

    private void OnProjectile1Despawn() {
        _skill1Attached = false;
    }

}
