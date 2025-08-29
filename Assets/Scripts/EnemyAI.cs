using UnityEngine;
using UnityEngine.AI;
using System.Collections; // コルーチンを使うために必要

public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    public float attackRange = 2f;
    public int attackDamage = 50;
    public float attackCooldown = 2.0f;
    public float attackWindUp = 0.5f; // 追加: 攻撃前の溜め時間

    private NavMeshAgent agent;
    private Transform player;
    private float currentCooldown;
    private bool isAttacking = false; // 追加: 現在、攻撃シーケンス中かどうかのフラグ

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (isAttacking) return; // 攻撃シーケンス中は他の処理をしない

        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }
        else
        {
            // プレイヤーと敵の距離を計算
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // 攻撃範囲内なら攻撃シーケンスを開始
            if (distanceToPlayer <= attackRange)
            {
                StartCoroutine(AttackSequence());
            }
        }
        
        // 常にプレイヤーを追いかける
        if (!agent.isStopped)
        {
            agent.SetDestination(player.position);
        }
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
}