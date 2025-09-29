using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq; // FindObjectsOfTypeで使う可能性のため（今回は不要ですが作法として）
using System.Collections.Generic; // Listを使うため


public class EnemyAI : MonoBehaviour
{
    // AIの状態を定義
    public enum AIState { Idle, Chasing, Attacking }

    [Header("Stats")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 7f;
    public int attackDamage = 50;
    public float attackRange = 2f;
    public float attackWindUp = 0.5f;
    public float attackCooldown = 2.0f;

    [Header("Vision")]
    public float viewRadius = 10f;
    [Range(0, 360)]
    public float viewAngle = 90f;

    [Header("Patrol")]
    public Transform[] patrolPoints;

    [Header("Detection")]
    public GameObject alertIconPrefab;
    public AudioClip alertSfx;
    public Vector3 iconOffset = new Vector3(0, 2.5f, 0);

    // --- Private Variables ---
    private NavMeshAgent agent;
    private Transform player;
    private AIState currentState;
    private int currentPatrolIndex;
    private float lastAttackTime; // 最後に攻撃した時間
    private Animator animator;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // ★重要: 復活時は必ずIdle状態から始める
        currentState = AIState.Idle;
        agent.isStopped = true; // 最初は停止
        lastAttackTime = 0;
    }

    void Update()
    {
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsOnPlatform)
        {
            // もしプレイヤーがプラットフォーム上にいるなら、AIの思考を停止する
            agent.isStopped = true;
            return; // このフレームの以降の処理はすべて中断
        }
        else
        {
            // プレイヤーがプラットフォーム上にいないなら、AIの思考を再開させる
            // ただし、攻撃シーケンスの邪魔はしないようにする
            if (currentState != AIState.Attacking)
            {
                agent.isStopped = false;
            }
        }
        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
        // 状態に応じた処理を実行
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Chasing:
                UpdateChasing();
                break;
            case AIState.Attacking:
                // Attacking状態では何もしない（コルーチンが全てを管理するため）
                break;
        }
    }

    // --- 各状態のアップデート関数 ---

    void UpdateIdle()
    {
        // プレイヤーを発見したら、追跡モードに切り替え
        if (CanSeePlayer())
        {
            currentState = AIState.Chasing;
            agent.speed = chaseSpeed;
            
            // 2. 発見時の効果音を鳴らす
            AudioManager.instance.PlaySfx(alertSfx);

            // 3. 「！」アイコンを表示するコルーチンを開始
            StartCoroutine(ShowAlertIcon());
        }

        // 目的地に近づいたら、次の巡回地点へ
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPatrolPoint();
        }
    }
    
    private IEnumerator ShowAlertIcon()
    {
        // 自分の頭上の位置に「！」プレハブを生成
        GameObject icon = Instantiate(alertIconPrefab, transform.position + iconOffset, Quaternion.identity, transform);

        // アイコンが常にカメラの方を向くようにする
        Transform playerCamera = Camera.main.transform;

        float timer = 0f;
        while (timer < 1.5f)
        {
            // LookAtでカメラの方向を向かせ、Y軸とZ軸の回転を固定してビルボード効果を作る
            icon.transform.LookAt(playerCamera);
            icon.transform.rotation = Quaternion.Euler(0, icon.transform.eulerAngles.y, 0);

            timer += Time.deltaTime;
            yield return null; // 1フレーム待つ
        }

        // 1.5秒後にアイコンを破壊する
        Destroy(icon);
    }

    void UpdateChasing()
    {
        // 追跡中にプレイヤーを見失ったら、巡回モードに戻る（オプション）
        // if (!CanSeePlayer())
        // {
        //     currentState = AIState.Patrolling;
        //     agent.speed = patrolSpeed;
        //     GoToNextPatrolPoint();
        //     return;
        // }
        NavMeshHit navMeshHit;
        if (NavMesh.SamplePosition(player.position, out navMeshHit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(navMeshHit.position);
        }
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 攻撃範囲に入り、かつクールダウンが終わっていたら攻撃状態に移行
        if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackSequence());
        }
        else
        {
            // 範囲外なら、追いかけ続ける
            agent.SetDestination(player.position);
        }
    }

    // --- 行動関数 ---

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
    
    IEnumerator AttackSequence()
    {
        currentState = AIState.Attacking; // 攻撃状態に移行
        agent.isStopped = true; // その場に停止

        // 1. 溜め
        yield return new WaitForSeconds(attackWindUp);

        // 2. 攻撃実行（溜めが終わった後もまだ範囲内にいたら）
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Vector3 knockbackDirection = (player.position - transform.position).normalized;
                playerController.TakeDamage(attackDamage, knockbackDirection);
            }
        }
        
        lastAttackTime = Time.time; // 最後に攻撃した時間を記録

        // 3. 攻撃終了後、追跡状態に戻る
        agent.isStopped = false;
        currentState = AIState.Chasing;
    }

    bool CanSeePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) > viewRadius)
            return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > viewAngle / 2)
            return false;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, viewRadius))
        {
            if (hit.transform == player)
                return true;
        }
        return false;
    }
}