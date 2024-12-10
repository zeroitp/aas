namespace IO.Swagger.Registry.Lib.V3.Models;

using System.Collections.Generic;
using System;

public class EntityChangedNotificationMessage
{
    public List<EntityChangedItem> Items { get; } = new List<EntityChangedItem>();
    public void AddItem(EntityType type, Guid id, string name, EntityChangedAction action, string upn)
    {
        Items.Add(new EntityChangedItem(type, id, name, action, upn));
    }
}

public class EntityChangedItem
{
    public string EntityType { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Action { get; set; }
    public string Upn { get; set; }

    public EntityChangedItem(EntityType type, Guid id, string name, EntityChangedAction action, string upn)
    {
        EntityType = type.ToString();
        Id = id;
        Name = name;
        Action = action.ToString();
        Upn = upn.ToString();
    }
}

public enum EntityType
{
    Asset
}

public enum EntityChangedAction
{
    Add,
    Update,
    Delete
}