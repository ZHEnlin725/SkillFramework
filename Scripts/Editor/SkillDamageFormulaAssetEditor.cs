using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomEditor(typeof(SkillDamageFormulaAsset))]
    public sealed class SkillDamageFormulaAssetEditor : UnityEditor.Editor
    {
        private readonly float[] previewValues =
        {
            100f,
            1.5f,
            25f,
            0f
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Damage Formula", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("rootExpression"),
                new GUIContent("Root Expression"),
                true);

            string formulaText = GetFormulaText();
            if (!string.IsNullOrEmpty(formulaText))
            {
                EditorGUILayout.HelpBox(formulaText, MessageType.None);
            }

            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2f);
            DrawPreview();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                int keyIndex = 0;
                foreach (SkillValueKey key in System.Enum.GetValues(typeof(SkillValueKey)))
                {
                    if (key == SkillValueKey.None)
                    {
                        continue;
                    }

                    int index = keyIndex++;
                    previewValues[index] = EditorGUILayout.FloatField(
                        ObjectNames.NicifyVariableName(key.ToString()),
                        previewValues[index]);
                }

                float result = GetPreviewResult();
                EditorGUILayout.HelpBox($"Result: {result:0.###}", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        private float GetPreviewResult()
        {
            SkillDamageFormulaAsset formulaAsset = (SkillDamageFormulaAsset)target;
            if (formulaAsset == null || formulaAsset.RootExpression == null)
            {
                return 0f;
            }

            SkillDamageFormulaContext context = new SkillDamageFormulaContext(
                new SkillContext(null, 0),
                null,
                new PreviewValueProvider(previewValues));
            return formulaAsset.Calculate(context);
        }

        private string GetFormulaText()
        {
            SkillDamageFormulaAsset formulaAsset = (SkillDamageFormulaAsset)target;
            return formulaAsset != null
                ? FormatExpression(formulaAsset.RootExpression)
                : string.Empty;
        }

        private static string FormatExpression(SkillDamageExpression expression)
        {
            switch (expression)
            {
                case null:
                    return string.Empty;
                case SkillConstantExpression constantExpression:
                    return constantExpression.Value.ToString("0.###");
                case SkillValueExpression valueExpression:
                    return valueExpression.Key == SkillValueKey.None
                        ? "None"
                        : valueExpression.Key.ToString();
                case SkillBinaryExpression binaryExpression:
                    string left = FormatExpression(binaryExpression.Left);
                    string right = FormatExpression(binaryExpression.Right);
                    if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                    {
                        return string.Empty;
                    }

                    string operatorText = binaryExpression.Operator switch
                    {
                        SkillExpressionOperator.Add => "+",
                        SkillExpressionOperator.Subtract => "-",
                        SkillExpressionOperator.Multiply => "*",
                        SkillExpressionOperator.Divide => "/",
                        _ => "?"
                    };
                    return $"({left} {operatorText} {right})";
                default:
                    return expression.ToString();
            }
        }

        private sealed class PreviewValueProvider : ISkillValueProvider
        {
            private readonly float[] values;

            public PreviewValueProvider(float[] values)
            {
                this.values = values;
            }

            public bool TryGet(
                SkillValueKey key,
                SkillContext context,
                ISkillTarget target,
                out float value)
            {
                int index = (int)key - 1;
                if (values != null &&
                    index >= 0 &&
                    index < values.Length)
                {
                    value = values[index];
                    return true;
                }

                value = 0f;
                return false;
            }
        }
    }
}

