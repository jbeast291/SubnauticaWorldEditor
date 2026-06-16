namespace SNCoreEditor.UndoSystem;

public interface IUndoable
{
    public IUndoableAction GetSnapshot();
    public void Restore(IUndoableAction snapshot);
}