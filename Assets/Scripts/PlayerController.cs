using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // --- Public Variables ---
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float dashSpeed = 10.0f;
    public float rotationSpeed = 15.0f; // 回転を速くするため少し値を上げました

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -20f; // 少し重力を強くすると、よりキビキビ動きます
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Interaction")]
    public float interactDistance = 3f;
    public Transform playerEyes;

    [Header("Combat")]
    public int maxHealth = 100;
    public float knockbackForce = 15f;
    public int attackDamage = 50;
    public float attackRange = 1.5f;
    public Transform attackPoint;

    // --- Private Variables ---
    private CharacterController characterController;
    private Animator animator;
    private Transform mainCameraTransform;
    private Transform playerModel; // ★追加: モデルのTransform

    private Vector3 playerVelocity;
    private Vector3 knockbackVelocity;
    private bool isGrounded;
    private int currentHealth;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCameraTransform = Camera.main.transform;

        // ★修正点: Playerオブジェクトの直下にある見た目の親を見つける ★
        // Animatorが付いているオブジェクトの親（つまり見た目の一番親）を取得するか、
        // もしAnimatorがPlayer直下に付いているならそのままAnimatorのTransformを使う
        if (animator.transform.parent != transform) // Animatorの親がPlayer自身でなければ
        {
            playerModel = animator.transform.parent; // Animatorの親を使う
        }
        else
        {
            playerModel = animator.transform; // Animator自体がPlayer直下ならそれを使う
        }
        // もしくは、AnimatorコンポーネントがアタッチされているGameObjectを探す
        // playerModel = animator.gameObject.transform; // これでも良い場合があります

        currentHealth = maxHealth;
        FindObjectOfType<GameManager>().UpdateHealthUI(currentHealth, maxHealth);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. 状態の更新（地面判定）
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // --- 2. 移動量の計算 ---
        Vector3 finalMove = HandleMovementAndRotation();
        HandleInteraction();
        HandleAttack();

        if (knockbackVelocity.magnitude > 0.2f)
        {
            finalMove += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 5f * Time.deltaTime);
        }
        
        // 3. 重力の適用
        playerVelocity.y += gravity * Time.deltaTime;
        finalMove += playerVelocity;
        
        // 4. 最終的な移動命令
        characterController.Move(finalMove * Time.deltaTime);
    }
    private Vector3 HandleMovementAndRotation()
    {
        // --- 入力と速度の計算 ---
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        bool isDashing = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isDashing ? dashSpeed : moveSpeed;

        // --- 移動方向の計算 ---
        Vector3 moveDirection = (mainCameraTransform.forward * verticalInput + mainCameraTransform.right * horizontalInput);
        // 移動に使うベクトルからはY軸（上下）の情報を完全に抜き去る
        moveDirection.y = 0;
        // 水平にした後で、長さを1に正規化する（これにより斜め移動でも速度が一定になる）
        moveDirection.Normalize();

        // --- アニメーションの更新 ---
        float animationSpeed = new Vector2(horizontalInput, verticalInput).magnitude;
        animator.SetFloat("Speed", animationSpeed, 0.1f, Time.deltaTime);

        // --- ジャンプ ---
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            animator.SetTrigger("Jump");
            // playerVelocity.yに直接代入することで、ジャンプの連打を防ぐ
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // --- 回転処理 ---
        if (moveDirection != Vector3.zero)
        {
            // 回転には、すでに水平になったmoveDirectionをそのまま使える
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- 最終的な移動量を返す ---
        return moveDirection * currentSpeed;
    }
    void HandleInteraction()
    {
        Vector3 rayOrigin = playerEyes.position;
        Vector3 rayDirection = Camera.main.transform.forward;
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactDistance))
        {
            if (hit.collider.CompareTag("Interactable") && Input.GetKeyDown(KeyCode.F))
            {
                TreasureBox treasure = hit.collider.GetComponent<TreasureBox>();
                if (treasure != null) treasure.OnInteract();

                Van van = hit.collider.GetComponent<Van>();
                if (van != null) van.OnInteract();
            }
        }
    }
    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Attack");

            Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);

            foreach (Collider enemyCollider in hitEnemies)
            {
                if (enemyCollider.CompareTag("Enemy"))
                {
                    EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        // 追加: ノックバックの方向を計算（プレイヤーから敵へ）
                        Vector3 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;
                        // 変更: 計算した方向をTakeDamage関数に渡す
                        enemyHealth.TakeDamage(attackDamage, knockbackDirection);
                    }
                }
            }
        }
    }

    public void TakeDamage(int damage, Vector3 knockbackDirection)
    {
        currentHealth -= damage;
        FindObjectOfType<GameManager>().UpdateHealthUI(currentHealth, maxHealth);
        knockbackVelocity = knockbackDirection * knockbackForce;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("プレイヤーが力尽きた...");
        FindObjectOfType<GameManager>().GameOver();
    }
}