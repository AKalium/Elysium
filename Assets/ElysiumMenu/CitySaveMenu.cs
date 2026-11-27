using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal sealed class CitySaveMenu
{
    private readonly RectTransform root;
    private readonly Text heading;
    private readonly Text status;
    private readonly Button[] choices = new Button[CitySaveSlots.Count + 1];
    private readonly Text[] labels = new Text[CitySaveSlots.Count + 1];
    private readonly Text[] details = new Text[CitySaveSlots.Count];
    private readonly UnityAction close;
    private readonly Func<CitySave> capture;
    private readonly Action<int, CitySave> load;
    private bool confirming;

    public CitySaveMenu(Transform parent, Button template, UnityAction close,
        Func<CitySave> capture = null, Action<int, CitySave> load = null)
    {
        this.close = close;
        this.capture = capture;
        this.load = load;
        root = new GameObject(capture == null ? "Load Game Panel" : "Pause Menu", typeof(RectTransform))
            .GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        heading = CreateLabel(template, "Heading", 250, 82, 800, 105);
        status = CreateLabel(template, "Status", -200, 30, 780, 54);
        for (int i = 0; i < choices.Length; i++)
        {
            choices[i] = AddButton(template, root, "Choice " + (i + 1), 120 - i * 120);
            labels[i] = choices[i].GetComponentInChildren<Text>();
            if (i == CitySaveSlots.Count) continue;
            details[i] = Object.Instantiate(labels[i], choices[i].transform);
            details[i].name = "Save Details";
            details[i].fontSize = 26;
            details[i].rectTransform.anchoredPosition = new Vector2(0, -21);
        }
        root.gameObject.SetActive(false);
    }

    public bool IsOpen => root.gameObject.activeSelf;
    public void Hide() => root.gameObject.SetActive(false);

    public void ShowPause(UnityAction resume, UnityAction exit, UnityAction openSave)
    {
        BeginPage("Paused");
        SetChoice(0, "Resume", resume);
        SetChoice(1, "Save Game", openSave);
        SetChoice(2, "Main Menu", exit);
        SelectFirst();
    }

    public void ShowSlots(string message = "")
    {
        BeginPage(capture == null ? "Load Game" : "Save Game", message);
        for (int i = 0; i < CitySaveSlots.Count; i++)
        {
            int slot = i + 1;
            var state = CitySaveSlots.Read(slot, out var save);
            string detail = state == CitySaveSlots.SlotState.Filled ? $"Day {save.day}  |  {save.LocalSaveTime}"
                : state == CitySaveSlots.SlotState.Empty ? "Empty" : "Unreadable save";

            if (capture == null)
                SetChoice(i, $"Slot {slot}", () => load(slot, save), save != null, 38);
            else
                SetChoice(i, $"Slot {slot}", () =>
                {
                    if (state == CitySaveSlots.SlotState.Empty) Save(slot);
                    else ConfirmOverwrite(slot);
                }, true, 38);

            labels[i].rectTransform.anchoredPosition = new Vector2(0, 13);
            details[i].text = detail;
            details[i].gameObject.SetActive(true);
        }

        SetChoice(3, "Back", close);
        ((RectTransform)choices[3].transform).anchoredPosition = new Vector2(0, -290);
        SelectFirst();
    }

    public void Back()
    {
        if (confirming) ShowSlots();
        else close();
    }

    private void ConfirmOverwrite(int slot)
    {
        BeginPage($"Overwrite Slot {slot}?");
        confirming = true;
        SetChoice(0, "Overwrite", () => Save(slot));
        SetChoice(1, "Cancel", () => ShowSlots());
        EventSystem.current?.SetSelectedGameObject(choices[1].gameObject);
    }

    private void Save(int slot)
    {
        try
        {
            CitySaveSlots.Write(slot, capture());
            ShowSlots($"Saved to Slot {slot}.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not save Slot {slot}: {exception.Message}");
            ShowSlots($"Could not save Slot {slot}.");
        }
    }

    private void BeginPage(string title, string message = "")
    {
        root.gameObject.SetActive(true);
        heading.text = title;
        status.text = message;
        confirming = false;
        foreach (var choice in choices) choice.gameObject.SetActive(false);
        foreach (var detail in details) detail.gameObject.SetActive(false);
    }

    private void SetChoice(int index, string label, UnityAction action, bool enabled = true, int fontSize = 54)
    {
        var button = choices[index];
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        button.interactable = enabled;
        labels[index].rectTransform.anchoredPosition = Vector2.zero;
        labels[index].fontSize = fontSize;
        labels[index].text = label;
    }

    private void SelectFirst()
    {
        foreach (var choice in choices)
        {
            if (!choice.gameObject.activeSelf || !choice.interactable) continue;
            EventSystem.current?.SetSelectedGameObject(choice.gameObject);
            break;
        }
    }

    internal static Button AddButton(Button template, Transform parent, string label, float y)
    {
        var button = Object.Instantiate(template, parent);
        button.name = label;
        button.onClick = new Button.ButtonClickedEvent();
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.Vertical;
        button.navigation = navigation;
        ((RectTransform)button.transform).anchoredPosition = new Vector2(0, y);
        button.GetComponentInChildren<Text>().text = label;
        return button;
    }

    private Text CreateLabel(Button template, string name, float y, int fontSize, int width, int height)
    {
        var label = Object.Instantiate(template.GetComponentInChildren<Text>(), root);
        label.name = name;
        label.rectTransform.anchoredPosition = new Vector2(0, y);
        label.rectTransform.sizeDelta = new Vector2(width, height);
        label.fontSize = fontSize;
        return label;
    }
}
