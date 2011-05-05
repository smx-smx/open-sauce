using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;   // debugger display


using YeloDebug.Exceptions;

using System.Xml;
//////////////////////
// rules /////////////
//////////////////////
// always throw an exception for connection drops, except when disconnecting
// getmem will crash if trying to read bad memory address, use isaddressvalid beforehand or specify a safemode to do that automatically
//
//
//
//
// assumes single threaded use, multiple threaded applications will NEED to have their own Xbox object per thread...


// todo: use scratch buffer in history address range instead of xbe hader...

// have reconnectAttempts, maxReconnectAttempts, and reconnectDelay
// when connected, reset attempts


// have something patch 64meg memory switch and do a warm reboot



// 0xD0000000
// 0xF0000000   Physical address space
// 0xFD000000   Nvidia GPU address space
// 0xFEF00000   NIC address space
// 0xFF000000   Flash address space

// dont assume xbdm is loaded at base 0xB0000000, xmt's is at 0xB0011000
// YeloDebug not compatable with Complex_4627Debug.bin bios
// it loads xbdm at 0xB0011000 instead of 0xB0000000 and my xbdm hacks use static addressing (might fix later on)

// todo: for remote execution, have a way to specify type of return value...
// for floats ill need to store st(0) into eax, 
// doubles will need to be stored somewhere and passed a pointer to it in eax

namespace YeloDebug
{
	#region Enums

    public enum Surface
    {
        BackBuffer = 0,
        Depth_Stencil = 1,
        FrontBuffer = 2,
        ThirdBuffer = 3,
        Texture0 = 4,
        Texture1 = 5,
        Texture2 = 6,
        Texture3 = 7,
        RenderTarget = 8,
        Overlay = 9
    }

    public enum ATACommandType
    {
        Read = 0,
        Write = 1,
        NonData = 2
    };

    public enum ATADevice
    {
        HardDrive = 0,
        DVDROM = 1
    }

    /// <summary>
    /// Ethernet link status flags.
    /// </summary>
    [Flags]
    public enum LinkStatus
    {
        /// <summary>
        /// Ethernet cable is connected and active.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Ethernet link is set to 100 Mbps.
        /// </summary>
        Speed100Mbps = 2,

        /// <summary>
        /// Ethernet link is set to 10 Mbps.
        /// </summary>
        Speed10Mbps = 4,

        /// <summary>
        /// Ethernet link is in full duplex mode.
        /// </summary>
        FullDuplex = 8,

        /// <summary>
        /// Ethernet link is in half duplex mode.
        /// </summary>
        HalfDuplex = 16
    };
    
    public enum AVPack
    {
        SideCart,
        HDTV,
        VGA,
        RFU,
        SVideo,
        Undefined,
        StandardRGB,
        Missing,
        Unknown
    };


    public enum StopOnFlags
    {
        /// <summary>
        /// Suspend title execution when a thread is created.
        /// </summary>
        CreateThread,

        /// <summary>
        /// Suspend title execution when a first-chance exception occurs.
        /// </summary>
        FCE,

        /// <summary>
        /// Suspend title execution when a string is sent to debug out.
        /// </summary>
        DebugStr
    };

    [Flags]
    public enum BreakpointType
    {
        /// <summary>
        /// Clear this data breakpoint.
        /// </summary>
        None = 1,

        /// <summary>
        /// Break if the address is written to.
        /// </summary>
        Write = 2,

        /// <summary>
        /// Break if the address is read from or written to.
        /// </summary>
        ReadWrite = 4,

        /// <summary>
        /// Break if the address is executed.
        /// </summary>
        Execute = 8
    };

    public enum NotificationType
    {
        None,
        Breakpoint,
        DebugString,
        Execution,
        SingleStep,
        ModuleLoad,
        ModuleUnload,
        CreateThread,
        DestroyThread,
        Exception,
        ClockInt,
        Assert,
        DataBreak,
        RIP,
        ThreadSwitch,
        SectionLoad,
        SectionUnload,
        Fiber,
        NotifyMax
    };

    public enum XboxVersion
    {
        Devkit,
        DebugKit,   // Green
        V1_0,
        V1_1,
        V1_2,
        V1_3,
        V1_4,
        V1_6,
        Unknown
    };


    /// <summary>
    /// Xbox boot type.
    /// </summary>
	public enum BootFlag : uint
	{
		/// <summary>
		/// When the reboot is complete, the system software will wait 15 
		/// seconds before launching the default title. If you call DmGo 
		/// during this time, the system software will launch the title immediately.
		/// </summary>
		Wait,

		/// <summary>
		/// Perform a "warm" reboot of the console.
		/// </summary>
		Warm,

		/// <summary>
		/// Prevent debugging after reboot. The Xbox debug manager will not be 
		/// loaded on any subsequent warm reboot; a cold boot is required.
		/// </summary>
		NoDebug,

		/// <summary>
		/// When the reboot is complete, the system software will wait 
		/// (with no timeout) before launching the default title. Calling 
		/// DmGo while the system is waiting will launch the title.
		/// </summary>
		Stop,

		/// <summary>
		/// Complete shut-down of the system before rebooting.
		/// </summary>
		Cold,

		/// <summary>
		/// Warm reboot to active title.
		/// </summary>
		Current
	};

    /// <summary>
    /// Xbox video standard.
    /// </summary>
	public enum VideoStandard : byte
	{
		NTSCAmerica = 1,
		NTSCJapan = 2,
		PAL = 3
	};

    /// <summary>
    /// Xbox video flags.
    /// </summary>
	[Flags]
	public enum VideoFlags
	{
		Normal		= 0,
		Widescreen	= 0x1,
		HDTV_720p	= 0x2,
		HDTV_1080i	= 0x4,
		HDTV_480p	= 0x8,
		Letterbox	= 0x10,
		PAL_60Hz	= 0x40
	};

    /// <summary>
    /// Xbox memory allocation type.
    /// </summary>
	public enum AllocationType : byte
	{
		Debug,
		Virtual,
		Physical,
		System
	};

    /// <summary>
    /// Xbox memory flags.
    /// </summary>
	[Flags]
	public enum MEMORY_FLAGS
	{
		PAGE_VIDEO				= 0x0,
		PAGE_NOACCESS			= 0x1,
		PAGE_READONLY			= 0x2,
		PAGE_READWRITE			= 0x4,
		PAGE_WRITECOPY			= 0x8,
		PAGE_EXECUTE			= 0x10,
		PAGE_EXECUTE_READ		= 0x20,
		PAGE_EXECUTE_READWRITE	= 0x40,
		PAGE_EXECUTE_WRITECOPY	= 0x80,
		PAGE_GUARD				= 0x100,
		PAGE_NOCACHE			= 0x200,
		PAGE_WRITECOMBINE		= 0x400,
		MEM_COMMIT				= 0x1000,
		MEM_RESERVE				= 0x2000,
		MEM_DECOMMIT			= 0x4000,
		MEM_RELEASE				= 0x8000,
		MEM_FREE				= 0x10000,
		MEM_PRIVATE				= 0x20000,
		MEM_RESET				= 0x80000,
		MEM_TOP_DOWN			= 0x100000,
		MEM_NOZERO				= 0x800000
	};

	/// <summary>
	/// Represents one of the 4 possible Xbox LED states.
	/// </summary>
	public enum LEDState : byte
	{
		Off		= 0,
		Red		= 0x80,
		Green	= 8,
		Orange	= 0x88
	};

	/// <summary>
	/// Xbox gamepad buttons.
	/// </summary>
	[Flags]
	public enum Buttons : ushort
	{
		Up					    = 1 << 0,
		Down				    = 1 << 1,
		Left				    = 1 << 2,
		Right				    = 1 << 3,
		Start					= 1 << 4,
		Back					= 1 << 5,
		LeftThumb				= 1 << 6,
		RightThumb				= 1 << 7,
		LightGunOnScreen		= 1 << 13,
		LightGunFrameDoubler	= 1 << 14,
		LightGunLineDoubler	    = 1 << 15
	};

	/// <summary>
	/// Xbox analog gamepad buttons.
	/// </summary>
	[Flags]
	public enum AnalogButtons
	{
		A,
		B,
		X,
		Y,
		Black,
		White,
		LeftTrigger,
		RightTrigger
	};

    /// <summary>
    /// Xbox response type.
    /// </summary>
	public enum ResponseType
	{
		// Success
		SingleResponse		            = 200,  //OK
		Connected			            = 201,
		MultiResponse		            = 202,  //terminates with period
		BinaryResponse		            = 203,
		ReadyForBinary		            = 204,
		NowNotifySession	            = 205,  // notificaiton channel/ dedicated connection

		// Errors
		UndefinedError		            = 400,
        MaxConnectionsExceeded          = 401,
		FileNotFound		            = 402,
		NoSuchModule		            = 403,
		MemoryNotMapped		            = 404,  //setzerobytes or setmem failed
        NoSuchThread                    = 405,
		ClockNotSet                     = 406,  //linetoolong or clocknotset
		UnknownCommand		            = 407,
        NotStopped                      = 408,
        FileMustBeCopied                = 409,
		FileAlreadyExists	            = 410,
        DirectoryNotEmpty               = 411,
        BadFileName                     = 412,
        FileCannotBeCreated             = 413,
		AccessDenied		            = 414,
        NoRoomOnDevice                  = 415,
        NotDebuggable                   = 416,
        TypeInvalid                     = 417,
        DataNotAvailable                = 418,
		BoxIsNotLocked		            = 420,
		KeyExchangeRequired	            = 421,
        DedicatedConnectionRequired     = 422,
        InvalidArgument                 = 423,
        ProfileNotStarted               = 424,
        ProfileAlreadyStarted           = 425,
        D3DDebugCommandNotImplemented   = 480,
        D3DInvalidSurface               = 481,
        VxTaskPending                   = 496,
        VxTooManySessions               = 497,
	};

    /// <summary>
    /// Receive wait type.
    /// </summary>
	public enum WaitType
	{
		/// <summary>
		/// Does not wait.
		/// </summary>
		None,

		/// <summary>
		/// Waits for data to start being received.
		/// </summary>
		Partial,

		/// <summary>
		/// Waits for data to start and then stop being received.
		/// </summary>
		Full,

        /// <summary>
        /// Waits for data to stop being received.
        /// </summary>
        Idle
	};

	/// <summary>
	/// Items to include in a memdump.
	/// </summary>
	[Flags]
	public enum DumpFlags
	{
		/// <summary>
		/// Xbe code segment.
		/// </summary>
		Code,

        /// <summary>
        /// Xbe data segment.
        /// </summary>
        Data,

		/// <summary>
		/// System pages. (kernel, stacks, pools, etc...)
		/// </summary>
		System,

		/// <summary>
		/// Debugger pages.
		/// </summary>
		Debug
	};


    /// <summary>
    /// Devices on the System Management Controller
    /// </summary>
    public enum SMCDevices
    {
        SMBus = 0x20,   // slave addresses
        VideoEncoderConnexant = 0x8a,
        VideoEncoderFocus = 0xd4,
        VideoEncoderXcalibur = 0xe0,
        TempMonitor = 0x98,
        EEPROM = 0xA8
    };


    /// <summary>
    /// Command codes available for the SMBus.
    /// </summary>
    public enum SMBusCommand
    {
        FirmwareVersion = 0x1,  // smc firmware version
        Reset = 0x2,
        TrayState = 0x3,
        VideoMode = 0x4,
        FanOverride = 0x5,
        RequestFanSpeed = 0x6,
        LedOverride = 0x7,
        LedStates = 0x8,
        CpuTemperature = 0x9,
        AirTemperature = 0xA,
        AudioClamp = 0xB,
        DvdTrayOperation = 0xC,
        OsResume = 0xD,
        WriteErrorCode = 0xE,
        ReadErrorCode = 0xF,
        ReadFanSpeed = 0x10,
        InterruptReason = 0x11,
        WriteRamTestResults = 0x12,
        WriteRamType = 0x13,
        ReadRamTestResults = 0x14,
        ReadRamType = 0x15,
        LastRegisterWritten = 0x16,
        LastByteWritten = 0x17,
        SoftwareInterrupt = 0x18,
        OverrideResetOnTrayOpen = 0x19,
        OsReady = 0x1A,
        ScratchRegister = 0x1B,
        ChallengeValue0 = 0x1C,
        ChallengeValue1 = 0x1D,
        ChallengeValue2 = 0x1E,
        ChallengeValue3 = 0x1F,
        ChallengeResponse0 = 0x20,
        ChallengeResponse1 = 0x21
    };

    /// <summary>
    /// Flags for the SMBus reset command.
    /// </summary>
    public enum ResetCommand
    {
        Reset = 0x01,
        PowerCycle = 0x40,
        ShutDown = 0x80
    };



    /// <summary>
    /// Flags for the SMBus tray state command.
    /// </summary>
    [Flags]
    public enum TrayState : byte
    {
        /// <summary>
        /// Drive is busy.
        /// </summary>
        Busy = 0x1,

        /// <summary>
        /// Tray is open.
        /// </summary>
        Open = 0x10,

        /// <summary>
        /// Tray is ejecting empty.
        /// </summary>
        Ejecting = 0x20,

        /// <summary>
        /// Tray is opening empty.
        /// </summary>
        Opening = 0x30,

        /// <summary>
        /// Tray is closed with no media.
        /// </summary>
        NoMedia = 0x40,

        /// <summary>
        /// Tray is closing
        /// </summary>
        Closing = 0x50,

        /// <summary>
        /// Tray detected media to be present.
        /// </summary>
        MediaPresent = 0x60,

        /// <summary>
        /// Tray is resetting
        /// </summary>
        Reset = 0x70,

        /// <summary>
        /// No optical drive detected.
        /// </summary>
        None = 0x5

        //Loading = 0x1,           // loading rom
        //EjectingFull = 0x21,         // ejecting with rom in tray
        //EjectingEmpty = 0x31,       // ejecting empty tray
        //Open = 0x10,
        //Closing = 0x51,             // closing tray
        //ClosedAndEmpty = 0x40,      // closed with no rom
        //ClosedAndFull = 0x60      // closed with rom
    };



    /// <summary>
    /// Commands that can be sent to the video encoder.
    /// </summary>
    public enum VideoEncoderCommand // TVEncoderSMBusID
    {
        Detect = 0x00
        //Unknown = 0x5 // subcommand > 0 spits out random numbers, its fucking weird...
    };

    /// <summary>
    /// Values for the video encoder.
    /// </summary>
    public enum VideoEncoder
    {
        Unknown = 0,
        Connexant = 1,
        Focus = 2,
        Xcalibur = 3
    };

    /// <summary>
    /// Sub-commands for the PIC led command.
    /// </summary>
    public enum LEDSubCommand
    {
        Default = 0x00,
        Custom = 0x01
    };

    /// <summary>
    /// Sub-commands for the PIC eject command.
    /// </summary>
    public enum EjectSubCommand
    {
        Eject = 0x00,
        Load = 0x01
    };

    public enum FanModeSubCommand
    {
        Default = 0x0,
        Custom = 0x1
    };


    //Reason for interrupt
    public enum InterruptReason
    {
        PowerButton = 0x01,
        AvCableRemoved = 0x10,
        EjectButton = 0x20
    };

    /// <summary>
    /// Sub-commands for reset on eject PIC command.
    /// </summary>
    public enum ResetOnEjectSubCommand
    {
        Enable = 0x00,
        Disable = 0x01
    };

    /// <summary>
    /// Scratch register values.
    /// </summary>
    public enum ScratchRegisterValue
    {
        EjectAfterBoot = 0x01,
        DisplayError = 0x02,
        NoAnimation = 0x04,
        RunDashboard = 0x08
    };

    /// <summary>
    /// Xbox device.
    /// </summary>
    public enum Drive
    {
        CDRom,
        DriveC,
        DriveE,
        DriveF,
        //DriveG, // seems to be disabled in debug bios
        //DriveH, // seems to be disabled in debug bios
        DriveX,
        DriveY,
        DriveZ
    }

    /// <summary>
    /// Xbox drive name.
    /// </summary>
    public enum DriveLabel
    {
        A,  // DVD-ROM drive
        B,  // Volume
        C,  // Main volume
        D,  // Active title media
        E,  // Game development volume
        F,  // Memory unit 1A
        G,  // Memory unit 1B
        H,  // Memory unit 2A
        I,  // Memory unit 2B
        J,  // Memory unit 3A
        K,  // Memory unit 3B
        L,  // Memory unit 4A
        M,  // Memory unit 4B
        N,  // Secondary active utility drive
        O,  // Volume
        P,  // Utility drive for unknown title
        Q,  // Utility drive for unknown title  
        R,  // Utility drive for unknown title
        S,  // Persistent data for all titles
        T,  // Persistent data for active title
        U,  // Saved games for active title
        V,  // Saved games for all titles
        W,  // Persistant data for alternate title
        X,  // Saved games for alternate title
        Y,  // Reserved/unmappable while in debug???
        Z   // Active utility drive
    };

	#endregion

	#region Structs


    public class SurfaceInformation
    {
        public uint Size;
        public uint Format;
        public uint Address;
        public uint PushBufferPut;
    };


    public class ATAInputRegisters
    {
        public byte Features;      // Used for specifying SMART "commands".
        public byte SectorCount;   // IDE sector count register
        public byte SectorNumber;  // IDE sector number register
        public byte CylinderLow;   // IDE low order cylinder value
        public byte CylinderHigh;  // IDE high order cylinder value
        public byte DriveHead;     // IDE drive/head register
        public byte Command;       // Actual IDE command.
        public readonly byte Reserved = 0;  // reserved for future use.  Must be zero.
    };
    public struct ATAOutputRegisters
    {
        public byte Error;
        public byte SectorCount;
        public byte SectorNumber;
        public byte CylinderLow;
        public byte CylinderHigh;
        public byte DriveHead;
        public byte Status;
    };
    public class ATACommandObject
    {
        public ATAInputRegisters InputRegisters;
        public ATAOutputRegisters OutputRegisters;
        public byte[] Data = new byte[512];
        public uint DataSize = 512;
    };

    public class IDERegisters
    {
        public byte Features;
        public byte SectorCount;
        public byte SectorNumber;
        public byte CylinderLow;
        public byte CylinderHigh;
        public byte DriveHead;
        public byte CommandRegister;
        public byte HostSendsData;
    };
    public unsafe class ATAPassThrough
    {
        public IDERegisters Registers;
        public uint DataBufferSize;
        public byte* DataBuffer;
    };

    public struct ProcessorInformation
    {
        public string Identification;
        public uint Stepping;
        public uint Model;
        public uint Family;
    };

    public struct ProductionInfo
    {
        public string Country;
        public uint LineNumber;
        public uint Week;
        public uint Year;
    };
    //public struct ThreadStopInfo
    //{
    //    public bool IsStopped;
    //    public uint Address;
    //    public uint Thread;

    //}

    //// maybe do separate notification structs for each type: BreakNotification, ExecutionNotification etc...
    //public struct Notification
    //{
    //    NotificationType Type;
    //    string Message;
    //}

    public struct ThreadInfo
    {
        public uint ID;
        public uint Suspend;
        public uint Priority;
        public uint TlsBase;
        public uint Start;
        public uint Base;
        public uint Limit;
        public DateTime CreationTime;
    };

	/// <summary>
	/// Module information.
	/// </summary>
	public class ModuleInfo
	{
		/// <summary>
		/// Name of the module that was loaded.
		/// </summary>
		public string Name;

		/// <summary>
		/// Address that the module was loaded to.
		/// </summary>
		public uint BaseAddress;

		/// <summary>
		/// Size of the module.
		/// </summary>
		public uint Size;

		/// <summary>
		/// Time stamp of the module.
		/// </summary>
        public DateTime TimeStamp;

		/// <summary>
		/// Checksum of the module.
		/// </summary>
		public uint Checksum;

        /// <summary>
        /// Sections contained within the module.
        /// </summary>
        public List<ModuleSection> Sections;
	};

    /// <summary>
    /// Module section information.
    /// </summary>
    public class ModuleSection
    {
        public string Name;
        public uint Base;
        public uint Size;
        public uint Index;
        public uint Flags;
    };

    /// <summary>
    /// Structure that contains information about the XBE.
    /// </summary>
	public class XbeInfo
	{
		public string LaunchPath;
        public DateTime TimeStamp;
		public uint Checksum;
		public uint StackSize;
	};

    /// <summary>
    /// Xbox file information.
    /// </summary>
	public class FileInformation
	{
		public string Name;
		public ulong Size;
		public FileAttributes Attributes;
		public DateTime CreationTime;
		public DateTime ChangeTime;
	};

    /// <summary>
    /// Xbox memory statistics.
    /// </summary>
	public class MemoryStatistics
	{
		public uint TotalPages;
		public uint AvailablePages;
		public uint StackPages;
		public uint VirtualPageTablePages;
		public uint SystemPageTablePages;
		public uint PoolPages;
		public uint VirtualMappedPages;
		public uint ImagePages;
		public uint FileCachePages;
		public uint ContiguousPages;
		public uint DebuggerPages;
	};

    /// <summary>
    /// Xbox memory region.
    /// </summary>
	[StructLayout(LayoutKind.Sequential)]
    public class MemoryRegion
    {
        public UIntPtr BaseAddress;
        public uint Size;
        public MEMORY_FLAGS Protect;
    };

    /// <summary>
    /// Xbox memory address range.
    /// </summary>
	public class AddressRange // really should be a struct, but then you give up the parameterless ctor. sacrifices mike?
	{
		public uint Low;
		public uint High;

		/// <summary>
		/// Specifies a default range of all addresses.
		/// </summary>
		public AddressRange()
		{
			Low = 0;
			High = 0xFFFFFFFF;
		}

		/// <summary>
		/// Specifies a custom address range.
		/// </summary>
		/// <param name="low"></param>
		/// <param name="high"></param>
		public AddressRange(uint low, uint high)
		{
			Low = low;
			High = high;
		}
	};

    /// <summary>
    /// Xbox memory allocation entry.
    /// </summary>
	public struct AllocationEntry
	{
		public uint Address;
		public uint Size;
		public AllocationType Type;

		public AllocationEntry(uint address, uint size, AllocationType type)
		{
			Address = address;
			Size = size;
			Type = type;
		}
	};

    /// <summary>
    /// Basic xbox memory information.
    /// </summary>
	public struct MEMORY_BASIC_INFORMATION
	{
		public uint BaseAddress;
		public uint AllocationBase;
		public MEMORY_FLAGS AllocationProtect;
		public uint RegionSize;
		public MEMORY_FLAGS State;
		public MEMORY_FLAGS Protect;
		public MEMORY_FLAGS Type;
	};

	/// <summary>
	/// Cpu general purpose register context.  Only the registers you change will be set before the call.
	/// </summary>
	public class CPUContext
	{
        // general purpose - assumes integer assignment
        public object Eax;
        public object Ebx;
        public object Ecx;
        public object Edx;
        public object Esi;
        public object Edi;
        public object Esp;
        public object Ebp;

        // sse - assumes floating point assignment
        public object Xmm0;
        public object Xmm1;
        public object Xmm2;
        public object Xmm3;
        public object Xmm4;
        public object Xmm5;
        public object Xmm6;
        public object Xmm7;
	};

	/// <summary>
	/// Xbox gamepad input state.
	/// </summary>
	public class InputState
	{
		public Buttons Buttons = 0;
		public byte[] AnalogButtons = new byte[8];
		public short ThumbLX = 0;
		public short ThumbLY = 0;
		public short ThumbRX = 0;
		public short ThumbRY = 0;

		public void AssignState(InputState state)
		{
			this.Buttons = state.Buttons;
			this.AnalogButtons = state.AnalogButtons;
			this.ThumbLX = state.ThumbLX;
			this.ThumbLY = state.ThumbLY;
			this.ThumbRX = state.ThumbRX;
			this.ThumbRY = state.ThumbRY;
		}
	};

    /// <summary>
    /// Xbox command status response.
    /// </summary>
	public class StatusResponse
	{
        private string full;
        public string Full { get { return full; } }
        private ResponseType type;
        public ResponseType Type { get { return type; } }
        private string message;
        public string Message { get { return message; } }
        public bool Success { get { return ((int)type & 200) == 200; } }

		public StatusResponse(string full, ResponseType type, string message)
		{
            this.full = full;
			this.type = type;
            this.message = message;
		}
	};

    /// <summary>
    /// Xbox debug connection information.
    /// </summary>
    public struct DebugConnection
    {
        public IPAddress IP;
        public string Name;
        public DebugConnection(IPAddress ip, string name)
        {
            IP = ip;
            Name = name;
        }
    };
	#endregion

	/// <summary>
	/// Xbox debug connection.
	/// </summary> 
	public partial class Xbox : IDisposable
	{
		#region Fields
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string xdkRegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\XboxSDK";

        public XboxHistory History;
        public XboxGamepad Gamepad;

        // since this spends most of its time sleeping waiting for responses from the xbox...
        // for people who dont like cpu usage looking like 100% utilization, even though the app
        // will readily relinquish its time slice for others if asked to do so...
        public int SleepTime = 0;   // zero to be efficient, 1 to make it look like its not using up cpu

		#endregion

		#region Properties

        /// <summary>
        /// Gets the main connection used for pc to xbox communication.
        /// </summary>
        public TcpClient Connection { get { return connection; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TcpClient connection = new TcpClient();

        /// <summary>
        /// Gets the xbox kernel information.
        /// </summary>
        public XboxKernel Kernel { get { return kernel; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private XboxKernel kernel;

        /// <summary>
        /// Gets or sets the maximum waiting time given (in milliseconds) for a response.
        /// </summary>
        public int Timeout { get { return timeout; } set { timeout = value; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int timeout = 5000;

		/// <summary>
		/// Gets the current connection status known to YeloDebug.  For an actual status update you need to Ping() the xbox.
		/// </summary>
		public bool Connected	{ get { return connected; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool connected = false;

		/// <summary>
		/// Gets the xbox debug ip address.
		/// </summary>
		public IPAddress DebugIP   { get { return debugIP; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPAddress debugIP;

        /// <summary>
        /// Gets the xbox debug name.
        /// </summary>
        public string DebugName { get { return debugName; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string debugName;

        /// <summary>
        /// Gets or sets whether or not the notification session will be enabled.
        /// </summary>
        public bool EnableNotificationSession { get { return notificationSessionEnabled; } set { notificationSessionEnabled = value; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool notificationSessionEnabled = false;

        /// <summary>
        /// Gets the notification listener registered with the xbox that listens for incoming notification session requests.
        /// </summary>
        public TcpListener NotificationListener { get { return notificationListener; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TcpListener notificationListener;

        /// <summary>
        /// Gets the current notification session registered with the xbox.
        /// </summary>
        public TcpClient NotificationSession { get { return notificationSession; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TcpClient notificationSession;

        /// <summary>
        /// Gets or sets the xbox notification port.
        /// </summary>
        public int NotificationPort { get { return notificationPort; } set { notificationPort = value; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int notificationPort = 731;

        /// <summary>
        /// Gets or sets the list of notifications.
        /// </summary>
        public List<string> Notifications { get { return notifications; } set { notifications = value; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private List<string> notifications = new List<string>();

		/// <summary>
		/// Gets the xbox game ip address.
		/// </summary>
        public IPAddress TitleIP { get { return titleIP; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IPAddress titleIP;

		/// <summary>
		/// Gets the xbox process id.
		/// </summary>
		public uint ProcessID { get	{ return processID;	} }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private uint processID = 0;

        /// <summary>
        /// Gets a list of modules loaded by the xbox.
        /// </summary>
        public List<ModuleInfo> Modules { get { return modules; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private List<ModuleInfo> modules;

        /// <summary>
        /// Gets xbox executable information.
        /// </summary>
        public XbeInfo XbeInfo { get { return xbeInfo; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private XbeInfo xbeInfo;

        /// <summary>
        /// Gets the xbox debug monitor version.
        /// </summary>
        public Version DebugMonitorVersion { get { return debugMonitorVersion; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Version debugMonitorVersion;

        /// <summary>
        /// Gets the xbox kernel version.  Note that non-debug kernel build versions are substituted with the build year instead.
        /// </summary>
        public Version KernelVersion { get { return kernelVersion; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Version kernelVersion;

        /// <summary>
        /// Gets the xbox hard drive key.
        /// </summary>
        public byte[] HardDriveKey { get { return hardDriveKey; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[] hardDriveKey;

        /// <summary>
        /// Gets the xbox EEPROM key.
        /// </summary>
        public byte[] EEPROMKey { get { return eepromKey; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[] eepromKey;

        /// <summary>
        /// Gets the xbox signature key.
        /// </summary>
        public byte[] SignatureKey { get { return signatureKey; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[] signatureKey;

        /// <summary>
        /// Gets the xbox lan key.
        /// </summary>
        public byte[] LanKey { get { return lanKey; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[] lanKey;

        /// <summary>
        /// Gets the alternate xbox signature keys.
        /// </summary>
        public byte[][] AlternateSignatureKeys { get { return alternateSignatureKeys; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[][] alternateSignatureKeys;

        /// <summary>
        /// Gets the xbox hard drive serial number.
        /// </summary>
        public string HardDriveSerial { get { return hardDriveSerial; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string hardDriveSerial;

        /// <summary>
        /// Gets the xbox hard drive model.
        /// </summary>
        public string HardDriveModel { get { return hardDriveModel; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string hardDriveModel;

        /// <summary>
        /// Gets the xbox serial number.
        /// </summary>
        public ulong SerialNumber { get { return serialNumber; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong serialNumber;

        /// <summary>
        /// Gets the xbox mac address.
        /// </summary>
        public string MacAddress { get { return macAddress; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string macAddress;

        /// <summary>
        /// Gets the xbox version.
        /// </summary>
        public string Version
        {
            get
            {
                if (version == null)
                {
                    // xbox version info
                    CallAddressEx(Kernel.HalReadSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.FirmwareVersion, 0, History.ScratchBuffer);
                    CallAddressEx(Kernel.HalReadSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.FirmwareVersion, 0, History.ScratchBuffer + 1);
                    CallAddressEx(Kernel.HalReadSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.FirmwareVersion, 0, History.ScratchBuffer + 2);
                    string code = ASCIIEncoding.ASCII.GetString(GetMemory(History.ScratchBuffer, 3));
                    switch (code)
                    {
                        case "01D":
                        case "D01":
                        case "1D0":
                        case "0D1": version = "Xbox Development Kit"; break;
                        case "P01": version = "Xbox v1.0"; break;
                        case "P05": version = "Xbox v1.1"; break;
                        case "P11":
                        case "1P1":
                        case "11P":
                            if (videoEncoderType == VideoEncoder.Focus) version = "1.4";
                            else version = "Xbox v1.2/3"; break;
                        case "P2L": version = "Xbox v1.6"; break;
                        case "B11":
                        case "DBG": version = "Xbox Debug Kit"; break;   // green

                        default: version = code + ": Unknown Xbox Version"; break;
                    }
                    return version;
                }
                else return version;
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string version;

        /// <summary>
        /// Gets the xbox hardware info.
        /// </summary>
        public string HardwareInfo { get { return hardwareInfo; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string hardwareInfo;

        /// <summary>
        /// Gets the video encoder type installed on the xbox.
        /// </summary>
        public VideoEncoder VideoEncoderType { get { return videoEncoderType; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VideoEncoder videoEncoderType;

        /// <summary>
        /// Gets the xbox processor information.
        /// </summary>
        public ProcessorInformation Processor { get { return processor; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProcessorInformation processor;

        /// <summary>
        /// Gets the current xbox ethernet link status.
        /// </summary>
        public LinkStatus LinkStatus { get { return linkStatus; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LinkStatus linkStatus;

        /// <summary>
        /// Gets the xbox EEPROM.
        /// </summary>
        public byte[] EEPROM { get { return eeprom; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[] eeprom;

        /// <summary>
        /// Gets or sets the last xbox connection used.
        /// </summary>
        public string LastConnectionUsed
        {
            get { return (string)Microsoft.Win32.Registry.GetValue(xdkRegistryPath, "XboxName", string.Empty); }
            set { Microsoft.Win32.Registry.SetValue(xdkRegistryPath, "XboxName", value); }
        }

        /// <summary>
        /// Gets the xbox manufacturer production information.
        /// </summary>
        public ProductionInfo ProductionInfo { get { return productionInfo; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProductionInfo productionInfo;

        /// <summary>
        /// Gets the current AV-Pack status.
        /// </summary>
        public AVPack AVPack
        {
            get
            {
                CallAddressEx(Kernel.HalReadSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.VideoMode, 0, History.ScratchBuffer);
                return (AVPack)GetByte(History.ScratchBuffer);
            }
        }

		/// <summary>
		/// Gets or sets the xbox system time.
		/// </summary>
		public unsafe DateTime SystemTime
		{
			get
			{
				StatusResponse response = SendCommand("systime");
				if (response.Type == ResponseType.SingleResponse)
				{
					string ticks = string.Format("0x{0}{1}",
						response.Message.Substring(7, 7),
						response.Message.Substring(21).PadLeft(8, '0')
						);
					return DateTime.FromFileTime(Convert.ToInt64(ticks, 16));
				}
				else throw new ApiException("Failed to get xbox system time.");
			}
            set
            {
                long fileTime = value.ToFileTimeUtc();
                int lo = *(int*)&fileTime;
                int hi = *((int*)&fileTime + 1);

                StatusResponse response = SendCommand(string.Format("setsystime clockhi=0x{0} clocklo=0x{1} tz=1", Convert.ToString(hi, 16), Convert.ToString(lo, 16)));
                if (response.Type != ResponseType.SingleResponse)
                    throw new ApiException("Failed to set xbox system time.");
            }
		}

        /// <summary>
        /// Gets the amount of time the xbox has been powered on for or the time since its last cold boot.
        /// </summary>
        public TimeSpan TimePoweredOn
        {
            get
            {
                //rdtsc
                //mov	dword ptr ds:[010004h], eax
                //mov	dword ptr ds:[010008h], edx
                //mov	eax, 02DB0000h	;fake success
                //retn	010h
                SetMemory(XboxHistory.ScriptBufferAddress, Util.HexStringToBytes("0F31A304000100891508000100B80000DB02C21000"));
                SendCommand("crashdump");

                uint performaceFrequency;
                if (processor.Model == 11) performaceFrequency = 1481200000; // DreamX console
                else if (processor.Model == 8 && processor.Stepping == 6) performaceFrequency = 999985000;   // Intel Pentium III Coppermine
                else performaceFrequency = 733333333;

                return TimeSpan.FromSeconds(GetUInt64(0x10004) / performaceFrequency);    // performanceFrequency in Hz (counts per second)
            }
        }


        /// <summary>
        /// Gets a list of xbox threads.
        /// </summary>
		public List<ThreadInfo> Threads
		{
			get
			{
				SendCommand("threads");
				List<uint> threadIDs = new List<uint>();
				string line = ReceiveSocketLine();
				while (line[0] != '.')
				{
                    threadIDs.Add((uint)Util.GetResponseInfo(line, 0));
					line = ReceiveSocketLine();
				}

                List<ThreadInfo> threads = new List<ThreadInfo>();
                foreach (uint id in threadIDs)
                {
                    ThreadInfo threadInfo = new ThreadInfo();
                    SendCommand("threadinfo thread={0}", id);
                    threadInfo.ID = id;
                    List<object> info = Util.ExtractResponseInformation(ReceiveSocketLine());
                    threadInfo.Suspend = (uint)info[0];
                    threadInfo.Priority = (uint)info[1];
                    threadInfo.TlsBase = (uint)info[2];
                    threadInfo.Start = (uint)info[3];
                    threadInfo.Base = (uint)info[4];
                    threadInfo.Limit = (uint)info[5];
                    long ticks = (uint)info[7];
                    ticks |= (((long)(uint)info[6] << 32));
                    threadInfo.CreationTime = DateTime.FromFileTime(ticks);
                    threads.Add(threadInfo);
                    ReceiveSocketLine();    //'.'
                }
                return threads;
			}
		}

        /// <summary>
        /// Gets the xbox CPU temperature in degrees celsius.
        /// </summary>
        public uint CPUTemperature
        {
            get
            {
                CallAddressEx(Kernel.HalReadSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.CpuTemperature, 0, History.ScratchBuffer);
                return GetUInt32(History.ScratchBuffer);
            }
        }

        /// <summary>
        /// Gets the xbox air temperature in degrees celsius.
        /// </summary>
        public uint AirTemperature
        {
            get
            {
                CallAddressEx(Kernel.HalReadSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.AirTemperature, 0, History.ScratchBuffer);
                uint temp = GetUInt32(History.ScratchBuffer);
                if (version == "Xbox v1.6") temp = (uint)(temp * 0.8f); // v1.6 box shows temp too high
                return temp;
            }
        }

        /// <summary>
        /// Gets or sets the xbox fan speed percentage.
        /// </summary>
        public int FanSpeed
        {
            get
            {
                CallAddressEx(Kernel.HalReadSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.ReadFanSpeed, 0, History.ScratchBuffer);
                int result = GetInt32(History.ScratchBuffer);
                return (int)(((float)result / 50) * 100);
            }
            set
            {
                int speed = (int)(value * 0.5f);
                CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.FanOverride, 0, FanModeSubCommand.Custom);
                CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.RequestFanSpeed, 0, speed);
                CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.FanOverride, 0, FanModeSubCommand.Custom);
                CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.RequestFanSpeed, 0, speed);
            }
        }

        public SurfaceInformation DisplayBufferInformation
        {
            get
            {
                SurfaceInformation si = new SurfaceInformation();
                StatusResponse response = SendCommand("getsurf id={0}", (int)Surface.FrontBuffer);
                List<object> info = Util.ExtractResponseInformation(response.Message);
                si.Size = (uint)info[0];
                si.Format = (uint)info[1];
                si.Address = (uint)info[2];
                si.PushBufferPut = (uint)info[3];
                return si;
            }
        }

		#endregion

		#region Constructor / Destructor
		/// <summary>
		/// Xbox connection.
		/// </summary>
		public Xbox()
        {
            // load settings file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("YeloDebugSettings.xml");

            // check that setting and assembly versions match
            string assemblyVersion = Assembly.GetExecutingAssembly().FullName.Substring(19, 7);
            string settingsVersion = xmlDoc.GetElementsByTagName("Version")[0].InnerText;
            if (assemblyVersion != settingsVersion) throw new ApiException("YeloDebug version does not match the version of the settings file.");

            // get settings information
            xdkRegistryPath = xmlDoc.GetElementsByTagName("XdkRegistryPath")[0].InnerText;
            //notificationSessionEnabled = Convert.ToBoolean(xmlDoc.GetElementsByTagName("NotificationSession")[0].InnerText);
            //notificationPort = Convert.ToInt32(xmlDoc.GetElementsByTagName("NotificationPort")[0].InnerText);
        }

        ~Xbox() { Dispose(); }

        public void Dispose()
        {
            Disconnect();
        }
		#endregion

		#region Connection
        /// <summary>
        /// Returns a list containing all consoles detected on the network.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Updated for use with multiple NICs by xmt ;D</remarks>
        public List<DebugConnection> QueryXboxConnections()
        {
            List<DebugConnection> connections = new List<DebugConnection>();
            List<Socket> socks = new List<Socket>();

            // create our connections
            foreach (NetworkInterface i in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation ua in i.GetIPProperties().UnicastAddresses)
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    s.EnableBroadcast = true;

                    try
                    {
                        // broadcast our request on xbox port 731
                        s.Bind(new IPEndPoint(ua.Address, 0));
                        s.SendTo(new byte[] { 3, 0 }, new IPEndPoint(IPAddress.Broadcast, 731));
                        socks.Add(s);
                    }
                    catch { /* failed broadcast */}
                }
            }

            DateTime before = DateTime.Now;
            TimeSpan elapse = new TimeSpan();
            do
            {
                Thread.Sleep(1);
                foreach (Socket s in socks)
                {
                    while (s.Available > 0)
                    {
                        // parse any information returned
                        byte[] data = new byte[s.Available];
                        EndPoint end = new IPEndPoint(IPAddress.Any, 0);
                        s.ReceiveFrom(data, ref end);
                        IPEndPoint endpoint = (IPEndPoint)end;
                        connections.Add(new DebugConnection(((IPEndPoint)end).Address, ASCIIEncoding.ASCII.GetString(data, 2, data.Length - 2).Replace("\0", "")));
                    }
                }
                elapse = DateTime.Now - before;
            }
            while (elapse.TotalMilliseconds < 25);            // wait for response

            if (connections.Count == 0)
                throw new NoConnectionException("No xbox connection detected.");

            // close the connections
            foreach (Socket s in socks)
            {
                s.Close();
            }

            // check to make sure that each box has unique connection information...(case sensitive)
            for (int i = 0; i < connections.Count; i++)
                for (int j = 0; j < connections.Count; j++)
                    if (i != j && (connections[i].Name == connections[j].Name || connections[i].IP == connections[j].IP))
                        throw new NoConnectionException("Multiple consoles found that have the same connection information.  Please ensure that each box connected to the network has different debug names and ips.");

            return connections;
        }


        private void Initialize(string xboxIP)
        {
            // establish debug session
            connection = new TcpClient();
            connection.SendTimeout = 250;
            connection.ReceiveTimeout = 250;
            connection.ReceiveBufferSize = 0x100000 * 3;    // todo: check on this
            connection.SendBufferSize = 0x100000 * 3;
            connection.NoDelay = true;
            connection.Connect(xboxIP, 731);
            connected = Ping(100);  // make sure it is successful
            if (connected)
            {
                // make sure they are using the current xbdm.dll v7887
                debugMonitorVersion = new Version(SendCommand("dmversion").Message);
                if (DebugMonitorVersion != new Version("1.00.7887.1"))
                {
                    Disconnect();   // unsafe to proceed, so disconnect...
                    throw new ApiException("Must use our hacked xbdm.dll v1.00.7887.1 before connecting");
                }

                // check correct module entrypoint
                SendCommand("modules");
                modules = new List<ModuleInfo>();
                string line = ReceiveSocketLine();
                while (line[0] != '.')
                {
                    ModuleInfo module = new ModuleInfo();
                    module.Sections = new List<ModuleSection>();
                    List<object> info = Util.ExtractResponseInformation(line);
                    module.Name = (string)info[0];
                    module.BaseAddress = (uint)info[1];

                    if (module.Name == "xbdm.dll" && module.BaseAddress != 0xB0000000)
                        throw new Exception("You seem to be most likely running the Complex v4627 Debug Bios.  YeloDebug is not compatible with this bios.");

                    module.Size = (uint)info[2];
                    module.Checksum = (uint)info[3];

                    module.TimeStamp = Util.TimeStampToUniversalDateTime((uint)info[4]);
                    modules.Add(module);
                    line = ReceiveSocketLine();
                }
                foreach (ModuleInfo module in modules)
                {
                    SendCommand("modsections name=\"{0}\"", module.Name);
                    List<string> response = ReceiveMultilineResponseList();
                    foreach (string r in response)
                    {
                        ModuleSection modSection = new ModuleSection();
                        List<object> info = Util.ExtractResponseInformation(r);
                        modSection.Name = (string)info[0];
                        modSection.Base = (uint)info[1];
                        modSection.Size = (uint)info[2];
                        modSection.Index = (uint)info[3];
                        modSection.Flags = (uint)info[4];
                        module.Sections.Add(modSection);
                    }
                }

                // register our notification session
                if (notificationSessionEnabled)
                    RegisterNotificationSession(notificationPort);

                // must have for our shitty setmem hack to work ;P
                CreateFile("E:\\fUkM$DeVs", FileMode.Create);

                //initialize main components - order specific!!!
                MemoryStream = new XboxMemoryStream(this);
                MemoryStream.SafeMode = true;
                MemoryReader = new BinaryReader(MemoryStream);
                MemoryWriter = new BinaryWriter(MemoryStream);
                kernel = new XboxKernel(this);
                History = new XboxHistory(this);
                Gamepad = new XboxGamepad(this);
                eeprom = ReadEEPROM();

                // get xbox production information
                ProductionInfo pInfo = new ProductionInfo();
                string serial = ASCIIEncoding.ASCII.GetString(eeprom, 0x34, 12);
                switch (serial[11])
                {
                    case '2': pInfo.Country = "Mexico"; break;
                    case '3': pInfo.Country = "Hungary"; break;
                    case '5': pInfo.Country = "China"; break;
                    case '6': pInfo.Country = "Taiwan"; break;
                    default: pInfo.Country = "Unknown"; break;
                }
                pInfo.LineNumber = Convert.ToUInt32(serial.Substring(0, 1));
                pInfo.Week = Convert.ToUInt32(serial.Substring(8, 2));
                pInfo.Year = Convert.ToUInt32("200" + serial[7]);
                productionInfo = pInfo;

                // xbox video encoder type
                if (CallAddressEx(Kernel.HalReadSMBusValue, null, true, SMCDevices.VideoEncoderXcalibur, VideoEncoderCommand.Detect, 0, History.ScratchBuffer) == 0) videoEncoderType = VideoEncoder.Xcalibur;
                else if (CallAddressEx(Kernel.HalReadSMBusValue, null, true, SMCDevices.VideoEncoderConnexant, VideoEncoderCommand.Detect, 0, History.ScratchBuffer) == 0) videoEncoderType = VideoEncoder.Connexant;
                else if (CallAddressEx(Kernel.HalReadSMBusValue, null, true, SMCDevices.VideoEncoderFocus, VideoEncoderCommand.Detect, 0, History.ScratchBuffer) == 0) videoEncoderType = VideoEncoder.Focus;
                else videoEncoderType = VideoEncoder.Unknown;

                // processor information
                SetMemory(XboxHistory.ScriptBufferAddress, Util.HexStringToBytes("B8010000000FA2A300000100B80000DB02C21000"));
                SendCommand("crashdump");
                uint eax = GetUInt32(0x10000);
                processor.Stepping = eax & 0xf;
                processor.Model = (eax >> 4) & 0xf;
                processor.Family = (eax >> 8) & 0xf;
                if (processor.Model == 11) { processor.Identification = "1.48 GHz Intel Tualatin Celeron (DreamX)"; }
                else if (processor.Model == 8 && processor.Stepping == 6) { processor.Identification = "1.00 GHz Intel Pentium III Coppermine"; }
                else { processor.Identification = "733.33 MHz Intel Pentium III"; }

                // hardware info
                uint ver = GetUInt32(Kernel.HardwareInfo);
                string vstr = Convert.ToString(ver, 16).PadLeft(8, '0');
                string vstr2 = Util.HexBytesToString(GetMemory(Kernel.HardwareInfo + 4, 2)).Insert(2, " ");
                hardwareInfo = vstr + " " + vstr2;

                macAddress = BitConverter.ToString(eeprom, 0x40, 6).Replace('-', ':');

                serialNumber = Convert.ToUInt64(ASCIIEncoding.ASCII.GetString(eeprom, 0x34, 12));
                lanKey = GetMemory(Kernel.XboxLANKey, 16);
                signatureKey = GetMemory(Kernel.XboxSignatureKey, 16);
                eepromKey = GetMemory(Kernel.XboxEEPROMKey, 16);
                hardDriveKey = GetMemory(Kernel.XboxHDKey, 16);

                byte[] hdModelInfo = GetMemory(Kernel.HalDiskModelNumber, 40);
                uint unk1 = BitConverter.ToUInt32(hdModelInfo, 0);
                uint index = BitConverter.ToUInt32(hdModelInfo, 4);
                hardDriveModel = ASCIIEncoding.ASCII.GetString(hdModelInfo, 8, 32).Trim().Replace("\0", "");

                byte[] hdSerialInfo = GetMemory(Kernel.HalDiskSerialNumber, 32);
                unk1 = BitConverter.ToUInt32(hdSerialInfo, 0);
                index = BitConverter.ToUInt32(hdSerialInfo, 4);
                hardDriveSerial = ASCIIEncoding.ASCII.GetString(hdSerialInfo, 8, 16).Trim().Replace("\0", "");

                alternateSignatureKeys = new byte[16][];
                byte[] keyData = GetMemory(Kernel.XboxAlternateSignatureKeys, 256);
                for (int i = 0; i < 16; i++)
                {
                    alternateSignatureKeys[i] = new byte[16];
                    Buffer.BlockCopy(keyData, i * 16, alternateSignatureKeys[i], 0, 16);
                }

                StringBuilder krnlStr = new StringBuilder();
                byte[] krnlVersion = GetMemory(Kernel.XboxKrnlVersion, 8);
                krnlStr.AppendFormat("{0}.{1}.{2}.{3}",
                    BitConverter.ToUInt16(krnlVersion, 0),
                    BitConverter.ToUInt16(krnlVersion, 2),
                    BitConverter.ToUInt16(krnlVersion, 4),
                    BitConverter.ToUInt16(krnlVersion, 6)
                    );
                kernelVersion = new Version(krnlStr.ToString());

                try
                {
                    // OPTIONAL - will fail on some boxes that return "not debuggable" error
                    processID = Convert.ToUInt32(SendCommand("getpid").Message.Substring(6), 16);

                    SendCommand("xbeinfo running");
                    xbeInfo = new XbeInfo();
                    line = ReceiveSocketLine();
                    XbeInfo.TimeStamp = Util.TimeStampToUniversalDateTime((uint)Util.GetResponseInfo(line, 0));
                    XbeInfo.Checksum = (uint)Util.GetResponseInfo(line, 1);
                    line = ReceiveSocketLine();
                    XbeInfo.LaunchPath = (string)Util.GetResponseInfo(line, 0);
                    ReceiveSocketLine();    // '.'
                }
                catch { }

                try
                {
                    string hex = SendCommand("altaddr").Message.Substring(7);
                    titleIP = new IPAddress(Util.HexStringToBytes(hex));
                }
                catch { }
                linkStatus = (LinkStatus)CallAddressEx(Kernel.PhyGetLinkState, null, true, 0);

                MemoryStream.SafeMode = false;
            }
            else throw new NoConnectionException("Unable to connect.");
        }

        /// <summary>
        /// Connects to an xbox on the network. If multiple consoles are detected this method 
        /// will attempt to connect to the last connection used. If that connection or information
        /// is unavailable this method will fail.
        /// </summary>
        public void Connect()
        {
            try
            {
                connected = Ping(100); // update connection status
                if (!connected)
                {
                    Disconnect();   // destroy any old connection we might have had

                    // determines the debug names and ips of all debug xboxes on the network
                    List<DebugConnection> connections = QueryXboxConnections();

                    // attempt to narrow the list down to one connection
                    if (connections.Count == 1)
                    {
                        //store debug info
                        debugName = LastConnectionUsed = connections[0].Name;
                        debugIP = connections[0].IP;
                    }
                    else if (connections.Count > 1)
                    {
                        bool found = false;
                        foreach (DebugConnection dbgConnection in connections)
                        {
                            if (LastConnectionUsed == null) break;
                            if (dbgConnection.IP.ToString() == LastConnectionUsed || dbgConnection.Name == LastConnectionUsed)
                            {
                                //store debug info
                                debugName = LastConnectionUsed = dbgConnection.Name;
                                debugIP = dbgConnection.IP;
                                found = true;
                                break;
                            }
                        }
                        if (!found) throw new NoConnectionException("Unable to distinguish between multiple connections. Please turn off all other consoles or try to connect again using a specific ip.");
                    }
                    else throw new NoConnectionException("Unable to detect a connection.");

                    // establish debug session
                    Initialize(debugIP.ToString());
                }
            }
            catch (Exception ex)
            {
                connected = false;
            }
        }

		/// <summary>
		/// Connects to a specified xbox by name or ip.
		/// </summary>
		/// <param name="debugInfo">Case insensitive information may either be a debug ip or the name of a specific xbox.</param>
        public void Connect(string xbox)
        {
            try
            {
                connected = Ping(100); // update connection status
                if (!connected)
                {
                    Disconnect();   // destroy any old connection we might have had

                    // determines the debug name and ip of the specified xbox system
                    int index = -1;
                    List<DebugConnection> connections = QueryXboxConnections();
                    for (int i = 0; i < connections.Count; i++)
                        if (connections[i].Name.ToLower() == xbox.ToLower() || connections[i].IP.ToString().ToLower() == xbox.ToLower())
                            index = i;
                    if (index == -1) throw new NoConnectionException("Unable to connect to the specified xbox.");

                    //store debug info
                    debugName = LastConnectionUsed = connections[index].Name;
                    debugIP = connections[index].IP;

                    // establish debug session
                    Initialize(debugIP.ToString());
                }
            }
            catch
            {
                connected = false; 
            }
        }

        /// <summary>
        /// Use this to connect to an xbox outside your network.
        /// </summary>
        /// <param name="ip"></param>
        public void ConnectToIP(string ip)
        {
            try
            {
                connected = Ping(100); // update connection status
                if (!connected)
                {
                    Disconnect();   // destroy any old connection we might have had
                    Initialize(ip);
                }
            }
            catch
            {
                connected = false;
            }
        }


        /// <summary>
        /// Registers a notification session on the specified port.
        /// </summary>
        /// <param name="port"></param>
		public void RegisterNotificationSession(int port)
		{
			try
			{
                if (notificationSessionEnabled)
                {
                    // get string representation of host ip
                    string local = connection.Client.LocalEndPoint.ToString();
                    local = local.Remove(local.IndexOf(':'));

                    // set up a listener for the session
                    notificationListener = new TcpListener(new IPEndPoint(IPAddress.Parse(local), port));
                    notificationListener.Start();

                    // drop any old session and attempt to open up a new one
                    SendCommand("notifyat port=" + port + " drop");
                    StatusResponse res = SendCommand("notifyat port=" + port);
                    if (res.Type == ResponseType.SingleResponse && notificationListener.Pending())
                    {
                        notificationSession = new TcpClient();
                        notificationSession.Client = notificationListener.AcceptSocket();
                        notificationSession.ReceiveBufferSize = 0x100000;
                        ReceiveNotifications();
                    }
                    else notificationSessionEnabled = false;
                }
			}
            catch (Exception ex)
            { 
                notificationSessionEnabled = false;
                notificationListener.Stop();
            }
		}

        /// <summary>
        /// Registers a notification session.
        /// </summary>
        public void RegisterNotificationSession()
        {
            RegisterNotificationSession(731);
        }

		/// <summary>
		/// Disconnects from the xbox.
		/// </summary>
		public void Disconnect()
		{
            try
            {
                // attempt to clean up if still connected
                if (Ping())
                {
                    Gamepad.OverrideControllers(false);    // release controllers
                    if (AllocationTable.Count > 0) FreeAllMemory();    // release any memory we have allocated here
                    SendCommand("notifyat port=" + notificationPort + " drop"); // drop the notification session
                    SendCommand("bye"); // we cant leave without saying goodbye ;)
                }
            }
            catch { }
            finally // extra cleanup
            {
                connected = false;
                version = null;


                if (MemoryStream != null) MemoryStream.Dispose();

                if (notificationSession != null) notificationSession.Close();
            }
		}

        /// <summary>
        /// Attempts to re-establish a connection with the currently selected xbox console.
        /// </summary>
        /// <param name="timeout"></param>
        /// <remarks>Bugfix by xmt</remarks>
		public bool Reconnect(int timeout)
		{
			Disconnect();   // close our old connection
            DateTime Before = DateTime.Now;

            while (!Ping(100))
            {
                try
                {
                    Connect();    // create a new one using the current connection information
                }
                catch
                {
                    TimeSpan Elapse = DateTime.Now - Before;
                    if (Elapse.TotalMilliseconds > timeout)
                    {
                        Disconnect();
                        //throw new TimeoutException("Connection lost - unable to reconnect.");
                        return false;
                    }
                }
            }
            return true;
		}

        /// <summary>
        /// Re-establishes a connection with the currently selected xbox console.
        /// </summary>
        public bool Reconnect()
        {
            return Reconnect(1000);
        }

        /// <summary>
        /// Checks the connection status between xbox and pc.
        /// This function only checks what YeloDebug believes to be the current connection status.
        /// For a true status check you will need to ping the xbox regularly.
        /// </summary>
        public void ConnectionCheck()
        {
            //takes too much time to ping when used by continuously called functions 
            //if connection drops attempt to reconnect, otherwise fuck them, let it crash and burn...
            if (!connected) Reconnect(250);    // try to re-establish a connection
        }

        /// <summary>
        /// Retrieves actual xbox connection status. Average execution time of 3600 executions per second.
        /// </summary>
        /// <param name="waitTime">Time to wait for a response</param>
        /// <returns>Connection status</returns>
        public bool Ping(int waitTime)
        {
            int oldTimeOut = timeout;
            try
            {
                if (connection != null)
                {
                    if (connection.Available > 0)
                        connection.Client.Receive(new byte[connection.Available]);

                    connection.Client.Send(ASCIIEncoding.ASCII.GetBytes(Environment.NewLine));
                    timeout = waitTime;
                    FlushSocketBuffer(16);    // throw out garbage response "400- Unknown Command\r\n"
                    connected = true;
                    return true;
                }
                return false;
            }
            catch
            {
                connected = false;
                Connection.Close();
                return false;
            }
            finally
            {
                timeout = oldTimeOut;   // make sure to restore old timeout
            }
        }

        /// <summary>
        /// Retrieves actual xbox connection status. Average execution time of 3600 executions per second.
        /// </summary>
        /// <returns>Connection status</returns>
        public bool Ping()
        {
            return Ping(Timeout);
        }

        /// <summary>
        /// Reboots the xbox with the specified BootFlag.
        /// </summary>
        /// <param name="flag"></param>
		public void Reboot(BootFlag flag)
		{
			switch (flag)
			{
				case BootFlag.Cold:		SendCommand("reboot");			break;
				case BootFlag.Warm:		SendCommand("reboot warm");		break;
				case BootFlag.NoDebug:	SendCommand("reboot nodebug");	break;
				case BootFlag.Wait:		SendCommand("reboot wait");		break;
				case BootFlag.Stop:		SendCommand("reboot stop");		break;
				case BootFlag.Current:
					FlushSocketBuffer();
                    connection.Client.Send(ASCIIEncoding.ASCII.GetBytes("magicboot title=\"" + XbeInfo.LaunchPath + "\" debug" + Environment.NewLine));
                    if (connection.Available > 0)
                        connection.Client.Receive(new byte[connection.Available]);
					break;
			}
            Reconnect(15000);
		}

		/// <summary>
		/// Performs a warm reboot on the xbox.
		/// </summary>
        public void Reboot()
        {
            Reboot(BootFlag.Warm);
        }

        /// <summary>
        /// Launches the specified xbox title and attempts to establish a new connection with that title.
        /// </summary>
        /// <param name="path"></param>
        public void LaunchTitle(string path)
        {
            FlushSocketBuffer();
            connection.Client.Send(ASCIIEncoding.ASCII.GetBytes("magicboot title=\"" + path + "\" debug" + Environment.NewLine));
            if (connection.Available > 0)
                connection.Client.Receive(new byte[connection.Available]);
            Reconnect(15000);
        }

		#endregion

		#region Command Processing

		/// <summary>
        /// Waits for the receive buffer to stop receiving, then clears it.
        /// Call this before you send anything to the xbox to help keep the channel in sync.
		/// </summary>
		public void FlushSocketBuffer()
		{
            Wait(WaitType.Idle);    // waits for the link to be idle...
            try
            {
                if (connection.Available > 0)
                    connection.Client.Receive(new byte[connection.Available]);
            }
            catch { connected = false; }
		}

		/// <summary>
		/// Waits for a specified amount and then flushes it from the socket buffer.
		/// </summary>
		/// <param name="size">Size to flush</param>
		public void FlushSocketBuffer(int size)
		{
            if (size > 0)
            {
                Wait(size);
                try
                {
                    connection.Client.Receive(new byte[size]);
                }
                catch { connected = false; }
            }
		}

		/// <summary>
		/// Waits for data to be received.  During execution this method will enter a spin-wait loop and appear to use 100% cpu when in fact it is just a suspended thread.  
        /// This is much more efficient than waiting a millisecond since most commands take fractions of a millisecond.
        /// It will either resume after the condition is met or throw a timeout exception.
		/// </summary>
		/// <param name="type">Wait type</param>
		public void Wait(WaitType type)
		{
            if (connection != null)
            {
                Stopwatch sw = Stopwatch.StartNew();
                switch (type)
                {
                    // waits for data to start being received
                    case WaitType.Partial:
                        while (connection.Available == 0)
                        {
                            Thread.Sleep(SleepTime);
                            if (sw.ElapsedMilliseconds > timeout)
                            {
                                if (!Ping(250)) Disconnect();  // only disconnect if actually disconnected
                                throw new TimeoutException();
                            }
                        }
                        break;

                    // waits for data to start and then stop being received
                    case WaitType.Full:

                        // do a partial wait first
                        while (connection.Available == 0)
                        {
                            Thread.Sleep(SleepTime);
                            if (sw.ElapsedMilliseconds > timeout)
                            {
                                if (!Ping(250)) Disconnect();  // only disconnect if actually disconnected
                                throw new TimeoutException();
                            }
                        }

                        // wait for rest of data to be received
                        int avail = connection.Available;
                        Thread.Sleep(SleepTime);
                        while (connection.Available != avail)
                        {
                            avail = connection.Available;
                            Thread.Sleep(SleepTime);
                        }
                        break;

                    // waits for data to stop being received
                    case WaitType.Idle:
                        int before = connection.Available;
                        Thread.Sleep(SleepTime);
                        while (connection.Available != before)
                        {
                            before = connection.Available;
                            Thread.Sleep(SleepTime);
                            if (sw.ElapsedMilliseconds > timeout)
                            {
                                if (!Ping(250)) Disconnect();  // only disconnect if actually disconnected
                                throw new TimeoutException();
                            }
                        }
                        break;
                }
            }
            else throw new NoConnectionException();
		}

        // todo: dont timeout if still receiving, currently it could timeout if receiving large information with small timeout...

		/// <summary>
		/// Waits for a specified amount of data to be received.  Use with file IO.
		/// </summary>
		/// <param name="targetLength">Amount of data to wait for</param>
		public void Wait(int targetLength)
		{
            if (connection != null)
            {
                if (connection.Available < targetLength) // avoid waiting if we already have data in our buffer...
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    while (connection.Available < targetLength)
                    {
                        Thread.Sleep(SleepTime);
                        if (sw.ElapsedMilliseconds > timeout)
                        {
                            if (!Ping(250)) Disconnect();  // only disconnect if actually disconnected
                            throw new TimeoutException();
                        }
                    }
                }
            }
            else throw new NoConnectionException();
		}

		/// <summary>
		/// Waits for a line of text to be received from the xbox.
		/// </summary>
		/// <returns></returns>
		public string ReceiveSocketLine()
		{
            if (connection != null)
            {
                string lineText;
                byte[] textBuffer = new byte[256];  // buffer large enough to contain a line of text

                Wait(WaitType.Partial); // wait for data to appear in the receive buffer

                Stopwatch sw = Stopwatch.StartNew();
                while (true)
                {
                    int avail = connection.Available;   // only get once
                    if (avail < textBuffer.Length)
                    {
                        connection.Client.Receive(textBuffer, avail, SocketFlags.Peek);
                        lineText = ASCIIEncoding.ASCII.GetString(textBuffer, 0, avail);
                    }
                    else
                    {
                        connection.Client.Receive(textBuffer, textBuffer.Length, SocketFlags.Peek);
                        lineText = ASCIIEncoding.ASCII.GetString(textBuffer);
                    }

                    int eolIndex = lineText.IndexOf("\r\n");
                    if (eolIndex != -1)
                    {
                        connection.Client.Receive(textBuffer, eolIndex + 2, SocketFlags.None);
                        return ASCIIEncoding.ASCII.GetString(textBuffer, 0, eolIndex);
                    }

                    // end of line not found yet, lets wait some more...
                    Thread.Sleep(SleepTime);
                    if (sw.ElapsedMilliseconds > timeout)
                    {
                        if (!Ping(250)) Disconnect();  // only disconnect if actually disconnected
                        throw new TimeoutException();
                    }
                }
            }
            else throw new NoConnectionException();
		}

        /// <summary>
        /// Receives multiple lines of text from the xbox.
        /// </summary>
        /// <returns></returns>
        public string ReceiveMultilineResponse()
        {
            if (connection != null)
            {
                StringBuilder response = new StringBuilder();
                string line = string.Empty;
                while (true)
                {
                    line = ReceiveSocketLine() + Environment.NewLine;
                    if (line[0] == '.') break;
                    else response.Append(line);
                }
                return response.ToString();
            }
            else throw new NoConnectionException();
        }

        public List<string> ReceiveMultilineResponseList()
        {
            List<string> lines = new List<string>();
            if (connection != null)
            {
                StringBuilder response = new StringBuilder();
                string line = string.Empty;
                while (true)
                {
                    line = ReceiveSocketLine();
                    if (line[0] == '.') break;
                    else lines.Add(line);
                }
                return lines;
            }
            else throw new NoConnectionException();
        }

		/// <summary>
		/// Receives a notification if one is present.
		/// </summary>
        /// <returns></returns>
		public bool ReceiveNotification()
		{
            if (connection != null)
            {
                if (NotificationSession.Connected)
                {
                    try
                    {
                        string lineText;
                        byte[] textBuffer = new byte[256];  // buffer large enough to contain a line of text

                        int avail = NotificationSession.Available;   // only get once
                        if (avail > 0)
                        {
                            if (avail < textBuffer.Length)
                            {
                                NotificationSession.Client.Receive(textBuffer, avail, SocketFlags.Peek);
                                lineText = ASCIIEncoding.ASCII.GetString(textBuffer, 0, avail);
                            }
                            else
                            {
                                NotificationSession.Client.Receive(textBuffer, textBuffer.Length, SocketFlags.Peek);
                                lineText = ASCIIEncoding.ASCII.GetString(textBuffer);
                            }

                            int eolIndex = lineText.IndexOf("\r\n");
                            if (eolIndex != -1)
                            {
                                NotificationSession.Client.Receive(textBuffer, eolIndex + 2, SocketFlags.None);
                                notifications.Add(ASCIIEncoding.ASCII.GetString(textBuffer, 0, eolIndex));
                                return true;
                            }
                            else return false;
                        }
                        else return false;
                    }
                    catch
                    {
                        Disconnect();
                        throw new NoConnectionException();
                    }
                }
                else throw new ApiException("Notification session has been dropped.");
            }
            else throw new NoConnectionException();
		}

        /// <summary>
        /// Receives any notifications that may be present.
        /// </summary>
        public void ReceiveNotifications()
        {
            if (connection != null)
            {
                while (ReceiveNotification());
            }
            else throw new NoConnectionException();
        }

        /// <summary>
        /// Waits for a status response to be received from the xbox.
        /// </summary>
        /// <returns>Status response</returns>
		public StatusResponse ReceiveStatusResponse()
		{
            if (connection != null)
            {
                string response = ReceiveSocketLine();
                return new StatusResponse(response, (ResponseType)Convert.ToInt32(response.ToString().Remove(3)), response.Remove(0, 5).ToString());
            }
            else throw new NoConnectionException();
		}

        /// <summary>
        /// Sends a command to the xbox.
        /// </summary>
        /// <param name="command">Command to be sent</param>
        /// <param name="args">Arguments</param>
        /// <returns>Status response</returns>
		public StatusResponse SendCommand(string command, params object[] args)
        {
            if (connection != null)
            {
                FlushSocketBuffer();

                try
                {
                    connection.Client.Send(ASCIIEncoding.ASCII.GetBytes(string.Format(command, args) + Environment.NewLine));
                }
                catch (Exception ex)
                {
                    Disconnect();
                    throw new NoConnectionException();
                }

                StatusResponse response = ReceiveStatusResponse();

                if (response.Success) return response;
                else throw new ApiException(response.Full);
            }
            else throw new NoConnectionException();
        }

        
        /// <summary>
        /// Sends binary data to the xbox.
        /// </summary>
        /// <param name="data"></param>
        public void SendBinaryData(byte[] data)
        {
            ConnectionCheck();
            FlushSocketBuffer();
            connection.Client.Send(data);
        }

        /// <summary>
        /// Sends binary data of specified length to the xbox.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        public void SendBinaryData(byte[] data, int length)
        {
            ConnectionCheck();
            FlushSocketBuffer();
            connection.Client.Send(data, length, SocketFlags.None);
        }

        /// <summary>
        /// Receives all available binary data sent from the xbox.
        /// </summary>
        /// <returns></returns>
        public byte[] ReceiveBinaryData()
        {
            if (connection.Available > 0)
            {
                byte[] binData = new byte[connection.Available];
                connection.Client.Receive(binData, binData.Length, SocketFlags.None);
                return binData;
            }
            else return null;
        }

        /// <summary>
        /// Receives binary data of specified size sent from the xbox.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public byte[] ReceiveBinaryData(int size)
        {
            Wait(size);
            byte[] binData = new byte[size];
            connection.Client.Receive(binData, binData.Length, SocketFlags.None);
            return binData;
        }

        /// <summary>
        /// Receives binary data of specified size sent from the xbox.
        /// </summary>
        /// <param name="data"></param>
        public void ReceiveBinaryData(ref byte[] data)
        {
            Wait(data.Length);
            connection.Client.Receive(data, data.Length, SocketFlags.None);
        }

        /// <summary>
        /// Receives binary data of specified size sent from the xbox.
        /// </summary>
        /// <param name="data"></param>
        public void ReceiveBinaryData(ref byte[] data, int offset, int size)
        {
            Wait(size);
            connection.Client.Receive(data, offset, size, SocketFlags.None);
        }

		#endregion

		#region Misc.

        /// <summary>
        /// Synchronizes the xbox system time with the computer's current time.
        /// </summary>
        public void SynchronizeTime()
        {
            SystemTime = DateTime.Now;
        }

        /// <summary>
        /// Benchmark utility function best when ran with xdk dash.  Lower memory situations will yeild lower speeds.
        /// </summary>
        /// <returns></returns>
		public string StreamTest()
		{
			float toMegs = 1.0f / (1024.0f * 1024.0f);

			int fileBufferSize = 0x40000;   // 256kb
			uint memBufferSize = 0x20000;   // 128kb

			if (MemoryStatistics.AvailablePages * 0x1000 < memBufferSize)
			{
				SetFileCacheSize(1);
				if (MemoryStatistics.AvailablePages * 0x1000 < memBufferSize)
					return "Need at least 128kb of available memory";
			}

            // memory tests
			uint TestBuffer = AllocateMemory(memBufferSize); //64kb
			byte[] membuf = new byte[memBufferSize];

			DateTime memReadStart = DateTime.Now;
			for (int i = 0; i < 400; i++)
				membuf = GetMemory(TestBuffer, memBufferSize);
			TimeSpan memReadElapse = DateTime.Now - memReadStart;
			string memReadSpeed = (((float)400 * (float)memBufferSize * toMegs) / (float)memReadElapse.TotalSeconds).ToString();

			DateTime memWriteStart = DateTime.Now;
            for (int i = 0; i < 400; i++)
            {
                MemoryStream.Write(TestBuffer, (int)membuf.Length, ref membuf, 0);
            }
			TimeSpan memWriteElapse = DateTime.Now - memWriteStart;
			string memWriteSpeed = (((float)400 * (float)memBufferSize * toMegs) / (float)memWriteElapse.TotalSeconds).ToString();

			FreeMemory(TestBuffer);

			// filestream tests
            XboxFileStream xbfs = new XboxFileStream(this, "E:\\test.bin");
			BinaryReader br = new BinaryReader(xbfs);
			BinaryWriter bw = new BinaryWriter(xbfs);
			byte[] filebuf = new byte[fileBufferSize];

			DateTime fileWriteStart = DateTime.Now;
			for (int i = 0; i < 16; i++)
			{
				xbfs.Position = 0;
				bw.Write(filebuf, 0, fileBufferSize);
			}
			TimeSpan fileWriteElapse = DateTime.Now - fileWriteStart;
			string fileWriteSpeed = (((float)16 * (float)fileBufferSize * toMegs) / (float)fileWriteElapse.TotalSeconds).ToString();

			DateTime fileReadStart = DateTime.Now;
			for (int i = 0; i < 16; i++)
			{
				xbfs.Position = 0;
				filebuf = br.ReadBytes(fileBufferSize);
			}
			TimeSpan fileReadElapse = DateTime.Now - fileReadStart;
			string fileReadSpeed = (((float)16 * (float)fileBufferSize * toMegs) / (float)fileReadElapse.TotalSeconds).ToString();

			xbfs.Close();
			DeleteFile("E:\\test.bin");

            // determine link speed
            //byte[] bigbuffer = new byte[fileBufferSize];
            //DateTime linkSpeedStart = DateTime.Now;
            //for (int i = 0; i < 100; i++)
            //{
            //    this.Connection.Client.Send(bigbuffer);
            //}
            //TimeSpan linkSpeedElapse = DateTime.Now - linkSpeedStart;
            //string linkSpeed = (((float)100 * (float)fileBufferSize * toMegs) / (float)linkSpeedElapse.TotalSeconds).ToString();

            // get speed
            float linkSpeed;
            if ((LinkStatus & LinkStatus.Speed100Mbps) == LinkStatus.Speed100Mbps) linkSpeed = 100.0f / 8;
            else linkSpeed = 10.0f / 8;
            if ((LinkStatus & LinkStatus.HalfDuplex) == LinkStatus.HalfDuplex) linkSpeed /= 2;

			StringBuilder results = new StringBuilder();
            results.AppendFormat("Max Theoretical Link Speed: {0}MB/s\n", linkSpeed);
			results.AppendFormat("Memory Read: {0}MB/s\n", memReadSpeed);
			results.AppendFormat("Memory Write: {0}MB/s\n", memWriteSpeed);
			results.AppendFormat("File Read: {0}MB/s\n", fileReadSpeed);
			results.AppendFormat("File Write: {0}MB/s\n", fileWriteSpeed);

			return results.ToString();
		}
		#endregion

        #region Console

        private void SendATACommand()
        {


        }

        public TrayState GetTrayState()
        {
            //// likely flags
            //0x1 = busy
            //0x10 = open
            //0x20 = dvd
            //0x40 = closed

            //// opening a closed empty tray
            //ClosedAndEmpty =  0x40	1000000
            //                  0x41	1000001
            //EjectingEmpty =   0x31	0110001
            //                  0x11	0010001
            //Open =            0x10	0010000

            //// closing an empty tray
            //Closing =         0x51    1010001
            //Loading =         0x01	0000001
            //                  0x41	1000001
            //ClosedAndEmpty =  0x40	1000000

            //// opening a closed full tray
            //ClosedAndFull =   0x60	1100000
            //EjectingFull =    0x21	0100001
            //EjectingEmpty =   0x31	0110001
            //                  0x11	0010001
            //Open =            0x10	0010000

            //// closing a full tray
            //Open =            0x10	0010000
            //Closing =         0x51	1010001     Busy | Open | Closed - weird, but interpret as it being open but in the process of closing
            //Loading =         0x01    0000001
            //                  0x61	1100001
            //ClosedAndFull =   0x60	1100000

            CallAddressEx(Kernel.HalReadSMBusValue, null, true, SMCDevices.SMBus, SMBusCommand.TrayState, 0, History.ScratchBuffer);
            return (TrayState)GetUInt16(History.ScratchBuffer);
        }

        /// <summary>
        /// Shuts down the xbox console and then turns it on again.
        /// </summary>
        public void CyclePower()
        {
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.Reset, 0, ResetCommand.PowerCycle);
            Thread.Sleep(25);   // let it shut down first
            Disconnect();
            throw new ApiException("Xbox has been shut down.  Make sure you are running in debug mode again before reconnecting.");
        }

        /// <summary>
        /// Resets the xbox console.
        /// </summary>
        public void Reset()
        {
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.Reset, 0, ResetCommand.Reset);
            Reconnect(15000);
        }

        /// <summary>
		/// Shuts down the xbox console.
		/// </summary>
		public void Shutdown()
		{
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.Reset, 0, ResetCommand.ShutDown);
            Thread.Sleep(25);  // let it shut down first
            Disconnect();
            throw new ApiException("Xbox has been shut down.  You cannot continue until you restart your xbox.");
		}

        /// <summary>
        /// Disables reset on DVD tray eject.
        /// </summary>
        public void DisableDVDEjectReset()
        {
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.OverrideResetOnTrayOpen, 0, ResetOnEjectSubCommand.Disable);
        }

        /// <summary>
        /// Enables reset on DVD tray eject.
        /// </summary>
        public void EnableDVDEjectReset()
        {
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.OverrideResetOnTrayOpen, 0, ResetOnEjectSubCommand.Enable);
        }

		/// <summary>
		/// Ejects xbox tray.
		/// </summary>
		public void EjectTray()
		{
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.DvdTrayOperation, 0, EjectSubCommand.Eject); // eject tray
			Thread.Sleep(250);
		}

		/// <summary>
		/// Loads xbox tray.
		/// </summary>
		public void LoadTray()
		{
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.DvdTrayOperation, 0, EjectSubCommand.Load); // load tray
			Thread.Sleep(250);
		}

		/// <summary>
		/// Sets the xbox LED state.
		/// </summary>
		/// <param name="state1">First LED state.</param>
		/// <param name="state2">Second LED state.</param>
		/// <param name="state3">Third LED state.</param>
		/// <param name="state4">Fourth LED state.</param>
		public void SetLEDState(LEDState state1, LEDState state2, LEDState state3, LEDState state4)
		{
			byte State = 0;
			State |= (byte)state1;
			State |= (byte)((byte)state2 >> 1);
			State |= (byte)((byte)state3 >> 2);
			State |= (byte)((byte)state4 >> 3);
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.LedStates, 0, State);
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.LedOverride, 0, LEDSubCommand.Custom);
			Thread.Sleep(10);
		}

        /// <summary>
        /// Restores the xbox LED to its default state.
        /// </summary>
        public void RestoreDefaultLEDState()
        {
            CallAddressEx(Kernel.HalWriteSMBusValue, null, false, SMCDevices.SMBus, SMBusCommand.LedOverride, 0, LEDSubCommand.Default);
            Thread.Sleep(10);
        }

        /// <summary>
        /// Gets the xbox video flags.
        /// </summary>
        /// <returns></returns>
		public VideoFlags GetVideoFlags()
		{
            CallAddressEx(Kernel.ExQueryNonVolatileSetting, null, false, 0x8, 0x10008, 0x10004, 4, 0);
			return (VideoFlags)((GetUInt32(0x10004) >> 16) & 0x5F);
		}

        /// <summary>
        /// Gets the xbox video standard.
        /// </summary>
        /// <returns></returns>
		public VideoStandard GetVideoStandard()
		{
            CallAddressEx(Kernel.ExQueryNonVolatileSetting, null, false, 0x103, 0x10008, 0x10004, 4, 0);
			return (VideoStandard)GetByte(0x10005);
		}

        /// <summary>
        /// Reads the xbox EEPROM.
        /// </summary>
        /// <returns></returns>
		public byte[] ReadEEPROM()
		{
			// build call script
            //xor	ecx, ecx
            //mov	eax, 012345678h			;temp buffer
            //readLoop:

            //pushad
            //mov	word ptr ds:[eax + ecx], 0	;ZeroMemory
            //lea	ebx, [eax + ecx]		;EEPROMDATA+i
            //push	ebx
            //push	0
            //push	ecx				;i
            //push	0A8h
            //mov	edx, 012345678h			;HalReadSMBusValue
            //call	edx
            //popad

            //push	ecx
            //mov	ecx, 010000h
            //spinwaitloop:
            //dw	090F3h				;pause	
            //loop	spinwaitloop
            //pop	ecx

            //inc	ecx
            //cmp	ecx, 0FFh
            //jl	readLoop
            //mov	eax, 02DB0000h		;fake success
            //retn	010h

            byte[] callScript = new byte[62];
            BinaryWriter call = new BinaryWriter(new System.IO.MemoryStream(callScript));
            call.BaseStream.Position = 0;
            call.Write((ushort)0xC933);
            call.Write((byte)0xB8);
            call.Write(History.ScratchBuffer);
            byte[] one = { 0x60, 0x66, 0xC7, 0x04, 0x01, 0x00, 0x00, 0x8D, 0x1C, 0x01, 0x53, 0x6A, 0x00, 0x51, 0x68, 0xA8, 0x00, 0x00, 0x00, 0xBA };
            call.Write(one);
            call.Write(Kernel.HalReadSMBusValue);
            byte[] two = { 0xFF, 0xD2, 0x61, 0x51, 0xB9 };
            call.Write(two);
            call.Write((int)0x100);   // modify spin count if we find out that its not reading all of the eeprom
            byte[] three = { 0xF3, 0x90, 0xE2, 0xFC, 0x59, 0x41, 0x81, 0xF9, 0xFF, 0x00, 0x00, 0x00, 0x7C, 0xD1, 0xB8, 0x00, 0x00, 0xDB, 0x02, 0xC2, 0x10, 0x00 };
            call.Write(three);  // change 0xF3 to 0x90 to get rid of the pause instruction
            call.Close();

			// inject script
            SetMemory(XboxHistory.ScriptBufferAddress, callScript);

            // execute script via hijacked crashdump function
            SendCommand("crashdump");

            return GetMemory(History.ScratchBuffer, 256);
		}
		#endregion

		#region Remote Execution
        // since this code gets called by the xbdm module and executed within its own thread, 
        // you won't be able to call certain pieces of code that require execution to take place
        // in a different thread (DirectX, some kernel functions, etc...)


		/// <summary>
		/// Simple function call with optional return value.  Average execution time of 1.3 ms without return or 1.75ms with return.
		/// </summary>
		/// <param name="address">Xbox procedure address.</param>
		/// <returns></returns>
		/// <remarks>0x10000 is reserved for function use.</remarks>
		public uint CallAddress(uint address, bool returnValue)
		{
			// call address and store result
            //pushad
            //mov   eax, address
			//call	eax
			//mov	dword ptr ds:[010000h], eax
            //popad
            //mov   eax, 02DB0000h  ;fake success
			//retn  10h
            using (System.IO.MemoryStream callBuffer = new System.IO.MemoryStream())
            using (BinaryWriter callScript = new BinaryWriter(callBuffer))
            {
                // build call script
                callBuffer.Position = 0;
                callScript.Write((byte)0x60); // pushad
                callScript.Write((byte)0xB8);
                callScript.Write(address);
                callScript.Write((ushort)0xD0FF);   // call
                callScript.Write((byte)0xA3); // mov dst, eax
                callScript.Write(0x10000);
                callScript.Write((byte)0x61); // popad
                byte[] script = { 0xB8, 0x00, 0x00, 0xDB, 0x02, 0xC2, 0x10, 0x00 };
                callScript.Write(script);

                // inject call script
                if (callBuffer.Length > XboxHistory.ScriptBufferSize) throw new Exception("Script too big. Try allocating more memory and specifying new script buffer information.");
                SetMemory(XboxHistory.ScriptBufferAddress, callBuffer.ToArray());
            }

            // execute script via hijacked crashdump function
            SendCommand("crashdump");

			// return the value of eax after the call
            if (returnValue) return GetUInt32(0x10000);
            else return 0;
		}


        //CallAddressEx usage
        //public void Function(Arg1, Arg2, Arg3);
        //CallAddressEx(FunctionAddress, null, false, Arg1, Arg2, Arg3);
        //assembly:
        //push  Arg3
        //push  Arg2
        //push  Arg1
        //call  Function
		/// <summary>
		/// Extended function call with optional context, arguments, and return value.  Average execution time of 1.3ms without return or 1.75ms with return.
        /// </summary>
		/// <param name="address">Xbox procedure address.</param>
		/// <param name="context">Cpu context.  This parameter may be null.</param>
		/// <param name="pushArgs">Arguments to be pushed before the call is made.  These are optional of course.</param>
		/// <returns></returns>
		public uint CallAddressEx(uint address, CPUContext context, bool returnValue, params object[] pushArgs)
		{
			// buffer to hold our call data
            using (System.IO.MemoryStream callScript = new System.IO.MemoryStream())
            using (BinaryWriter call = new BinaryWriter(callScript))
            {
                call.Write((byte)0x60); // pushad

                // push arguments in reverse order
                for (int i = pushArgs.Length - 1; i >= 0; i--)
                {
                    call.Write((byte)0x68); //push
                    call.Write(Util.ObjectToDwordBytes(pushArgs[i]));    // hack: improper conversion of floating point values
                }

                if (context != null)
                {
                    // assign registers
                    if (context.Eax != null)
                    {
                        call.Write((byte)0xB8); //mov eax
                        call.Write(Util.ObjectToDwordBytes(context.Eax));
                    }
                    if (context.Ebx != null)
                    {
                        call.Write((byte)0xBB); //mov ebx
                        call.Write(Util.ObjectToDwordBytes(context.Ebx));
                    }
                    if (context.Ecx != null)
                    {
                        call.Write((byte)0xB9); //mov ecx
                        call.Write(Util.ObjectToDwordBytes(context.Ecx));
                    }
                    if (context.Edx != null)
                    {
                        call.Write((byte)0xBA); //mov edx
                        call.Write(Util.ObjectToDwordBytes(context.Edx));
                    }
                    if (context.Esi != null)
                    {
                        call.Write((byte)0xBE); //mov esi
                        call.Write(Util.ObjectToDwordBytes(context.Esi));
                    }
                    if (context.Edi != null)
                    {
                        call.Write((byte)0xBF); //mov edi
                        call.Write(Util.ObjectToDwordBytes(context.Edi));
                    }
                    if (context.Esp != null)
                    {
                        call.Write((byte)0xBC); //mov esp
                        call.Write(Util.ObjectToDwordBytes(context.Esp));
                    }
                    if (context.Ebp != null)
                    {
                        call.Write((byte)0xBD); //mov ebp
                        call.Write(Util.ObjectToDwordBytes(context.Ebp));
                    }

                    // assign xmm registers
                    // remember that its a pointer, not a value you are db'ing
                    // so we need to dump the values somewhere, then store the pointers to those...

                    uint XmmContextBuffer = 0x10004;
                    if (context.Xmm0 != null)
                    {
                        SetMemory(XmmContextBuffer, Convert.ToSingle(context.Xmm0));    // shouldnt assume its floating point input but meh
                        call.Write(0x05100FF3); //movss xmm0
                        call.Write(XmmContextBuffer);   //dword ptr ds:[addr]
                    }
                    if (context.Xmm1 != null)
                    {
                        SetMemory(XmmContextBuffer + 4, Convert.ToSingle(context.Xmm1));
                        call.Write(0x0D100FF3); //movss xmm1
                        call.Write(XmmContextBuffer + 4);
                    }
                    if (context.Xmm2 != null)
                    {
                        SetMemory(XmmContextBuffer + 8, Convert.ToSingle(context.Xmm2));
                        call.Write(0x15100FF3); //movss xmm2
                        call.Write(XmmContextBuffer + 8);
                    }
                    if (context.Xmm3 != null)
                    {
                        SetMemory(XmmContextBuffer + 12, Convert.ToSingle(context.Xmm3));
                        call.Write(0x1D100FF3); //movss xmm3
                        call.Write(XmmContextBuffer + 12);
                    }
                    if (context.Xmm4 != null)
                    {
                        SetMemory(XmmContextBuffer + 16, Convert.ToSingle(context.Xmm4));
                        call.Write(0x25100FF3); //movss xmm4
                        call.Write(XmmContextBuffer + 16);
                    }
                    if (context.Xmm5 != null)
                    {
                        SetMemory(XmmContextBuffer + 20, Convert.ToSingle(context.Xmm5));
                        call.Write(0x2D100FF3); //movss xmm5
                        call.Write(XmmContextBuffer + 20);
                    }
                    if (context.Xmm6 != null)
                    {
                        SetMemory(XmmContextBuffer + 24, Convert.ToSingle(context.Xmm6));
                        call.Write(0x35100FF3); //movss xmm6
                        call.Write(XmmContextBuffer + 24);
                    }
                    if (context.Xmm7 != null)
                    {
                        SetMemory(XmmContextBuffer + 28, Convert.ToSingle(context.Xmm7));
                        call.Write(0x3D100FF3); //movss xmm7
                        call.Write(XmmContextBuffer + 28);
                    }
                }

                // call address and store result
                //pushad
                //push  Arg3
                //push  Arg2
                //push  Arg1
                //call	dword ptr ds:[CallAddress]
                //mov	dword ptr ds:[ReturnAddress], eax
                //popad
                //mov   eax, 02DB0000h  ;fake success
                //retn  10h
                call.Write((ushort)0x15FF);
                call.Write((uint)(XboxHistory.ScriptBufferAddress + call.BaseStream.Position + 18));
                call.Write((byte)0xA3);
                call.Write((uint)0x10000);
                call.Write((byte)0x61); // popad
                call.Write(0xDB0000B8);
                call.Write(0x0010C202);
                call.Write(address);

                // inject call script
                if (callScript.Length > XboxHistory.ScriptBufferSize) throw new Exception("Script too big. Try allocating more memory and specifying new script buffer information.");
                SetMemory(XboxHistory.ScriptBufferAddress, callScript.ToArray());
            }

            // execute script via hijacked crashdump function
            SendCommand("crashdump");

			// return the value of eax after the call
            if (returnValue) return GetUInt32(0x10000);
            else return 0;
		}
		#endregion

        #region Debugging

        /// <summary>
        /// Some debugging events require title execution to be suspended.
        /// But there are some debugging events where thread suspension is an option.
        /// For these events, you can elect to have the debugging subsystem suspend title execution by calling this function.
        /// </summary>
        /// <param name="flags"></param>
        public void StopOn(StopOnFlags flags)
        {
            SendCommand("stopon {0}", flags.ToString().Replace(",", ""));
        }

        /// <summary>
        /// Removes all breakpoints in an xbox title.
        /// </summary>
        public void ClearAllBreakpoints()
        {
            SendCommand("break clearall");
        }

        /// <summary>
        /// Sets a hardware breakpoint that suspends title execution when a particular section of memory is referenced.
        /// </summary>
        /// <param name="address">Address of the memory to watch.</param>
        /// <param name="size">The size, in bytes, of the memory to be watched.
        /// The only allowable values for this parameter are 1, 2, and 4.</param>
        /// <param name="type">Type of access to watch for.</param>
        public void SetBreakPoint(uint address, uint size, BreakpointType type)
        {
            SendCommand("break addr={0} size={1} {2}", address, size, type.ToString().Replace(",", ""));
        }

        /// <summary>
        /// Sets a breakpoint in an xbox title.
        /// </summary>
        /// <param name="address">Address where you would like to set a breakpoint.</param>
        public void SetBreakPoint(uint address)
        {
            SetBreakPoint(address, 1, BreakpointType.ReadWrite | BreakpointType.Execute);
        }

        /// <summary>
        /// Removes a breakpoint in an xbox title.
        /// </summary>
        /// <param name="address">Address where you would like to remove a breakpoint.</param>
        public void RemoveBreakPoint(uint address)
        {
            SendCommand("break clear addr={0}", address);
        }

        /// <summary>
        /// Sends a request to the xbox that the specified thread break as soon as possible.
        /// </summary>
        /// <param name="thread">ID of the thread to be halted. Send 0 as the thread ID to have the debugging subsystem select a thread to break into.</param>
        public void HaltThread(uint thread)
        {
            SendCommand("halt thread={0}", thread);
        }

        /// <summary>
        /// Resumes the execution of an xbox thread that has been stopped.
        /// </summary>
        /// <param name="thread">Thread ID.</param>
        public void ContinueThread(uint thread)
        {
            try { SendCommand("continue thread={0}", thread); }
            catch { }
        }

        /// <summary>
        /// Resumes the execution of all xbox threads that have been stopped.
        /// </summary>
        public void ContinueAllThreads()
        {
            foreach (ThreadInfo thread in Threads)
                try { SendCommand("continue thread={0}", thread.ID); }
                catch { /* 408- not stopped */ }
        }

        /// <summary>
        /// Suspends a given xbox thread.
        /// </summary>
        /// <param name="thread">ID of the thread to suspend.</param>
        public void SuspendThread(uint thread)
        {
            SendCommand("suspend thread={0}", thread);
        }

        // SuspendAllThreads

        /// <summary>
        /// Resumes a given xbox thread.
        /// </summary>
        /// <param name="thread">ID of the thread to resume.</param>
        public void ResumeThread(uint thread)
        {
            SendCommand("resume thread={0}", thread);
        }

        // ResumeAllThreads

        /// <summary>
        /// Determines whether the specified thread is stopped.
        /// </summary>
        /// <param name="thread"></param>
        /// <returns></returns>
        public bool IsThreadStopped(uint thread)
        {
            try
            {
                StatusResponse res = SendCommand("isstopped thread={0}", thread); return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// If the specified thread is stopped this will return information about the circumstances that forced the thread to stop.
        /// </summary>
        /// <param name="thread"></param>
        /// <returns></returns>
        public string GetThreadStopInfo(uint thread)
        {
            try
            {
                return SendCommand("isstopped thread={0}", thread).Message;
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Suspends all xbox title threads.
        /// </summary>
        public void Pause()
        {
            if (connection != null)
            {
                try { SendCommand("stop"); }
                catch { }
            }
            else throw new NoConnectionException();
        }

        /// <summary>
        /// Resumes all xbox title threads.
        /// </summary>
        public void Continue()
        {
            if (connection != null)
            {
                try { SendCommand("go"); }
                catch { }
            }
            else throw new NoConnectionException();
        }
        #endregion

    };
}