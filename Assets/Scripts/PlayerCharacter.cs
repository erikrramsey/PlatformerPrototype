using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerCharacter : NetworkBehaviour, ITakesDamage {
    #region Serialized

    [SerializeField] protected Stats _stats;

    [SerializeField] protected AttackObject[] _attackObjects;

    [SerializeField] protected GameObject damageNumber;
    [SerializeField] protected RawImage healthBarImage;

    [SerializeField] protected AnimationClip IdleAnim;
    [SerializeField] protected AnimationClip WalkAnim;
    [SerializeField] protected AnimationClip JumpAnim;

    
    #endregion

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    public NetworkVariable<TeamColor> teamColor = new NetworkVariable<TeamColor>();

    private Camera _camera;
    private Rigidbody2D _rigidbody;
    private Animator _animator;
    private ClientNetworkAnimator _networkAnimator;
    private GameOverlayUI _gameOverlayUI;
    private Vector3 spawn;
    private bool waitingForAnimationComplete;

    class Inputs {
        public Vector2 movement = new Vector2(0.0f, 0.0f);
        public bool jump = false;
        public bool skill1 = false;
        public bool skill2 = false;
    }
    
    private Inputs _inputs;
    private PlayerCharacterController _characterController;
    private float CameraOffset = 1.0f;

    #region UnityCallbacks
    protected virtual void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _networkAnimator = GetComponent<ClientNetworkAnimator>();
        spawn = transform.position;

        if (!IsOwner) return;


        _camera = Camera.main;
        _inputs = new Inputs();

        // Input system
        _characterController = GameObject.FindObjectOfType<PlayerInput>().CharacterController;
        EnableInputCallbacks();
    }

    void OnEnable() {
        if (!IsOwner) return;
        Debug.Log("Player Enabled");
        _characterController.Enable();
        EnableInputCallbacks();
    }

    private void OnDisable() {
        if (!IsOwner) return;
        Debug.Log("Player Disabled");
        _characterController.Disable();
        _characterController.PlayerCharacter.Movement.performed -= OnMovementPerformed;
        _characterController.PlayerCharacter.Movement.canceled -= OnMovementCanceled;
    }

    #region InputCallbacks
    private void EnableInputCallbacks() {
        _characterController.PlayerCharacter.Movement.performed += OnMovementPerformed;
        _characterController.PlayerCharacter.Movement.canceled += OnMovementCanceled;

        _characterController.PlayerCharacter.Skill1.performed += OnSkill1Performed;
        _characterController.PlayerCharacter.Skill1.canceled += OnSkill1Canceled;

        _characterController.PlayerCharacter.Skill2.performed += OnSkill2Performed;
        _characterController.PlayerCharacter.Skill2.canceled += OnSkill2Canceled;

        _characterController.PlayerCharacter.Jump.performed += OnJumpPerformed;
        _characterController.PlayerCharacter.Jump.canceled += OnJumpCanceled;
    }

    private void OnMovementPerformed(InputAction.CallbackContext context) {
        _inputs.movement = context.ReadValue<Vector2>();
    }

    private void OnMovementCanceled(InputAction.CallbackContext context) {
        _inputs.movement = new Vector2(0.0f, 0.0f);
    }

    private void OnSkill1Performed(InputAction.CallbackContext context) {
        _inputs.skill1 = context.ReadValue<float>() > 0.5f;
        Skill1Pressed();
    }

    private void OnSkill1Canceled(InputAction.CallbackContext context) {
        _inputs.skill1 = context.ReadValue<float>() > 0.5f;
        Skill1Released();
    }

    private void OnSkill2Performed(InputAction.CallbackContext context) {
        _inputs.skill2 = context.ReadValue<float>() > 0.5f;
        Skill2Pressed();
    }

    private void OnSkill2Canceled(InputAction.CallbackContext context) {
        _inputs.skill2 = context.ReadValue<float>() > 0.5f;
        Skill2Released();
    }
    
    private void OnSkillCanceled(InputAction.CallbackContext context) {
        Debug.LogError("attack 1 canceled (this shouldn't happen)");
        _inputs.skill1 = context.ReadValue<float>() > 0.5f;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context) {
        _inputs.jump = context.ReadValue<float>() > 0.5f;
    }
    
    private void OnJumpCanceled(InputAction.CallbackContext context) {
        _inputs.jump = context.ReadValue<float>() > 0.5f;
    }
    #endregion


    void LateUpdate() {
        if (!IsOwner) return;

        var cpos = _camera.transform.position;

        cpos.x = transform.position.x;
        if (Mathf.Abs(cpos.y - transform.position.y + CameraOffset) > 1.3f) {
            cpos.y = Mathf.Lerp(cpos.y, transform.position.y + CameraOffset, 3.0f * Time.deltaTime);
        }

        _camera.transform.position = cpos;
    }

    void FixedUpdate() {
        if (!IsOwner) return;
        if (waitingForAnimationComplete) return;

        var grounded = false;

        var rc = Physics2D.CircleCast(transform.position + Vector3.down, 0.4f, Vector2.down, 0.3f, LayerMask.GetMask(new[] {"Environment", "PTEnvironment"}));
        if (rc && _rigidbody.velocity.y < 0.1f) grounded = true;

        if (_inputs.movement.x > 0.1f) {
            _animator.Play(WalkAnim.name);
            _animator.Play("PlayerFlipRight", 1);
            _rigidbody.AddForce(new Vector2(_stats.HorizontalAccel, 0.0f));
        }
        if (_inputs.movement.x < -0.1f) {
            _animator.Play(WalkAnim.name);
            _animator.Play("PlayerFlipLeft", 1);
            _rigidbody.AddForce(new Vector2(-_stats.HorizontalAccel, 0.0f));
        }

        if (Mathf.Abs(_rigidbody.velocity.x) > _stats.HorizontalSpeed) _rigidbody.velocity = new Vector3(Mathf.Sign(_rigidbody.velocity.x) * _stats.HorizontalSpeed, _rigidbody.velocity.y, 0.0f);

        if (grounded) {
            if (_inputs.movement.x == 0) {
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x * 1 / _stats.HorizontalDecel, _rigidbody.velocity.y, 0.0f);
                _animator.Play(IdleAnim.name);
            }

            if (_inputs.jump) {
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, 0.0f, 0.0f);
                _rigidbody.AddForce(new Vector2(0.0f, _stats.JumpForce));
            }

            if (_inputs.movement.y < -0.5f) {
                var pp = rc.collider.GetComponent<PlatformEffector2D>();
                if (pp) {
                    if (pp.rotationalOffset == 0) 
                        StartCoroutine(PassThroughPlatform(pp));
                }
            }
        }
        
        if (!grounded && !_inputs.jump && _rigidbody.velocity.y > 0.0f) {
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, _rigidbody.velocity.y / _stats.JumpDampForce, 0.0f);
        }

        if (_inputs.skill1) Skill1Held();
        if (_inputs.skill2) Skill2Held();
    }

    public virtual void Skill1Pressed() {}
    public virtual void Skill1Held() {}
    public virtual void Skill1Released() {}

    public virtual void Skill2Pressed() {}
    public virtual void Skill2Held() {}
    public virtual void Skill2Released() {}

    IEnumerator PassThroughPlatform(PlatformEffector2D pp) {
        pp.rotationalOffset = 180;
        yield return new WaitForSeconds(0.5f);
        pp.rotationalOffset = 0;
    }

    IEnumerator WaitForAnimationComplete(float seconds) {
        waitingForAnimationComplete = true;
        yield return new WaitForSeconds(seconds);
        waitingForAnimationComplete = false;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!IsOwner) return;
        if (!(other.gameObject.layer == LayerMask.NameToLayer("RedHurtbox") || 
            other.gameObject.layer == LayerMask.NameToLayer("BlueHurtbox"))) return;

        Debug.Log("Player trigger enter " + other.name + ' ' + gameObject.layer + ' ' + other.gameObject.layer);
        var number = GameObject.Instantiate(damageNumber, other.transform.position + Vector3.up, Quaternion.identity);
        number.GetComponent<DamageNumber>().SetDamage(10.0f);
        
        other.GetComponent<ITakesDamage>().TakeDamageServerRpc(new Vector2(_attackObjects[0].knockback.x * transform.Find("Parts").localScale.x, _attackObjects[0].knockback.y), 10.0f);
    }


    #endregion

    #region Network

    public override void OnNetworkSpawn() {
        teamColor.OnValueChanged += teamColor_OnValueChanged;
        currentHealth.OnValueChanged += currentHealth_OnValueChanged;

        if (currentHealth.Value > 0) {
            currentHealth_OnValueChanged(currentHealth.Value, currentHealth.Value);
        }
        
        if (!IsOwner) return;
        _gameOverlayUI = GameObject.FindObjectOfType<GameOverlayUI>();
        _gameOverlayUI.setMaxHealth(_stats.MaxHealth, _stats.MaxHealth);

        SetCurrentHealthServerRpc(_stats.MaxHealth);
    }

    #region NetVariableCallbacks
    void currentHealth_OnValueChanged(float previous, float current) {
        healthBarImage.rectTransform.localScale = new Vector3(currentHealth.Value / _stats.MaxHealth, 1.0f, 1.0f);
        healthBarImage.uvRect = new Rect(0.0f, 0.0f, current / 20.0f, 1.0f);

        if (!IsOwner) return;
        if (current <= 0) {
            transform.position = spawn;
            _rigidbody.velocity = Vector2.zero;
            SpawnServerRpc();
            return;
        }

        _gameOverlayUI.setHealth(current);
    }
    
    public virtual void teamColor_OnValueChanged(TeamColor previous, TeamColor current) {
        if (current == TeamColor.red) {
            gameObject.layer = LayerMask.NameToLayer("RedHurtbox");

            foreach (SpriteRenderer sp in transform.GetComponentsInChildren<SpriteRenderer>()) {
                sp.color = Color.red;
            }
        } else if (current == TeamColor.blue) {
            gameObject.layer = LayerMask.NameToLayer("BlueHurtbox");

            foreach (SpriteRenderer sp in transform.GetComponentsInChildren<SpriteRenderer>()) {
                sp.color = Color.blue;
            }
        }
    }
    #endregion

    #region RPC's
    [ServerRpc]
    public virtual void SpawnProjectileServerRpc(int index, Vector3 direction) {
        var obj = GameObject.Instantiate(_attackObjects[index].projectile, transform.position, Quaternion.identity).GetComponent<Projectile>();
        obj.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
    }

    [ServerRpc]
    private void DieServerRpc() {
        StartCoroutine(WaitToSpawn());
    }

    [ServerRpc]
    private void SpawnServerRpc() {
        transform.position = spawn;
        currentHealth.Value = _stats.MaxHealth;
        gameObject.SetActive(true);
    }

    private IEnumerator WaitToSpawn() {
        yield return new WaitForSeconds(5.0f);
        SpawnServerRpc();
    }


    [ServerRpc]
    public void SetCurrentHealthServerRpc(float health) {
        currentHealth.Value = health;
    }


    [ClientRpc]
    public void SetPositionClientRpc(Vector3 position) {
        Debug.Log("Setting player position to: " + position + " on " + OwnerClientId);
        transform.position = position;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(Vector3 force, float damage) {
        Debug.Log("taking damage" + OwnerClientId);
        ClientRpcParams clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{OwnerClientId}
            }
        };

        currentHealth.Value -= damage;
        TakeDamageClientRpc(force, clientRpcParams);
    }

    [ClientRpc]
    public void TakeDamageClientRpc(Vector3 force, ClientRpcParams p = default) {
        _rigidbody.AddForce(force);
    }

    #endregion
    #endregion
}
