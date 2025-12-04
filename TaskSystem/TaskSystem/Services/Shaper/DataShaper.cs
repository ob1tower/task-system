using System.Dynamic;
using System.Reflection;
using TaskSystem.Services.Shaper.Interfaces;

namespace TaskSystem.Services.Shaper;

public class DataShaper<T> : IDataShaper<T>
{
    public PropertyInfo[] Properties { get; set; }

    public DataShaper()
    {
        // BindingFlags.Public - объект типа PropertyInfo будет получать информацию только о публичных (public) свойсвах из T
        // BindingFlags.Instance - объект типа PropertyInfo будет получать информацию только о нестатических (public) свойсвах из T
        Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public IEnumerable<ExpandoObject> ShapeData(IEnumerable<T> entities, string include)
    {
        IEnumerable<PropertyInfo> props = GetRequiredProperties(include);

        return FetchData(entities, props);
    }

    public ExpandoObject ShapeData(T entity, string include)
    {
        IEnumerable<PropertyInfo> props = GetRequiredProperties(include);

        return FetchData(entity, props);
    }

    private IEnumerable<PropertyInfo> GetRequiredProperties(string include)
    {
        // Обязательные опциональные поля для ответа
        List<PropertyInfo> props = [];

        // Check: были ли переданы какие-то поля в строке запроса
        if (!string.IsNullOrWhiteSpace(include))
        {
            // Получение всех свойств из строки запроса
            string[] fields = include.Split(
                ",",
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            // Check: существует ли свойство prop из строки запроса в сущности T
            foreach (string field in fields)
            {
                // Equals используется для сравнения строк с  режимом сравнения StringComparison
                // OrdinalIgnoreCase - режим сравнения строк, который выполняет сравнение
                // без учёта регистра (A == a) и без влияния культуры
                PropertyInfo? prop = Properties // id title productId, adfasdas, asdasdasd
                    .FirstOrDefault(i => i.Name.Equals(field.Trim(), StringComparison.OrdinalIgnoreCase));

                if (prop == null)
                    continue;

                props.Add(prop);
            }
        }
        else
        {
            props = Properties.ToList();
        }

        return props;
    }

    private static ExpandoObject FetchData(T entity, IEnumerable<PropertyInfo> props) 
    {
        ExpandoObject shapedObject = new();

        // Создание свойства info.Name со значением info.GetValue(entity)
        foreach (PropertyInfo prop in props)
        {
            object? value = prop.GetValue(entity);

            shapedObject.TryAdd(
                prop.Name,
                value);
        }

        return shapedObject;
    }

    private static IEnumerable<ExpandoObject> FetchData(
        IEnumerable<T> entities,
        IEnumerable<PropertyInfo> props)
    {
        List<ExpandoObject> shapedData = [];

        foreach (T entity in entities)
        {
            ExpandoObject shapedObject = FetchData(entity, props);
            shapedData.Add(shapedObject);
        }

        return shapedData;
    }
}
