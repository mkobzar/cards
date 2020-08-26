using Newtonsoft.Json;

namespace Cards
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Color
    {
        Blue,
        Green,
        Red,
        White,
        Yellow,
        None
    }
}
