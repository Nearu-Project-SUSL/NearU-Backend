namespace NearU.Api.DTOs.Transport
{
    public class TrainRouteDto
    {
        public int Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string StartStation { get; set; } = string.Empty;
        public string EndStation { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string? ArrivalTime { get; set; }
        public string? TrainName { get; set; }
        public string? Notes { get; set; }
    }

    public class TrainRouteUpdateDto
    {
        public string RouteName { get; set; } = string.Empty;
        public string StartStation { get; set; } = string.Empty;
        public string EndStation { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string? ArrivalTime { get; set; }
        public string? TrainName { get; set; }
        public string? Notes { get; set; }
    }
}