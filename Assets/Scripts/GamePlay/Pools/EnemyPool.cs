using UnityEngine;

public class EnemyPool : PoolMgr<Enemy>
{
    private Enemy m_prefab;
    private Transform m_storage;

    public EnemyPool(int capacity , Transform storage , Enemy prefab) :
        base(capacity , PoolFullHandling.Extend)
    {
        m_prefab = prefab;
        m_storage = storage;
        PreCreate(capacity);
    }

    protected override Enemy Generate()
    {
        Enemy item = GameObject.Instantiate(m_prefab);
        item.transform.SetParent(m_storage);
        item.transform.localScale = m_prefab.transform.localScale;
        item.gameObject.SetActive(false);
        return item;
    }

    public override Enemy Get()
    {
        Enemy item = null;
        if (m_availables.Count > 0)
            item = m_availables.Dequeue();
        else
            item = TryGenerate();

        if (item != null)
            m_useds.Add(item);

        item.gameObject.SetActive(true);
        return item;
    }

    public override void Recycle(Enemy item)
    {
        base.Recycle(item);
        item.gameObject.SetActive(false);
        item.transform.SetParent(m_storage);
        item.transform.localPosition = Vector3.zero;
        item.Initialize();
    }
}