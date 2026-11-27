namespace GimDeks.App.Services;

public sealed record UndoEntry(string Label, Action Undo, Action Redo);

/// <summary>In-memory undo and redo stack for editing actions in this session.</summary>
public sealed class UndoRedoService
{
    private readonly Stack<UndoEntry> _undo = new();
    private readonly Stack<UndoEntry> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string UndoLabel => CanUndo ? _undo.Peek().Label : "";
    public string RedoLabel => CanRedo ? _redo.Peek().Label : "";

    public event Action? Changed;

    public void Push(string label, Action undo, Action redo)
    {
        _undo.Push(new UndoEntry(label, undo, redo));
        _redo.Clear();
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (!_undo.TryPop(out var entry))
        {
            return;
        }
        entry.Undo();
        _redo.Push(entry);
        Changed?.Invoke();
    }

    public void Redo()
    {
        if (!_redo.TryPop(out var entry))
        {
            return;
        }
        entry.Redo();
        _undo.Push(entry);
        Changed?.Invoke();
    }
}
