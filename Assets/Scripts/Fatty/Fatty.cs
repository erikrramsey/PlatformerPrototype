using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class Fatty : PlayerCharacter {

    [Header("Skill 1 Stuff")]
    [SerializeField] private float MaxChargeTime;
    [SerializeField] private float MinChargeTime;
    [SerializeField] private float MaxRange;
    [SerializeField] private float Skill1Angle;
    [SerializeField] private float ChargeSlowDown;
    [SerializeField] private int arcResolution;
    [SerializeField] private float Attack1GravityScale;
    [SerializeField] private Image ChargeBar;
    [SerializeField] private RawImage ChargeImage;

    [Header("Skill 2 Stuff")]
    [SerializeField] private Vector2 Skill2Velocity;

    [Header("Skill 3 Stuff")]
    [SerializeField] private float Skill3Force;
    [SerializeField] private float Skill3Duration;


    private float currentChargeTime = 0.0f;
    private float currentRange;
    private LineRenderer arcRenderer;

    protected override void Start() {
        if (!IsOwner) return;

        arcRenderer = GetComponent<LineRenderer>();
        arcRenderer.enabled = false;
        base.Start();
    }

    protected override void Skill1Pressed() {
        _stats.AddToMultiMod(StatType.HorizontalSpeed, -ChargeSlowDown);
        ChargeBar.gameObject.SetActive(true);

        arcRenderer.positionCount = 1;
    }

    protected override void Skill1Held() {
        if (currentChargeTime < MaxChargeTime) currentChargeTime += Time.fixedDeltaTime;
        currentRange = (currentChargeTime / MaxChargeTime) * MaxRange;
        ChargeImage.rectTransform.localScale = new Vector3(currentChargeTime / MaxChargeTime, 1.0f, 1.0f);

        if (currentChargeTime >= MaxChargeTime) {
            Skill1Released();
            Skill1Pressed();
        } else if (currentChargeTime >= MinChargeTime) {
            ChargeImage.color = Color.yellow;
            arcRenderer.enabled = true;

            Vector3 trajectory = (Quaternion.AngleAxis(Skill1Angle, Vector3.forward) * Vector3.right) * currentRange;
            float flip = Mathf.Sign((Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).x);
            trajectory.x *= flip;
            CalculateArc(arcResolution, trajectory);

        } else {
            ChargeImage.color = Color.red;
        }
    }

    protected override void Skill1Released() {
        ChargeBar.gameObject.SetActive(false);
        _stats.AddToMultiMod(StatType.HorizontalSpeed, ChargeSlowDown);
        ChargeImage.rectTransform.localScale = new Vector3(currentChargeTime / MaxChargeTime, 1.0f, 1.0f);

        if (currentChargeTime >= MinChargeTime) {
            Vector3 trajectory = (Quaternion.AngleAxis(Skill1Angle, Vector3.forward) * Vector3.right) * currentRange;

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
        obj.SetInitialForce(direction);
        obj.SetDamage(15.0f);
        obj.GetComponent<NetworkObject>().Spawn();
    }

    protected override void Skill2Released() {
        if (!skill2Ready) return;
        CooldownSkill(2);

        float flip = Mathf.Sign((Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).x);
        Skill2ServerRpc(flip);
    }

    [ServerRpc]
    private void Skill2ServerRpc(float direction) {
        var obj = GameObject.Instantiate(_attackObjects[1].projectile, transform.position, Quaternion.identity).GetComponent<FattyAttack2Projectile>();
        obj.Setup(teamColor.Value);
        obj.SetInitialForce(Skill2Velocity * direction / Time.fixedDeltaTime);
        obj.GetComponent<NetworkObject>().Spawn();
    }

    protected override void Skill3Pressed() {
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.AddForce(Vector2.up * Skill3Force);
        StartCoroutine(performTimedAction(
            Skill3Duration,
            () => { DisableInputs(); _inputs.jump = true; },
            () => { EnableInputs(); _inputs.jump = false; }
        ));
    }

    private void CalculateArc(int resolution, Vector3 force) {
        arcRenderer.positionCount = resolution + 1;

        float rads = Mathf.Deg2Rad * Skill1Angle;
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
