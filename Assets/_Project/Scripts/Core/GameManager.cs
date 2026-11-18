using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ContainerShip
{
    public enum GameState { Playing, Victory, Defeat }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static bool IsPlaying => Instance == null || Instance.State == GameState.Playing;

        public Health playerHealth;
        public GameState State { get; private set; } = GameState.Playing;
        public event Action<GameState> StateChanged;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Start() { if (playerHealth != null) playerHealth.Died += _ => Lose(); }

        void Update()
        {
            if (State != GameState.Playing && InputReader.Instance != null &&
                InputReader.Instance.Restart.WasPressedThisFrame()) Restart();
        }

        public void Win() { End(GameState.Victory); }
        public void Lose() { End(GameState.Defeat); }

        void End(GameState s)
        {
            if (State != GameState.Playing) return;
            State = s;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StateChanged?.Invoke(s);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
