using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    public VisualElement ui;
    public Button playBtn;
    public Button exitBtn;
    public VisualElement tracker;
    public VisualElement sim;
    public VisualElement play;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;

        playBtn = ui.Q<Button>("PlayBtn");
        playBtn.clicked += OnClickPlayBtn;
        exitBtn = ui.Q<Button>("ExitBtn");
        exitBtn.clicked += OnClickExitBtn;

        tracker = ui.Q<VisualElement>("TrackContainer");
        sim = ui.Q<VisualElement>("SimContainer");
        play = ui.Q<VisualElement>("PlayContainer");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnClickPlayBtn()
    {
        tracker.ToggleInClassList("hide");
        sim.ToggleInClassList("hide");
        
        play.ToggleInClassList("fullscreen");
        play.ToggleInClassList("containPlay");

        if(play.ClassListContains("fullscreen"))
        {
            playBtn.text = "Stop";
        }
        else
        {
            playBtn.text = "Play";
        }
    }

    private void OnClickExitBtn()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}
