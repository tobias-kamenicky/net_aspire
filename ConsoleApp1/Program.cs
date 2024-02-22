using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<Benchmarks>();

public class Benchmarks
{
    [Params(10, 100, 1000)]
    public int Count { get; set; }

    private string[] _array;
    private List<string> _list;
    private string _needle;

    [GlobalSetup]
    public void Setup()
    {
        _array = Enumerable.Range(0, Count).Select(_ => Random.Shared.Next().ToString()).ToArray();
        _list = _array.ToList();
        _needle = _array[Count / 2];
    }

    [Benchmark]
    public bool Array_Any()
    {
        return _array.Any(x => x == _needle);
    }

    [Benchmark]
    public bool List_Any()
    {
        return _list.Any(x => x == _needle);
    }

    [Benchmark]
    public bool Array_Exists()
    {
        return Array.Exists(_array, x => x == _needle);
    }

    [Benchmark]
    public bool List_Exists()
    {
        return _list.Exists(x => x == _needle);
    }
}
