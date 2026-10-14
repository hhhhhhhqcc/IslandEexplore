using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
      // ===== 单例模式：整个游戏只存在一个 GameManager =====
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // 如果已经有实例了，销毁重复的
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 切换场景时不销毁这个对象
        DontDestroyOnLoad(gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("浮岛开拓者 - 游戏启动成功！");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
