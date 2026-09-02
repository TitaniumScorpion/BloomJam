/// <summary>
/// Anything a player attack can damage. Implemented by every enemy and weak point so
/// attacks can dispatch damage with one lookup instead of testing each concrete type.
///
/// Deliberately NOT implemented by PlayerHealth — player attacks resolve their target
/// through this interface, and the player must never be a valid target for them.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}
