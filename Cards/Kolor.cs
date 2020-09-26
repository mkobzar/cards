using Newtonsoft.Json;

namespace Cards
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Kolor
    {
        Blue,
        Green,
        Red,
        Black,
        Yellow,
        None
    }
}
