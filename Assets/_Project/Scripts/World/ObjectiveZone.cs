using UnityEngine;
namespace ContainerShip
{
    /// <summary>Trigger volume. Entering it updates the current objective text.</summary>
    [RequireComponent(typeof(Collider))]
    public class ObjectiveZone : MonoBehaviour
    {
        public string newObjective = "Activate the control console";
        public string message = "CONTROL ROOM REACHED";
        bool _used;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (_used || other.GetComponentInParent<PlayerController>() == null) return;
            _used = true;
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.SetObjective(newObjective);
            GameEvents.Message(message, NotifyKind.Info);
        }
    }
}
