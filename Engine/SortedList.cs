using System;
using System.Collections;
using System.Collections.Generic;

namespace Hefty.Engine;

public class SortedList<T> : IEnumerable<T>
    where T : IComparable
{
    private readonly List<T> items = [];
    private bool isSorted = false;

    private readonly List<T> addQueue = [];
    private readonly List<T> removeQueue = [];

    public void QueueAdd(T item)
    {
        addQueue.Add(item);
    }

    public void QueueRemove(T item)
    {
        removeQueue.Add(item);
    }

    public void ApplyQueues()
    {
        foreach (var item in removeQueue)
        {
            BinaryRemove(item);
        }
        removeQueue.Clear();
        foreach (var item in addQueue)
        {
            items.Add(item);
            isSorted = false;
        }
		if (!isSorted)
		{
			items.Sort();
			isSorted = true;
		}
        addQueue.Clear();
    }

    public void BinaryRemove(T item)
    {
        if (!isSorted)
        {
            items.Sort();
            isSorted = true;
        }
        int index = items.BinarySearch(item);
        if (index >= 0)
        {
            items.RemoveAt(index);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

	public void Clear()
	{
		items.Clear();
		addQueue.Clear();
		removeQueue.Clear();
		isSorted = false;
	}
}
