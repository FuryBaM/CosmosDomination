using UnityEngine;
[RequireComponent (typeof(BoxCollider2D))]
public abstract class NodeAction : MonoBehaviour
{
    public string ActionName;

    public abstract void SetAction(string action);
}
