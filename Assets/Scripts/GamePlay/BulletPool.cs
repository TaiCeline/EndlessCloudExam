using UnityEngine;

public class BulletPool : PoolMgr<Bullet>
{
    private Bullet m_prefab;
    private Transform m_storage;

    public BulletPool(int capacity , Transform storage , Bullet prefab) :
        base(capacity , PoolFullHandling.Extend)
    {
        m_prefab = prefab;
        m_storage = storage;
        PreCreate(capacity);
    }

    protected override Bullet Generate()
    {
        Bullet item = GameObject.Instantiate(m_prefab);
        item.transform.SetParent(m_storage);
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = m_prefab.transform.localScale;
        item.gameObject.SetActive(false);
        return item;
    }

    public override Bullet Get()
    {
        Bullet item = null;
        if (m_availables.Count > 0)
            item = m_availables.Dequeue();
        else
            item = TryGenerate();

        if (item != null)
            m_useds.Add(item);

        item.gameObject.SetActive(true);
        return item;
    }

    public override void Recycle(Bullet item)
    {
        base.Recycle(item);
        item.gameObject.SetActive(false);
        item.transform.SetParent(m_storage);
        item.transform.localPosition = Vector3.zero;
    }
}