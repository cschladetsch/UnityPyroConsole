using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ConsoleInput
    : TMP_InputField
{
    public Action<string> OnSubmitLine;
    public bool NeedUpdate;

    // Consume the submit so base class doesn't use it.
    public override void OnSubmit(BaseEventData eventData)
    {
    }

    protected override void Append(char input)
    {
        switch (input)
        {
            case '\n' when Input.GetKey(KeyCode.LeftControl):
                OnSubmitLine?.Invoke(GetSelection());
                break;
            case '\n':
                base.Append(input);
                break;
            case '"':
                base.Append('"');
                base.Append('"');
                selectionAnchorPosition = selectionAnchorPosition - 1;
                break;
            default:
                base.Append(input);
                break;
        }

        NeedUpdate = true;
    }

    private string GetSelection()
    {
        if (!gameObject.activeSelf)
            return string.Empty;

        var start = selectionStringFocusPosition;
        var end = selectionStringAnchorPosition;
        if (start == end)
            return "";

        if (start <= end)
            return m_Text.Substring(start, end - start);

        var tmp = start;
        start = end;
        end = tmp;

        return m_Text.Substring(start, end - start);
    }
}
