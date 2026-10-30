using System.Text.Json.Serialization;

namespace ToDoWebApp.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskPriority
    {
        Düşük,
        Orta,
        Yüksek
    }
}
