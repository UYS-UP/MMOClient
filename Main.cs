using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using MessagePack;
using UnityEngine;
using Random = System.Random;

public class Main : MonoBehaviour
{

    
    private void Start()
    {
        GameClient.Instance.Connect().Forget();
        // UIService.Instance.ShowView<LogView>(layer: UILayer.System, onBegin: CustomLog.Initialize);
        UIService.Instance.ShowView<LoginView>(layer: UILayer.Normal);

    }

    private void Update()
    {
        
    }
    

}
