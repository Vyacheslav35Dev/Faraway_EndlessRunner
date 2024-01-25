using Cysharp.Threading.Tasks;
using Game.Scripts.Consumable.Types;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = Game.Scripts.Character.CharacterController;

namespace Game.Scripts.Consumable
{
    public abstract class Consumable : MonoBehaviour
    {
        public float duration;
        public Sprite icon;
        public AudioClip activatedSound;
        public bool canBeSpawned = true;

        public bool Active
        {
            get { return _active; }
        }

        public float TimeActive
        {
            get { return _timeActive; }
        }

        protected bool _active = true;
        protected float _timeActive;
        
        public abstract ConsumableType GetConsumableType();
        public abstract string GetConsumableName();
        public abstract int GetPrice();
        public abstract int GetPremiumCost();

        public void ResetTime()
        {
            _timeActive = 0;
        }
        
        public virtual bool CanBeUsed(CharacterController c)
        {
            return true;
        }

        public virtual void Started(CharacterController c)
        {
            _timeActive = 0;

            if (activatedSound != null)
            {
                c.powerupSource.clip = activatedSound;
                c.powerupSource.Play();
            }
        }

        public async UniTask TimedRelease(GameObject obj, float time)
        {
            await UniTask.WaitForSeconds(time);
            Addressables.ReleaseInstance(obj);
        }

        public virtual void Tick(CharacterController c)
        {
            _timeActive += Time.deltaTime;
            if (_timeActive >= duration)
            {
                _active = false;
                return;
            }
        }

        public virtual void Ended(CharacterController c)
        {
            if (activatedSound != null && c.powerupSource.clip == activatedSound)
                c.powerupSource.Stop();

            foreach (var consumable in c.consumables.Values)
            {
                if (consumable.Active && consumable.activatedSound != null)
                {
                    c.powerupSource.clip = consumable.activatedSound;
                    c.powerupSource.Play();
                }
            }
        }
    }
}
