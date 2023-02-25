using static MauiBleTest.ble.blemain.BLEControlBase;
using System.Timers;
using MauiBleTest.BLEUse;

namespace MauiBleTest;

public partial class MainPage : ContentPage
{
	private System.Timers.Timer _lastRecv = new System.Timers.Timer();

	private int _timCount = 0;
	private const int ABORT_TIMER = 10;
	private const UInt32 POLLING_DATA = 1;  //!<	ポーリングデータを受信
	private const UInt32 SWITCH_ON = 2;     //!<	スイッチオン

	BLEUseClass _bleUses;

	public MainPage()
	{
		InitializeComponent();

		if (App._bleUse == null)
		{
			App._bleUse = new BLEUse.BLEUseClass();
		}

		_bleUses = App._bleUse;

		_bleUses.OnRecvData += _bleUse_OnRecvData;
		_bleUses.OnConnect += BleUses_OnConnect;
		_bleUses.OnDisConnect += BleUses_OnDisConnect;

		_lastRecv.Interval = 1000;
		_lastRecv.Elapsed += _lastRecv_Elapsed;

		StatConn.Text = "接続前";
	}


	/// <summary>
	/// 受信電文のタイムアウト判定
	/// </summary>
	/// <param name="sender">送信元</param>
	/// <param name="e">イベント</param>
	private void _lastRecv_Elapsed(object sender, ElapsedEventArgs e)
	{
		if (_timCount > ABORT_TIMER)
		{
			_lastRecv.Stop();
			_bleUses.DisConnect();
			_bleUses.StartConnect();
			_timCount = 0;
			Device.BeginInvokeOnMainThread(() =>
			StatRcv.Text = "タイムアウト!");
		}
		else
		{
			Device.BeginInvokeOnMainThread(() =>
			StatRcv.Text = "正常");
			_timCount++;
		}
	}

	private void BleUses_OnDisConnect(object sender, EventArgs e)
	{
		Device.BeginInvokeOnMainThread(() =>
		StatConn.Text = "切断された");
	}

	private void BleUses_OnConnect(object sender, EventArgs e)
	{
		_timCount = 0;
		_lastRecv.Start();
		Device.BeginInvokeOnMainThread(() =>
		StatConn.Text = "接続された");
	}

	private void _bleUse_OnRecvData(object sender, EventArgsRecvBLEData e)
	{
		byte[] rcv = e.bdata;
		if (rcv.Length == 4)
		{
			string rsltString;
			_timCount = 0;
			UInt32 rcvdata = BitConverter.ToUInt32(rcv);
			switch (rcvdata)
			{
				case POLLING_DATA:
					rsltString = "まだ";
					break;
				case SWITCH_ON:
					rsltString = "いっぱい";
					break;
				default:
					rsltString = "不正データ受信";
					break;

			}

			Device.BeginInvokeOnMainThread(() => StatBath.Text = rsltString);
		}
	}
	private void OnCounterClicked(object sender, EventArgs e)
	{
	
	}
}

