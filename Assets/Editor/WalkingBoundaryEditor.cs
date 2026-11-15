using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WalkingBoundary))]
public sealed class WalkingBoundaryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();
        if (serializedObject.FindProperty("route").intValue != (int)WalkingBoundary.Route.Hybrid)
            return;

        bool diagonal = serializedObject.FindProperty("hybridDiagonal").boolValue;
        EditorGUILayout.LabelField("目前階段", diagonal ? "斜直線" : "方形");
        if (!GUILayout.Button(diagonal ? "切換到方形限縮" : "切換到斜直線限縮"))
            return;

        WalkingBoundary boundary = (WalkingBoundary)target;
        Undo.RecordObject(boundary, "Switch hybrid route phase");
        boundary.SetHybridDiagonal(!diagonal);
        PrefabUtility.RecordPrefabInstancePropertyModifications(boundary);
        EditorUtility.SetDirty(boundary);
        SceneView.RepaintAll();
    }
}
