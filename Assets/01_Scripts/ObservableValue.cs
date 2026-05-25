using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IObservOnlyValue<T>
{
    event Action<T> OnValueChanged;
    
    T Value { get; }
}
public class ObservableValue<T> : IObservOnlyValue<T>
{
    private T _value;
    private Func<T, T> _changeProcessor;

    public event Action<T> OnValueChanged;
    public T Value
    {
        get => _value;
        set
        {
            T prevValue = _value;

            if (_changeProcessor == null)
                _value = value;
            else
                _value = _changeProcessor(value);

            if (!EqualityComparer<T>.Default.Equals(prevValue, _value))
                OnValueChanged?.Invoke(_value);
        }
    }

    public ObservableValue(T value = default, Func<T, T> changeProcessor = null)
    {
        _value = value;
        _changeProcessor = changeProcessor;
    }
}

public interface IObservOnlyArray<T>
{
    public event Action<IReadOnlyList<T>> OnAnyValueChanged;
    public event Action<int, T> OnValueChanged;
    public IReadOnlyList<T> Values { get; }
    public T this[int index] { get; }
}
public class ObservableArray<T> : IObservOnlyArray<T>
{
    private T[] _array;
    private Func<int, T, T> _changeProcessor;

    public event Action<IReadOnlyList<T>> OnAnyValueChanged;
    public event Action<int, T> OnValueChanged;
    public int Length => _array.Length;
    public IReadOnlyList<T> Values => _array;
    public T this[int index]
    {
        get => _array[index];
        set
        {
            T prevValue = _array[index];
            T changedValue;

            if (_changeProcessor == null)
                changedValue = _array[index] = value;
            else
                changedValue = _array[index] = _changeProcessor(index, value);

            if (!EqualityComparer<T>.Default.Equals(prevValue, changedValue))
            {
                OnValueChanged?.Invoke(index, changedValue);
                OnAnyValueChanged?.Invoke(Values);
            }
        }
    }

    public ObservableArray(int length, Func<int, T, T> changeProcessor = null)
    {
        _array = new T[length];
        _changeProcessor = changeProcessor;
    }
    public ObservableArray(T[] values, Func<int, T, T> changeProcessor = null)
    {
        _array = values;
        _changeProcessor = changeProcessor;
    }
}