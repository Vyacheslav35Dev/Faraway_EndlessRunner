using Cysharp.Threading.Tasks;
using Game.Scripts.GameState.GameManager;
using Game.Scripts.Tracks;
using UnityEngine;

namespace Game.Scripts.GameState.States
{
    public class GameOverState : State
    {
        [SerializeField]
        private GameObject _rootCanvas;
        
        [SerializeField]
        private FinishPopup _finishPopup;
        
        [SerializeField] 
        private TrackManager _trackManager;

        public override async UniTask Enter(State from)
        {
            _rootCanvas.gameObject.SetActive(true);
            _finishPopup.gameObject.SetActive(true);
            _finishPopup.Init(GameOver);
        }

        public override async UniTask Exit(State to)
        {
            _rootCanvas.gameObject.SetActive(false);
            _finishPopup.gameObject.SetActive(false);
        }

        public override void Tick()
        {
            
        }

        public override StateType GetStateType()
        {
            return StateType.GameOver;
        }

        private void GameOver()
        {
            if (_trackManager.CharacterController.Coins > PlayerPrefs.GetInt("Score", 0))
            {
                PlayerPrefs.SetInt("Score", _trackManager.CharacterController.Coins);
            }
            _stateManager.SwitchState(StateType.Lobby);
        }
    }
}