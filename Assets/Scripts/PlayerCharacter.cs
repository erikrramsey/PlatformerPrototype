using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerCharacter : NetworkBehaviour, ITakesDamage, ITakesDebuff {
    #region Serialized

    [SerializeField] protected Stats _stats;

    [SerializeField] protected AttackObject[] _attackObjects;

    [SerializeField] protected GameObject damageNumber;
    [SerializeField] protected GameObject takeDamageNumber;
    [SerializeField] protected RawImage healthBarImage;

    [SerializeField] protected AnimationClip IdleAnim;
    [SerializeField] protected AnimationClip WalkAnim;
    [SerializeField] protected AnimationClip JumpAnim;

    public NetworkVariable<TeamColor> teamColor = new NetworkVariable<TeamColor>();
    #endregion

    protected NetworkVariable<float> currentHealth = new NetworkVariable<float>();

    private Camera _camera;
    protected Rigidbody2D _rigidbody;
    protected Animator _animator;
    private ClientNetworkAnimator _networkAnimator;
    private GameOverlayUI _gameOverlayUI;
    private Vector3 spawn;
    private bool waitingForAnimationComplete;
    private Dictionary<Debuff, float> debuffTimer = new Dictionary<Debuff, float>();

    protected bool skill2Ready;

    protected class Inputs {
        public Vector2 movement = new Vector2(0.0f, 0.0f);
        public bool jump = false;
        public bool skill1 = false;
        public bool skill2 = false;
        public bool skill3 = false;
    }
    
    protected Inputs _inputs;
    private PlayerCharacterController _characterController;
    private float CameraOffset = 1.0f;

    #region UnityCallbacks
    protected virtual void Awake() {
        _stats.Initialize();
    }

    protected virtual void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _networkAnimator = GetComponent<ClientNetworkAnimator>();
        spawn = transform.position;

        if (!IsOwner) return;


        _camera = Camera.main;
        _inputs = new Inputs();
        skill2Ready = true;

        // Input system
        _characterController = PlayerInput.Singleton.CharacterController;
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

        _characterController.PlayerCharacter.Skill3.performed += OnSkill3Performed;
        _characterController.PlayerCharacter.Skill3.canceled += OnSkill3Canceled;

        _characterController.PlayerCharacter.Jump.performed += OnJumpPerformed;
        _characterController.PlayerCharacter.Jump.canceled += OnJumpCanceled;

        _characterController.PlayerCharacter.EscMenu.performed += OnEscMenuPerformed;
        _characterController.PlayerCharacter.EscMenu.performed += OnEscMenuCanceled;
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

    private void OnSkill3Performed(InputAction.CallbackContext context) {
        _inputs.skill3 = context.ReadValue<float>() > 0.5f;
        Skill3Pressed();
    }

    private void OnSkill3Canceled(InputAction.CallbackContext context) {
        _inputs.skill3 = context.ReadValue<float>() > 0.5f;
        Skill3Released();
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

    private void OnEscMenuPerformed(InputAction.CallbackContext context) {
        PlayerManager.Singleton.Shutdown();
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnEscMenuCanceled(InputAction.CallbackContext context) {
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

        // Iterate through all debuffs and remove those that are done
        List<Debuff> completedDebuffs = new List<Debuff>();
        foreach (var key in new List<Debuff>(debuffTimer.Keys)) {
            debuffTimer[key] -= Time.fixedDeltaTime;
            if (debuffTimer[key] <= 0) {
                completedDebuffs.Add(key);
            }
        }

        foreach (var done in completedDebuffs) {
            debuffTimer.Remove(done);
        }

        completedDebuffs.Clear();

        var grounded = false;

        var rc = Physics2D.CircleCast(transform.position + Vector3.down, 0.4f, Vector2.down, 0.3f, LayerMask.GetMask(new[] {"Environment", "PTEnvironment"}));
        if (rc && _rigidbody.velocity.y < 0.1f) grounded = true;


        if (_inputs.movement.x > 0.1f) {
            _animator.Play(WalkAnim.name);
            _animator.Play("PlayerFlipRight", 1);
            _rigidbody.AddForce(new Vector2(_stats.GetStat(StatType.HorizontalAccel), 0.0f));
        }
        if (_inputs.movement.x < -0.1f) {
            _animator.Play(WalkAnim.name);
            _animator.Play("PlayerFlipLeft", 1);
            _rigidbody.AddForce(new Vector2(-_stats.GetStat(StatType.HorizontalAccel), 0.0f));
        }


        if (Mathf.Abs(_rigidbody.velocity.x) > _stats.GetStat(StatType.HorizontalSpeed)) _rigidbody.velocity = new Vector3(Mathf.Sign(_rigidbody.velocity.x) * _stats.GetStat(StatType.HorizontalSpeed), _rigidbody.velocity.y, 0.0f);

        if (grounded) {
            if (_inputs.movement.x == 0) {
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x * 1 / _stats.GetStat(StatType.HorizontalDecel), _rigidbody.velocity.y, 0.0f);
                _animator.Play(IdleAnim.name);
            }

            if (_inputs.jump) {
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, 0.0f, 0.0f);
                _rigidbody.AddForce(new Vector2(0.0f, _stats.GetStat(StatType.JumpForce)));
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
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, _rigidbody.velocity.y / _stats.GetStat(StatType.JumpDampForce), 0.0f);
        }

        if (_inputs.skill1) Skill1Held();
        if (_inputs.skill2) Skill2Held();
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
        GameplayManager.Singleton.OnGameEndEvent += OnGameEnd;
        _gameOverlayUI = GameObject.FindObjectOfType<GameOverlayUI>();
        _gameOverlayUI.setMaxHealth(_stats.GetStat(StatType.MaxHealth), _stats.GetStat(StatType.MaxHealth));

        SetCurrentHealthServerRpc(_stats.GetStat(StatType.MaxHealth));
    }

    #region NetVariableCallbacks
    void currentHealth_OnValueChanged(float previous, float current) {
        healthBarImage.rectTransform.localScale = new Vector3(currentHealth.Value / _stats.GetStat(StatType.MaxHealth), 1.0f, 1.0f);
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
    private void SpawnServerRpc() {
        transform.position = spawn;
        currentHealth.Value = _stats.GetStat(StatType.MaxHealth);
        gameObject.SetActive(true);
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
        TakeDamageClientRpc(force, damage, clientRpcParams);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDebuffServerRpc(Debuff debuff, float duration, float value, bool isMult = false) {
        ClientRpcParams clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{OwnerClientId}
            }
        };
        TakeDebuffClientRpc(debuff, duration, value, isMult, clientRpcParams);
    }
    
    [ClientRpc]
    public void TakeDebuffClientRpc(Debuff debuff, float duration, float value, bool isMult = false, ClientRpcParams p = default) {

        if (debuffTimer.ContainsKey(debuff)) {
            debuffTimer[debuff] += duration;
        } else {
            debuffTimer.Add(debuff, duration);
        }


        switch (debuff) {
            case Debuff.Stun:
                StartCoroutine(performActionAfterCondition(() => {
                    return debuffTimer.ContainsKey(debuff) && debuffTimer[debuff] > 0.0f;
                }, () => {
                    _characterController.Disable();
                }, () => {
                    _characterController.Enable();
                }));
                break;

            case Debuff.JumpSlow:
                StartCoroutine(performActionAfterCondition(() => {
                    return debuffTimer.ContainsKey(debuff) && debuffTimer[debuff] > 0.0f;
                }, () => {
                    _stats.AddToMultiMod(StatType.JumpForce, value);
                }, () => {
                    _stats.AddToMultiMod(StatType.JumpForce, -value);
                }));
                break;
        }

        Debug.Log("Player: " + OwnerClientId + " Debuff: " + debuff + " " + debuffTimer[debuff]);
    }

    [ClientRpc]
    public void TakeDamageClientRpc(Vector3 force, float damage, ClientRpcParams p = default) {
        var number = GameObject.Instantiate(takeDamageNumber, transform.position + Vector3.up, Quaternion.identity);
        number.GetComponent<DamageNumber>().SetDamage(10.0f);
        
        _rigidbody.AddForce(force);
    }

    #endregion
    #endregion

    #region PureVirtual
    protected virtual void Skill1Pressed() {}
    protected virtual void Skill1Held() {}
    protected virtual void Skill1Released() {}

    protected virtual void Skill2Pressed() {}
    protected virtual void Skill2Held() {}
    protected virtual void Skill2Released() {}

    protected virtual void Skill3Pressed() {}
    protected virtual void Skill3Held() {}
    protected virtual void Skill3Released() {}
    #endregion

    #region Utility
    protected IEnumerator PassThroughPlatform(PlatformEffector2D pp) {
        pp.rotationalOffset = 180;
        yield return new WaitForSeconds(0.5f);
        pp.rotationalOffset = 0;
    }

    protected IEnumerator WaitForAnimationComplete(float seconds) {
        waitingForAnimationComplete = true;
        yield return new WaitForSeconds(seconds);
        waitingForAnimationComplete = false;
    }

    protected IEnumerator performActionAfterCondition(Func<bool> testCondition, Action first, Action second) {
        first();
        while (testCondition()) yield return null;
        second();
    }

    protected IEnumerator performTimedAction(float time, Action first, Action second) {
        first();
        yield return new WaitForSeconds(time);
        second();
    }

    protected IEnumerator performOnFixedForTime(float time, Action action) {
        while (time > 0) {
            action();
            time -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    // Used for stuns / ability overrides
    protected void DisableInputs() {
        _characterController.Disable();
    }

    protected void EnableInputs() {
        _characterController.Enable();
    }

    protected void OnGameEnd(TeamColor loser) {
        _characterController.Disable();
    }

    #endregion
}
