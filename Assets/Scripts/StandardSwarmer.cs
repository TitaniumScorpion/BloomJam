/// <summary>
/// The basic swarm enemy: fragile, fast, and spawned in packs.
///
/// All of its behaviour is the shared flight AI in FlyingChaserEnemy — this type exists to
/// give the plain swarmer its own component identity, which prefabs and pool tags reference.
/// Add anything swarmer-specific here; tuning that should affect every flying chaser belongs
/// on the base class.
/// </summary>
public class StandardSwarmer : FlyingChaserEnemy
{
}
