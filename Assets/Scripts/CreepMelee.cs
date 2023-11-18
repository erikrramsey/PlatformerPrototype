using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class CreepMelee : NetworkBehaviour, ITakesDamage, ITakesDebuff {
    [SerializeField] private float maxHealth;
    [SerializeField] private float horizontalSpeed;
    [SerializeField] private float horizontalAccel;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float meleeDamage;

    [SerializeField] private RawImage healthBarImage;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    private NetworkVariable<TeamColor> teamColor = new NetworkVariable<TeamColor>();

    private Rigidbody2D _rigidbody;
    private Animator _animator;
    private Vector2 forward;
    private LayerMask enemyLayer;

    private bool cooldown = false;

    public override void OnNetworkSpawn() {
        teamColor.OnValueChanged += teamColor_OnValueChanged;
        currentHealth.OnValueChanged += currentHealth_OnValueChanged;

        // Synchronize on late client
        if (currentHealth.Value > 0) {
            currentHealth_OnValueChanged(currentHealth.Value, currentHealth.Value);
        }

        if (!IsOwner) return;
        GameplayManager.Singleton.OnGameEndEvent += OnGameEnd;
        currentHealth.Value = maxHealth;

        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkDespawn() {
        if (!IsOwner) return;

        GameplayManager.Singleton.OnGameEndEvent -= OnGameEnd;
    }

    void OnGameEnd(TeamColor loser) {
        enabled = false;
    }

    public void FixedUpdate() {
        if (!IsOwner) return;
        if (cooldown) return;


        Vector2 forceToAdd = Vector2.zero;

        var grounded = false;
        var rc = Physics2D.CircleCast(transform.position, 0.5f, Vector2.down, 0.3f, LayerMask.GetMask("Environment"));
        if (rc) grounded = true;

        // Check if something in front
        var creatureCasts = Physics2D.CircleCastAll(transform.position + (Vector3)forward, 0.35f, forward, 0.2f, enemyLayer);
        if (grounded) {
            forceToAdd = forward * horizontalAccel;
        }


        if (!grounded) return;

        foreach (var creatureCast in creatureCasts) {
            string layer = LayerMask.LayerToName(creatureCast.transform.gameObject.layer);

            // If the target in front is on our team stop only if its a creep
            if (layer == LayerMask.LayerToName(gameObject.layer)) {
                if (creatureCast.transform.GetComponent<CreepMelee>()) {
                        forceToAdd = Vector2.zero;
                        _rigidbody.velocity = Vector3.zero;
                        _animator.Play("CreepIdle");
                }
            // else attack
            } else {
                forceToAdd = Vector2.zero;
                _rigidbody.velocity = Vector3.zero;
                _animator.Play("CreepAttack");
                StartCoroutine(AddCooldown(attackCooldown));
            }
        }

        _rigidbody.AddForce(forceToAdd);
        if (Mathf.Abs(_rigidbody.velocity.x) >= horizontalSpeed) _rigidbody.velocity = new Vector2(Mathf.Sign(_rigidbody.velocity.x) * horizontalSpeed, _rigidbody.velocity.y);
        _animator.SetFloat("SpeedX", Mathf.Abs(_rigidbody.velocity.x));
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!IsOwner) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("RedHitbox")) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("BlueHitbox")) return;

        Debug.Log("Creep trigger enter " + other.name + ' ' + gameObject.layer + ' ' + other.gameObject.layer);
        
        other.GetComponent<ITakesDamage>().TakeDamageServerRpc(meleeDamage);
    }

    IEnumerator AddCooldown(float seconds) {
        cooldown = true;
        yield return new WaitForSeconds(seconds);
        cooldown = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage) {
        Debug.Log("Creep taking damage" + OwnerClientId);

        currentHealth.Value -= damage;
        if (currentHealth.Value > maxHealth) currentHealth.Value = maxHealth;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDebuffServerRpc(Debuff debuff, float duration, Vector3 value, bool isMult = false) {
        switch (debuff) {
            case Debuff.Knockback:
                _rigidbody.AddForce(value);
                break;
            default:
                Debug.LogError("Unhandled debuff " + debuff.ToString());
                break;
        }
    }

    [ServerRpc]
    public void SetTeamServerRpc(TeamColor teamc) {
        Debug.Log("Setting creep team: " + teamc);
        teamColor.Value = teamc;
    }

    public TeamColor GetTeamColor() { return teamColor.Value; }

    void teamColor_OnValueChanged(TeamColor previous, TeamColor current) {
        Debug.Log("Creep team changed: " + current);
        Color col = Color.white;
        if (current == TeamColor.red) {
            gameObject.layer = LayerMask.NameToLayer("RedHurtbox");
            transform.Find("Parts/Weapon").gameObject.layer = LayerMask.NameToLayer("RedHitbox");
            col = Color.red;
            forward = Vector2.right;
        } else if (current == TeamColor.blue) {
            gameObject.layer = LayerMask.NameToLayer("BlueHurtbox");
            transform.Find("Parts").transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
            transform.Find("Parts/Weapon").gameObject.layer = LayerMask.NameToLayer("BlueHitbox");
            col = Color.blue;
            forward = Vector2.left;
        }

        enemyLayer = LayerMask.GetMask(new string[] {"BlueHurtbox", "RedHurtbox"});

        foreach (SpriteRenderer sp in transform.GetComponentsInChildren<SpriteRenderer>()) {
            sp.color = col;
        }
    }

    void currentHealth_OnValueChanged(float previous, float current) {
        if (current <= 0.0f) GetComponent<NetworkObject>().Despawn();

        healthBarImage.rectTransform.localScale = new Vector3(currentHealth.Value / maxHealth, 1.0f, 1.0f);
        healthBarImage.uvRect = new Rect(0.0f, 0.0f, current / 20.0f, 1.0f);
    }

}
