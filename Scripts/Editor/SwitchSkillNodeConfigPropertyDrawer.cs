using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(SwitchSkillNodeConfig))]
    public sealed class SwitchSkillNodeConfigPropertyDrawer :
        CompositeSkillNodeConfigPropertyDrawer
    {
        protected override bool IsExtraProperty(SerializedProperty property)
        {
            return base.IsExtraProperty(property) ||
                   property.name == "BranchConditions";
        }

        protected override float GetExtraHeight(SerializedProperty property)
        {
            float childListHeight = GetChildList(property).GetHeight();
            float conditionsHeight = SkillConditionListEditorUtility.GetList(
                property, "BranchConditions", "Branch Conditions").GetHeight();
            return childListHeight +
                   EditorGUIUtility.standardVerticalSpacing +
                   conditionsHeight;
        }

        protected override void DrawExtraFields(Rect position, SerializedProperty property, GUIContent label)
        {
            var childList = GetChildList(property);
            float childListHeight = childList.GetHeight();
            base.DrawExtraFields(
                new Rect(position.x, position.y, position.width, childListHeight),
                property,
                label);

            Rect conditionsRect = new Rect(
                position.x,
                position.y + childListHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                SkillConditionListEditorUtility.GetList(
                    property, "BranchConditions", "Branch Conditions").GetHeight());
            SkillConditionListEditorUtility.GetList(
                property, "BranchConditions", "Branch Conditions")
                .DoList(conditionsRect);
        }
    }
}
