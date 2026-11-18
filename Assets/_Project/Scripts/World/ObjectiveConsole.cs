using UnityEngine;
namespace ContainerShip
{
    /// <summary>The control-room console. Hold the Interact key (F) to activate it and win.</summary>
    public class ObjectiveConsole : MonoBehaviour, IInteractable
    {
        public float holdTime = 2.5f;
        public Renderer screen;
        public string prompt = "HOLD [F]  ACTIVATE CONTROL CONSOLE";
        bool _done;

        public string Prompt => prompt;
        public float HoldTime => holdTime;
        public bool CanInteract => !_done;

        public void Interact(GameObject interactor)
        {
            if (_done) return;
            _done = true;
            SoundFX.Play2D(SoundFX.NVOn, 0.7f);
            if (screen != null) screen.material.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1f) * 2f);
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Complete();
            else if (GameManager.Instance != null) GameManager.Instance.Win();
        }
    }
}
