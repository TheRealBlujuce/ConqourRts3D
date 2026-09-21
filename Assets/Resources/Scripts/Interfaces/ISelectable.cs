using UnityEngine;

public enum SelectableType
{
    Unit,
    Building
}

public interface ISelectable
{
    bool IsSelected { get; }
    SelectableType SelectableType { get; }
    
    void Select();
    void Deselect();
}