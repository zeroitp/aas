namespace IO.Swagger.Registry.Lib.V3.Models;

public class NotificationType
{
    public const string ASSET = "asset";
    public const string ASSET_CHANGE = "asset_changed";
    public const string ASSET_LIST_CHANGE = "asset_list_changed";
    public const string LOCK_ENTITY = "lock_entity";
    public const string REQUEST_UNLOCK_ENTITY = "request_unlock_entity";
    public const string REQUEST_UNLOCK_ENTITY_REJECTED = "request_unlock_entity_rejected";
    public const string REQUEST_UNLOCK_ENTITY_ACCEPTED = "request_unlock_entity_accepted";
}