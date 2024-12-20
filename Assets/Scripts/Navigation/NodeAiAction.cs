using UnityEngine;

public class NodeAiAction : NodeAction
{
    public enum ActionType
    {
        DoubleJump = 'd',
        Jump = 'j'
    }

    [SerializeField]
    private ActionType type;

    public override void SetAction(string action)
    {
        switch (action.ToLower())
        {
            case "d":
                type = ActionType.DoubleJump;
                ActionName = "DoubleJump";
                break;
            case "j":
                type = ActionType.Jump;
                ActionName = "Jump";
                break;
            default:
                Debug.LogWarning($"Unknown action type: {action}");
                break;
        }
    }

    public void SetAction(ActionType type)
    {
        this.type = type;
        switch (type)
        {
            case ActionType.DoubleJump:
                ActionName = "DoubleJump";
                break;
            case ActionType.Jump:
                ActionName = "Jump";
                break;
        }
    }
}
