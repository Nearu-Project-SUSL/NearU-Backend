using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.DTOs;


namespace NearU_Backend_Revised.Services
{
    public class TrainRouteService : ITrainRouteService
    {
        private readonly ITrainRouteRepository _repository;

        public TrainRouteService(ITrainRouteRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TrainRouteDto>> GetAllAsync()
        {
            var routes = await _repository.GetAllAsync();
            return routes.Select(MapToDto).ToList();
        }

        public async Task<TrainRouteDto?> GetByIdAsync(int id)
        {
            var route = await _repository.GetByIdAsync(id);
            return route == null ? null : MapToDto(route);
        }

        public async Task<TrainRouteDto> CreateAsync(TrainRouteUpdateDto dto)
        {
            var route = new TrainRoute
            {
                RouteName = dto.RouteName,
                StartStation = dto.StartStation,
                EndStation = dto.EndStation,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                TrainName = dto.TrainName,
                Notes = dto.Notes
            };

            var created = await _repository.AddAsync(route);
            return MapToDto(created);
        }

        public async Task<TrainRouteDto?> UpdateAsync(int id, TrainRouteUpdateDto dto)
        {
            var route = new TrainRoute
            {
                Id = id,
                RouteName = dto.RouteName,
                StartStation = dto.StartStation,
                EndStation = dto.EndStation,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                TrainName = dto.TrainName,
                Notes = dto.Notes
            };

            var updated = await _repository.UpdateAsync(route);
            return updated == null ? null : MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static TrainRouteDto MapToDto(TrainRoute route)
        {
            return new TrainRouteDto
            {
                Id = route.Id,
                RouteName = route.RouteName,
                StartStation = route.StartStation,
                EndStation = route.EndStation,
                DepartureTime = route.DepartureTime,
                ArrivalTime = route.ArrivalTime,
                TrainName = route.TrainName,
                Notes = route.Notes
            };
        }
    }
}