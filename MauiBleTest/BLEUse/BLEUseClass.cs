using MauiBleTest.ble;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MauiBleTest.ble.blemain;

namespace MauiBleTest.BLEUse
{
	public class BLEUseClass : BLEControlBase
	{

		private TBLEConnectInfo _bleInfo;

		public BLEUseClass() : base()
		{

			_bleInfo.TerminalName = "BathBuzzer";

			//_bleInfo.UUService = "4fafc201-1fb5-459e-8fcc-c5c9c331914b";
			//_bleInfo.UUCharacteristic = "beb5483e-36e1-4688-b7f5-ea07361b26a8";

			_bleInfo.UUService = "d1583dcd-73b2-4455-8efa-3632ae8ecfcb";
			_bleInfo.UUCharacteristic = "6538a81a-0601-4c85-a75d-812ce857b14b";

			//BLECtrlLED = new BLEControlESPLED(_bleInfo);
			SetBleConnectInfo(_bleInfo);
			StartConnect();
		}
	}
}
