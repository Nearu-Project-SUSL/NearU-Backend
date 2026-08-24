using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.DTOs;


namespace NearU_Backend_Revised.Services
{
    public class BusRouteService : IBusRouteService
    {
        private readonly IBusRouteRepository _repository;

        public BusRouteService(IBusRouteRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<BusRouteDto>> GetAllAsync()
        {
            var routes = await _repository.GetAllAsync();
            return routes.Select(MapToDto).ToList();
        }

        public async Task<BusRouteDto?> GetByIdAsync(int id)
        {
            var route = await _repository.GetByIdAsync(id);
            return route == null ? null : MapToDto(route);
        }

        public async Task<BusRouteDto> CreateAsync(BusRouteUpdateDto dto)
        {
            var route = new BusRoute
            {
                RouteName = dto.RouteName,
                StartPoint = dto.StartPoint,
                EndPoint = dto.EndPoint,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                BusNumber = dto.BusNumber,
                Notes = dto.Notes
            };

            var created = await _repository.AddAsync(route);
            return MapToDto(created);
        }

        public async Task<BusRouteDto?> UpdateAsync(int id, BusRouteUpdateDto dto)
        {
            var route = new BusRoute
            {
                Id = id,
                RouteName = dto.RouteName,
                StartPoint = dto.StartPoint,
                EndPoint = dto.EndPoint,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                BusNumber = dto.BusNumber,
                Notes = dto.Notes
            };

            var updated = await _repository.UpdateAsync(route);
            return updated == null ? null : MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static BusRouteDto MapToDto(BusRoute route)
        {
            return new BusRouteDto
            {
                Id = route.Id,
                RouteName = route.RouteName,
                StartPoint = route.StartPoint,
                EndPoint = route.EndPoint,
                DepartureTime = route.DepartureTime,
                ArrivalTime = route.ArrivalTime,
                BusNumber = route.BusNumber,
                Notes = route.Notes
            };
        }
    }
}