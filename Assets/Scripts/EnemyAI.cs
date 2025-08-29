using UnityEngine;
using UnityEngine.AI;
using System.Collections; // コルーチンを使うために必要

public class EnemyAI : MonoBehaviour
{
    public enum AIState { Patrolling, Chasing }
    [Header("Stats")]
    public float attackRange = 2f;
    public int attackDamage = 50;
    public float attackCooldown = 2.0f;
    public float attackWindUp = 0.5f; // 追加: 攻撃前の溜め時間
    [Header("Patrol")]
    public Transform[] patrolPoints;
    [Header("Vision")]
    public float viewRadius = 10f; // 視界の半径
    [Range(0, 360)]
    public float viewAngle = 90f;  // 視界の角度
    [Header("Stats")]
    public float patrolSpeed = 3.5f; // 追加: 巡回時の速度
    public float chaseSpeed = 7f;   // 追加: 追跡時の速度

    private NavMeshAgent agent;
    private Transform player;
    private float currentCooldown;
    private bool isAttacking = false; // 追加: 現在、攻撃シーケンス中かどうかのフラグ
    private AIState currentState; // 現在のAIの状態
    private int currentPatrolIndex;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentState = AIState.Patrolling; // 初期状態は「巡回」
        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();
    }

    void Update()
{
    // 現在の状態で処理を分岐する
    switch (currentState)
    {
        case AIState.Patrolling:
            // --- 巡回中の処理 ---
            // プレイヤーを発見したら、追跡モードに切り替え
            if (CanSeePlayer())
            {
                currentState = AIState.Chasing;
                agent.speed = chaseSpeed;
            }

            // 目的地に近づいたら、次の巡回地点へ
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                GoToNextPatrolPoint();
            }
            break;

        case AIState.Chasing:
            // --- 追跡中の処理（今までのUpdateとほぼ同じ） ---
            if (isAttacking) return;

            if (currentCooldown > 0)
            {
                currentCooldown -= Time.deltaTime;
            }
            else
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer <= attackRange)
                {
                    StartCoroutine(AttackSequence());
                }
            }

            if (!agent.isStopped)
            {
                agent.SetDestination(player.position);
            }
            break;
    }
}
    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return; // 巡回地点がなければ何もしない

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        // 次のインデックスを計算（最後まで行ったら最初に戻る）
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    // 攻撃シーケンス（溜め→攻撃→クールタイム）を管理するコルーチン
    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        agent.isStopped = true; // 溜めのためにその場に停止

        // --- 1. 溜め ---
        yield return new WaitForSeconds(attackWindUp);

        // --- 2. 攻撃 ---
        // 溜めが終わった後、まだプレイヤーが範囲内にいるか再チェック
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("敵の攻撃がヒット！");
                Vector3 knockbackDirection = (player.position - transform.position).normalized;
                playerController.TakeDamage(attackDamage, knockbackDirection);
            }
        }

        // --- 3. クールタイム ---
        currentCooldown = attackCooldown;
        isAttacking = false;
        agent.isStopped = false; // 追跡を再開
    }
    // 新しい関数：プレイヤーが見えるかどうかを判定する
    bool CanSeePlayer()
    {
        // 距離のチェック
        if (Vector3.Distance(transform.position, player.position) > viewRadius)
        {
            return false;
        }

        // 角度のチェック
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > viewAngle / 2)
        {
            return false;
        }

        // 障害物のチェック（Raycast）
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, viewRadius))
        {
            // Rayがプレイヤーに当たっていれば、障害物なしと判断
            if (hit.transform == player)
            {
                return true;
            }
        }

        return false;
    }
}