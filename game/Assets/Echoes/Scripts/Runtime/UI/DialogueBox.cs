using System;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// Bottom-of-screen dialogue panel. E advances line by line; the last line closes it and then runs the
    /// optional callback. While open it blocks movement through GameFlow.IsDialogueOpen.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text speakerText;
        [SerializeField] Text bodyText;
        [SerializeField] Text hintText;

        string[] lines;
        int index;
        Action onClosed;

        public bool IsOpen { get; private set; }
        public string CurrentLine => IsOpen ? lines[index] : string.Empty;
        public int LineIndex => index;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            if (IsOpen && !GameFlow.IsPaused && GameFlow.TryConsumeInteract()) Advance();
        }

        public void Open(string speaker, string[] dialogueLines, Action closed = null)
        {
            if (IsOpen || dialogueLines == null || dialogueLines.Length == 0) return;

            lines = dialogueLines;
            index = 0;
            onClosed = closed;
            IsOpen = true;
            GameFlow.IsDialogueOpen = true;

            if (speakerText != null) speakerText.text = speaker;
            if (panel != null) panel.SetActive(true);
            ShowLine();
        }

        public void Advance()
        {
            if (!IsOpen) return;
            index++;
            if (index >= lines.Length) Close();
            else ShowLine();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameFlow.IsDialogueOpen = false;
            if (panel != null) panel.SetActive(false);

            Action callback = onClosed;
            onClosed = null;
            if (callback != null) callback();
        }

        void ShowLine()
        {
            if (bodyText != null) bodyText.text = lines[index];
            if (hintText != null) hintText.text = index < lines.Length - 1 ? "E  Continue" : "E  Close";
        }
    }
}
