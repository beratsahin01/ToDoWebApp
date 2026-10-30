using System.Text.Json.Serialization;

namespace ToDoWebApp.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GroupRole
    {
        Admin,
        Viewer
    }
}
