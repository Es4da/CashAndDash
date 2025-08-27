using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使うために必要

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent; // 敵を動かすためのNavMeshAgent
    private Transform player;   // 追いかける対象（プレイヤー）のTransform

    void Start()
    {
        // 自分のNavMeshAgentコンポーネントを取得
        agent = GetComponent<NavMeshAgent>();
        // "Player"というタグがついたオブジェクトを探して、そのTransformを取得
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        // 毎フレーム、プレイヤーの位置を目的地として設定する
        agent.SetDestination(player.position);
    }
}