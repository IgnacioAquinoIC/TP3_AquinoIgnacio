using UnityEngine;
namespace ContainerShip
{
    public interface IInteractable
    {
        string Prompt { get; }
        float HoldTime { get; }
        bool CanInteract { get; }
        void Interact(GameObject interactor);
    }

    /// <summary>Looks at IInteractable objects in front of the camera; hold the Interact key (F) to use them.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public PlayerController player;
        public float range = 2.6f;
        public string Prompt { get; private set; } = "";
        public float Progress { get; private set; }

        IInteractable _current, _last; float _held;

        void Update()
        {
            Prompt = ""; Progress = 0f; _current = null;
            if (!GameManager.IsPlaying || InputReader.Instance == null) { _held = 0f; return; }
            var cam = player.playerCamera.transform;
            if (Ballistics.Raycast(cam.position, cam.forward, range, transform, out RaycastHit hit))
            {
                var it = hit.collider.GetComponentInParent<IInteractable>();
                if (it != null && it.CanInteract) _current = it;
            }
            if (_current != _last) _held = 0f;
            _last = _current;
            if (_current == null) { _held = 0f; return; }

            Prompt = _current.Prompt;
            if (InputReader.Instance.Interact.IsPressed())
            {
                _held += Time.deltaTime;
                Progress = _current.HoldTime <= 0f ? 1f : Mathf.Clamp01(_held / _current.HoldTime);
                if (Progress >= 1f) { _current.Interact(gameObject); _held = 0f; }
            }
            else _held = 0f;
        }
    }
}
