using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class Classic : BaseUIForm 
{
    [SerializeField] GameObject UIControl;
    EventSystem eventSystem;
    private void Awake()
    {
        eventSystem = Services.Service.Get<EventSystem>();
        eventSystem.AddListener(EEvent.OnLoadClassic, EnterGame);
    }
    private void EnterGame()
    {
        if (transform.Find("UIControl") == null)
        {
            GameObject now = Instantiate(UIControl, transform);  
        }
    }
}
   