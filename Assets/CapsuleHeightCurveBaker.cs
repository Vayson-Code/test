using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RuntimeCapsuleHeightBaker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform head;
    [SerializeField] private Transform bottomR;
    [SerializeField] private Transform bottomL;

    [Header("Settings")]
    [SerializeField] private float initialHeight = 1.8f;
    [SerializeField] private float initialCenterY = 0.9f;
    [SerializeField] private string jumpStatePath = "Jump.keep";
    [SerializeField] private int layerIndex = 0;
    [SerializeField] private bool isLoopingAnimation = true; // NEW: Set to false for non-looping anims
    
    [Header("Output")]
    [SerializeField] private string outputFileName = "CapsuleCurves";
    [SerializeField] private AnimationClip targetAnimationClip;

    private bool isRecording = false;
    private bool wasInJumpState = false;
    private List<KeyframeData> recordedData = new List<KeyframeData>();
    private float recordingStartTime;
    private float lastRecordedTime = 0f;

    private struct KeyframeData
    {
        public float time;
        public float normalizedHeight;
        public float normalizedCenterY;
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool isInJumpState = stateInfo.IsName(jumpStatePath);

        

        // Start recording when entering jump state
        if (isInJumpState && !wasInJumpState && !isRecording)
        {
            StartRecording();
        }

        // Record every frame while in jump state
        if (isRecording && isInJumpState)
        {
            float currentTime = Time.time - recordingStartTime;
            RecordFrame(currentTime);
            lastRecordedTime = currentTime;
        }

        // Stop recording when exiting jump state OR when non-looping animation completes
        if (isRecording)
        {
            bool shouldStop = false;
            
            if (!isInJumpState && wasInJumpState)
            {
                shouldStop = true; // Exited the state
            }
            else if (!isLoopingAnimation && stateInfo.normalizedTime >= 1.0f)
            {
                shouldStop = true; // Non-looping animation finished
            }
            
            if (shouldStop)
            {
                StopRecording();
            }
        }

        wasInJumpState = isInJumpState;
    }

    string GetCurrentStateName()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(layerIndex);
        if (clipInfo.Length > 0)
        {
            return clipInfo[0].clip.name;
        }
        return "Unknown";
    }

    void StartRecording()
    {
        Debug.Log($"🔴 RECORDING STARTED - {jumpStatePath} animation began");
        Debug.Log($"Looping: {isLoopingAnimation}");
        recordedData.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        lastRecordedTime = 0f;
    }

    void RecordFrame(float currentTime)
    {
        // Calculate height
        float distanceToBottomR = Vector3.Distance(head.position, bottomR.position);
        float distanceToBottomL = Vector3.Distance(head.position, bottomL.position);
        float maxHeight = Mathf.Max(distanceToBottomR, distanceToBottomL);
        float normalizedHeight = maxHeight / initialHeight;

        // Calculate center Y position
        Vector3 lowestBottom = bottomR.position.y < bottomL.position.y ? bottomR.position : bottomL.position;
        float currentCenterY = (head.position.y + lowestBottom.y) / 2f;
        float rootY = transform.position.y;
        float centerYRelativeToRoot = currentCenterY - rootY;
        float normalizedCenterY = centerYRelativeToRoot / initialCenterY;

        recordedData.Add(new KeyframeData
        {
            time = currentTime,
            normalizedHeight = normalizedHeight,
            normalizedCenterY = normalizedCenterY
        });

        // Visual feedback
        Debug.DrawLine(head.position, bottomR.position, Color.green);
        Debug.DrawLine(head.position, bottomL.position, Color.blue);
        
        Vector3 centerPos = new Vector3(transform.position.x, currentCenterY, transform.position.z);
        Debug.DrawLine(centerPos + Vector3.left * 0.2f, centerPos + Vector3.right * 0.2f, Color.yellow);
        Debug.DrawLine(centerPos + Vector3.forward * 0.2f, centerPos + Vector3.back * 0.2f, Color.yellow);
    }

    void StopRecording()
    {
        isRecording = false;
        Debug.Log($"✅ RECORDING STOPPED - Captured {recordedData.Count} frames");
        Debug.Log($"Duration: {lastRecordedTime:F2} seconds");
        
#if UNITY_EDITOR
        SaveCurvesToAnimation();
#endif
    }

#if UNITY_EDITOR
    void SaveCurvesToAnimation()
    {
        if (recordedData.Count == 0)
        {
            Debug.LogError("No data to save!");
            return;
        }

        // Validation check
        if (targetAnimationClip != null)
        {
            string path = AssetDatabase.GetAssetPath(targetAnimationClip);
            
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("❌ Target animation clip is not a saved asset!");
                return;
            }

            if (path.EndsWith(".fbx") || path.EndsWith(".FBX"))
            {
                Debug.LogError("❌ Cannot edit FBX animations! Create a duplicate .anim file first.");
                return;
            }

            // SAFETY CHECK: Compare recorded duration to animation length
            float animLength = targetAnimationClip.length;
            float recordedLength = lastRecordedTime;
            
            if (Mathf.Abs(animLength - recordedLength) > 0.5f)
            {
                Debug.LogWarning($"⚠️ Duration mismatch! Animation: {animLength:F2}s, Recorded: {recordedLength:F2}s");
                Debug.LogWarning("The curves might not align properly. Continue anyway? Check console.");
            }
        }

        // Create both curves
        AnimationCurve heightCurve = new AnimationCurve();
        AnimationCurve centerCurve = new AnimationCurve();

        foreach (var data in recordedData)
        {
            heightCurve.AddKey(data.time, data.normalizedHeight);
            centerCurve.AddKey(data.time, data.normalizedCenterY);
        }

        // Smooth both curves
        SmoothCurve(heightCurve);
        SmoothCurve(centerCurve);

        Debug.Log($"✅ Created curves with {heightCurve.keys.Length} keyframes each");
        Debug.Log($"📊 Height range: {GetMinValue(heightCurve):F2} to {GetMaxValue(heightCurve):F2}");
        Debug.Log($"📊 Center range: {GetMinValue(centerCurve):F2} to {GetMaxValue(centerCurve):F2}");

        if (targetAnimationClip != null)
        {
            InjectCurvesIntoAnimation(heightCurve, centerCurve);
        }
        else
        {
            SaveCurvesAsAsset(heightCurve, centerCurve);
        }
    }

    void SmoothCurve(AnimationCurve curve)
    {
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
    }

    void InjectCurvesIntoAnimation(AnimationCurve heightCurve, AnimationCurve centerCurve)
    {
        string path = AssetDatabase.GetAssetPath(targetAnimationClip);

        // Create a backup before modifying
        string backupPath = path.Replace(".anim", "_BACKUP.anim");
        AssetDatabase.CopyAsset(path, backupPath);
        Debug.Log($"💾 Backup created: {backupPath}");

        // Inject both curves
        targetAnimationClip.SetCurve("", typeof(Animator), "capsuleHeight", heightCurve);
        targetAnimationClip.SetCurve("", typeof(Animator), "capsuleCenterY", centerCurve);

        EditorUtility.SetDirty(targetAnimationClip);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Both curves injected into {targetAnimationClip.name}!");
        Debug.Log("📝 Curves added: 'capsuleHeight' and 'capsuleCenterY'");
        
        Selection.activeObject = targetAnimationClip;
    }

    void SaveCurvesAsAsset(AnimationCurve heightCurve, AnimationCurve centerCurve)
    {
        string path = $"Assets/{outputFileName}.asset";
        
        CurvesAsset asset = ScriptableObject.CreateInstance<CurvesAsset>();
        asset.heightCurve = heightCurve;
        asset.centerCurve = centerCurve;
        
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Curves saved to {path}");
        
        Selection.activeObject = asset;
    }

    float GetMinValue(AnimationCurve curve)
    {
        float min = float.MaxValue;
        foreach (var key in curve.keys)
            if (key.value < min) min = key.value;
        return min;
    }

    float GetMaxValue(AnimationCurve curve)
    {
        float max = float.MinValue;
        foreach (var key in curve.keys)
            if (key.value > max) max = key.value;
        return max;
    }
#endif

    void OnGUI()
    {
        if (isRecording)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 450, 30), $"🔴 RECORDING {jumpStatePath}... ({recordedData.Count} frames)");
            GUI.Label(new Rect(10, 40, 450, 30), $"Duration: {(Time.time - recordingStartTime):F2}s");
        }
        else
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 400, 70), $"Waiting for '{jumpStatePath}' animation...\nPress P to see current state\nLooping: {isLoopingAnimation}");
        }
    }
}

#if UNITY_EDITOR
public class CurvesAsset : ScriptableObject
{
    public AnimationCurve heightCurve;
    public AnimationCurve centerCurve;
}

[CustomEditor(typeof(CurvesAsset))]
public class CurvesAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CurvesAsset curvesAsset = (CurvesAsset)target;
        
        EditorGUILayout.LabelField("Capsule Height Curve", EditorStyles.boldLabel);
        EditorGUILayout.CurveField(curvesAsset.heightCurve);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Capsule Center Y Curve", EditorStyles.boldLabel);
        EditorGUILayout.CurveField(curvesAsset.centerCurve);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Height Keyframes: {curvesAsset.heightCurve.keys.Length}\nCenter Keyframes: {curvesAsset.centerCurve.keys.Length}", MessageType.Info);
        
        if (GUILayout.Button("Copy Height Curve Data"))
        {
            CopyCurveData(curvesAsset.heightCurve);
        }
        
        if (GUILayout.Button("Copy Center Curve Data"))
        {
            CopyCurveData(curvesAsset.centerCurve);
        }
    }
    
    void CopyCurveData(AnimationCurve curve)
    {
        string data = "";
        foreach (var key in curve.keys)
        {
            data += $"{key.time:F4}, {key.value:F4}\n";
        }
        GUIUtility.systemCopyBuffer = data;
        Debug.Log("Curve data copied to clipboard!");
    }
}
#endif