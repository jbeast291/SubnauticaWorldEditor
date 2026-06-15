namespace SNStructureEditor.UndoSystem;

public interface IOriginator
{
    public IMemento GetSnapshot();
}