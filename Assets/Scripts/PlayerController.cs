using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float dashSpeed = 10.0f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10.0f;
    public float jumpHeight = 1.2f;

    // --- 地面判定用の変数を追加 ---
    public Transform groundCheck;      // 地面判定オブジェクトのTransform
    public float groundDistance = 0.2f; // 地面判定の球体の半径
    public LayerMask groundMask;       // 地面レイヤーを判別するためのマスク

    private CharacterController characterController;
    private Vector3 playerVelocity;
    private Transform mainCameraTransform;
    private bool isGrounded; // bool変数をUpdateの外に移動
    private Animator animator;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
        animator = GetComponentInChildren<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- 新しい地面判定 ---
        // groundCheckの位置に、半径groundDistanceの球体を作り、groundMaskに接触しているか判定
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // 0fから-2fに変更。より地面に吸い付きやすくなる
        }

        // --- 入力と移動 ---
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 追加: ダッシュボタン（デフォルトは左Shift）が押されているか？
        bool isDashing = Input.GetKey(KeyCode.LeftShift);
        // 押されていればdashSpeedを、そうでなければmoveSpeedを使用
        float currentSpeed = isDashing ? dashSpeed : moveSpeed;

        Vector3 cameraForward = mainCameraTransform.forward;
        Vector3 cameraRight = mainCameraTransform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // 変更: 水平方向の実際の速度を計算
        float speed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;
        // 変更: Animatorの"Speed"パラメータに値をセット
        animator.SetFloat("Speed", speed);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- ジャンプ処理 ---
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            animator.SetTrigger("Jump"); 
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // --- 垂直方向の移動 ---
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }
}