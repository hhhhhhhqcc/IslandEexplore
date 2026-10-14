using UnityEngine;

[CreateAssetMenu(fileName = "ResourceDatabase", menuName = "浮岛开拓者/资源数据库")]
public class ResourceDatabase : ScriptableObject
{
    public ResourceData[] allResources;

    /// <summary>
    /// 根据类型和权重随机选一个资源
    /// </summary>
    public ResourceData GetRandomForIsland(string islandTypeTag)
    {
        float total = 0f;
        foreach (var r in allResources)
        {
            foreach (var tag in r.spawnOnIslands)
                if (tag == islandTypeTag)
                    total += r.spawnWeight;
        }
        if (total <= 0) return null;

        float roll = Random.Range(0f, total);
        foreach (var r in allResources)
        {
            foreach (var tag in r.spawnOnIslands)
            {
                if (tag == islandTypeTag)
                {
                    roll -= r.spawnWeight;
                    if (roll <= 0) return r;
                }
            }
        }
        return null;
    }

    public ResourceData GetByName(string name)
    {
        foreach (var r in allResources)
            if (r.resourceName == name) return r;
        return null;
    }
}