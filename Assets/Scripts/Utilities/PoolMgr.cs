using System.Collections.Generic;
using UnityEngine;

abstract public class PoolMgr<T> where T : class
{
    protected Queue<T> m_availables = new Queue<T>();
    protected List<T> m_useds = new List<T>();

    PoolFullHandling m_fullHandlingMethod;
    int m_capacity;

    public PoolMgr(int capacity , PoolFullHandling fullHandlingMathod)
    {
        m_capacity = Mathf.Max(1 , capacity);
        m_fullHandlingMethod = fullHandlingMathod;
    }

    public void PreCreate(int count)
    {
        while (count-- > 0)
        {
            T obj = TryGenerate();
            if (obj != null)
                m_availables.Enqueue(obj);
            else
                break;
        }
    }

    virtual public T Get()
    {
        T obj = null;
        if (m_availables.Count > 0)
            obj = m_availables.Dequeue();
        else
            obj = TryGenerate();

        if (obj != null)
            m_useds.Add(obj);

        return obj;
    }

    virtual public void Recycle(T obj)
    {
        if (m_useds.Remove(obj))
            m_availables.Enqueue(obj);
    }

    protected T TryGenerate()
    {
        T obj = null;
        int usedCount = m_useds.Count;

        if (usedCount + m_availables.Count >= m_capacity)
        {
            switch (m_fullHandlingMethod)
            {
                case PoolFullHandling.Extend :
                    obj = Generate();
                    break;
                
                case PoolFullHandling.RepeatUse :
                    if (m_useds.Count > 0)
                    {
                        obj = m_useds[0];
                        m_useds.RemoveAt(0);
                    }
                    break;
                
                case PoolFullHandling.None :
                default :
                    break;
            }
        }
        else
        {
            obj = Generate();
        }

        return obj;
    }

    abstract protected T Generate();
}