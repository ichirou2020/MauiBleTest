namespace MauiBleTest;

public partial class App : Application
{
	/// <summary>BLEをコントロールするクラス</summary>
	static public BLEUse.BLEUseClass _bleUse;
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}
}
