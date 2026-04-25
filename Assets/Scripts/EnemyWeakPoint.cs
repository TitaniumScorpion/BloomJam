using UnityEngine;

public class EnemyWeakPoint : MonoBehaviour
{
    [Tooltip("Drag the parent object with the AdvancedEnemy script here")]
    public AdvancedEnemy parentEnemy;

    public void TakeDamage(int damage)
    {
        // Pass the damage up to the main boss script
        if (parentEnemy != null)
            parentEnemy.TakeDamage(damage);
    }
}