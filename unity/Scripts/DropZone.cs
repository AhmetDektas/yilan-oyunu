using UnityEngine;

public enum DropZoneType { Wood, Meat }

/// <summary>
/// Attach to the Yemekhane and Depo trigger areas. Walking a carrying
/// PlayerController into this zone empties the matching carry into the
/// GameManager stockpile. Collider2D must have "Is Trigger" checked.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DropZone : MonoBehaviour
{
    public DropZoneType type;

    void OnTriggerStay2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (type == DropZoneType.Wood && player.CarryWood > 0)
        {
            int amount = player.CarryWood;
            player.DepositWood(amount);
            GameManager.Instance.DepositWood(amount);
        }
        else if (type == DropZoneType.Meat && player.CarryMeat > 0)
        {
            int amount = player.CarryMeat;
            player.DepositMeat(amount);
            GameManager.Instance.DepositMeat(amount);
        }
    }
}
