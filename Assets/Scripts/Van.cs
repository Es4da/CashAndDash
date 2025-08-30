using UnityEngine;

public class Van : MonoBehaviour
{
    public AudioClip depositSfx;
    public void OnInteract()
    {
        if (GameManager.instance.currentMoney > 0 && depositSfx != null)
        {
            AudioManager.instance.PlaySfx(depositSfx);
        }
        GameManager.instance.DeliverMoney();
    }
}