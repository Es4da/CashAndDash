using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public float attackRange = 2f;
    public int attackDamage = 30;
    public float attackCooldown = 2.0f;

    private NavMeshAgent agent;
    private Transform player;
    private float currentCooldown;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentCooldown = 0;
    }

    void Update()
    {
        // クールタイムが終わったかどうかをチェック
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0)
            {
                // クールタイム終了！追跡を再開
                agent.isStopped = false;
            }
        }

        // プレイヤーと敵の距離を計算
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 攻撃範囲内で、かつクールタイムが終わっていたら攻撃
        if (distanceToPlayer <= attackRange && currentCooldown <= 0)
        {
            AttackPlayer();
        }
        
        // エージェントが停止中でなければ、プレイヤーを追いかける
        if (!agent.isStopped)
        {
            agent.SetDestination(player.position);
        }
    }

    void AttackPlayer()
    {
        Debug.Log("敵がプレイヤーに攻撃！");

        // 追跡を停止！
        agent.isStopped = true;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Vector3 knockbackDirection = (player.position - transform.position).normalized;
            playerController.TakeDamage(attackDamage, knockbackDirection);
        }
        
        // クールタイムを設定
        currentCooldown = attackCooldown;
    }
}