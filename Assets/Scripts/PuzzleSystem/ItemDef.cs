using UnityEngine;


[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "NewItemDefinition")]
public class ItemDef : ScriptableObject
{
   
    public string itemId;           
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemCarryType carryType; 
    public string[] interactionTags;
}

public enum ItemCarryType
{
    Hotbar,
    TwoHanded
}
