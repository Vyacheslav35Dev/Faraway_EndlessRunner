using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.Coins;
using Game.Scripts.Infra.Music;
using Game.Scripts.Themes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = Game.Scripts.Character.CharacterController;
using GameObject = UnityEngine.GameObject;

namespace Game.Scripts.Tracks
{
    public class TrackManager : MonoBehaviour
    {
        private const float k_FloatingOriginThreshold = 10000f;
        private const float k_CountdownToStartLength = 5f;
        private const float k_CountdownSpeed = 1.5f;
        private const float k_StartingSegmentDistance = 2f;
        private const int k_StartingSafeSegments = 2;
        private const int k_StartingCoinPoolSize = 256;
        private const int k_DesiredSegmentCount = 10;
        private const float k_SegmentRemovalDistance = -30f;
        private const float k_Acceleration = 0.2f;
        
        public static int s_StartHash = Animator.StringToHash("Start");

        public delegate int MultiplierModifier(int current);
        public MultiplierModifier modifyMultiply;

        [Header("Character & Movements")]
        public CharacterController CharacterController;
        public float minSpeed = 5.0f;
        public float maxSpeed = 10.0f;
        public int speedStep = 4;
        public float laneOffset = 1.0f;

        public bool invincible = false;

        [Header("Objects")]
        public ConsumableDatabase consumableDatabase;
        public MeshFilter skyMeshFilter;

        [Header("Parallax")]
        public Transform parallaxRoot;
        public float parallaxRatio = 0.5f;
        
        public System.Action<TrackSegment> newSegmentCreated;
        public System.Action<TrackSegment> currentSegementChanged;
        
        public float WorldDistance { get { return _totalWorldDistance; } }
        public float Speed { get { return _speed; } }
        public float SpeedRatio { get { return (_speed - minSpeed) / (maxSpeed - minSpeed); } }
        
        public bool IsMoving { get { return _isMoving; } }
        public bool IsLoaded { get; set; }
        public bool FirstObstacle { get; set; }

       
        private int _trackSeed = -1;

        private float _currentSegmentDistance;
        private float _totalWorldDistance;
        private bool _isMoving;
        private float _speed;

        private float _timeSincePowerup;
        private int _multiplier;

        private List<TrackSegment> _segments = new List<TrackSegment>();
        private List<TrackSegment> _pastSegments = new List<TrackSegment>();
        private int _safeSegmentLeft;

        private ThemeData _currentTheme { get { return _currentThemeData; } }
        
        [SerializeField]
        private ThemeData _currentThemeData;
        
        private int _currentZone;
        private int _previousSegment = -1;

        private int _score;
        private float _scoreAccum;
        private bool _rerun;
        private float _currentZoneDistance;
        
        private Vector3 m_CameraOriginalPos = Vector3.zero;

        [SerializeField] 
        private AssetReference _character;
        
        protected void Awake()
        {
            _scoreAccum = 0.0f;
        }

        public void StartMove(bool isRestart = true)
        {
            CharacterController.StartMoving();
            _isMoving = true;
            if (isRestart)
                _speed = minSpeed;
        }

        public void StopMove()
        {
            _isMoving = false;
        }

        public async UniTask Begin()
        {
            FirstObstacle = true;
            m_CameraOriginalPos = Camera.main.transform.position;
            
            if (_trackSeed != -1)
                Random.InitState(_trackSeed);
            else
                Random.InitState((int)System.DateTime.Now.Ticks);

            _currentSegmentDistance = k_StartingSegmentDistance;
            _totalWorldDistance = 0.0f;
            
            var charObj = await Addressables.InstantiateAsync(_character);
            var player = charObj.GetComponent<Character.Character>();
                
            CharacterController.character = player;
            CharacterController.trackManager = this;

            CharacterController.Init();
            
            player.transform.SetParent(CharacterController.characterCollider.transform, false);
            Camera.main.transform.SetParent(CharacterController.transform, true);
            
            _currentZone = 0;
            _currentZoneDistance = 0;

            skyMeshFilter.sharedMesh = _currentThemeData.skyMesh;
            RenderSettings.fogColor = _currentThemeData.fogColor;
            RenderSettings.fog = true;

            gameObject.SetActive(true);
            CharacterController.gameObject.SetActive(true);
            CharacterController.Coins = 0;
                
            _score = 0;
            _scoreAccum = 0;

            _safeSegmentLeft = k_StartingSafeSegments;

            Coin.coinPool = new Pooler(_currentTheme.collectiblePrefab, k_StartingCoinPoolSize);

            CharacterController.Begin();
            await WaitToStart();
            IsLoaded = true;
        }
        
        async UniTask WaitToStart()
        {
            CharacterController.character.animator.Play(s_StartHash);
            CharacterController.StartRunning();
            await UniTask.Delay(1000);
            StartMove();
        }

        public void End()
        {
            foreach (TrackSegment seg in _segments)
            {
                Addressables.ReleaseInstance(seg.gameObject);
                _spawnedSegments--;
            }

            for (int i = 0; i < _pastSegments.Count; ++i)
            {
                Addressables.ReleaseInstance(_pastSegments[i].gameObject);
            }

            _segments.Clear();
            _pastSegments.Clear();

            CharacterController.End();

            gameObject.SetActive(false);
            Addressables.ReleaseInstance(CharacterController.character.gameObject);
            CharacterController.character = null;

            Camera.main.transform.SetParent(null);
            Camera.main.transform.position = m_CameraOriginalPos;

            CharacterController.gameObject.SetActive(false);

            for (int i = 0; i < parallaxRoot.childCount; ++i)
            {
                _parallaxRootChildren--;
                Destroy(parallaxRoot.GetChild(i).gameObject);
            }
        }


        private int _parallaxRootChildren = 0;
        private int _spawnedSegments = 0;
        void Update()
        {
            while (_spawnedSegments < (k_DesiredSegmentCount))
            {
                SpawnNewSegment().Forget();
                _spawnedSegments++;
            }

            if (parallaxRoot != null && _currentTheme.cloudPrefabs.Length > 0)
            {
                while (_parallaxRootChildren < _currentTheme.cloudNumber)
                {
                    float lastZ = parallaxRoot.childCount == 0 ? 0 : parallaxRoot.GetChild(parallaxRoot.childCount - 1).position.z + _currentTheme.cloudMinimumDistance.z;

                    GameObject cloud = _currentTheme.cloudPrefabs[Random.Range(0, _currentTheme.cloudPrefabs.Length)];
                    if (cloud != null)
                    {
                        GameObject obj = Instantiate(cloud);
                        obj.transform.SetParent(parallaxRoot, false);

                        obj.transform.localPosition =
                            Vector3.up * (_currentTheme.cloudMinimumDistance.y +
                                          (Random.value - 0.5f) * _currentTheme.cloudSpread.y)
                            + Vector3.forward * (lastZ + (Random.value - 0.5f) * _currentTheme.cloudSpread.z)
                            + Vector3.right * (_currentTheme.cloudMinimumDistance.x +
                                               (Random.value - 0.5f) * _currentTheme.cloudSpread.x);

                        obj.transform.localScale = obj.transform.localScale * (1.0f + (Random.value - 0.5f) * 0.5f);
                        obj.transform.localRotation = Quaternion.AngleAxis(Random.value * 360.0f, Vector3.up);
                        _parallaxRootChildren++;
                    }
                }
            }

            if (!_isMoving)
                return;

            float scaledSpeed = _speed * Time.deltaTime;
            _scoreAccum += scaledSpeed;
            _currentZoneDistance += scaledSpeed;

            int intScore = Mathf.FloorToInt(_scoreAccum);
            if (intScore != 0) AddScore(intScore);
            _scoreAccum -= intScore;

            _totalWorldDistance += scaledSpeed;
            _currentSegmentDistance += scaledSpeed;

            if (_currentSegmentDistance > _segments[0].WorldLength)
            {
                _currentSegmentDistance -= _segments[0].WorldLength;

                _pastSegments.Add(_segments[0]);
                _segments.RemoveAt(0);
                _spawnedSegments--;

                if (currentSegementChanged != null) currentSegementChanged.Invoke(_segments[0]);
            }

            Vector3 currentPos;
            Quaternion currentRot;
            Transform characterTransform = CharacterController.transform;

            _segments[0].GetPointAtInWorldUnit(_currentSegmentDistance, out currentPos, out currentRot);
            
            bool needRecenter = currentPos.sqrMagnitude > k_FloatingOriginThreshold;

            if (parallaxRoot != null)
            {
                Vector3 difference = (currentPos - characterTransform.position) * parallaxRatio; ;
                int count = parallaxRoot.childCount;
                for (int i = 0; i < count; i++)
                {
                    Transform cloud = parallaxRoot.GetChild(i);
                    cloud.position += difference - (needRecenter ? currentPos : Vector3.zero);
                }
            }

            if (needRecenter)
            {
                int count = _segments.Count;
                for (int i = 0; i < count; i++)
                {
                    _segments[i].transform.position -= currentPos;
                }

                count = _pastSegments.Count;
                for (int i = 0; i < count; i++)
                {
                    _pastSegments[i].transform.position -= currentPos;
                }
                
                _segments[0].GetPointAtInWorldUnit(_currentSegmentDistance, out currentPos, out currentRot);
            }

            characterTransform.rotation = currentRot;
            characterTransform.position = currentPos;

            if (parallaxRoot != null && _currentTheme.cloudPrefabs.Length > 0)
            {
                for (int i = 0; i < parallaxRoot.childCount; ++i)
                {
                    Transform child = parallaxRoot.GetChild(i);
                    
                    if ((child.localPosition - currentPos).z < -50)
                    {
                        _parallaxRootChildren--;
                        Destroy(child.gameObject);
                    }
                }
            }
            
            for (int i = 0; i < _pastSegments.Count; ++i)
            {
                if ((_pastSegments[i].transform.position - currentPos).z < k_SegmentRemovalDistance)
                {
                    _pastSegments[i].Cleanup();
                    _pastSegments.RemoveAt(i);
                    i--;
                }
            }

            PowerupSpawnUpdate();
            
            _multiplier = 1 + Mathf.FloorToInt((_speed - minSpeed) / (maxSpeed - minSpeed) * speedStep);

            if (modifyMultiply != null)
            {
                foreach (MultiplierModifier part in modifyMultiply.GetInvocationList())
                {
                    _multiplier = part(_multiplier);
                }
            }

            MusicPlayer.instance.UpdateVolumes(SpeedRatio);
        }

        private void PowerupSpawnUpdate()
        {
            _timeSincePowerup += Time.deltaTime;
        }

        private readonly Vector3 _offScreenSpawnPos = new Vector3(-100f, -100f, -100f);
        private async UniTask SpawnNewSegment()
        {
            int segmentUse = Random.Range(0, _currentThemeData.zones[_currentZone].prefabList.Length);
            if (segmentUse == _previousSegment) segmentUse = (segmentUse + 1) % _currentThemeData.zones[_currentZone].prefabList.Length;

            var segmentToUseOp = await _currentThemeData.zones[_currentZone].prefabList[segmentUse].InstantiateAsync(_offScreenSpawnPos, Quaternion.identity);
            
            TrackSegment newSegment = (segmentToUseOp).GetComponent<TrackSegment>();

            Vector3 currentExitPoint;
            Quaternion currentExitRotation;
            if (_segments.Count > 0)
            {
                _segments[_segments.Count - 1].GetPointAt(1.0f, out currentExitPoint, out currentExitRotation);
            }
            else
            {
                currentExitPoint = transform.position;
                currentExitRotation = transform.rotation;
            }

            newSegment.transform.rotation = currentExitRotation;

            Vector3 entryPoint;
            Quaternion entryRotation;
            newSegment.GetPointAt(0.0f, out entryPoint, out entryRotation);


            Vector3 pos = currentExitPoint + (newSegment.transform.position - entryPoint);
            newSegment.transform.position = pos;
            newSegment.manager = this;

            newSegment.transform.localScale = new Vector3((Random.value > 0.5f ? -1 : 1), 1, 1);
            newSegment.objectRoot.localScale = new Vector3(1.0f / newSegment.transform.localScale.x, 1, 1);

            if (_safeSegmentLeft <= 0)
            {
                SpawnObstacle(newSegment);
            }
            else
                _safeSegmentLeft -= 1;

            _segments.Add(newSegment);

            if (newSegmentCreated != null) newSegmentCreated.Invoke(newSegment);
        }
        
        private void SpawnObstacle(TrackSegment segment)
        {
            if (segment.possibleObstacles.Length != 0)
            {
                for (int i = 0; i < segment.obstaclePositions.Length; ++i)
                {
                    AssetReference assetRef = segment.possibleObstacles[Random.Range(0, segment.possibleObstacles.Length)];
                    SpawnFromAssetReference(assetRef, segment, i).Forget();
                }
            }

            SpawnCoinAndPowerup(segment).Forget();
        }

        private async UniTask SpawnFromAssetReference(AssetReference reference, TrackSegment segment, int posIndex)
        {
            var obj = await Addressables.LoadAssetAsync<GameObject>(reference);
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            obstacle.Spawn(segment, segment.obstaclePositions[posIndex]);
        }

        private async UniTask SpawnCoinAndPowerup(TrackSegment segment)
        {
            const float increment = 3f;
            float currentWorldPos = 0.0f;
            int currentLane = Random.Range(0, 3);
            
            while (currentWorldPos < segment.WorldLength)
            {
                Vector3 pos;
                Quaternion rot;
                
                float powerupChance = Mathf.Clamp01(Mathf.Floor(_timeSincePowerup) * 0.5f * 0.001f);
                segment.GetPointAtInWorldUnit(currentWorldPos, out pos, out rot);

                bool laneValid = true;
                int testedLane = currentLane;
                while (Physics.CheckSphere(pos + ((testedLane - 1) * laneOffset * (rot * Vector3.right)), 0.4f, 1 << 9))
                {
                    testedLane = (testedLane + 1) % 3;
                    if (currentLane == testedLane)
                    {
                        laneValid = false;
                        break;
                    }
                }

                currentLane = testedLane;

                if (laneValid)
                {
                    pos = pos + ((currentLane - 1) * laneOffset * (rot * Vector3.right));
                    
                    GameObject toUse = null;
                    if (Random.value < powerupChance)
                    {
                        int picked = Random.Range(0, consumableDatabase.consumbales.Length);

                        //if the powerup can't be spawned, we don't reset the time since powerup to continue to have a high chance of picking one next track segment
                        if (consumableDatabase.consumbales[picked].canBeSpawned)
                        {
                            _timeSincePowerup = 0.0f;
                            powerupChance = 0.0f;

                            var result = await Addressables.InstantiateAsync(consumableDatabase.consumbales[picked].gameObject.name, pos, rot);
                            toUse = result;
                            toUse.transform.SetParent(segment.transform, true);
                        }
                    }
                    else
                    {
                        toUse = Coin.coinPool.Get(pos, rot);
                        toUse.transform.SetParent(segment.collectibleTransform, true);
                    }

                    if (toUse != null)
                    {
                        Vector3 oldPos = toUse.transform.position;
                        toUse.transform.position += Vector3.back;
                        toUse.transform.position = oldPos;
                    }
                }

                currentWorldPos += increment;
            }
        }

        public void AddScore(int amount)
        {
            int finalAmount = amount;
            _score += finalAmount * _multiplier;
        }
    }
}