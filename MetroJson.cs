using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InlineTelegramBot;

public class MetroJson
{
    public List<Line> Lines { get; set; }
}

public class Line
{
    public List<Station> Stations { get; set; }
}

public class Station
{
    public string Id { get; set; }
    public string Name { get; set; }

    [NotMapped]
    public LineData Line { get; set; }
    public string LineName { get; set; }
    public string LineHexColor { get; set; }

    [JsonPropertyName("lat")]
    public double Latitude { get; set; }
    [JsonPropertyName("lng")]
    public double Longitude { get; set; }

    public (double lat, double lon) Coords { get => (Latitude, Longitude); } 

    public int MinutesToMirea { get; set; }

}

public class LineData
{
    public string Name { get; set; }

    [JsonPropertyName("hex_color")]
    public string HexColor { get; set; }
}