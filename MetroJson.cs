using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InlineTelegramBot;

public class MetroJson
{
    // root json object containing the list of all metro lines
    public List<Line> Lines { get; set; }
}

public class Line
{
    // list of stations belonging to this specific line
    public List<Station> Stations { get; set; }
}

public class Station
{
    // unique identifier for the station
    public string Id { get; set; }

    // name of the station
    public string Name { get; set; }

    [NotMapped]
    // nested line data object from json. ignored by ef core during database mapping
    public LineData Line { get; set; }

    // name of the line the station belongs to
    public string LineName { get; set; }

    // color emoji of the line for ui display
    public string LineColorEmoji { get; set; }

    [JsonPropertyName("lat")]
    // geographical latitude parsed from the json "lat" property
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    // geographical longitude parsed from the json "lng" property
    public double Longitude { get; set; }

    // tuple returning latitude and longitude for easier distance calculations
    public (double lat, double lon) Coords { get => (Latitude, Longitude); }

    // calculated estimated travel time to mirea in minutes
    public int MinutesToMirea { get; set; }

}

public class LineData
{
    // line name parsed from the nested json object
    public string Name { get; set; }
}