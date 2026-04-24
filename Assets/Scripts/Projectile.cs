using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 80f;
    public float lifetime = 2f;
    public int damage = 1; // Standard swarmer takes 1 shot, so 1 damage is perfect

    private Rigidbody rb;

    private void Awake()
    {
        // Awake is called once when the object is first instantiated by the ObjectPooler
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Energy projectiles fly straight
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Prevents fast bullets from passing through walls
    }

    private void OnEnable()
    {
        // OnEnable is called every time the ObjectPooler activates this bullet
        rb.linearVelocity = transform.forward * speed;

        // Deactivate after a set time instead of destroying
        Invoke(nameof(Deactivate), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke(); // Clean up the invoke if the bullet hits something early
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // TODO: Check if the 'other' collider is an enemy and apply damage
        
        // Deactivate the projectile upon hitting anything
        gameObject.SetActive(false);
    }
}