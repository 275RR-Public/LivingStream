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
    public VisualElement innerPlay;
    public Label playLabel;
    
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
        innerPlay = ui.Q<VisualElement>("InnerPlayContainer");

        playLabel = ui.Q<Label>("PlayTitleLbl");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnClickPlayBtn()
    {
        tracker.ToggleInClassList("hide");
        sim.ToggleInClassList("hide");
        exitBtn.ToggleInClassList("hide");
        
        play.ToggleInClassList("fullscreen");
        play.ToggleInClassList("containPlay");
        
        if(play.ClassListContains("fullscreen"))
        {
            playBtn.text = "Stop";
            playBtn.style.marginTop = 80;
            playBtn.style.width = Length.Percent(20);

            innerPlay.style.backgroundColor = new Color(0.1f,0.1f,0.2f,.2f);
            innerPlay.style.borderLeftWidth = 1;
            innerPlay.style.borderRightWidth = 1;

            playLabel.style.fontSize = 100;
        }
        else
        {
            playBtn.text = "Play";
            playBtn.style.marginTop = 40;
            playBtn.style.width = Length.Percent(60);

            innerPlay.style.backgroundColor = new Color(0,0,0,0);
            innerPlay.style.borderLeftWidth = 0;
            innerPlay.style.borderRightWidth = 0;

            playLabel.style.fontSize = 60;
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
