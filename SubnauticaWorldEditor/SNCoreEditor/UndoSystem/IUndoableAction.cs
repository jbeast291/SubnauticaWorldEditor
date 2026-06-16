using System.Collections;

namespace SNCoreEditor.UndoSystem;

public interface IUndoableAction
{
    public IEnumerator Restore();
    // Used to synchronize undoing multiple actions that occured in the same frame
    public int SaveFrame { get; }
    public bool Invalid { get; }
}