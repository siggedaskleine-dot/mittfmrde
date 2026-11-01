namespace MittFmrte.Core.Presets;

/// <summary>One recorded edit, with the action that reverts it.</summary>
public sealed record Change(string Description, Action Undo);

/// <summary>Keeps every applied change so the user can undo the last preset.</summary>
public sealed class ChangeLog
{
    private readonly Stack<IReadOnlyList<Change>> _batches = new();

    public bool CanUndo => _batches.Count > 0;

    public void Record(IReadOnlyList<Change> batch)
    {
        if (batch.Count > 0) _batches.Push(batch);
    }

    /// <summary>Reverts the most recent batch. Returns how many changes were undone.</summary>
    public int UndoLast()
    {
        if (!_batches.TryPop(out var batch)) return 0;
        foreach (var c in batch.Reverse()) c.Undo();
        return batch.Count;
    }
}
