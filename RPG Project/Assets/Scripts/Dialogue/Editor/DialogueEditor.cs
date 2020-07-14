using System;
using UnityEditor;
using UnityEngine;

namespace RPG.Dialogue.Editor
{
    public class DialogueEditor : EditorWindow
    {
        Dialogue selectedDialogue;
        Vector2 scrollPosition;

        [NonSerialized]
        GUIStyle nodeStyle;
        [NonSerialized]
        DialogueNode draggingNode = null;
        [NonSerialized]
        Vector2 draggingOffset;
        [NonSerialized]
        DialogueNode creatingNode = null;
        [NonSerialized]
        DialogueNode linkingParent = null;
        [NonSerialized]
        DialogueNode deletingNode = null;
        [NonSerialized]
        bool draggingCanvas = false;

        [MenuItem("Window/Dialogue Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OpenDialogue(int instanceID, int line)
        {
            Dialogue dialogue = EditorUtility.InstanceIDToObject(instanceID) as Dialogue;
            if (dialogue != null)
            {
                ShowEditorWindow();
                return true;
            }
            return false; // we did not handle the open
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            nodeStyle.normal.textColor = Color.white;
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
            nodeStyle.padding = new RectOffset(20, 20, 20, 20);
        }

        private void OnSelectionChanged()
        {
            Dialogue newDialogue = Selection.activeObject as Dialogue;
            if (newDialogue != null)
            {
                selectedDialogue = newDialogue;
            }
            Repaint();
        }

        // Only called when there is a reason. E.g. GetWindow called.
        private void OnGUI() {

            if (selectedDialogue == null)
            {
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            Rect scrollContentRect = GUILayoutUtility.GetRect(4000, 4000);
            GUI.DrawTextureWithTexCoords(scrollContentRect, EditorGUIUtility.Load("background.png") as Texture2D, new Rect(0, 0, 80, 80));
            foreach (DialogueNode node in selectedDialogue.GetAllNodes())
            {
                DrawConnection(node);
            }
            foreach (DialogueNode node in selectedDialogue.GetAllNodes())
            {
                DrawNode(node);
            }

            EditorGUILayout.EndScrollView();

            if (creatingNode != null)
            {
                Undo.RecordObject(selectedDialogue, "Dialogue Node Created");
                selectedDialogue.CreateNode(creatingNode);
                creatingNode = null;
            }

            if (deletingNode != null)
            {
                Undo.RecordObject(selectedDialogue, "Dialogue Node Deleted");
                selectedDialogue.DeleteNode(deletingNode);
                deletingNode = null;
            }

            ProcessEvent(Event.current);
        }

        private void DrawNode(DialogueNode node)
        {
            GUILayout.BeginArea(node.GetRect(), nodeStyle);
            EditorGUILayout.LabelField(node.name, EditorStyles.whiteLabel);
            EditorGUI.BeginChangeCheck();
            node.SetText(EditorGUILayout.TextArea(node.GetText(), EditorStyles.textArea));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("X"))
            {
                deletingNode = node;
            }
            DrawLinkButton(node);
            if (GUILayout.Button("+"))
            {
                creatingNode = node;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawConnection(DialogueNode node)
        {
            Vector2 ThisCenterRight = new Vector2(node.GetRect().xMax, node.GetRect().center.y);
            foreach (DialogueNode childNode in selectedDialogue.GetChildren(node))
            {
                Vector2 ChildCentreLeft = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
                Vector2 HandleOffset = ChildCentreLeft - ThisCenterRight;
                HandleOffset.x = HandleOffset.x * 0.8f;
                HandleOffset.y = 0;
                Handles.DrawBezier(
                    ThisCenterRight, ChildCentreLeft, 
                    ThisCenterRight + HandleOffset, ChildCentreLeft - HandleOffset, 
                    Color.white, null, 4f);
            }
        }

        private void DrawLinkButton(DialogueNode node)
        {
            if (linkingParent == null)
            {
                if (GUILayout.Button("link"))
                {
                    linkingParent = node;
                }
            }
            else if (linkingParent == node)
            {
                if (GUILayout.Button("cancel"))
                {
                    linkingParent = null;
                }
            }
            else if (linkingParent.HasChild(node.name))
            {
                if (GUILayout.Button("unlink"))
                {
                    linkingParent.RemoveChild(node.name);
                    linkingParent = null;
                }
            }
            else
            {
                if (GUILayout.Button("child"))
                {
                    linkingParent.AddChild(node.name);
                    linkingParent = null;
                }
            }
        }

        private void ProcessEvent(Event e)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                draggingNode = GetNodeAtPoint(e.mousePosition + scrollPosition);
                // Show why need for more natural.
                if (draggingNode != null)
                {
                    draggingOffset = draggingNode.GetRect().position - e.mousePosition;
                }
                else
                {
                    draggingCanvas = true;
                    draggingOffset = scrollPosition + e.mousePosition;
                }
            }
            else if (e.type == EventType.MouseUp && draggingNode != null)
            {
                draggingNode = null;
                draggingCanvas = false;
            }
            else if (e.type == EventType.MouseDrag && draggingNode != null)
            {
                Undo.RecordObject(selectedDialogue, "Reposition Dialogue Node");
                draggingNode.SetPosition(e.mousePosition + draggingOffset);
                // Show lag before adding this.
                GUI.changed = true;
            }
            else if (e.type == EventType.MouseDrag && draggingCanvas)
            {
                scrollPosition = draggingOffset - e.mousePosition;
                GUI.changed = true;
            }
        }

        private DialogueNode GetNodeAtPoint(Vector2 point)
        {
            foreach (DialogueNode node in selectedDialogue.GetAllNodes())
            {
                if (node.GetRect().Contains(point))
                {
                    return node;
                }
            }
            return null;
        }
    }
}