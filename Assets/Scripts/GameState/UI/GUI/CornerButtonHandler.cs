using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CornerButtonHandler : MonoBehaviour {
    public Button SlowestSpeedButton;
    public Button SlowSpeedButton;
    public Button NormalSpeedButton;
    public Button FastSpeedButton;
    public Button FastestSpeedButton;
    public Button DeleteStructureButton;
    public Button OptionButton;
    public Button PauseButton;
    public Button QuickSaveButton;
    public Button QuickLoadButton;
    Button CurrentSpeedButton;

    // Use this for initialization
    void Start () {
        WorldController.Instance.RegisterSpeedChange(OnSpeedChange);
        OnSpeedChange(WorldController.Instance.CurrentSpeed,1);
        PauseButton.onClick.AddListener(() => WorldController.Instance.ChangeGameSpeed(GameSpeed.Paused));
        SlowestSpeedButton.onClick.AddListener(() => WorldController.Instance.ChangeGameSpeed(GameSpeed.Slowest));
        SlowSpeedButton.onClick.AddListener(() => WorldController.Instance.ChangeGameSpeed(GameSpeed.Slow));
        NormalSpeedButton.onClick.AddListener(() => WorldController.Instance.ChangeGameSpeed(GameSpeed.Normal));
        FastSpeedButton.onClick.AddListener(() => WorldController.Instance.ChangeGameSpeed(GameSpeed.Fast));
        FastestSpeedButton.onClick.AddListener(() => WorldController.Instance.ChangeGameSpeed(GameSpeed.Fastest));
        OptionButton.onClick.AddListener(() => UIController.Instance.TogglePauseMenu());
        DeleteStructureButton.onClick.AddListener(() => BuildController.Instance.DestroyToolSelect());
        QuickLoadButton.onClick.AddListener(() => SaveController.Instance.QuickLoad());
        QuickSaveButton.onClick.AddListener(() => SaveController.Instance.QuickSave());
    }

    private void OnSpeedChange(GameSpeed gameSpeed, float speed) {
        if(CurrentSpeedButton!= null)
            CurrentSpeedButton.interactable = true;
        switch (gameSpeed) {
            case GameSpeed.Paused:
                PauseButton.interactable = false;
                CurrentSpeedButton = PauseButton;
                break;
            case GameSpeed.StopMotion:
            case GameSpeed.Slowest:
                SlowestSpeedButton.interactable = false;
                CurrentSpeedButton = SlowestSpeedButton;
                break;
            case GameSpeed.Slow:
                SlowSpeedButton.interactable = false;
                CurrentSpeedButton = SlowSpeedButton;
                break;
            case GameSpeed.Normal:
                NormalSpeedButton.interactable = false;
                CurrentSpeedButton = NormalSpeedButton;
                break;
            case GameSpeed.Fast:
                FastSpeedButton.interactable = false;
                CurrentSpeedButton = FastSpeedButton;
                break;
            case GameSpeed.LudicrousSpeed:
            case GameSpeed.Fastest:
                FastestSpeedButton.interactable = false;
                CurrentSpeedButton = FastestSpeedButton;
                break;
        }
    }
}
