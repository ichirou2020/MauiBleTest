using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

namespace MauiBleTest.ble
{
	public class blemain
	{// ここにはBLEControlクラスで使用するがクラス内に閉じ込めないで全体の定数や型で使えるような構造体、列挙型を定義する
		#region enum定義色々
		/// <summary>接続状態</summary>
		public enum ConnectStat : int
		{
			/// <summary>切断</summary>
			DISCONNECT,
			/// <summary>接続中</summary>
			CONNECTING,
			/// <summary>接続完了</summary>
			CONNECT
		}

		/// <summary>関数実行結果</summary>
		public enum BLERESULT : int
		{
			/// <summary>OK</summary>
			OK,
			/// <summary>NG</summary>
			NG,
			/// <summary>BUSY</summary>
			BUSY,
		}
		#endregion

		#region 構造体
		public struct TBLEConnectInfo
		{
			/// <summary>端末名称</summary>
			public string TerminalName;
			/// <summary>サービスUUID</summary>
			public string UUService;
			/// <summary>Characteristic UUID</summary>
			public string UUCharacteristic;
		}
		#endregion


		/// <summary>
		/// BLEをコントロールするクラス
		/// </summary>
		/// <remarks>plugin.BLEを使いやすいようにラッピングする。Instatnceをシングルトンで定義することにより、
		/// App.xaml.cs等で実態を定義することなく、BLEControlクラスを使用することが出来る</remarks>
		public class BLEControlBase
		{
			///// <summary>最大接続個数</summary>
			//public const int BLE_CONNECTCNT_MAX = 8;



			#region シングルトン定義
			///// <summary>BLEコントロールのインスタンスを保持する</summary>
			//private static BLEControlBase _instance = new BLEControlBase();
			///// <summary>BLEControlをシングルトンで使えるようにする</summary>
			//public static BLEControlBase Instance { get => _instance; }
			#endregion

			#region 内部変数 
			/// <summary>接続完了</summary><remarks>内部で管理する接続変数→状態が変化すると</remarks>
			private ConnectStat _IsConnect = ConnectStat.DISCONNECT;
			//		/// <summary>受信許可</summary><remarks>true=電文受信可能 / false=電文受信不許可</remarks>
			//		private bool _IsRecieve = false;

			/// <summary>BLEオブジェクト</summary>
			IBluetoothLE _BluetoothLe;
			/// <summary>BLEアダプタ</summary>
			IAdapter _adapter;
			/// <summary>BLEサービス</summary>
			private IService _service = null;
			/// <summary>サーバからの電文受信用のオブジェクト</summary>
			ICharacteristic _characteristicread;
			/// <summary>イベントを管理するクラス</summary><remarks>項目が追加または削除されたとき、あるいはリスト全体が更新されたときに通知を行う動的なデータ コレクションを表します</remarks>
			private ObservableCollection<IDevice> _deviceList = new ObservableCollection<IDevice>();
			/// <summary>BLEデバイス</summary>
			IDevice _device = null;
			/// <summary>再接続する際の、scan開始のきっかけ用タイマー</summary>
			System.Timers.Timer _restartTimer = new System.Timers.Timer();
			/// <summary>再接続する際の、scan開始の時間</summary>
			const int TIM_RESTART = 5000;

			///// <summary>サービスUUID</summary>
			//private string _UUService = string.Empty;
			///// <summary>Characteristic UUID</summary>
			//private string _UUCharacteristic = string.Empty;

			private TBLEConnectInfo _bleconInfo;
			#endregion

			/// <summary>
			/// 受信データ
			/// </summary>
			public class EventArgsRecvBLEData : EventArgs
			{
				/// <summary>ESP32から受信した電文</summary>
				public byte[] bdata;
			}

			#region イベント関連デリゲート
			/// <summary>データ受信イベント</summary>
			/// <param name="sender">送信元</param>
			/// <param name="e">受信データ</param>
			//public delegate void OnRecvDataDelegate(object sender, EventArgsRecvBLEData e);
			#endregion

			#region イベント関連

			/// <summary>データ受信イベント</summary>
			public event EventHandler<EventArgsRecvBLEData> OnRecvData;

			/// <summary>
			/// 接続イベント
			/// </summary>
			public event EventHandler OnConnect;
			/// <summary>
			/// 切断イベント
			/// </summary>
			public event EventHandler OnDisConnect;
			/// <summary>
			/// データ受信イベント
			/// </summary>
			//public event EventHandler OnRecvData(int a, int b);
			#endregion



			///// <summary>コンストラクタ</summary>
			///// <remarks>アプリ終了まで保持するのでデストラクタは不要</remarks>
			//public BLEControlBase(TBLEConnectInfo bleConInfo)
			//{
			//}

			/// <summary>
			/// 接続情報設定
			/// </summary>
			/// <param name="bleConInf"></param>
			public void SetBleConnectInfo(TBLEConnectInfo bleConInf)
			{
				_bleconInfo = bleConInf;                               //接続情報を保存する
																	   //デバイス名、UUServiceID,UUCharacteristic

			}
			/// <summary>
			/// コンストラクタ
			/// </summary>
			public BLEControlBase()
			{
				_IsConnect = ConnectStat.DISCONNECT;

				_BluetoothLe = Plugin.BLE.CrossBluetoothLE.Current;     //BLEの検索する
				_adapter = _BluetoothLe.Adapter;                        //アダプターの準備
				_adapter.ScanTimeout = 10000;                            //検索タイムアウト
				_adapter.DeviceDiscovered += BLE_DeviceDiscoverd;       //検索時の発見イベント
				_adapter.ScanTimeoutElapsed += BLE_ScanTimeoutElapsed;  //検索タイムアウトイベント
				_adapter.DeviceConnectionLost += _adapter_DeviceConnectionLost; //デバイスロスト
				_adapter.DeviceConnected += _adapter_DeviceConnected;           //接続イベント
				_adapter.DeviceDisconnected += _adapter_DeviceDisconnected;     //切断イベント
																				//再接続
				_restartTimer.Elapsed += _restartTimer_Elapsed;         //再接続用タイマー
				_restartTimer.Interval = TIM_RESTART;                   //時間
			}

			private void _restartTimer_Elapsed(object sender, ElapsedEventArgs e)
			{
				_restartTimer.Stop();
				Scan();
			}

			/// <summary>
			/// アダプターによる切断検知
			/// </summary>
			/// <param name="sender">送信元</param>
			/// <param name="e">イベント内容</param>
			private void _adapter_DeviceDisconnected(object sender, DeviceEventArgs e)
			{
				EventArgs ev = new EventArgs();
				string str = e.Device.Name;

				Debug.WriteLine("切断:" + str);
				if (OnDisConnect != null)
				{
					OnDisConnect(this, ev);
				}
			}

			/// <summary>
			/// アダプターによる接続検知
			/// </summary>
			/// <param name="sender">送信元</param>
			/// <param name="e">イベント内容</param>
			private void _adapter_DeviceConnected(object sender, DeviceEventArgs e)
			{
				EventArgs ev = new EventArgs();
				string str = e.Device.Name;

				Debug.WriteLine("接続:" + str);
				if (OnConnect != null)
				{
					OnConnect(this, ev);
				}
			}

			/// <summary>
			/// アダプターによるデバイスロスト検知
			/// </summary>
			/// <param name="sender">送信元</param>
			/// <param name="e">イベント内容</param>
			private void _adapter_DeviceConnectionLost(object sender, DeviceErrorEventArgs e)
			{
				string devicename = e.Device.Name;
				EventArgs ev = new EventArgs();
				DisConnect();
				Debug.WriteLine("デバイスロスト:" + devicename);
				if (OnDisConnect != null)
				{
					OnDisConnect(this, ev);
				}
				_restartTimer.Start();
			}

			/// <summary>
			/// 検索タイムアウトイベント
			/// </summary>
			/// <param name="sender">送信元</param>
			/// <param name="e">イベント内容</param>
			private void BLE_ScanTimeoutElapsed(object sender, EventArgs e)
			{
				//TODO:タイムアウト時はやり直す
				//throw new NotImplementedException();
				DisConnect();
				Scan();
			}

			/// <summary>
			/// 検索発見イベント
			/// </summary>
			/// <param name="sender">送信元</param>
			/// <param name="e">イベント内容</param>
			private void BLE_DeviceDiscoverd(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
			{
				IDevice device = e.Device;

				if (device.Name == _bleconInfo.TerminalName)    //指定したデバイス名ならば
				{
					Debug.WriteLine("指定したデバイスを発見");
					_device = device;
					//TODO:	_adapter.StopScanningForDevicesAsync();
					Connect(_device);                           //接続
				}
				//throw new NotImplementedException();
			}

			/// <summary>
			/// 接続状態を取得する。BLEを操作する際や、釦の表示非表示の切り替えに使用する
			/// </summary>
			public ConnectStat GetConnectStat
			{
				get => _IsConnect;
			}

			/// <summary>BLE接続開始</summary>
			/// <remarks>スプラッシュ画面かメインページからBLEの接続を指示する→一度開始後は切断しても自動で再接続を試みる</remarks>
			/// <returns>関数の実行結果</returns>
			public BLERESULT StartConnect()
			{
				Scan();

				return BLERESULT.OK;
			}

			/// <summary>
			/// 切断
			/// </summary>
			async public void DisConnect()
			{
				_IsConnect = ConnectStat.DISCONNECT;

				if (_device == null)
				{
					return;
				}
				if (_adapter == null)
				{
					return;
				}
				if (_characteristicread != null)
				{
					_characteristicread.ValueUpdated -= Characteristicread_ValueUpdated;    //受信イベント削除
				}
				await _adapter.DisconnectDeviceAsync(_device);                          //デバイスを切断状態とする
			}

			/// <summary>
			/// Peripheralを検索開始
			/// </summary>
			async private void Scan()
			{
				if (_BluetoothLe.State == BluetoothState.Off)
				{
					_deviceList.Clear();
					//await DisplayAlert("BLE", "off", "OK");
					return;
				}
				if (_adapter.IsScanning)
				{
					return;
				}
				_deviceList.Clear();
				await _adapter.StartScanningForDevicesAsync();
			}

			/// <summary>
			/// 接続処理
			/// </summary>
			/// <param name="device">接続処理するデバイス</param>
			async private void Connect(IDevice device)
			{
				try
				{
					await _adapter.ConnectToDeviceAsync(device);
					//	var services = await device.GetServicesAsync();	//発見したサービスを片っ端から捕まえる
					//	var service = await device.GetServiceAsync(Guid.Parse("00001101-0000-1000-8000-00805f9b34fb")); //指定したサービスを捕まえる

					//_device = device;

					var services = await device.GetServicesAsync(); //発見したサービスを片っ端から捕まえる
					Guid id1 = services[0].Id;
					Guid id2 = services[1].Id;
					Guid id3 = services[2].Id;

					var service = await device.GetServiceAsync(Guid.Parse(_bleconInfo.UUService)); //指定したサービスを捕まえる
					if (service != null)
					{
						Debug.WriteLine("サービス抽出開始");
						await _adapter.StopScanningForDevicesAsync();   //見つけたので検索終了

						_service = service;
						//_IsRecieve = true;
						_IsConnect = ConnectStat.CONNECT;       //指定したサービスIDを捕まえて初めて接続扱いにする
																//Read();
						startRecieve();

					}
				}
				catch (DeviceConnectionException e)
				{
					// ... could not connect to device
				}
			}


			/// <summary>
			/// 
			/// </summary>
			private void endRecieve()
			{
				//_IsRecieve = false;
			}

			//private System.Timers.Timer _tim;

			//private bool _rec = true;
			/// <summary>
			/// 受信開始
			/// </summary>
			/// <remarks>受信開始するために、受信イベントをイベントハンドラに登録する</remarks>
			async private void startRecieve()
			{
				_characteristicread = await _service.GetCharacteristicAsync(Guid.Parse(_bleconInfo.UUCharacteristic));
				if (_characteristicread.CanRead == true)
				{
					Debug.WriteLine("受信可能になった");
					_characteristicread.ValueUpdated += Characteristicread_ValueUpdated;    //ValueUpdateイベント登録
					await _characteristicread.StartUpdatesAsync();
					//characteristicread.ValueUpdated += (o, args) =>
					//{
					//	var bytes = args.Characteristic.Value;
					//};
					//await characteristicread.StartUpdatesAsync();
				}
				return;

				//while (true)
				//{
				//	if(_rec == true)
				//	{
				//		_rec = false;
				//		Read();
				//	}
				//	Task.Delay(100).Wait();
				//}
				//_tim = new System.Timers.Timer();
				//_tim.Interval = 100;
				//_tim.Elapsed += _tim_Elapsed;
				//_tim.Start();

			}

			//private void _tim_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
			//{
			//	if (_rec == true)
			//	{
			//		_rec = false;
			//		Read();
			//	}
			//}

			/// <summary>
			/// データ受信イベント
			/// </summary>
			/// <param name="sender">送信元</param>
			/// <param name="e">受信データ</param>
			/// <remarks>送信側のペリフェラルではNotifyを通知許可すること</remarks>
			private void Characteristicread_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
			{
				byte[] b = e.Characteristic.Value;  //受信した電文
				if (b.Length != 0)
				{
					EventArgsRecvBLEData eBle = new EventArgsRecvBLEData(); //イベント情報を作成し
					eBle.bdata = b;

					Debug.Write("受信データ:");
					int dataLen = b.Length;
					if (dataLen >= 16)
					{
						dataLen = 16;
					}
					for (int i = 0; i < dataLen; i++)
					{
						Debug.Write(b[i].ToString("x00") + " ");
					}
					Debug.WriteLine("");

					OnRecvData(this, eBle);                         //上位にイベント通知
				}
			}

			//別スレッドを作って無限ループを作る必要あり
			/// <summary>
			/// 電文受信(未使用)
			/// </summary>
			async private void Read()
			{
				if (_service == null)
				{
					//_IsRecieve = false;
					return;
				}
				var characteristic = await _service.GetCharacteristicAsync(Guid.Parse(_bleconInfo.UUCharacteristic));
				//var characteristic = await service.GetCharacteristicAsync(Guid.Parse("d8de624e-140f-4a22-8594-e2216b84a5f2"));
				//var characteristics = await service.GetCharacteristicsAsync();
				var bytes = await characteristic.ReadAsync();
				if (bytes.Length != 0)
				{
					EventArgsRecvBLEData e = new EventArgsRecvBLEData();
					e.bdata = bytes;
					OnRecvData(this, e);
					Debug.WriteLine(bytes[0].ToString() + bytes[1].ToString() + bytes[5].ToString() + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
				}
				//_rec = true;
			}

			/// <summary>排他ロック用オブジェクト</summary>
			private SemaphoreSlim lockObj = new SemaphoreSlim(1, 1);

			/// <summary>
			/// データ送信
			/// </summary>
			/// <param name="bdata">接続先にデータを送信する</param>
			async public void Write(byte[] bdata)
			{
				try
				{
					await lockObj.WaitAsync();  //排他ロック開始

					if (_IsConnect != ConnectStat.CONNECT)
					{
						//切断状態だったら無視
						Debug.WriteLine("切断中の送信");
						return;
					}
					if (_service == null)   //サービスが確保されていなかったら
					{
						Debug.WriteLine("サービス接続前");
						return;             //終了
					}


					//var characteristic = await service.GetCharacteristicAsync(Guid.Parse("d8de624e-140f-4a22-8594-e2216b84a5f2"));
					var characteristic = await _service.GetCharacteristicAsync(Guid.Parse(_bleconInfo.UUCharacteristic));
					//var a = characteristic.GetDescriptorAsync(Guid.Parse(_bleconInfo.UUCharacteristic));
					if (characteristic.CanWrite == true)
					{
						bool bo = await characteristic.WriteAsync(bdata);
						if (bo == false)
						{
							DisConnect();//送信失敗したら切断
						}
					}
				}
				catch (Exception e)
				{
					//送信失敗したら切断
					DisConnect();
					//throw new Exception(e.Message);
				}
				finally
				{
					lockObj.Release();          //排他ロック終了
				}
			}

			//async public void Write2(byte[] data)
			//{
			//	var ch = await _service.GetCharacteristicAsync(Guid.Parse(_bleconInfo.UUCharacteristic));
			//	bool rslt = _write(ch, data).Result;
			//}

			//private async Task<bool> _write(ICharacteristic aChx, byte[] data)
			//{
			//	bool success = false; // Default to fail unless explicit success
			//	int timeout = 20; // Very generous! Should complete in milliseconds
			//	var writetask = aChx.WriteAsync(data);
			//	if (await Task.WhenAny(writetask, Task.Delay(timeout)) == writetask)
			//	{
			//		// task completed within timeout... how'd we do?
			//		if (TaskStatus.RanToCompletion == writetask.Status)
			//		{
			//			success = writetask.Result; // This is the result you're after!
			//		}
			//		else
			//		{
			//			// This is a failure not associated with the timeout
			//			// Basically, anything other than RanToCompletion is a failure.
			//			// E.g. TaskStatus.Faulted means that WriteAsync overtly failed
			//		}
			//	}
			//	else
			//	{
			//		// WriteAsync() took too long
			//	}
			//	return success;
			//}

		}
	}
}
