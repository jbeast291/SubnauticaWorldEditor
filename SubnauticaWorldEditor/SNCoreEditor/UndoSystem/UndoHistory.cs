using System.Collections.Generic;

namespace SNCoreEditor.UndoSystem;

internal class UndoHistory
{
    private Stack<IUndoableAction> _mementos;

    public void TakeSnapshot(IUndoableAction snapshot)
    {
        _mementos.Push(snapshot);
    }

    public void Undo()
    {
        _mementos.Pop().Restore();
    }
}