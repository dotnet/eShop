namespace eShop.ClientApp.Models.Location;

public class GeolocationException : Exception
{
    public GeolocationException(GeolocationError error)
        : base("A geolocation error occurred: " + error)
    {
        if (!Enum.IsDefined(typeof(GeolocationError), error))
        {
            throw new ArgumentException("error is not a valid GeolocationError member", nameof(error));
        }

        Error = error;
    }

    public GeolocationException(GeolocationError error, Exception innerException)
        : base("A geolocation error occurred: " + error, innerException)
    {
        if (!Enum.IsDefined(typeof(GeolocationError), error))
        {
            throw new ArgumentException("error is not a valid GeolocationError member", nameof(error));
        }

        Error = error;
    }

    public GeolocationError Error { get; private set; }
}
