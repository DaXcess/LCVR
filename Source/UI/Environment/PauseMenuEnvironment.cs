namespace LCVR.UI.Environment;

public class PauseMenuEnvironment : MainMenuEnvironment
{
    private MenuCanvas menuCanvas;
    
    private new void Awake()
    {
        base.Awake();

        menuCanvas = GetComponentInChildren<MenuCanvas>();
        
        settingsManager.DisableCategory("interaction");
        settingsManager.DisableCategory("car");
    }
    
    public void EnterEnvironment()
    {
        keyboard.Close();
        menuCanvas.ResetPosition(true);
        
        mainCamera.enabled = true;
    }

    public void ExitEnvironment()
    {
        mainCamera.enabled = false;
    }
}
