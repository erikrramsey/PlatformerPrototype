using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MirrorObjects : MonoBehaviour {
    [SerializeField] public Transform MirroredObjectsParent;
}


#if UNITY_EDITOR
[CustomEditor(typeof(MirrorObjects))]
public class MyScriptEditor : Editor {
    SerializedProperty MirroedOP;

    void OnEnable() {
        MirroedOP = serializedObject.FindProperty("MirroredObjectsParent");
    }
    public override void OnInspectorGUI()
    {
        var parent = target as MirrorObjects;
        EditorGUILayout.PropertyField(MirroedOP);
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("Mirror objects")) {
            foreach (Transform tr in parent.transform) {
                var newGuy = GameObject.Instantiate(tr.gameObject, parent.MirroredObjectsParent.transform);
                newGuy.transform.position = new Vector3(-tr.transform.position.x, tr.transform.position.y, tr.transform.position.z);
            }
        }
    }
}
#endif