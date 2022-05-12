using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthTracker.DataService.IRepository
{
    public interface IGenericRepository<T> where T : class
    {
        // Get all Entities
        Task<IEnumerable<T>> GetAll();

        // Get single entity by Id
        Task<T> GetById(Guid id);

        // Create an entity
        Task<bool> Add(T entity);

        // Delete an entity
        Task<bool> Delete(Guid id, string userId);

        // Update an entity or add if it does not exist
        Task<bool> Upsert(T entity);
    }
}
