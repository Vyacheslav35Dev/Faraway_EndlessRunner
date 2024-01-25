using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.Coins;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace Game.Scripts.Character
{
	[RequireComponent(typeof(AudioSource))]
	public class CharacterCollider : MonoBehaviour
	{
		private const float MagnetSpeed = 10f;
		private const int CoinsLayerIndex = 8;
		private const int ObstacleLayerIndex = 9;
		private const int PowerupLayerIndex = 10;

		static int _hitHash = Animator.StringToHash("Hit");
		static int _blinkingValueHash;
		
		public CharacterController controller;

		[Header("Sound")]
		public AudioClip coinSound;
		public AudioClip premiumSound;
		
		[HideInInspector]
		public List<GameObject> magnetCoins = new List<GameObject>();
		
		protected bool _invincible;
		
		protected BoxCollider _collider;
		public new AudioSource audio { get { return _audio; } }
		protected AudioSource _audio;
		
		protected readonly Vector3 _slidingColliderScale = new Vector3 (1.0f, 0.5f, 1.0f);
		protected readonly Vector3 _notSlidingColliderScale = new Vector3(1.0f, 2.0f, 1.0f);

		protected void Start()
		{
			_collider = GetComponent<BoxCollider>();
			_audio = GetComponent<AudioSource>();
		}

		protected void Update()
		{
			foreach (var coin in magnetCoins)
			{
				coin.transform.position = Vector3.MoveTowards(coin.transform.position, transform.position, MagnetSpeed * Time.deltaTime);
			}
		}
		
		public void Init()
		{
			_hitHash = Shader.PropertyToID("_BlinkingValue");
			_invincible = false;
		}

		protected void OnTriggerEnter(Collider collider)
		{
			if (collider.gameObject.layer == CoinsLayerIndex)
			{
				if (magnetCoins.Contains(collider.gameObject))
					magnetCoins.Remove(collider.gameObject);

				Coin.coinPool.Free(collider.gameObject);
				controller.Coins += 1;
				_audio.PlayOneShot(coinSound);
			}
			/*else if(collider.gameObject.layer == ObstacleLayerIndex)
			{
				if (_invincible)
				{
					return;
				}
				collider.enabled = false;
				
				controller.character.animator.SetTrigger(_hitHash);
				controller.CurrentLife--;
				if (controller.CurrentLife > 0)
				{
					controller.StopMoving();
					_audio.PlayOneShot(controller.character.hitSound);
				}
				else
				{
					controller.StopMoving(true);
					_audio.PlayOneShot(controller.character.deathSound);
				}
			}
			else if(collider.gameObject.layer == PowerupLayerIndex)
			{
				var consumable = collider.GetComponent<Consumable.Consumable>();
				if(consumable != null)
				{
					controller.UseConsumable(consumable);
				}
			}*/
		}

		public void SetInvincibleExplicit(bool invincible)
		{
			_invincible = invincible;
		}
	}
}
