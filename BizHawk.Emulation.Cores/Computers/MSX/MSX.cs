﻿using System;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	[Core(
		"MSXHawk",
		"",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class MSX : IEmulator, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ISettable<MSX.MSXSettings, MSX.MSXSyncSettings>
	{
		[CoreConstructor("MSX")]
		public MSX(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			Settings = (MSXSettings)settings ?? new MSXSettings();
			SyncSettings = (MSXSyncSettings)syncSettings ?? new MSXSyncSettings();
			CoreComm = comm;

			RomData = rom;

			if (RomData.Length % BankSize != 0)
			{
				Array.Resize(ref RomData, ((RomData.Length / BankSize) + 1) * BankSize);
			}

			MSX_Pntr = LibMSX.MSX_create();
			LibMSX.MSX_load(MSX_Pntr, RomData, (uint)RomData.Length, 0);

			blip_L.SetRates(3579545, 44100);
			blip_R.SetRates(3579545, 44100);

			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(this);

			SetupMemoryDomains();

			InputCallbacks = new InputCallbackSystem();

			int new_header_size = LibMSX.MSX_getheaderlength(MSX_Pntr);
			StringBuilder new_header = new StringBuilder(new_header_size);
			LibMSX.MSX_getheader(MSX_Pntr, new_header);

			Tracer = new TraceBuffer { Header = new_header.ToString() };

			var serviceProvider = ServiceProvider as BasicServiceProvider;
			serviceProvider.Register<ITraceable>(Tracer);
		}

		public void HardReset()
		{

		}

		IntPtr MSX_Pntr { get; set; } = IntPtr.Zero;

		// Constants
		private const int BankSize = 16384;

		// ROM
		public byte[] RomData;

		// Machine resources
		private IController _controller = NullController.Instance;

		private int _frame = 0;

		public DisplayType Region => DisplayType.NTSC;

		#region Trace Logger
		private ITraceable Tracer;

		private LibMSX.TraceCallback tracecb;

		private void MakeTrace(int t)
		{


			StringBuilder new_d = new StringBuilder(500);
			StringBuilder new_r = new StringBuilder(500);

			LibMSX.MSX_getdisassembly(MSX_Pntr, new_d, t);
			LibMSX.MSX_getregisterstate(MSX_Pntr, new_r, t);

			Tracer.Put(new TraceInfo
			{
				Disassembly = new_d.ToString().PadRight(36),
				RegisterInfo = new_r.ToString()
			});
		}

		#endregion

		private MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { "System Bus" });
		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;
	}
}
