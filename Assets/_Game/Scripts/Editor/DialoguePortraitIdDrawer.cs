using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialoguePortraitIdAttribute))]
public class DialoguePortraitIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DialogueActorSO actor = ResolveActor(property);

        if (actor == null || actor.Portraits == null || actor.Portraits.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        List<string> portraitIds = new List<string>();
        List<GUIContent> optionLabels = new List<GUIContent>();

        portraitIds.Add(string.Empty);
        optionLabels.Add(new GUIContent("Default"));

        for (int i = 0; i < actor.Portraits.Count; i++)
        {
            DialoguePortraitData portraitData = actor.Portraits[i];

            if (portraitData == null || string.IsNullOrWhiteSpace(portraitData.PortraitId))
            {
                continue;
            }

            portraitIds.Add(portraitData.PortraitId);
            optionLabels.Add(new GUIContent($"{portraitData.DisplayName} ({portraitData.PortraitId})"));
        }

        int selectedIndex = portraitIds.IndexOf(property.stringValue);
        if (selectedIndex < 0)
        {
            portraitIds.Add(property.stringValue);
            optionLabels.Add(new GUIContent($"Missing: {property.stringValue}"));
            selectedIndex = portraitIds.Count - 1;
        }

        EditorGUI.BeginProperty(position, label, property);
        int nextIndex = EditorGUI.Popup(position, label, selectedIndex, optionLabels.ToArray());
        property.stringValue = portraitIds[nextIndex];
        EditorGUI.EndProperty();
    }

    private DialogueActorSO ResolveActor(SerializedProperty property)
    {
        SerializedProperty actorOverrideProperty = property.serializedObject.FindProperty(
            property.propertyPath.Replace("._portraitId", "._actorOverride"));

        if (actorOverrideProperty != null && actorOverrideProperty.objectReferenceValue is DialogueActorSO actorOverride)
        {
            return actorOverride;
        }

        if (property.serializedObject.targetObject is DialogueGraphSO dialogueGraph)
        {
            return dialogueGraph.DefaultActor;
        }

        return null;
    }
}
