
using System;

public class DialogueController : IDisposable
{
    private readonly DialogueModel dialogueModel;
    private readonly DialogueView dialogueView;


    public DialogueController(DialogueView dialogueView)
    {
        dialogueModel = GameContext.Instance.Get<DialogueModel>();
        this.dialogueView = dialogueView;
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        dialogueModel.OnNodeChanged += OnNodeChanged;
        dialogueModel.OnDialogueEnd += OnDialogueEnd;
    }

    private void UnregisterEvents()
    {
        dialogueModel.OnNodeChanged -= OnNodeChanged;
        dialogueModel.OnDialogueEnd -= OnDialogueEnd;
    }

    private void OnNodeChanged(DialogueNode node)
    {
        dialogueView.ShowDialogue(node);
    }

    private void OnDialogueEnd()
    {
        dialogueView.HideDialogue();
    }

    public void SelectOption(DialogueOption option)
    {
        dialogueModel.SelectOption(option);
    }
    
    public void Dispose()
    {
        UnregisterEvents();
    }
}
