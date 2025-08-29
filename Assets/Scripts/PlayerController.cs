using UnityEngine;

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
    public int maxHealth = 100;
    public float knockbackForce = 15f;
    public int attackDamage = 50;
    public float attackRange = 1.5f;
    public Transform attackPoint;

    private CharacterController characterController;
    private Animator animator;
    private Transform mainCameraTransform;
    private Transform playerModel;

    private Vector3 playerVelocity;
    private Vector3 knockbackVelocity;
    private bool isGrounded;
    private int currentHealth;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCameraTransform = Camera.main.transform;
        playerModel = animator.transform;

        currentHealth = maxHealth;
        UpdateHealthUI(); // 自身のHP UIを更新
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        Vector3 finalMove = Vector3.zero;

        if (knockbackVelocity.magnitude > 0.2f)
        {
            finalMove += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 5f * Time.deltaTime);
        }
        else
        {
            finalMove += HandleMovementAndRotation();
            HandleInteraction();
            HandleAttack();
        }

        playerVelocity.y += gravity * Time.deltaTime;
        finalMove += playerVelocity;
        
        characterController.Move(finalMove * Time.deltaTime);
    }
    
    private Vector3 HandleMovementAndRotation()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        bool isDashing = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isDashing ? dashSpeed : moveSpeed;
        
        Vector3 moveDirection = (mainCameraTransform.forward * verticalInput + mainCameraTransform.right * horizontalInput);
        moveDirection.y = 0;
        moveDirection.Normalize();

        float animationSpeed = new Vector2(horizontalInput, verticalInput).magnitude;
        animator.SetFloat("Speed", animationSpeed, 0.1f, Time.deltaTime);

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            animator.SetTrigger("Jump");
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

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

                MissionGiver missionGiver = hit.collider.GetComponent<MissionGiver>();
                if (missionGiver != null) missionGiver.OnInteract();
            }
        }
    }
    
    void HandleAttack()
    {
        // 攻撃硬直中などは攻撃できないようにする、などのロジックを後で追加できる
        if (Input.GetMouseButtonDown(0) && isGrounded) // 地上にいる時だけ攻撃可能にする
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
                        Vector3 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;
                        enemyHealth.TakeDamage(attackDamage, knockbackDirection);
                    }
                }
            }
        }
    }

    public void TakeDamage(int damage, Vector3 knockbackDirection)
    {
        currentHealth -= damage;
        UpdateHealthUI();
        
        knockbackVelocity = knockbackDirection * knockbackForce;
        
        if (currentHealth <= 0) Die();
    }
    
    // 自身のHPが変動した時に、GameManagerが持つUIの参照を直接更新する
    void UpdateHealthUI()
    {
        if (GameManager.instance != null && GameManager.instance.healthText != null)
        {
            GameManager.instance.healthText.text = "HP: " + currentHealth.ToString() + " / " + maxHealth.ToString();
        }
    }

    private void Die()
    {
        Debug.Log("プレイヤーが力尽きた...");
        GameManager.instance.GameOver();
    }
}