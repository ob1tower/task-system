using System.Dynamic;

namespace TaskSystem.Services.Shaper.Interfaces;

public interface IDataShaper<T>
{
    IEnumerable<ExpandoObject> ShapeData(IEnumerable<T> entities, string include);
    ExpandoObject ShapeData(T entity, string include);
}
