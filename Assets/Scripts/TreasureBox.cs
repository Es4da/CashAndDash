using UnityEngine;

public class TreasureBox : MonoBehaviour
{
    public int moneyValue = 100;

    private Collider boxCollider; // 追加: 当たり判定への参照
    private MeshRenderer meshRenderer; // 追加: 見た目への参照

    void Awake()
    {
        // 起動時に、自分のコンポーネントを取得しておく
        boxCollider = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();

        // ゲーム開始時は非表示＆非アクティブにする
        SetInactive();
    }

    // プレイヤーから呼び出される関数
    public void OnInteract()
    {
        GameManager.instance.AddMoney(moneyValue);
        Destroy(gameObject);
    }

    // ★追加: 宝箱を「起動」する関数
    public void Activate()
    {
        boxCollider.enabled = true;
        meshRenderer.enabled = true;
        // ここに、起動した時の音や光のエフェクトを後で追加できる
    }

    // ★追加: 宝箱を「非表示」にする関数
    public void SetInactive()
    {
        boxCollider.enabled = false;
        meshRenderer.enabled = false;
    }
}