using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10.0f; // 追加: キャラクターの旋回速度
    public float jumpHeight = 1.2f;

    private CharacterController characterController;
    private Vector3 playerVelocity;
    private Transform mainCameraTransform; // 追加: メインカメラのTransform

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        // 追加: 最初に一度だけメインカメラの情報を取得しておく
        mainCameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // --- 移動方向の計算（カメラの向きを基準にする）---
        // 変更点: カメラの前方方向と右方向を基準に
        Vector3 cameraForward = mainCameraTransform.forward;
        Vector3 cameraRight = mainCameraTransform.right;

        // 変更点: Y軸（高さ）の情報を無視して、水平なベクトルにする
        cameraForward.y = 0;
        cameraRight.y = 0;

        // 変更点: 正規化して長さを1に保つ
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;


        // --- 水平方向の移動 ---
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // --- キャラクターの向きを移動方向に合わせる ---
        // 追加: 移動入力がある場合のみ向きを変える
        if (moveDirection != Vector3.zero)
        {
            // 目的の向きを計算
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // 現在の向きから目的の向きへ、滑らかに回転させる
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }


        // --- 垂直方向（重力）の計算と適用 ---
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }
}