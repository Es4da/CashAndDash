using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float dashSpeed = 10.0f;
    public float rotationSpeed = 15.0f;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Interaction")]
    public float interactDistance = 3f;
    public Transform playerEyes;

    [Header("Combat")]
    public float knockbackForce = 15f;
    public int attackDamage = 50;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public CinemachineFreeLook freeLookCamera;
    public float cameraStartAngleOffset = 180f;
    public Transform attackPoint;
    private CharacterController characterController;
    private Animator animator;
    private Transform mainCameraTransform;
    private Transform playerModel;
    private bool isDead = false;

    private Vector3 playerVelocity;
    private Vector3 knockbackVelocity;
    private bool isGrounded;
    private AudioSource footstepSource;
    private float currentAttackCooldown = 0f;
    private HighlightEffect currentHighlight;
    private float currentStamina; // ★追加
    private float staminaRegenTimer;
    public bool IsOnPlatform { get; private set; }

    [Header("Audio")]
    public AudioClip attackSfx;
    public AudioClip hitSfx;
    public AudioClip jumpSfx;
    [Header("Stamina")] // ★追加
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f; // 1秒間に消費するスタミナ
    public float staminaRegenRate = 15f; // 1秒間に回復するスタミナ
    public float staminaRegenDelay = 2f; // 回復が始まるまでの待機時間
    [Header("UI")] // ★追加: UI参照用のヘッダー
    public Image damageFlashPanel;
    public float flashDuration = 0.1f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCameraTransform = Camera.main.transform;
        playerModel = animator.transform;

        StartCoroutine(InitializePlayer());
        footstepSource = GetComponent<AudioSource>();
    }
    private IEnumerator InitializePlayer()
    {
        // まず1フレームだけ待つ。これにより、他の全てのオブジェクトが準備完了するのを待つ。
        yield return null;

        currentStamina = maxStamina;
        UpdateHealthUI();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (isDead) return;
        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
        }
        CheckGroundedStatus();
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // 常にプレイヤーの入力を受け付ける
        Vector3 finalMove = HandleMovementAndRotation();
        HandleInteraction();
        HandleAttack();

        // ノックバックの勢いが残っていれば、それを移動量に「加算」する
        if (knockbackVelocity.magnitude > 0.2f)
        {
            finalMove += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 5f * Time.deltaTime);
        }

        // 重力を適用
        playerVelocity.y += gravity * Time.deltaTime;
        finalMove += playerVelocity;

        // 最終的な移動命令
        characterController.Move(finalMove * Time.deltaTime);
        HandleFootsteps();
    }
    private void CheckGroundedStatus()
    {
        RaycastHit hit;
        // 足元から真下に短いRayを飛ばす
        if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, 0.3f, groundMask))
        {
            isGrounded = true;
            // 当たった地面のレイヤーが"Platform"かどうかを判別
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Platform"))
            {
                IsOnPlatform = true;
            }
            else
            {
                IsOnPlatform = false;
            }
        }
        else
        {
            isGrounded = false;
            IsOnPlatform = false;
        }
    }
    void HandleFootsteps()
    {
        // ★★★ ここからが最重要修正点 ★★★
        // もしAudioManagerが演出モードなら
        if (AudioManager.instance.isCutsceneMode)
        {
            // 足音は絶対に止める
            if (footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
            return; // そして、以降の処理は一切しない
        }
        // ★★★ ここまで ★★★

        // 地面にいて、かつ水平方向の速度が少しでもあれば
        if (isGrounded && new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude > 0.1f)
        {
            // もし足音が再生されていなければ
            if (!footstepSource.isPlaying)
            {
                // 再生を開始する
                footstepSource.Play();
            }
        }
        else
        {
            // 上の条件を満たさない場合（空中にいる、または止まっている）
            if (footstepSource.isPlaying)
            {
                // 再生を停止する
                footstepSource.Stop();
            }
        }
    }
    public void StopFootsteps()
    {
        if (footstepSource != null && footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    private Vector3 HandleMovementAndRotation()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        bool isDashing = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0;
        float currentSpeed = isDashing ? dashSpeed : moveSpeed;

        Vector3 moveDirection = (mainCameraTransform.forward * verticalInput + mainCameraTransform.right * horizontalInput);
        moveDirection.y = 0;
        moveDirection.Normalize();

        float animationSpeed = new Vector2(horizontalInput, verticalInput).magnitude;
        animator.SetFloat("Speed", animationSpeed, 0.1f, Time.deltaTime);

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            animator.SetTrigger("Jump");
            AudioManager.instance.PlaySfx(jumpSfx);
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        HandleStamina(isDashing);
        return moveDirection * currentSpeed;
    }
    void HandleStamina(bool isDashing)
    {
        if (isDashing)
        {
            // ダッシュ中ならスタミナを消費
            currentStamina -= staminaDrainRate * Time.deltaTime;
            staminaRegenTimer = 0f; // 回復タイマーをリセット
        }
        else
        {
            // ダッシュしていないなら回復の準備
            staminaRegenTimer += Time.deltaTime;
            if (staminaRegenTimer >= staminaRegenDelay && currentStamina < maxStamina)
            {
                // 一定時間経過後、スタミナを回復
                currentStamina += staminaRegenRate * Time.deltaTime;
            }
        }
        // スタミナが0未満や最大値を超えないように制限
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        // UIを更新
        GameManager.instance.UpdateStaminaUI(currentStamina, maxStamina);
    }

    void HandleInteraction()
{
    Vector3 rayOrigin = playerEyes.position;
    Vector3 rayDirection = Camera.main.transform.forward;
    RaycastHit hit;
    
    // 現在、視線の先に何がハイライトされるべきかを保持する変数
    HighlightEffect highlightableObject = null;

    // --- ステップ1: 視線の先のオブジェクトを調べる ---
    if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactDistance))
    {
        // 当たったオブジェクトからHighlightEffectコンポーネントを探す
        highlightableObject = hit.collider.GetComponent<HighlightEffect>();
    }

    // --- ステップ2: ハイライトの状態を管理する ---
    // もし、前回光らせていた物と、今回光らせるべき物が違うなら
    if (currentHighlight != highlightableObject)
    {
        // 前回光らせていた物があれば、まずそれを消灯する
        if (currentHighlight != null)
        {
            currentHighlight.ToggleHighlight(false);
        }
        
        // 新しく光らせるべき物があれば、それを点灯する
        if (highlightableObject != null)
        {
            highlightableObject.ToggleHighlight(true);
        }

        // 現在光っている物を更新する
        currentHighlight = highlightableObject;
    }
    
    // --- ステップ3: インタラクトの実行 ---
    // もし何かが光っている状態で、Fキーが押されたら
    if (currentHighlight != null && Input.GetKeyDown(KeyCode.F))
    {
        // 光っているオブジェクトから、各種コンポーネントを探して実行する
        TreasureBox treasure = currentHighlight.GetComponent<TreasureBox>();
        if (treasure != null) treasure.OnInteract();

        Van van = currentHighlight.GetComponent<Van>();
        if (van != null) van.OnInteract();

        MissionGiver missionGiver = currentHighlight.GetComponent<MissionGiver>();
        if (missionGiver != null) missionGiver.OnInteract();
    }
}

    void HandleAttack()
    {
        // 攻撃硬直中などは攻撃できないようにする、などのロジックを後で追加できる
        if (Input.GetMouseButtonDown(0) && isGrounded && currentAttackCooldown <= 0) // 地上にいる時だけ攻撃可能にする
        {
            currentAttackCooldown = attackCooldown;
            animator.SetTrigger("Attack");
            if (attackSfx != null)
            {
                AudioManager.instance.PlaySfx(attackSfx);
            }

            Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);
            foreach (Collider enemyCollider in hitEnemies)
            {
                if (enemyCollider.CompareTag("Enemy"))
                {
                    EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        Vector3 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;
                        enemyHealth.TakeDamage(attackDamage, knockbackDirection);
                    }
                }
            }
        }
    }

    public void TakeDamage(int damage, Vector3 knockbackDirection)
    {
        if (GameManager.instance.isPlayerInvincible)
        {
            return; // この関数の処理をここで中断する
        }
        GameManager.instance.DoHitStop(0.2f);
        GameManager.instance.playerCurrentHealth -= damage;
        AudioManager.instance.PlaySfx(hitSfx);
        UpdateHealthUI();
        StartCoroutine(DamageFlashEffect());

        knockbackVelocity = knockbackDirection * knockbackForce;

        if (GameManager.instance.playerCurrentHealth <= 0) Die();
    }
    private IEnumerator DamageFlashEffect()
    {
        if (damageFlashPanel == null) yield break;

        // フェードイン
        damageFlashPanel.color = new Color(1f, 0f, 0f, 0.15f); // 半透明の赤
        yield return new WaitForSeconds(flashDuration / 2);

        // フェードアウト
        damageFlashPanel.color = new Color(1f, 0f, 0f, 0f); // 透明に戻す
    }

    // 自身のHPが変動した時に、GameManagerが持つUIの参照を直接更新する
    public void UpdateHealthUI()
    {
        if (GameManager.instance != null && GameManager.instance.healthText != null)
        {
            GameManager.instance.healthText.text = "HP: " + GameManager.instance.playerCurrentHealth + " / " + GameManager.instance.playerMaxHealth;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("プレイヤーが力尽きた...");
        GameManager.instance.GameOver();
    }
    public void TriggerDeathAnimation()
    {
        animator.SetTrigger("Die");
    }
    void OnDrawGizmosSelected()
    {
        // attackPointが設定されていないとエラーになるので、チェックする
        if (attackPoint == null)
        {
            return;
        }

        // Gizmoの色を赤に設定
        Gizmos.color = Color.red;
        // attackPointの位置に、attackRangeを半径としたワイヤーフレームの球体を描画
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}