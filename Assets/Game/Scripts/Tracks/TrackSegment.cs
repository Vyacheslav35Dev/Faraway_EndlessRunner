﻿using Game.Scripts.Coins;
using Game.Scripts.Tracks;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This defines a "piece" of the track. This is attached to the prefab and contains data such as what obstacles can spawn on it.
/// It also defines places on the track where obstacles can spawn. The prefab is placed into a ThemeData list.
/// </summary>
public class TrackSegment : MonoBehaviour
{
    public Transform pathParent;
    public TrackManager manager;

	public Transform objectRoot;
	public Transform collectibleTransform;

    public AssetReference[] possibleObstacles; 

    [HideInInspector]
    public float[] obstaclePositions;

    public float WorldLength { get { return _worldLength; } }

    private float _worldLength;

    void OnEnable()
    {
        UpdateWorldLength();

		GameObject obj = new GameObject("ObjectRoot");
		obj.transform.SetParent(transform);
		objectRoot = obj.transform;

		obj = new GameObject("Collectibles");
		obj.transform.SetParent(objectRoot);
		collectibleTransform = obj.transform;
    }
    
    public void GetPointAtInWorldUnit(float wt, out Vector3 pos, out Quaternion rot)
    {
        var t = wt / _worldLength;
        GetPointAt(t, out pos, out rot);
    }
    
	public void GetPointAt(float t, out Vector3 pos, out Quaternion rot)
    {
        var clampedT = Mathf.Clamp01(t);
        var scaledT = (pathParent.childCount - 1) * clampedT;
        var index = Mathf.FloorToInt(scaledT);
        var segmentT = scaledT - index;

        Transform orig = pathParent.GetChild(index);
        if (index == pathParent.childCount - 1)
        {
            pos = orig.position;
            rot = orig.rotation;
            return;
        }

        Transform target = pathParent.GetChild(index + 1);

        pos = Vector3.Lerp(orig.position, target.position, segmentT);
        rot = Quaternion.Lerp(orig.rotation, target.rotation, segmentT);
    }

    private void UpdateWorldLength()
    {
        _worldLength = 0;

        for (int i = 1; i < pathParent.childCount; ++i)
        {
            Transform orig = pathParent.GetChild(i - 1);
            Transform end = pathParent.GetChild(i);

            Vector3 vec = end.position - orig.position;
            _worldLength += vec.magnitude;
        }
    }

	public void Cleanup()
	{
		while(collectibleTransform.childCount > 0)
		{
			Transform t = collectibleTransform.GetChild(0);
			t.SetParent(null);
            Coin.coinPool.Free(t.gameObject);
		}

	    Addressables.ReleaseInstance(gameObject);
	}

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (pathParent == null)
            return;

        Color c = Gizmos.color;
        Gizmos.color = Color.red;
        for (int i = 1; i < pathParent.childCount; ++i)
        {
            Transform orig = pathParent.GetChild(i - 1);
            Transform end = pathParent.GetChild(i);

            Gizmos.DrawLine(orig.position, end.position);
        }

        Gizmos.color = Color.blue;
        for (int i = 0; i < obstaclePositions.Length; ++i)
        {
            Vector3 pos;
            Quaternion rot;
            GetPointAt(obstaclePositions[i], out pos, out rot);
            Gizmos.DrawSphere(pos, 0.5f);
        }

        Gizmos.color = c;
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TrackSegment))]
class TrackSegmentEditor : Editor
{
    protected TrackSegment m_Segment;

    public void OnEnable()
    {
        m_Segment = target as TrackSegment;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Add obstacles"))
        {
            ArrayUtility.Add(ref m_Segment.obstaclePositions, 0.0f);
        }

        if (m_Segment.obstaclePositions != null)
        {
            int toremove = -1;
            for (int i = 0; i < m_Segment.obstaclePositions.Length; ++i)
            {
                GUILayout.BeginHorizontal();
                m_Segment.obstaclePositions[i] = EditorGUILayout.Slider(m_Segment.obstaclePositions[i], 0.0f, 1.0f);
                if (GUILayout.Button("-", GUILayout.MaxWidth(32)))
                    toremove = i;
                GUILayout.EndHorizontal();
            }

            if (toremove != -1)
                ArrayUtility.RemoveAt(ref m_Segment.obstaclePositions, toremove);
        }
    }
}

#endif