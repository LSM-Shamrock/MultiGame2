using System.Collections.Generic;
using System.Linq;

public abstract class TableData
{
    public abstract int Key { get; }
}

public class Table<T> where T : TableData
{
    public IReadOnlyList<T> List { get; }
    public IReadOnlyDictionary<int, T> Dictionary { get; }
    public Table(IReadOnlyList<T> datas)
    {
        List = datas;
        Dictionary = datas.ToDictionary(e => e.Key);
    }
}
