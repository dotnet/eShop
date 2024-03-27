namespace eShop.ClientApp.Models.Location;

public class Position
{
    public Position()
    {
    }

    public Position(double latitude, double longitude)
    {
        Timestamp = DateTimeOffset.UtcNow;
        Latitude = latitude;
        Longitude = longitude;
    }

    public Position(Position position)
    {
        if (position == null)
        {
            throw new ArgumentNullException(nameof(position));
        }

        Timestamp = position.Timestamp;
        Latitude = position.Latitude;
        Longitude = position.Longitude;
        Altitude = position.Altitude;
        AltitudeAccuracy = position.AltitudeAccuracy;
        Accuracy = position.Accuracy;
        Heading = position.Heading;
        Speed = position.Speed;
    }

    public DateTimeOffset Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double Accuracy { get; set; }
    public double AltitudeAccuracy { get; set; }
    public double Heading { get; set; }
    public double Speed { get; set; }
}
