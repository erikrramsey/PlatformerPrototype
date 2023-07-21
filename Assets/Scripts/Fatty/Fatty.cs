using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class Fatty : PlayerCharacter {

    [Header("Attack 1 Stuff")]
    [SerializeField] private float MaxChargeTime;
    [SerializeField] private float MinChargeTime;
    [SerializeField] private float MaxRange;
    [SerializeField] private float Attack1Angle;
    [SerializeField] private float ChargeSlowDown;
    [SerializeField] private int arcResolution;
    [SerializeField] private float Attack1GravityScale;

    [SerializeField] private Image ChargeBar;
    [SerializeField] private RawImage ChargeImage;


    private float currentChargeTime = 0.0f;
    private float currentRange;
    private LineRenderer arcRenderer;

    protected override void Start() {
        if (!IsOwner) return;

        arcRenderer = GetComponent<LineRenderer>();
        arcRenderer.enabled = false;
        base.Start();
    }

    public override void Skill1Pressed() {
        _stats.horizontalSpeedModifier -= ChargeSlowDown;
        ChargeBar.gameObject.SetActive(true);

        arcRenderer.positionCount = 1;
    }

    public override void Skill1Held() {
        if (currentChargeTime < MaxChargeTime) currentChargeTime += Time.fixedDeltaTime;
        currentRange = (currentChargeTime / MaxChargeTime) * MaxRange;
        ChargeImage.rectTransform.localScale = new Vector3(currentChargeTime / MaxChargeTime, 1.0f, 1.0f);

        if (currentChargeTime >= MaxChargeTime) {
            Skill1Released();
            Skill1Pressed();
        } else if (currentChargeTime >= MinChargeTime) {
            ChargeImage.color = Color.yellow;
            arcRenderer.enabled = true;

            Vector3 trajectory = (Quaternion.AngleAxis(Attack1Angle, Vector3.forward) * Vector3.right) * currentRange;
            float flip = Mathf.Sign((Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).x);
            trajectory.x *= flip;
            CalculateArc(arcResolution, trajectory);

        } else {
            ChargeImage.color = Color.red;
        }
    }

    public override void Skill1Released() {
        ChargeBar.gameObject.SetActive(false);
        _stats.horizontalSpeedModifier += ChargeSlowDown;
        ChargeImage.rectTransform.localScale = new Vector3(currentChargeTime / MaxChargeTime, 1.0f, 1.0f);

        if (currentChargeTime >= MinChargeTime) {
            Vector3 trajectory = (Quaternion.AngleAxis(Attack1Angle, Vector3.forward) * Vector3.right) * currentRange;

            float flip = Mathf.Sign((Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).x);
            trajectory.x *= flip;
            Skill1ServerRpc(trajectory);
        }

        currentChargeTime = 0.0f;
        arcRenderer.enabled = false;
    }

    [ServerRpc]
    private void Skill1ServerRpc(Vector3 direction) {
        var obj = GameObject.Instantiate(_attackObjects[0].projectile, transform.position, Quaternion.identity).GetComponent<FattyAttack1Projectile>();
        obj.Setup(teamColor.Value);
        obj.setInitialForce(direction);
        obj.setDamage(15.0f);
        obj.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc]
    private void Skill2ServerRpc(Vector3 direction) {
        var obj = GameObject.Instantiate(_attackObjects[1].projectile, transform.position, Quaternion.identity).GetComponent<FattyAttack2Projectile>();
        obj.Setup(teamColor.Value);
        obj.setInitialForce(direction);
        obj.setDamage(15.0f);
        obj.GetComponent<NetworkObject>().Spawn();
    }


    private void CalculateArc(int resolution, Vector3 force) {
        arcRenderer.positionCount = resolution + 1;

        float rads = Mathf.Deg2Rad * Attack1Angle;
        float v = Vector3.Magnitude(force * Time.fixedDeltaTime);
        float g = -Physics2D.gravity.y * Attack1GravityScale;
        float distance = (v * v * Mathf.Sin(2 * rads)) / g + 0.5f;

        for (int i = 0; i <= resolution; i++) {
            float t = (float)i / (float)resolution;
            float x = t * distance;
            float y = x * Mathf.Tan(rads) - ((g * x * x) / (2 * v * v * Mathf.Cos(rads) * Mathf.Cos(rads)));
            arcRenderer.SetPosition(i, new Vector2(x * Mathf.Sign(force.x), y) + (Vector2)transform.position);
        }
    }
}
