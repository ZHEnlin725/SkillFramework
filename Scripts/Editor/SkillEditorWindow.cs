using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    public sealed class SkillEditorWindow : EditorWindow
    {
        private const float RowHeight = 26f;
        private const float HeaderHeight = 24f;
        private const float RowLabelWidth = 190f;
        private const float ResizeHandleWidth = 8f;
        private const int DefaultNodeDuration = 10;
        private const int PreviewFrameRate = 30;
        private const string ChildrenArrayMarker = ".Children.Array.data[";

        private SkillConfig skillConfig;
        private SerializedObject serializedSkill;
        private SerializedProperty nodesProperty;

        private int selectedNodeIndex = -1;
        private string selectedNodePath;
        private readonly List<string> selectedNodePaths = new();
        private readonly List<SkillNodeRow> nodeRows = new();
        private int cursorFrame;
        private float pixelsPerFrame = 8f;
        private Vector2 timelineScroll;
        private Vector2 inspectorScroll;

        private int dragNodeIndex = -1;
        private bool dragResize;
        private float dragStartMouseX;
        private int dragOriginalStartFrame;
        private int dragOriginalDuration;
        private int dragRowSourceIndex = -1;
        private int dragRowTargetIndex = -1;
        [SerializeField] private GameObject previewPrefab;
        private GameObject previewInstance;
        private PreviewRenderUtility previewRenderUtility;
        private int previewPickerControlId;
        private SkillEditorPreviewAdapter previewAdapter;
        private Skill previewSkill;
        private static GameObject currentPreviewPrefab;
        public static GameObject CurrentPreviewPrefab => currentPreviewPrefab;
        private bool previewPlaying;
        private double previewLastUpdateTime;
        private double previewAccumulatedSeconds;
        private float previewCameraYaw;
        private float previewCameraPitch = 25f;
        private float previewCameraDistance;
        private bool previewCameraDragging;
        private Vector3 previewCameraTarget = Vector3.zero;
        private float previewCameraRadius = 1f;
        [SerializeField] private bool disablePreviewAnimationMotion;

        private readonly struct SkillNodeRow
        {
            public string PropertyPath { get; }
            public int Depth { get; }

            public SkillNodeRow(string propertyPath, int depth)
            {
                PropertyPath = propertyPath;
                Depth = depth;
            }
        }

        [MenuItem("SkillFramework/Skill Editor")]
        public static void Open()
        {
            SkillEditorWindow window = GetWindow<SkillEditorWindow>("Skill Editor");
            window.minSize = new Vector2(860f, 520f);
        }

        private void OnEnable()
        {
            RefreshSerializedSkill();
            if (previewPrefab != null)
            {
                currentPreviewPrefab = previewPrefab;
            }

            EditorApplication.update += TickPreviewSkill;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TickPreviewSkill;
            CleanupPreview();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is SkillConfig selectedConfig && selectedConfig != skillConfig)
            {
                skillConfig = selectedConfig;
                selectedNodeIndex = -1;
                selectedNodePath = null;
                RefreshSerializedSkill();
                Repaint();
            }
        }

        private void OnGUI()
        {
            currentPreviewPrefab = previewPrefab;
            HandlePreviewPickerEvents();
            DrawToolbar();

            if (skillConfig == null)
            {
                EditorGUILayout.HelpBox("Create a skill config or select an existing one to begin editing.", MessageType.Info);
                return;
            }

            serializedSkill.Update();

            BuildNodeRows();
            DrawSkillSettings();
            EditorGUILayout.Space(6f);
            DrawPreviewModel();
            EditorGUILayout.Space(6f);
            DrawTimeline();
            EditorGUILayout.Space(6f);
            DrawSelectedNode();

            EnsureSkillDurationCoversNodes();
            serializedSkill.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                SkillConfig selectedConfig = (SkillConfig)EditorGUILayout.ObjectField(
                    skillConfig,
                    typeof(SkillConfig),
                    false,
                    GUILayout.MinWidth(240f));

                if (selectedConfig != skillConfig)
                {
                    skillConfig = selectedConfig;
                    selectedNodeIndex = -1;
                    RefreshSerializedSkill();
                }

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                {
                    CreateSkillConfig();
                }

                if (skillConfig != null)
                {
                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                    {
                        SaveSkillConfig();
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                    {
                        GenericMenu menu = new GenericMenu();
                        AddNodeTypeMenuItems(menu, cursorFrame);
                        menu.ShowAsContext();
                    }

                    if (GUILayout.Button("Set Preview Model", EditorStyles.toolbarButton, GUILayout.Width(130f)))
                    {
                        previewPickerControlId = EditorGUIUtility.GetControlID(FocusType.Passive);
                        EditorGUIUtility.ShowObjectPicker<GameObject>(
                            previewPrefab,
                            false,
                            "t:Prefab",
                            previewPickerControlId);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewModel()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Preview Model", EditorStyles.boldLabel);
                GUILayout.Label(
                    previewPrefab != null ? previewPrefab.name : "None",
                    EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                bool newDisableAnimationMotion = EditorGUILayout.ToggleLeft(
                    "禁用位移动画",
                    disablePreviewAnimationMotion,
                    GUILayout.Width(112f));
                if (newDisableAnimationMotion != disablePreviewAnimationMotion)
                {
                    disablePreviewAnimationMotion = newDisableAnimationMotion;
                    previewAdapter?.SetRootMotionDisabled(disablePreviewAnimationMotion);
                    previewAdapter?.Reset();
                    Repaint();
                }

                using (new EditorGUI.DisabledScope(previewPrefab == null || skillConfig == null))
                {
                    if (GUILayout.Button(previewPlaying ? "Stop" : "Play", GUILayout.Width(52f)))
                    {
                        if (previewPlaying)
                        {
                            StopPreviewSkill();
                        }
                        else
                        {
                            PlayPreviewSkill();
                        }
                    }
                }

                if (GUILayout.Button("Clear", GUILayout.Width(52f)) && previewPrefab != null)
                {
                    previewPrefab = null;
                    DestroyPreviewInstance();
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (previewSkill != null)
            {
                EditorGUILayout.LabelField(
                    $"Frame {previewSkill.Context.CurrentFrameId}",
                    EditorStyles.miniLabel);
            }

            if (previewPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a prefab to preview the model in the editor.",
                    MessageType.Info);
                return;
            }

            Rect previewRect = GUILayoutUtility.GetRect(160f, 160f, GUILayout.ExpandWidth(true));
            if (previewRect.width < 1f || previewRect.height < 1f)
            {
                return;
            }

            EnsurePreviewInstance();
            if (previewInstance == null)
            {
                GUI.Label(previewRect, "Failed to create preview instance.", EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawPreviewCamera(previewRect);
            }
            EditorGUILayout.EndVertical();
        }

        private void HandlePreviewPickerEvents()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.ExecuteCommand)
            {
                return;
            }

            if (currentEvent.commandName != "ObjectSelectorUpdated" &&
                currentEvent.commandName != "ObjectSelectorClosed")
            {
                return;
            }

            GameObject selectedObject = EditorGUIUtility.GetObjectPickerObject() as GameObject;
            if (selectedObject == null)
            {
                return;
            }

            previewPrefab = selectedObject;
            currentPreviewPrefab = previewPrefab;
            DestroyPreviewInstance();
            EnsurePreviewInstance();
            Repaint();
        }

        private void EnsurePreviewInstance()
        {
            if (previewPrefab == null)
            {
                return;
            }

            if (previewRenderUtility == null)
            {
                previewRenderUtility = new PreviewRenderUtility();
                previewRenderUtility.camera.fieldOfView = 30f;
                previewRenderUtility.camera.nearClipPlane = 0.1f;
                previewRenderUtility.camera.farClipPlane = 100f;
                previewRenderUtility.camera.clearFlags = CameraClearFlags.Skybox;
            }

            if (previewInstance != null)
            {
                return;
            }

            previewInstance = GameObject.Instantiate(previewPrefab);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            previewInstance.transform.position = Vector3.zero;
            previewInstance.transform.rotation = Quaternion.identity;
            previewRenderUtility.AddSingleGO(previewInstance);
            previewAdapter = new SkillEditorPreviewAdapter(
                previewInstance,
                AddGameObjectToPreviewScene,
                ResolveEffectPrefab);
            previewAdapter.SetRootMotionDisabled(disablePreviewAnimationMotion);
            ResetPreviewCamera();
            UpdatePreviewCamera();
        }

        private void DrawPreviewCamera(Rect previewRect)
        {
            UpdatePreviewCamera();
            HandlePreviewCameraInput(previewRect);
            previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);
            previewRenderUtility.camera.Render();
            Texture previewTexture = previewRenderUtility.EndPreview();
            if (previewTexture != null)
            {
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit, false);
            }
        }

        private void UpdatePreviewCamera()
        {
            if (previewRenderUtility == null || previewInstance == null)
            {
                return;
            }

            float radius = Mathf.Max(0.5f, previewCameraRadius);

            if (previewCameraDistance <= 0f)
            {
                previewCameraDistance = radius / Mathf.Tan(
                    previewRenderUtility.camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            Quaternion orbitRotation = Quaternion.Euler(previewCameraPitch, previewCameraYaw, 0f);
            previewRenderUtility.camera.farClipPlane = previewCameraDistance + radius + 10f;
            previewRenderUtility.camera.transform.position =
                previewCameraTarget - orbitRotation * Vector3.forward * previewCameraDistance;
            previewRenderUtility.camera.transform.LookAt(previewCameraTarget);
        }

        private void ResetPreviewCamera()
        {
            if (previewInstance != null)
            {
                Bounds bounds = GetPreviewBounds();
                previewCameraTarget = bounds.center;
                previewCameraRadius = Mathf.Max(0.5f, bounds.extents.magnitude);
            }
            else
            {
                previewCameraTarget = Vector3.zero;
                previewCameraRadius = 1f;
            }

            previewCameraYaw = 0f;
            previewCameraPitch = 25f;
            previewCameraDistance = 0f;
            previewCameraDragging = false;
        }

        private void HandlePreviewCameraInput(Rect previewRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type == EventType.ScrollWheel &&
                previewRect.Contains(currentEvent.mousePosition))
            {
                previewCameraDistance = Mathf.Clamp(
                    previewCameraDistance * Mathf.Pow(1.1f, currentEvent.delta.y),
                    0.2f,
                    50f);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseDown &&
                     currentEvent.button == 1 &&
                     previewRect.Contains(currentEvent.mousePosition))
            {
                previewCameraDragging = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag &&
                     currentEvent.button == 1 &&
                     previewCameraDragging)
            {
                previewCameraYaw += currentEvent.delta.x * 0.3f;
                previewCameraPitch = Mathf.Clamp(
                    previewCameraPitch + currentEvent.delta.y * 0.3f,
                    -85f,
                    85f);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseUp &&
                     currentEvent.button == 1 &&
                     previewCameraDragging)
            {
                previewCameraDragging = false;
                currentEvent.Use();
            }
        }

        private void AddGameObjectToPreviewScene(GameObject instance)
        {
            previewRenderUtility?.AddSingleGO(instance);
        }

        private static GameObject ResolveEffectPrefab(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return prefab;
            }

            string searchName = Path.GetFileNameWithoutExtension(path);
            string[] guids = AssetDatabase.FindAssets($"t:Prefab {searchName}");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(assetPath),
                        searchName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                }
            }

            return null;
        }

        private void PlayPreviewSkill()
        {
            if (skillConfig == null || previewPrefab == null)
            {
                return;
            }

            EnsurePreviewInstance();
            if (previewInstance == null || previewAdapter == null)
            {
                return;
            }

            previewAdapter.Reset();
            previewSkill = skillConfig.CreateSkill(previewAdapter);
            if (previewSkill == null)
            {
                return;
            }

            previewSkill.Play();
            previewPlaying = previewSkill.IsPlaying;
            previewLastUpdateTime = 0d;
            previewAccumulatedSeconds = 0d;
            Repaint();
        }

        private void StopPreviewSkill()
        {
            previewSkill?.Stop();
            previewSkill = null;
            previewPlaying = false;
            previewAccumulatedSeconds = 0d;
            previewAdapter?.Reset();
            Repaint();
        }

        private void TickPreviewSkill()
        {
            if (!previewPlaying)
            {
                return;
            }

            if (previewSkill == null || !previewSkill.IsPlaying)
            {
                previewPlaying = false;
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            if (previewLastUpdateTime <= 0d)
            {
                previewLastUpdateTime = currentTime;
                return;
            }

            previewAccumulatedSeconds += currentTime - previewLastUpdateTime;
            previewLastUpdateTime = currentTime;

            float frameDuration = 1f / PreviewFrameRate;
            while (previewAccumulatedSeconds >= frameDuration && previewSkill.IsPlaying)
            {
                previewSkill.Tick();
                previewAdapter?.Tick(frameDuration);
                previewAccumulatedSeconds -= frameDuration;
            }

            if (!previewSkill.IsPlaying)
            {
                previewPlaying = false;
            }

            Repaint();
        }

        private Bounds GetPreviewBounds()
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool hasBounds = false;
            foreach (Renderer renderer in previewInstance.GetComponentsInChildren<Renderer>())
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        private void DestroyPreviewInstance()
        {
            StopPreviewSkill();
            previewAdapter?.Dispose();
            previewAdapter = null;

            if (previewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(previewInstance);
            }

            previewInstance = null;
            ResetPreviewCamera();
        }

        private void CleanupPreview()
        {
            DestroyPreviewInstance();
            previewRenderUtility?.Cleanup();
            previewRenderUtility = null;
        }

        private void DrawSkillSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Skill", EditorStyles.boldLabel);

                SerializedProperty skillId = serializedSkill.FindProperty("SkillId");
                SerializedProperty durationFrames = serializedSkill.FindProperty("DurationFrames");
                SerializedProperty cooldownFrames = serializedSkill.FindProperty("CooldownFrames");

                EditorGUILayout.PropertyField(skillId);
                EditorGUILayout.PropertyField(durationFrames);
                EditorGUILayout.PropertyField(cooldownFrames);

                cursorFrame = ClampCursorFrame(cursorFrame);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTimeline()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Cursor", GUILayout.Width(46f));
                    cursorFrame = ClampCursorFrame(
                        EditorGUILayout.IntField(cursorFrame, GUILayout.Width(56f)));
                    EditorGUILayout.LabelField("Zoom", GUILayout.Width(38f));
                    pixelsPerFrame = GUILayout.HorizontalSlider(pixelsPerFrame, 3f, 24f, GUILayout.Width(130f));
                }
                EditorGUILayout.EndHorizontal();

                int visibleDuration = GetVisibleDuration();
                float contentWidth = Mathf.Max(
                    RowLabelWidth + visibleDuration * pixelsPerFrame + 24f,
                    position.width - 18f);
                float contentHeight = HeaderHeight + Mathf.Max(1, nodeRows.Count) * RowHeight + 12f;

                timelineScroll = EditorGUILayout.BeginScrollView(timelineScroll);
                {
                    Rect timelineRect = GUILayoutUtility.GetRect(
                        contentWidth,
                        contentHeight,
                        GUILayout.Width(contentWidth),
                        GUILayout.Height(contentHeight));

                    DrawTimelineBackground(timelineRect, visibleDuration);
                    DrawTimelineRows(timelineRect);
                    HandleTimelineEvents(timelineRect);
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTimelineBackground(Rect timelineRect, int visibleDuration)
        {
            EditorGUI.DrawRect(timelineRect, new Color(0.14f, 0.15f, 0.17f));

            int tickInterval = pixelsPerFrame < 6f ? 10 : 5;
            Color tickColor = new Color(1f, 1f, 1f, 0.08f);
            Color tickLabelColor = new Color(1f, 1f, 1f, 0.38f);

            for (int frame = 0; frame <= visibleDuration; frame += tickInterval)
            {
                float x = GetFrameX(timelineRect, frame);
                Rect lineRect = new Rect(x, timelineRect.y + 16f, 1f, timelineRect.height - 16f);
                EditorGUI.DrawRect(lineRect, tickColor);

                Rect labelRect = new Rect(x + 2f, timelineRect.y + 2f, 42f, 14f);
                GUI.Label(labelRect, frame.ToString(), EditorStyles.miniLabel);
                GUI.color = tickLabelColor;
            }

            float cursorX = GetFrameX(timelineRect, cursorFrame);
            EditorGUI.DrawRect(
                new Rect(cursorX, timelineRect.y, 1f, timelineRect.height),
                new Color(1f, 0.78f, 0.25f, 0.85f));

            GUI.color = Color.white;
        }

        private void DrawTimelineRows(Rect timelineRect)
        {
            for (int i = 0; i < nodeRows.Count; i++)
            {
                SkillNodeRow row = nodeRows[i];
                SerializedProperty nodeProperty = serializedSkill.FindProperty(row.PropertyPath);
                if (nodeProperty == null)
                {
                    continue;
                }

                SkillNodeConfig nodeConfig = nodeProperty.managedReferenceValue as SkillNodeConfig;
                Rect rowRect = new Rect(
                    timelineRect.x,
                    timelineRect.y + HeaderHeight + i * RowHeight,
                    timelineRect.width,
                    RowHeight);

                EditorGUI.DrawRect(
                    rowRect,
                    i % 2 == 0
                        ? new Color(1f, 1f, 1f, 0.025f)
                        : new Color(1f, 1f, 1f, 0.0f));

                if (dragRowSourceIndex >= 0 &&
                    dragRowTargetIndex == i &&
                    dragRowSourceIndex != i)
                {
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.08f));
                }

                int nodeId = nodeProperty.FindPropertyRelative("NodeId").intValue;
                int startFrame = nodeProperty.FindPropertyRelative("StartFrameId").intValue;
                int durationFrames = nodeProperty.FindPropertyRelative("DurationFrames").intValue;
                durationFrames = Mathf.Max(1, durationFrames);

                Rect labelRect = new Rect(
                    rowRect.x + 6f + row.Depth * 14f,
                    rowRect.y + 4f,
                    RowLabelWidth - 12f,
                    RowHeight - 8f);

                string nodeTypeName = GetNodeTypeName(nodeConfig);
                string indentText = row.Depth > 0 ? new string(' ', row.Depth * 3) : string.Empty;
                GUI.Label(labelRect, $"{indentText}[{nodeId}] {nodeTypeName}", EditorStyles.miniLabel);

                SerializedProperty enabledProperty = nodeProperty.FindPropertyRelative("Enabled");
                Color nodeColor = enabledProperty == null || enabledProperty.boolValue
                    ? GetNodeColor(nodeConfig)
                    : new Color(0.45f, 0.45f, 0.45f);
                float barX = GetFrameX(timelineRect, startFrame);
                float barWidth = Mathf.Max(8f, durationFrames * pixelsPerFrame);
                Rect barRect = new Rect(barX, rowRect.y + 5f, barWidth, RowHeight - 10f);
                EditorGUI.DrawRect(barRect, nodeColor);

                if (IsNodeSelected(row.PropertyPath))
                {
                    Rect outlineRect = new Rect(barRect.x - 1f, barRect.y - 1f, barRect.width + 2f, barRect.height + 2f);
                    Color previousColor = GUI.color;
                    GUI.color = Color.white;
                    EditorGUI.DrawRect(new Rect(outlineRect.x, outlineRect.y, outlineRect.width, 1f), Color.white);
                    EditorGUI.DrawRect(new Rect(outlineRect.x, outlineRect.yMax - 1f, outlineRect.width, 1f), Color.white);
                    EditorGUI.DrawRect(new Rect(outlineRect.x, outlineRect.y, 1f, outlineRect.height), Color.white);
                    EditorGUI.DrawRect(new Rect(outlineRect.xMax - 1f, outlineRect.y, 1f, outlineRect.height), Color.white);
                    GUI.color = previousColor;
                }

                Rect barLabelRect = new Rect(barRect.x + 5f, barRect.y + 1f, barRect.width - 10f, barRect.height);
                GUI.Label(barLabelRect, $"{startFrame} - {startFrame + durationFrames - 1}", EditorStyles.miniLabel);
            }
        }

        private void HandleTimelineEvents(Rect timelineRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null || !timelineRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                int nodeIndex = GetNodeIndexAt(timelineRect, currentEvent.mousePosition);
                if (nodeIndex >= 0)
                {
                    SkillNodeRow row = nodeRows[nodeIndex];
                    bool additive = currentEvent.control || currentEvent.command;
                    SelectNode(nodeIndex, additive, currentEvent.shift);

                    Rect barRect = GetNodeBarRect(timelineRect, nodeIndex);
                    dragNodeIndex = nodeIndex;
                    dragResize = currentEvent.mousePosition.x > barRect.xMax - ResizeHandleWidth;
                    dragStartMouseX = currentEvent.mousePosition.x;
                    dragOriginalStartFrame = GetNodeStartFrame(nodeIndex);
                    dragOriginalDuration = GetNodeDuration(nodeIndex);
                    currentEvent.Use();
                    Repaint();
                }
                else
                {
                    int rowIndex = GetTimelineRowIndexAt(timelineRect, currentEvent.mousePosition);
                    if (rowIndex >= 0)
                    {
                        bool additive = currentEvent.control || currentEvent.command;
                        SelectNode(rowIndex, additive, currentEvent.shift);

                        if (CanDragTimelineRow(rowIndex))
                        {
                            dragRowSourceIndex = rowIndex;
                            dragRowTargetIndex = rowIndex;
                        }

                        currentEvent.Use();
                        Repaint();
                    }
                    else
                    {
                        ClearNodeSelection();
                        currentEvent.Use();
                    }
                }
            }
            else if (currentEvent.type == EventType.MouseDrag &&
                     currentEvent.button == 0 &&
                     dragRowSourceIndex >= 0)
            {
                int targetIndex = GetTimelineRowIndexAt(timelineRect, currentEvent.mousePosition);
                dragRowTargetIndex = targetIndex >= 0 &&
                    CanSwapTimelineNodes(dragRowSourceIndex, targetIndex)
                    ? targetIndex
                    : -1;
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && dragNodeIndex >= 0)
            {
                int deltaFrames = Mathf.RoundToInt((currentEvent.mousePosition.x - dragStartMouseX) / pixelsPerFrame);

                if (dragResize)
                {
                    int duration = Mathf.Max(1, dragOriginalDuration + deltaFrames);
                    SetNodeDuration(dragNodeIndex, duration);
                }
                else
                {
                    int startFrame = Mathf.Max(0, dragOriginalStartFrame + deltaFrames);
                    SetNodeStartFrame(dragNodeIndex, startFrame);
                }

                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseUp && dragNodeIndex >= 0)
            {
                dragNodeIndex = -1;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && dragRowSourceIndex >= 0)
            {
                if (dragRowTargetIndex >= 0 && dragRowTargetIndex != dragRowSourceIndex)
                {
                    SwapTimelineNodes(dragRowSourceIndex, dragRowTargetIndex);
                }

                dragRowSourceIndex = -1;
                dragRowTargetIndex = -1;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.ContextClick)
            {
                int frame = GetFrameFromPosition(timelineRect, currentEvent.mousePosition);
                GenericMenu menu = new GenericMenu();
                AddNodeTypeMenuItems(menu, frame);
                if (selectedNodePaths.Count > 0)
                {
                    menu.AddSeparator(string.Empty);
                    menu.AddItem(
                        new GUIContent($"Delete Selected ({selectedNodePaths.Count})"),
                        false,
                        DeleteSelectedNodes);
                }

                menu.ShowAsContext();
                currentEvent.Use();
            }
        }

        private void DrawSelectedNode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (!string.IsNullOrEmpty(selectedNodePath) && IsTopLevelNodePath(selectedNodePath))
                    {
                        if (GUILayout.Button("Duplicate", GUILayout.Width(76f)))
                        {
                            DuplicateSelectedNode();
                        }
                    }

                    if (selectedNodePaths.Count > 0)
                    {
                        string deleteLabel = selectedNodePaths.Count > 1
                            ? $"Delete ({selectedNodePaths.Count})"
                            : "Delete";

                        if (GUILayout.Button(deleteLabel, GUILayout.Width(84f)))
                        {
                            DeleteSelectedNodes();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                SerializedProperty nodeProperty = GetSelectedNodeProperty();
                if (nodeProperty == null)
                {
                    EditorGUILayout.HelpBox("Select a node in the timeline to edit its properties.", MessageType.Info);
                }
                else
                {
                    inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll, GUILayout.MaxHeight(260f));
                    DrawNodeInspector(nodeProperty);
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawNodeInspector(SerializedProperty nodeProperty)
        {
            EditorGUILayout.PropertyField(nodeProperty, GUIContent.none, true);
        }

        private void ShowChildNodeMenu(string childPropertyPath)
        {
            GenericMenu menu = new GenericMenu();
            AddChildNodeMenuItems(menu, childPropertyPath);
            menu.ShowAsContext();
        }

        private void AddChildNodeMenuItems(GenericMenu menu, string childPropertyPath)
        {
            foreach (SkillNodeConfigType nodeConfigType in SkillNodeConfigTypeCache.GetTypes())
            {
                Type nodeType = nodeConfigType.Type;
                menu.AddItem(
                    new GUIContent(nodeConfigType.DisplayName),
                    false,
                    () => AddChildNode(nodeType, childPropertyPath));
            }
        }

        private void AddChildNode(Type nodeType, string childPropertyPath)
        {
            serializedSkill.Update();
            SerializedProperty childProperty = serializedSkill.FindProperty(childPropertyPath);
            if (childProperty == null)
            {
                return;
            }

            childProperty.managedReferenceValue = SkillNodeConfigTypeCache.Create(nodeType);
            childProperty.FindPropertyRelative("NodeId").intValue = GetNextNodeId();
            SkillNodeEditorUtility.InitializeChildNodeTiming(childProperty, DefaultNodeDuration);

            serializedSkill.ApplyModifiedProperties();
            ClearNodeSelection();
            selectedNodePath = childPropertyPath;
            selectedNodeIndex = -1;
            Repaint();
        }

        private void AddNodeTypeMenuItems(GenericMenu menu, int startFrame)
        {
            foreach (SkillNodeConfigType nodeConfigType in SkillNodeConfigTypeCache.GetTypes())
            {
                Type nodeType = nodeConfigType.Type;
                menu.AddItem(
                    new GUIContent(nodeConfigType.DisplayName),
                    false,
                    () => AddNode(nodeType, startFrame));
            }
        }

        private void AddNode(Type nodeType, int startFrame)
        {
            if (skillConfig == null)
            {
                return;
            }

            serializedSkill.Update();
            int nextNodeId = GetNextNodeId();

            nodesProperty.arraySize++;
            int nodeIndex = nodesProperty.arraySize - 1;
            SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(nodeIndex);
            nodeProperty.managedReferenceValue = SkillNodeConfigTypeCache.Create(nodeType);

            nodeProperty.FindPropertyRelative("NodeId").intValue = nextNodeId;
            nodeProperty.FindPropertyRelative("StartFrameId").intValue = Mathf.Max(0, startFrame);
            nodeProperty.FindPropertyRelative("DurationFrames").intValue = DefaultNodeDuration;

            serializedSkill.ApplyModifiedProperties();
            ClearNodeSelection();
            selectedNodePath = nodesProperty.GetArrayElementAtIndex(nodeIndex).propertyPath;
            selectedNodeIndex = nodeIndex;
            Repaint();
        }

        private void DuplicateSelectedNode()
        {
            if (selectedNodeIndex < 0 || selectedNodeIndex >= nodesProperty.arraySize)
            {
                return;
            }

            serializedSkill.Update();
            SkillNodeConfig sourceConfig = GetNodeConfig(selectedNodeIndex);
            if (sourceConfig == null)
            {
                return;
            }

            SkillNodeConfig copiedConfig = CreateCopy(sourceConfig);
            SkillNodeEditorUtility.PrepareCopiedNode(copiedConfig, nodesProperty);
            int insertIndex = selectedNodeIndex + 1;
            nodesProperty.InsertArrayElementAtIndex(insertIndex);
            SerializedProperty copiedProperty = nodesProperty.GetArrayElementAtIndex(insertIndex);
            copiedProperty.managedReferenceValue = copiedConfig;
            copiedProperty.FindPropertyRelative("NodeId").intValue = GetNextNodeId();

            serializedSkill.ApplyModifiedProperties();
            ClearNodeSelection();
            selectedNodePath = nodesProperty.GetArrayElementAtIndex(insertIndex).propertyPath;
            selectedNodeIndex = insertIndex;
            Repaint();
        }

        private void CreateSkillConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Skill Config",
                "NewSkill",
                "asset",
                "Select a location for the skill config.");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            skillConfig = CreateInstance<SkillConfig>();
            AssetDatabase.CreateAsset(skillConfig, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            selectedNodeIndex = -1;
            RefreshSerializedSkill();
            Selection.activeObject = skillConfig;
        }

        private void SaveSkillConfig()
        {
            if (skillConfig == null)
            {
                return;
            }

            serializedSkill.ApplyModifiedProperties();
            EditorUtility.SetDirty(skillConfig);
            AssetDatabase.SaveAssets();
        }

        private void RefreshSerializedSkill()
        {
            serializedSkill = skillConfig != null ? new SerializedObject(skillConfig) : null;
            nodesProperty = serializedSkill != null ? serializedSkill.FindProperty("Nodes") : null;
        }

        private void BuildNodeRows()
        {
            nodeRows.Clear();
            if (nodesProperty == null)
            {
                return;
            }

            for (int i = 0; i < nodesProperty.arraySize; i++)
            {
                SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);
                AddNodeRow(nodeProperty, 0);
            }
        }

        private void AddNodeRow(SerializedProperty nodeProperty, int depth)
        {
            if (nodeProperty == null || nodeProperty.managedReferenceValue == null)
            {
                return;
            }

            nodeRows.Add(new SkillNodeRow(nodeProperty.propertyPath, depth));

            if (nodeProperty.managedReferenceValue is DecoratorSkillNodeConfig)
            {
                SerializedProperty childNode = nodeProperty.FindPropertyRelative("ChildNode");
                if (childNode != null && childNode.managedReferenceValue != null)
                {
                    AddNodeRow(childNode, depth + 1);
                }
            }
            else if (nodeProperty.managedReferenceValue is CompositeSkillNodeConfig)
            {
                SerializedProperty children = nodeProperty.FindPropertyRelative("Children");
                if (children == null)
                {
                    return;
                }

                for (int i = 0; i < children.arraySize; i++)
                {
                    SerializedProperty childNode = children.GetArrayElementAtIndex(i);
                    if (childNode != null && childNode.managedReferenceValue != null)
                    {
                        AddNodeRow(childNode, depth + 1);
                    }
                }
            }
        }

        private void EnsureSkillDurationCoversNodes()
        {
            if (skillConfig == null)
            {
                return;
            }

            SerializedProperty durationFrames = serializedSkill.FindProperty("DurationFrames");
            int requiredDuration = durationFrames.intValue;

            for (int i = 0; i < nodeRows.Count; i++)
            {
                SerializedProperty nodeProperty = serializedSkill.FindProperty(nodeRows[i].PropertyPath);
                if (nodeProperty == null)
                {
                    continue;
                }

                SerializedProperty startProperty =
                    nodeProperty.FindPropertyRelative("StartFrameId");
                SerializedProperty durationProperty =
                    nodeProperty.FindPropertyRelative("DurationFrames");
                if (startProperty == null || durationProperty == null)
                {
                    continue;
                }

                int startFrame = startProperty.intValue;
                int duration = durationProperty.intValue;
                int endFrame = startFrame + Mathf.Max(1, duration);
                requiredDuration = Mathf.Max(requiredDuration, endFrame);
            }

            if (requiredDuration > durationFrames.intValue)
            {
                durationFrames.intValue = requiredDuration;
            }
        }

        private SerializedProperty GetSelectedNodeProperty()
        {
            if (string.IsNullOrEmpty(selectedNodePath) || serializedSkill == null)
            {
                return null;
            }

            return serializedSkill.FindProperty(selectedNodePath);
        }

        private static bool IsTopLevelNodePath(string propertyPath)
        {
            return !string.IsNullOrEmpty(propertyPath) &&
                   propertyPath.StartsWith("Nodes.Array.data[", StringComparison.Ordinal) &&
                   !propertyPath.Contains(".ChildNode");
        }

        private SkillNodeConfig GetNodeConfig(int index)
        {
            if (index < 0 || index >= nodesProperty.arraySize)
            {
                return null;
            }

            return nodesProperty.GetArrayElementAtIndex(index).managedReferenceValue as SkillNodeConfig;
        }

        private int GetNodeIndexAt(Rect timelineRect, Vector2 mousePosition)
        {
            int row = Mathf.FloorToInt((mousePosition.y - timelineRect.y - HeaderHeight) / RowHeight);
            if (row < 0 || row >= nodeRows.Count)
            {
                return -1;
            }

            Rect barRect = GetNodeBarRect(timelineRect, row);
            Rect hitRect = new Rect(barRect.x - 2f, barRect.y - 2f, barRect.width + 4f, barRect.height + 4f);
            return hitRect.Contains(mousePosition) ? row : -1;
        }

        private int GetTimelineRowIndexAt(Rect timelineRect, Vector2 mousePosition)
        {
            int row = Mathf.FloorToInt((mousePosition.y - timelineRect.y - HeaderHeight) / RowHeight);
            return row >= 0 && row < nodeRows.Count ? row : -1;
        }

        private bool IsNodeSelected(string propertyPath)
        {
            return !string.IsNullOrEmpty(propertyPath) && selectedNodePaths.Contains(propertyPath);
        }

        private void SelectNode(int rowIndex, bool additive, bool range)
        {
            if (rowIndex < 0 || rowIndex >= nodeRows.Count)
            {
                return;
            }

            string propertyPath = nodeRows[rowIndex].PropertyPath;
            if (additive)
            {
                if (IsNodeSelected(propertyPath))
                {
                    selectedNodePaths.Remove(propertyPath);
                    if (selectedNodePath == propertyPath)
                    {
                        selectedNodePath = selectedNodePaths.Count > 0
                            ? selectedNodePaths[selectedNodePaths.Count - 1]
                            : null;
                        selectedNodeIndex = selectedNodePath != null && IsTopLevelNodePath(selectedNodePath)
                            ? GetTopLevelNodeIndexFromPath(selectedNodePath)
                            : -1;
                    }

                    return;
                }

                if (!selectedNodePaths.Contains(propertyPath))
                {
                    selectedNodePaths.Add(propertyPath);
                }
            }
            else if (range && !string.IsNullOrEmpty(selectedNodePath))
            {
                int startIndex = nodeRows.FindIndex(row => row.PropertyPath == selectedNodePath);
                if (startIndex >= 0)
                {
                    int minIndex = Mathf.Min(startIndex, rowIndex);
                    int maxIndex = Mathf.Max(startIndex, rowIndex);
                    selectedNodePaths.Clear();
                    for (int i = minIndex; i <= maxIndex; i++)
                    {
                        selectedNodePaths.Add(nodeRows[i].PropertyPath);
                    }
                }
                else
                {
                    selectedNodePaths.Clear();
                    selectedNodePaths.Add(propertyPath);
                }
            }
            else
            {
                selectedNodePaths.Clear();
                selectedNodePaths.Add(propertyPath);
            }

            selectedNodePath = propertyPath;
            selectedNodeIndex = nodeRows[rowIndex].Depth == 0
                ? GetTopLevelNodeIndexFromPath(propertyPath)
                : -1;
        }

        private void ClearNodeSelection()
        {
            selectedNodePaths.Clear();
            selectedNodePath = null;
            selectedNodeIndex = -1;
        }

        private void SwapTimelineNodes(int sourceRowIndex, int targetRowIndex)
        {
            if (sourceRowIndex < 0 ||
                targetRowIndex < 0 ||
                sourceRowIndex >= nodeRows.Count ||
                targetRowIndex >= nodeRows.Count ||
                sourceRowIndex == targetRowIndex)
            {
                return;
            }

            SkillNodeRow sourceRow = nodeRows[sourceRowIndex];
            SkillNodeRow targetRow = nodeRows[targetRowIndex];

            string sourceParentPath = GetParentTimelinePath(sourceRow.PropertyPath);
            string targetParentPath = GetParentTimelinePath(targetRow.PropertyPath);

            if (sourceParentPath == null && targetParentPath == null)
            {
                SwapTopLevelTimelineNodes(sourceRow.PropertyPath, targetRow.PropertyPath);
                return;
            }

            if (string.Equals(sourceParentPath, targetParentPath, StringComparison.Ordinal))
            {
                SwapCompositeChildTimelineNodes(sourceParentPath, sourceRow.PropertyPath, targetRow.PropertyPath);
            }
        }

        private bool CanDragTimelineRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= nodeRows.Count)
            {
                return false;
            }

            return nodeRows[rowIndex].Depth == 0 ||
                   nodeRows[rowIndex].PropertyPath.Contains(ChildrenArrayMarker);
        }

        private bool CanSwapTimelineNodes(int sourceRowIndex, int targetRowIndex)
        {
            if (sourceRowIndex < 0 ||
                targetRowIndex < 0 ||
                sourceRowIndex >= nodeRows.Count ||
                targetRowIndex >= nodeRows.Count)
            {
                return false;
            }

            string sourceParentPath = GetParentTimelinePath(nodeRows[sourceRowIndex].PropertyPath);
            string targetParentPath = GetParentTimelinePath(nodeRows[targetRowIndex].PropertyPath);
            return string.Equals(sourceParentPath, targetParentPath, StringComparison.Ordinal);
        }

        private static string GetParentTimelinePath(string propertyPath)
        {
            int markerIndex = propertyPath.LastIndexOf(
                ChildrenArrayMarker,
                StringComparison.Ordinal);
            return markerIndex < 0
                ? null
                : propertyPath.Substring(0, markerIndex);
        }

        private static int GetChildrenArrayIndexFromPath(string propertyPath)
        {
            int markerIndex = propertyPath.LastIndexOf(
                ChildrenArrayMarker,
                StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return -1;
            }

            int startIndex = markerIndex + ChildrenArrayMarker.Length;
            int endIndex = propertyPath.IndexOf(']', startIndex);
            return endIndex > startIndex &&
                   int.TryParse(
                       propertyPath.Substring(startIndex, endIndex - startIndex),
                       out int index)
                ? index
                : -1;
        }

        private void SwapTopLevelTimelineNodes(string sourcePropertyPath, string targetPropertyPath)
        {
            int sourceIndex = GetTopLevelNodeIndexFromPath(sourcePropertyPath);
            int targetIndex = GetTopLevelNodeIndexFromPath(targetPropertyPath);
            if (sourceIndex < 0 ||
                targetIndex < 0 ||
                sourceIndex >= skillConfig.Nodes.Count ||
                targetIndex >= skillConfig.Nodes.Count)
            {
                return;
            }

            serializedSkill.ApplyModifiedProperties();
            (skillConfig.Nodes[sourceIndex], skillConfig.Nodes[targetIndex]) =
                (skillConfig.Nodes[targetIndex], skillConfig.Nodes[sourceIndex]);
            serializedSkill.Update();
            EditorUtility.SetDirty(skillConfig);
            Repaint();
        }

        private void SwapCompositeChildTimelineNodes(
            string parentPropertyPath,
            string sourcePropertyPath,
            string targetPropertyPath)
        {
            SerializedProperty parentProperty =
                serializedSkill.FindProperty(parentPropertyPath);
            if (parentProperty == null ||
                parentProperty.managedReferenceValue is not CompositeSkillNodeConfig parentConfig ||
                parentConfig.Children == null)
            {
                return;
            }

            int sourceIndex = GetChildrenArrayIndexFromPath(sourcePropertyPath);
            int targetIndex = GetChildrenArrayIndexFromPath(targetPropertyPath);
            if (sourceIndex < 0 ||
                targetIndex < 0 ||
                sourceIndex >= parentConfig.Children.Count ||
                targetIndex >= parentConfig.Children.Count)
            {
                return;
            }

            serializedSkill.ApplyModifiedProperties();
            (parentConfig.Children[sourceIndex], parentConfig.Children[targetIndex]) =
                (parentConfig.Children[targetIndex], parentConfig.Children[sourceIndex]);
            serializedSkill.Update();
            EditorUtility.SetDirty(skillConfig);
            Repaint();
        }

        private void DeleteSelectedNodes()
        {
            if (selectedNodePaths.Count == 0)
            {
                return;
            }

            serializedSkill.Update();

            List<int> topLevelIndices = new();
            foreach (string propertyPath in selectedNodePaths)
            {
                if (IsTopLevelNodePath(propertyPath))
                {
                    int index = GetTopLevelNodeIndexFromPath(propertyPath);
                    if (index >= 0 && index < nodesProperty.arraySize && !topLevelIndices.Contains(index))
                    {
                        topLevelIndices.Add(index);
                    }
                }
            }

            foreach (string propertyPath in selectedNodePaths)
            {
                if (IsTopLevelNodePath(propertyPath))
                {
                    continue;
                }

                int parentIndex = GetTopLevelNodeIndexFromPath(propertyPath);
                if (topLevelIndices.Contains(parentIndex))
                {
                    continue;
                }

                SerializedProperty childProperty = serializedSkill.FindProperty(propertyPath);
                if (childProperty != null)
                {
                    childProperty.managedReferenceValue = null;
                }
            }

            topLevelIndices.Sort((left, right) => right.CompareTo(left));
            foreach (int index in topLevelIndices)
            {
                nodesProperty.DeleteArrayElementAtIndex(index);
            }

            serializedSkill.ApplyModifiedProperties();
            ClearNodeSelection();
            Repaint();
        }

        private Rect GetNodeBarRect(Rect timelineRect, int nodeIndex)
        {
            int startFrame = GetNodeStartFrame(nodeIndex);
            int durationFrames = GetNodeDuration(nodeIndex);
            float x = GetFrameX(timelineRect, startFrame);
            float width = Mathf.Max(8f, durationFrames * pixelsPerFrame);
            float y = timelineRect.y + HeaderHeight + nodeIndex * RowHeight + 5f;

            return new Rect(x, y, width, RowHeight - 10f);
        }

        private int GetNodeStartFrame(int nodeIndex)
        {
            return GetNodeProperty(nodeIndex).FindPropertyRelative("StartFrameId").intValue;
        }

        private int GetNodeDuration(int nodeIndex)
        {
            return Mathf.Max(
                1,
                GetNodeProperty(nodeIndex).FindPropertyRelative("DurationFrames").intValue);
        }

        private void SetNodeStartFrame(int nodeIndex, int startFrame)
        {
            SerializedProperty nodeProperty = GetNodeProperty(nodeIndex);
            SkillNodeEditorUtility.SetNodeStartFrame(nodeProperty, startFrame);
            SkillNodeEditorUtility.ClampChildNodeFrames(nodeProperty);
        }

        private void SetNodeDuration(int nodeIndex, int duration)
        {
            SerializedProperty nodeProperty = GetNodeProperty(nodeIndex);
            SerializedProperty durationProperty = nodeProperty.FindPropertyRelative("DurationFrames");
            int nextDuration = Mathf.Max(1, duration);
            if (!SkillNodeEditorUtility.CanSetSequenceDuration(nodeProperty, nextDuration))
            {
                return;
            }

            durationProperty.intValue = nextDuration;
            SkillNodeEditorUtility.ClampChildNodeFrames(nodeProperty);
        }

        private SerializedProperty GetNodeProperty(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= nodeRows.Count)
            {
                return null;
            }

            return serializedSkill.FindProperty(nodeRows[rowIndex].PropertyPath);
        }

        private int GetFrameFromPosition(Rect timelineRect, Vector2 mousePosition)
        {
            return ClampCursorFrame(Mathf.FloorToInt(
                (mousePosition.x - timelineRect.x - RowLabelWidth + timelineScroll.x) /
                pixelsPerFrame));
        }

        private int ClampCursorFrame(int frame)
        {
            int maxFrame = skillConfig != null
                ? Mathf.Max(0, skillConfig.DurationFrames - 1)
                : 0;
            return Mathf.Clamp(frame, 0, maxFrame);
        }

        private float GetFrameX(Rect timelineRect, int frame)
        {
            return timelineRect.x + RowLabelWidth + frame * pixelsPerFrame - timelineScroll.x;
        }

        private int GetVisibleDuration()
        {
            int visibleDuration = Mathf.Max(30, skillConfig.DurationFrames, cursorFrame + 10);

            for (int i = 0; i < nodeRows.Count; i++)
            {
                int endFrame = GetNodeStartFrame(i) + GetNodeDuration(i);
                visibleDuration = Mathf.Max(visibleDuration, endFrame + 10);
            }

            return visibleDuration;
        }

        private int GetNextNodeId()
        {
            return SkillNodeEditorUtility.GetNextNodeId(nodesProperty);
        }

        private int GetTopLevelNodeIndexFromPath(string propertyPath)
        {
            const string prefix = "Nodes.Array.data[";
            if (string.IsNullOrEmpty(propertyPath) || !propertyPath.StartsWith(prefix, StringComparison.Ordinal))
            {
                return -1;
            }

            int startIndex = prefix.Length;
            int endIndex = propertyPath.IndexOf(']', startIndex);
            if (endIndex < 0)
            {
                return -1;
            }

            return int.TryParse(propertyPath.Substring(startIndex, endIndex - startIndex), out int index)
                ? index
                : -1;
        }

        private static SkillNodeConfig CreateCopy(SkillNodeConfig sourceConfig)
        {
            return SkillNodeConfigTypeCache.CreateCopy(sourceConfig);
        }

        private static string GetNodeTypeName(SkillNodeConfig nodeConfig)
        {
            return SkillNodeConfigTypeCache.GetDisplayName(nodeConfig?.GetType());
        }

        private static string GetNodeDisplayName(SkillNodeConfig nodeConfig)
        {
            return GetNodeTypeName(nodeConfig);
        }

        private static Color GetNodeColor(SkillNodeConfig nodeConfig)
        {
            return nodeConfig switch
            {
                ConditionSkillNodeConfig => new Color(0.28f, 0.58f, 0.98f),
                DecoratorSkillNodeConfig => new Color(0.45f, 0.45f, 0.85f),
                CompositeSkillNodeConfig => new Color(0.55f, 0.75f, 0.45f),
                SkillDamageNodeConfig => new Color(0.92f, 0.32f, 0.32f),
                RangeDetectionSkillNodeConfig => new Color(0.98f, 0.65f, 0.22f),
                MovementSkillNodeConfig => new Color(0.28f, 0.76f, 0.44f),
                PlayAnimationSkillNodeConfig => new Color(0.72f, 0.52f, 0.95f),
                PlayAudioSkillNodeConfig => new Color(0.28f, 0.74f, 0.84f),
                PlayEffectSkillNodeConfig => new Color(0.86f, 0.46f, 0.78f),
                _ => new Color(0.6f, 0.6f, 0.6f)
            };
        }
    }
}
