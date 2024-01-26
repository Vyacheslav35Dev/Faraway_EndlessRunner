using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.Consumable.Types;
using Game.Scripts.Tracks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Scripts.Character
{
	public class CharacterController : MonoBehaviour
	{
		public readonly int MaxLife = 3;
		
		private const int StartingLane = 1;
		private const float GroundingSpeed = 80f;
		private const float TrackSpeedToJumpAnimSpeedRatio = 0.6f;
		private const float DefaultInvinsibleTime = 2f;

		private static int _deadHash = Animator.StringToHash ("Dead");
		private static int _runStartHash = Animator.StringToHash("runStart");
		private static int _movingHash = Animator.StringToHash("Moving");
		private static int _jumpingHash = Animator.StringToHash("Jumping");
		private static int _jumpingSpeedHash = Animator.StringToHash("JumpSpeed");
		
		[Header("Field")]
		public float laneChangeSpeed = 1.0f;
		
		public int CurrentLife;
	
		[Header("Controls")]
		public float jumpLength = 2.0f;
		public float jumpHeight = 1.2f;

		[Header("Sounds")]
		public AudioClip powerUpUseSound;
		public AudioSource powerupSource;

		public int Coins;

		private bool _isRunning;
		private bool _jumping;
		private float _jumpStart;
	
		private AudioSource _audio;

		private int _currentLane = StartingLane;
		private Vector3 _targetPosition = Vector3.zero;
		private Vector3 _startingPosition = Vector3.forward * 2f;

		[SerializeField] private GameObject _invincibilityIcon;
		[SerializeField] private GameObject _speedUpIcon;
		[SerializeField] private GameObject _speedDownIcon;

		[Header("Infra")]
		public TrackManager trackManager;
		public Character character;
		public CharacterCollider characterCollider;
		
		public Dictionary<ConsumableType, Consumable.Consumable> consumables { get { return _activeConsumables; } }
		private Dictionary<ConsumableType, Consumable.Consumable> _activeConsumables = new Dictionary<ConsumableType, Consumable.Consumable>();
		
#if !UNITY_STANDALONE
		private Vector2 _startingTouch;
		private bool _isSwiping = false;
#endif

		public void Init()
		{
			transform.position = _startingPosition;
			_targetPosition = Vector3.zero;

			_currentLane = StartingLane;
			characterCollider.transform.localPosition = Vector3.zero;
			
			CurrentLife = MaxLife;

			_audio = GetComponent<AudioSource>();
		}
		
		public void Begin()
		{
			_isRunning = false;
			character.animator.SetBool(_deadHash, false);
			characterCollider.Init();
		}
		
		public void StartRunning()
		{   
			StartMoving();
			if (character.animator)
			{
				character.animator.Play(_runStartHash);
				character.animator.SetBool(_movingHash, true);
			}
		}
		
		public void End()
		{
			CleanConsumable();
		}

		public void StartMoving()
		{
			_isRunning = true;
		}

		public void StopMoving(bool restart = false)
		{
			_isRunning = false;
			trackManager.StopMove();
			if (character.animator)
			{
				character.animator.SetBool(_movingHash, false);
			}

			if (!restart)
			{
				DelayAndRun().Forget();
			}
		}

		private async UniTask DelayAndRun()
		{
			await UniTask.Delay(1000);
			trackManager.StartMove();
		}
		
		protected void Update ()
		{
			
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				ChangeLane(-1);
			}
			else if(Input.GetKeyDown(KeyCode.RightArrow))
			{
				ChangeLane(1);
			}
			else if(Input.GetKeyDown(KeyCode.UpArrow))
			{
				Jump();
			}
#else
			if (Input.touchCount == 1)
	        {
				if(_isSwiping)
				{
					Vector2 diff = Input.GetTouch(0).position - _startingTouch;
					diff = new Vector2(diff.x/Screen.width, diff.y/Screen.width);
					if(diff.magnitude > 0.01f)
					{
						if(Mathf.Abs(diff.y) > Mathf.Abs(diff.x))
						{
							Jump();
						}
						else
						{
							if(diff.x < 0)
							{
								ChangeLane(-1);
							}
							else
							{
								ChangeLane(1);
							}
						}
							
						_isSwiping = false;
					}
	            }

				if(Input.GetTouch(0).phase == TouchPhase.Began)
				{
					_startingTouch = Input.GetTouch(0).position;
					_isSwiping = true;
				}
				else if(Input.GetTouch(0).phase == TouchPhase.Ended)
				{
					_isSwiping = false;
				}
	        }
#endif
			
			Vector3 verticalTargetPosition = _targetPosition;

			if(_jumping)
			{
				if (trackManager.IsMoving)
				{
					float correctJumpLength = jumpLength * (1.0f + trackManager.SpeedRatio);
					float ratio = (trackManager.WorldDistance - _jumpStart) / correctJumpLength;
					if (ratio >= 1.0f)
					{
						_jumping = false;
						character.animator.SetBool(_jumpingHash, false);
					}
					else
					{
						verticalTargetPosition.y = Mathf.Sin(ratio * Mathf.PI) * jumpHeight;
					}
				}
				else if(!AudioListener.pause)
				{
					verticalTargetPosition.y = Mathf.MoveTowards (verticalTargetPosition.y, 0, GroundingSpeed * Time.deltaTime);
					if (Mathf.Approximately(verticalTargetPosition.y, 0f))
					{
						character.animator.SetBool(_jumpingHash, false);
						_jumping = false;
					}
				}
			}

			characterCollider.transform.localPosition = Vector3.MoveTowards(characterCollider.transform.localPosition,
				verticalTargetPosition, laneChangeSpeed * Time.deltaTime);
		}

		private void Jump()
		{
			if (!_isRunning)
				return;
	    
			if (!_jumping)
			{
				float correctJumpLength = jumpLength * (1.0f + trackManager.SpeedRatio);
				_jumpStart = trackManager.WorldDistance;
				float animSpeed = TrackSpeedToJumpAnimSpeedRatio * (trackManager.Speed / correctJumpLength);

				character.animator.SetFloat(_jumpingSpeedHash, animSpeed);
				character.animator.SetBool(_jumpingHash, true);
				_audio.PlayOneShot(character.jumpSound);
				_jumping = true;
			}
		}

		private void ChangeLane(int direction)
		{
			if (!_isRunning)
				return;

			int targetLane = _currentLane + direction;

			if (targetLane < 0 || targetLane > 2)
				return;

			_currentLane = targetLane;
			_targetPosition = new Vector3((_currentLane - 1) * trackManager.laneOffset, 0, 0);
		}
		
		public void UseConsumable(Consumable.Consumable consumable)
		{
			characterCollider.audio.PlayOneShot(powerUpUseSound);

			if (_activeConsumables.ContainsKey(consumable.GetConsumableType()))
			{
				var item = _activeConsumables[consumable.GetConsumableType()];
				item.ResetTime();
				Addressables.ReleaseInstance(consumable.gameObject);
				return;
			}

			consumable.transform.SetParent(transform, false);
			consumable.gameObject.SetActive(false);

			_activeConsumables.Add(consumable.GetConsumableType(), consumable);
			consumable.Started(this);
		}

		public void CleanConsumable()
		{
			foreach (var consumable in _activeConsumables.Values)
			{
				consumable.Ended(this);
				Addressables.ReleaseInstance(consumable.gameObject);
			}

			_activeConsumables.Clear();
		}

		public async UniTask SetInvincible(float timer = DefaultInvinsibleTime)
		{
			await InvincibleTimer(timer);
		}
		
		public async UniTask SetSpeedUp(float timer, int value)
		{
			await SpeedUpTimer(timer, value);
		}
		
		public async UniTask SetSpedDown(float timer, int value)
		{
			await SpeedDownTimer(timer, value);
		}
		
		private async UniTask InvincibleTimer(float timer)
		{
			characterCollider.SetInvincibleExplicit(true);
			_invincibilityIcon.SetActive(true);

			float time = 0;
			while(time < timer)
			{
				await UniTask.Yield();
				time += Time.deltaTime;
			}
			
			_invincibilityIcon.SetActive(false);
			characterCollider.SetInvincibleExplicit(false);
			_activeConsumables.Remove(ConsumableType.INVINCIBILITY);
		}
		
		private async UniTask SpeedUpTimer(float timer, int value)
		{
			_speedUpIcon.SetActive(true);

			trackManager.SetSpeedModify(value);

			float time = 0;
			while(time < timer)
			{
				await UniTask.Yield();
				time += Time.deltaTime;
			}
			
			trackManager.SetSpeedModify(-value);
			_speedUpIcon.SetActive(false);
			_activeConsumables.Remove(ConsumableType.SPEED_UP);
		}
		
		private async UniTask SpeedDownTimer(float timer, int value)
		{
			_speedDownIcon.SetActive(true);
			trackManager.SetSpeedModify(-value);

			float time = 0;
			while(time < timer)
			{
				await UniTask.Yield();
				time += Time.deltaTime;
			}
			
			_speedDownIcon.SetActive(false);
			trackManager.SetSpeedModify(+value);
			_activeConsumables.Remove(ConsumableType.SPEED_DOWN);
		}
	}
}
