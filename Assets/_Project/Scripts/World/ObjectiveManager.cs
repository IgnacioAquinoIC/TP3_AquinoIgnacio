using UnityEngine;
namespace ContainerShip
{
    public class ObjectiveManager : MonoBehaviour
    {
        public static ObjectiveManager Instance { get; private set; }
        public string CurrentObjective = "Reach the control room";
        public bool Completed { get; private set; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetObjective(string text) { if (!Completed) CurrentObjective = text; }

        public void Complete()
        {
            if (Completed) return;
            Completed = true;
            CurrentObjective = "Objective complete";
            GameEvents.Message("OBJECTIVE COMPLETE", NotifyKind.Success);
            if (GameManager.Instance != null) GameManager.Instance.Win();
        }
    }
}
