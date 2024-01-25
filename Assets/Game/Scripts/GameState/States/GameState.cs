using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.GameState.GameManager;
using Game.Scripts.Infra.Music;
using Game.Scripts.Tracks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = Game.Scripts.Character.CharacterController;

namespace Game.Scripts.GameState.States
{
    public class GameState : State
    {
        static int _deadHash = Animator.StringToHash("Dead");
        
        [Header("UI")]
        [SerializeField] 
        private Canvas _rootCanvas;
        
        [SerializeField] 
        private TMP_Text _coinText;
        
        [SerializeField] 
        private TMP_Text _lifeCountText;
        
        [SerializeField]
        private AudioClip _gameTheme;

        [SerializeField] 
        private TrackManager _trackManager;
        
        [SerializeField] 
        private GameObject _rootUiBackground;
        
        private bool _finished;

        private string _localizeLifeString;

        public override async UniTask Enter(State from)
        {
            _finished = false;
            _rootCanvas.gameObject.SetActive(true);

            _localizeLifeString = _lifeCountText.text;
            
            if (MusicPlayer.instance.GetStem(0) != _gameTheme)
            {
                MusicPlayer.instance.SetStem(0, _gameTheme);
                await MusicPlayer.instance.RestartAllStems();
            }

            await _trackManager.Begin();
            _rootUiBackground.gameObject.SetActive(false);
        }

        public override async UniTask Exit(State to)
        {
            _trackManager.gameObject.SetActive(false);
            _rootCanvas.gameObject.SetActive(false);
        }

        public override void Tick()
        {
            if (_trackManager.IsLoaded)
            {
                if (_trackManager.CharacterController.CurrentLife <= 0 && !_finished)
                {
                    WaitForGameOver().Forget();
                }
                
                CharacterController characterController = _trackManager.CharacterController;
                
                List<Consumable.Consumable> toRemove = new List<Consumable.Consumable>();

                foreach (var consumablesValue in characterController.consumables.Values)
                {
                    consumablesValue.Tick(characterController);
                    if (!consumablesValue.Active)
                    {
                        toRemove.Add(consumablesValue);
                    }
                }

                foreach (var consumable in toRemove)
                {
                    consumable.Ended(characterController);

                    Addressables.ReleaseInstance(consumable.gameObject);
                    var type = consumable.GetConsumableType();
                    characterController.consumables.Remove(type);
                }
                
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            _coinText.text = _trackManager.CharacterController.Coins.ToString();
            _lifeCountText.text = $"{_localizeLifeString}: {_trackManager.CharacterController.CurrentLife}";
        }

        public override StateType GetStateType()
        {
            return StateType.Game;
        }
        
        private async UniTask WaitForGameOver()
        {
            if (_finished)
            {
                return;
            }
            _trackManager.CharacterController.character.animator.SetBool(_deadHash, true);
            
            _finished = true;
            _trackManager.StopMove();
            _trackManager.CharacterController.CleanConsumable();
            
            Shader.SetGlobalFloat("_BlinkingValue", 0.0f);
            
           _stateManager.SwitchState(StateType.GameOver);
           _trackManager.CharacterController.CurrentLife = _trackManager.CharacterController.MaxLife;
           _trackManager.End();
        }
    }
}