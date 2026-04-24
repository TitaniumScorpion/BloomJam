using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 80f;
    public float lifetime = 2f;
    public int damage = 1; // Standard swarmer takes 1 shot, so 1 damage is perfect

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Energy projectiles fly straight
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Prevents fast bullets from passing through walls
        
        // Instantly apply velocity so it flies forward
        rb.linearVelocity = transform.forward * speed;

        // Destroy after a set time to prevent cluttering the scene
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // TODO: Check if the 'other' collider is an enemy and apply damage
        
        // Destroy the projectile upon hitting anything
        Destroy(gameObject);
    }
}