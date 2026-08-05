using System;
using UnityEngine;

namespace MayorOfMedieval.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState { Playing, Paused, GameOver }

        [Header("Manager References")]
        [SerializeField] private GameObject resourceManagerRef;
        [SerializeField] private GameObject gridSystemRef;

        public GameState CurrentState { get; private set; } = GameState.Playing;

        public event Action<GameState, GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;
            GameState oldState = CurrentState;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(oldState, newState);
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
                SetGameState(GameState.Paused);
            else if (CurrentState == GameState.Paused)
                SetGameState(GameState.Playing);
        }
    }
}