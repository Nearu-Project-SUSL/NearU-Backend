namespace NearU.Api.DTOs.Transport
{
    public class BusRouteDto
    {
        public int Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string? ArrivalTime { get; set; }
        public string? BusNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class BusRouteUpdateDto
    {
        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string? ArrivalTime { get; set; }
        public string? BusNumber { get; set; }
        public string? Notes { get; set; }
    }
}