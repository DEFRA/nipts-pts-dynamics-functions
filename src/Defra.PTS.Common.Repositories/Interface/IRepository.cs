using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IEnumerable<TEntity> GetAll();
        Task<TEntity> Find(object id);
        Task Add(TEntity entity);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        void Delete(object id);
        Task<int> SaveChanges();
    }
}
