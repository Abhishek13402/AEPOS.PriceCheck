namespace AEPOS.PriceCheck.PopUpPages;

public partial class LoadingPopup : ContentView
{
    public LoadingPopup()
    {
        InitializeComponent();
    }
    public void Show()
    {
        popup.IsOpen = true;
    }

    public void Hide()
    {
        popup.IsOpen = false;
    }
}