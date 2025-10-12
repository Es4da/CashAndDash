using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使うために必要
using System.Collections; // コルーチンを使うために必要

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 300;
    public float knockbackForce = 10f; // 追加: 敵のノックバックの強さ
    public float knockbackDuration = 0.3f; // 追加: 敵のノックバックの時間
    public AudioClip hitSfx;

    private int currentHealth;
    private NavMeshAgent agent; // 追加: NavMeshAgentへの参照
    private EnemyAI enemyAI;

    void OnEnable()
    {
        // HPを全回復
        currentHealth = maxHealth;
    }

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>(); // 追加: NavMeshAgentを取得
        enemyAI = GetComponent<EnemyAI>();
    }

    // 変更: knockbackDirectionを受け取れるようにする
    public void TakeDamage(int damage, Vector3 knockbackDirection)
    {
        GameManager.instance.DoHitStop(0.1f);
        if (hitSfx != null)
        {
            AudioManager.instance.PlaySfx(hitSfx);
        }
        currentHealth -= damage;
        Debug.Log(gameObject.name + "が" + damage + "ダメージを受けた！ 残りHP: " + currentHealth);

        // 追加: ノックバックのコルーチンを開始
        if (agent != null)
        {
            StartCoroutine(KnockbackCoroutine(knockbackDirection));
        }

        if (enemyAI != null)
        {
            enemyAI.OnDamaged();
        }

        if (agent != null)
        {
            StartCoroutine(KnockbackCoroutine(knockbackDirection));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 追加: ノックバック処理の本体
    private IEnumerator KnockbackCoroutine(Vector3 knockbackDirection)
    {
        // agentの制御を一時的に無効にして、速度を直接上書きする
        agent.velocity = knockbackDirection * knockbackForce;
        agent.isStopped = true; // AIの追跡を一時停止

        yield return new WaitForSeconds(knockbackDuration);

        agent.isStopped = false; // AIの追跡を再開
        agent.velocity = Vector3.zero; // 速度をリセット
    }

    void Die()
    {
        Debug.Log(gameObject.name + "を倒した！");
        GameManager.instance.RespawnEnemy(gameObject);
        // ★変更: 自分自身を破壊せず、非表示にする
        gameObject.SetActive(false);
    }
}