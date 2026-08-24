using UnityEngine;

// Place this on a trigger collider inside the elevator cab, positioned so the player
// passes through it once they've fully walked back in after clearing a zone.
[RequireComponent(typeof(Collider))]
public class ElevatorEntryTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (ElevatorHub.IsActive) return; // Already in hub mode, nothing to do
        if (!QuotaManager.IsZoneCleared) return; // Zone still in progress — no early reset

        GameManager.Instance?.ReturnToElevatorHub();
    }
}
