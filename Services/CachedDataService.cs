using Microsoft.Extensions.Caching.Memory;
using TransportJournal.Data;
using TransportJournal.Models;
using Route = TransportJournal.Models.Route;

namespace TransportJournal.Services
{
    public class CachedDataService
    {
        private readonly TransportDbContext _context;
        private readonly IMemoryCache _cache;
        private const int RowCount = 20;

        public CachedDataService(TransportDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _cache = memoryCache;
        }

        public IEnumerable<Route> GetRoutes()
        {
            if (!_cache.TryGetValue("Routes", out IEnumerable<Route> routes))
            {
                routes = _context.Routes.Take(RowCount).ToList();
                _cache.Set("Routes", routes, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 28 + 240)
                });
            }
            return routes;
        }

        public IEnumerable<Schedule> GetSchedules()
        {
            if (!_cache.TryGetValue("Schedules", out IEnumerable<Schedule> schedules))
            {
                schedules = _context.Schedules.Take(RowCount).ToList();
                _cache.Set("Schedules", schedules, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 28 + 240)
                });
            }
            return schedules;
        }

        public IEnumerable<Personnel> GetPersonnel()
        {
            if (!_cache.TryGetValue("Personnel", out IEnumerable<Personnel> personnel))
            {
                personnel = _context.Personnel.Take(RowCount).ToList();
                _cache.Set("Personnel", personnel, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 28 + 240)
                });
            }
            return personnel;
        }

        public IEnumerable<Stop> GetStops()
        {
            if (!_cache.TryGetValue("Stops", out IEnumerable<Stop> stops))
            {
                stops = _context.Stops.Take(RowCount).ToList();
                _cache.Set("Stops", stops, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 28 + 240)
                });
            }
            return stops;
        }
    }
}
