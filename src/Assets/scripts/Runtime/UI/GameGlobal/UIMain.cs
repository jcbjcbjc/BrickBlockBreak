using Managers;
using NetWork;
using Services;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : BaseUIForm
{
	
	public Button Single;
	public Button Multiple;
	public Button Quartic;
	public Button Hexagon;
	public Button Classic;
	
	public Button Return;
	public bool _music=true;

	void Start()
	{
		Single.onClick.AddListener(() => {
			OnStartSingle();
		});
		Multiple.onClick.AddListener(() => {
			OnStartMultiple();
		});
		Quartic.onClick.AddListener(() => {
			OnStartQuartic();
		});
		Hexagon.onClick.AddListener(() => {
			OnStartQuartic();
		});
		Classic.onClick.AddListener(() => {
			OnStartClassic();
		});
		Return.onClick.AddListener(() => 
		{
			_close_game();
		});
	}
	void OnStartSingle() {

	}
	void OnStartMultiple()
	{
        ServiceLocator.Get<MatchService>().SendStartMatch();
		UIManager.GetInstance().CloseUIForms("UIMain");
    }

	void OnStartQuartic()
	{

	}

	void OnStartHexagon()
	{

	}

	void OnStartClassic()
	{

	}
	void OnStartTutor()
	{
	}
	public void _close_game()
	{
	}
}
