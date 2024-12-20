using UnityEngine;
public class CVar
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public object Value { get; private set; }

    public CVar(string name, object defaultValue, string description = "")
    {
        Name = name.ToLower();
        Value = defaultValue;
        Description = description;
    }

    public void SetValue(object newValue)
    {
        if (newValue.GetType() != Value.GetType())
        {
            Debug.LogWarning($"Type mismatch: Cannot set {Name} to value of type {newValue.GetType()}.");
            return;
        }
        Value = newValue;
    }

    public override string ToString()
    {
        return $"{Name} = {Value}";
    }
}
