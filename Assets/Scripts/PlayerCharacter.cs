using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerCharacter : NetworkBehaviour,
    ITakesDamage,
    ITakesDebuff,
    IHasGold {
    #region Serialized

    [SerializeField] protected Stats _stats;
    [SerializeField] protected float timeToSpawn;
    [SerializeField] protected int StartingGold;
    [SerializeField] protected int PlayerGoldValue;

    [SerializeField] protected AttackObject[] _attackObjects;

    [SerializeField] protected GameObject damageNumber;
    [SerializeField] protected GameObject takeDamageNumber;
    [SerializeField] protected Transform healthBarImage;

    [SerializeField] protected AnimationClip IdleAnim;
    [SerializeField] protected AnimationClip WalkAnim;
    [SerializeField] protected AnimationClip JumpAnim;
    [SerializeField] protected AnimationClip StunAnim;
    [SerializeField] protected float LowHealthKnockbackMulti;
    [SerializeField] protected float LowHealthStunMulti;
    [SerializeField] protected SpriteRenderer[] teamColorSprites;

    public NetworkVariable<TeamColor> teamColor = new NetworkVariable<TeamColor>();
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(1.0f);
    public NetworkVariable<int> currentGold = new NetworkVariable<int>();

    public HashSet<Item> items = new HashSet<Item>();
    #endregion

    private Camera _camera;
    protected Rigidbody2D _rigidbody;
    protected Collider2D _hurtbox;
    protected Animator _animator;
    private ClientNetworkAnimator _networkAnimator;
    protected bool waitingForAnimationComplete;
    private Dictionary<Debuff, float> debuffTimer = new Dictionary<Debuff, float>();
    protected ClientRpcParams ownerParams;
    protected Transform _playerWorldUI;

    private Transform LastDamageSource;

    protected bool skill1Ready;
    protected bool skill2Ready;
    protected bool skill3Ready;

    protected bool isAlive;
    protected bool isDisabled;
    protected bool grounded;

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
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _networkAnimator = GetComponent<ClientNetworkAnimator>();
        _camera = Camera.main;
        _inputs = new Inputs();
        _hurtbox = GetComponent<CapsuleCollider2D>();


        currentHealth.Value = 1.0f;
        currentGold.Value = StartingGold;
    }

    protected virtual void Start()
    {
        ownerParams = new ClientRpcParams {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{OwnerClientId}
            }
        };

        if (!IsOwner) return;

        skill1Ready = true;
        skill2Ready = true;
        skill3Ready = true;

        // Input system
        _characterController = PlayerInput.Singleton.CharacterController;
        EnableInputCallbacks();

        ShopOverlay.Singleton.localPlayer = this;
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

        _characterController.PlayerCharacter.ShopMenu.performed += OnShopMenuPerformed;
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

    private void OnShopMenuPerformed(InputAction.CallbackContext context) {
        ShopOverlay.Singleton.gameObject.SetActive(!ShopOverlay.Singleton.gameObject.activeSelf);
    }

    private void OnShopMenuCanceled(InputAction.CallbackContext context) {
    }
    #endregion

    void LateUpdate() {
        if (!IsOwner) return;
        if (!isAlive) return;

        var cpos = _camera.transform.position;

        cpos.x = transform.position.x;
        if (Mathf.Abs(cpos.y - transform.position.y + CameraOffset) > 1.3f) {
            cpos.y = Mathf.Lerp(cpos.y, transform.position.y + CameraOffset, 3.0f * Time.deltaTime);
        }

        float ppu = 32.0f;
        cpos = new Vector3(Mathf.Round(cpos.x *  ppu) / ppu, Mathf.Round(cpos.y * ppu) / ppu, cpos.z);
//        _playerWorldUI.position = new Vector3(Mathf.Round(transform.position.x *  ppu) / ppu, Mathf.Round(transform.position.y * ppu) / ppu, transform.position.z);

        _camera.transform.position = cpos;

    }

    protected virtual void FixedUpdate() {
        if (!IsOwner) return;
        if (!isAlive) return;

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


        grounded = false;

        var rc = Physics2D.CircleCast(transform.position + Vector3.down, 0.4f, Vector2.down, 0.3f, LayerMask.GetMask(new[] {"Environment", "PTEnvironment"}));
        if (rc && _rigidbody.velocity.y < 0.1f) grounded = true;

        if (_inputs.skill1) Skill1Held();
        if (_inputs.skill2) Skill2Held();
        if (_inputs.skill3) Skill3Held();


        if (isDisabled) return;

        if (_inputs.movement.x > 0.1f) {
            _animator.Play(WalkAnim.name);

            if (!waitingForAnimationComplete)
                _animator.Play("PlayerFlipRight", 1);
            _rigidbody.AddForce(new Vector2(_stats.GetStat(StatType.HorizontalAccel), 0.0f));
        }
        if (_inputs.movement.x < -0.1f) {
            _animator.Play(WalkAnim.name);
            if (!waitingForAnimationComplete)
                _animator.Play("PlayerFlipLeft", 1);
            _rigidbody.AddForce(new Vector2(-_stats.GetStat(StatType.HorizontalAccel), 0.0f));
        }


        if (Mathf.Abs(_rigidbody.velocity.x) > _stats.GetStat(StatType.HorizontalSpeed)) {
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x / _stats.GetStat(StatType.HorizontalDecel), _rigidbody.velocity.y, 0.0f);
        }

        if (grounded) {
            if (_inputs.movement.x == 0) {
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x / _stats.GetStat(StatType.HorizontalDecel), _rigidbody.velocity.y, 0.0f);
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
    }

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (!IsOwner) return;
        if (!(other.gameObject.layer == LayerMask.NameToLayer("RedHurtbox") || 
              other.gameObject.layer == LayerMask.NameToLayer("BlueHurtbox") ||
              other.gameObject.layer == LayerMask.NameToLayer("StageHazardHurtbox"))) return;

        Debug.Log("Player trigger enter " + other.name + ' ' + gameObject.layer + ' ' + other.gameObject.layer);

        OnEnemySkillHit(other);
    }

    protected virtual void OnEnemySkillHit(Collider2D other) {}

    #endregion

    #region Network
    public override void OnNetworkSpawn() {
        Debug.Log("Spawning player " + OwnerClientId);
        isAlive = true;
        isDisabled = false;

        teamColor.OnValueChanged += teamColor_OnValueChanged;
        currentHealth.OnValueChanged += currentHealth_OnValueChanged;
        currentGold.OnValueChanged += currentGold_OnValueChanged;

        _stats.OnStatChange += RefreshStatsUIServerRpc;
        
        if (!IsOwner) return;
        SetCurrentHealthServerRpc(_stats.GetStat(StatType.MaxHealth));

        GameplayManager.Singleton.OnGameEndEvent += OnGameEnd;
        GameOverlayUI.Singleton.setMaxHealth(_stats.GetStat(StatType.MaxHealth), _stats.GetStat(StatType.MaxHealth));
        GameplayManager.Singleton.OnClientLoadedServerRpc();
    }

    #region NetVariableCallbacks
    void currentHealth_OnValueChanged(float previous, float current) {
        healthBarImage.localScale = new Vector3(currentHealth.Value / _stats.GetStat(StatType.MaxHealth), 1.0f, 1.0f);

        if (!IsOwner) return;
        if (current <= 0 && isAlive) {
            OnDeath();
        }

        GameOverlayUI.Singleton.setHealth(current);
    }
    
    public virtual void teamColor_OnValueChanged(TeamColor previous, TeamColor current) {
        Color col = Color.white;
        int layer = -1;
        if (current == TeamColor.red) {
            gameObject.layer = LayerMask.NameToLayer("RedHurtbox");
            layer = LayerMask.NameToLayer("RedHitbox");
            col = Color.red;
        } else if (current == TeamColor.blue) {
            gameObject.layer = LayerMask.NameToLayer("BlueHurtbox");
            layer = LayerMask.NameToLayer("BlueHitbox");
            col = Color.blue;
        }

        foreach (SpriteRenderer sp in teamColorSprites) {
            sp.color = col;
            var hb = sp.GetComponent<Collider2D>();
            if (hb) {
                hb.gameObject.layer = layer;
            }
        }

        if (!IsOwner) return;

        transform.position = GameplayManager.Singleton.GetSpawn(teamColor.Value).position;
    }

    public virtual void currentGold_OnValueChanged(int previous, int current) {
        if (!IsOwner) return;
        GameOverlayUI.Singleton.setGold(current);
    }
    #endregion

    #region RPC's
    [ServerRpc]
    public void SetCurrentHealthServerRpc(float health) {
        Debug.Log("setting health " + health);
        currentHealth.Value = health;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddGoldServerRpc(int gold) {
        currentGold.Value += gold;
    }

    [ClientRpc]
    public void SetPositionClientRpc(Vector3 position) {
        Debug.Log("Setting player position to: " + position + " on " + OwnerClientId);
        transform.position = position;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(NetworkObjectReference dealer, float damage) {
        Debug.Log("Player taking damage" + OwnerClientId);

        if (dealer.TryGet(out NetworkObject dealerObj)) {
            LastDamageSource = dealerObj.transform;
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{OwnerClientId}
            }
        };

        currentHealth.Value -= damage;
        if (currentHealth.Value > _stats.GetStat(StatType.MaxHealth)) {
            currentHealth.Value = _stats.GetStat(StatType.MaxHealth);
        }

        TakeDamageClientRpc(damage, clientRpcParams);
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

    [ServerRpc(RequireOwnership = false)]
    public void TakeDebuffServerRpc(Debuff debuff, float duration, Vector3 value, bool isMult = false) {
        ClientRpcParams clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{OwnerClientId}
            }
        };
        TakeDebuffClientRpc(debuff, duration, value, isMult, clientRpcParams);
    }

    [ServerRpc]
    public void DespawnServerRpc() {
        GetComponent<NetworkObject>().Despawn();
    }
    
    [ClientRpc]
    public void TakeDebuffClientRpc(Debuff debuff, float duration, float value, bool isMult = false, ClientRpcParams p = default) {

        if (debuffTimer.ContainsKey(debuff)) {
            debuffTimer[debuff] += duration * GetStunMultiplier();
        } else {
            debuffTimer.Add(debuff, duration * GetStunMultiplier());
        }

        switch (debuff) {
            case Debuff.Stun:
                StartCoroutine(performActionAfterCondition(() => {
                    return debuffTimer.ContainsKey(debuff) && debuffTimer[debuff] > 0.0f;
                }, () => {
                    DisableInputs();
                    _animator.Play(StunAnim.name);
                }, () => {
                    EnableInputs();
                }));
                break;

            case Debuff.JumpSlow:
                StartCoroutine(performActionAfterCondition(() => {
                    return debuffTimer.ContainsKey(debuff) && debuffTimer[debuff] > 0.0f;
                }, () => {
                    //_stats.AddToMultiMod(StatType.JumpForce, value);
                }, () => {
                    //_stats.AddToMultiMod(StatType.JumpForce, -value);
                }));
                break;
        }

        Debug.Log("Player: " + OwnerClientId + " Debuff: " + debuff + " " + debuffTimer[debuff]);
    }

    [ClientRpc]
    public void TakeDebuffClientRpc(Debuff debuff, float duration, Vector3 value, bool isMult = false, ClientRpcParams p = default) {
        switch (debuff) {
            case Debuff.Knockback:
                _rigidbody.AddForce(value * GetKnockbackMultiplier());
                break;
            default:
                Debug.LogError("Unhandled debuff " + debuff.ToString());
                break;
        }
    }

    [ClientRpc]
    public void TakeDamageClientRpc(float damage, ClientRpcParams p = default) {
        var number = GameObject.Instantiate(takeDamageNumber, transform.position + Vector3.up, Quaternion.identity);
        number.GetComponent<DamageNumber>().SetDamage(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnClientsSpawnedServerRpc(ServerRpcParams rpcParams = default) {
        currentHealth.SetDirty(true);
        currentGold.SetDirty(true);
    }

    [ServerRpc(RequireOwnership=false)]
    public void RefreshStatsUIServerRpc() {
        RefreshStatsUIClientRpc();
    }

    [ClientRpc]
    public void RefreshStatsUIClientRpc() {
        currentHealth_OnValueChanged(currentHealth.Value, currentHealth.Value);
        currentGold_OnValueChanged(currentGold.Value, currentGold.Value);
        
        if (!IsOwner) return;
        GameOverlayUI.Singleton.setMaxHealth(_stats.GetStat(StatType.MaxHealth), currentHealth.Value);
        ShopOverlay.Singleton.SetItems(items);
    }

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
        //pp.rotationalOffset = 180;
        Physics2D.IgnoreCollision(_hurtbox, pp.GetComponent<Collider2D>(), true);
        yield return new WaitForSeconds(0.5f);
        Physics2D.IgnoreCollision(_hurtbox, pp.GetComponent<Collider2D>(), false);
        //pp.rotationalOffset = 0;
    }

    protected IEnumerator WaitForAnimationComplete(float seconds) {
        waitingForAnimationComplete = true;
        yield return new WaitForSeconds(seconds);
        waitingForAnimationComplete = false;
    }

    protected IEnumerator performActionAfterCondition(Func<bool> testCondition, Action first, Action second) {
        first();
        yield return new WaitForEndOfFrame();
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

    protected void CooldownSkill(int skill) {
        switch (skill) {
            case 1:
                StartCoroutine(performTimedAction(_stats.GetStat(StatType.Skill1Cooldown), () => skill1Ready = false, () => skill1Ready = true));
            return;
            case 2:
                StartCoroutine(performTimedAction(_stats.GetStat(StatType.Skill2Cooldown), () => skill2Ready = false, () => skill2Ready = true));
            return;
            case 3:
                StartCoroutine(performTimedAction(_stats.GetStat(StatType.Skill3Cooldown), () => skill3Ready = false, () => skill3Ready = true));
            return;
            default:
                Debug.LogError("Invalid skill cooldown: " + skill);
            return;
        }
    }
    
    protected Vector2 GetNormalizedAim() {
        var dir = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return (dir - (Vector2)transform.position).normalized;
    }

    // Used for stuns / ability overrides
    protected void DisableInputs() {
        _characterController.Disable();
        isDisabled = true;
    }

    protected void EnableInputs() {
        _characterController.Enable();
        isDisabled = false;
    }

    protected void OnGameEnd(TeamColor loser) {
        _characterController.Disable();
    }

    protected IEnumerator Spawn() {
        //GameOverlayUI.Singleton.PlaySpawnAnimation();
        transform.position = new Vector3(0.0f, 2000.0f, 0.0f);
        yield return new WaitForSeconds(timeToSpawn);
        SetCurrentHealthServerRpc(_stats.GetStat(StatType.MaxHealth));
        transform.position = GameplayManager.Singleton.GetSpawn(teamColor.Value).position;
        isAlive = true;
    }

    protected void OnDeath() {
        Debug.Log("Dead: " + OwnerClientId);
        LastDamageSource?.GetComponent<IHasGold>()?.AddGoldServerRpc(PlayerGoldValue);
        isAlive = false;
        StartCoroutine(Spawn());
    }

    public void AddItem(Item item) {
        items.Add(item);
        _stats.AddItem(items);
    }

    protected float GetKnockbackMultiplier() {
        return 1 + (LowHealthKnockbackMulti * (1 - (currentHealth.Value / _stats.GetStat(StatType.MaxHealth))));
    }

    protected float GetStunMultiplier() {
        return 1 + (LowHealthStunMulti * (1 - (currentHealth.Value / _stats.GetStat(StatType.MaxHealth))));
    }

    #endregion
    #endregion
}
