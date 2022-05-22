using System.Runtime.InteropServices;
using CoreGraphics;

namespace AvaloniaVS.Mac.Utils;

public class Screen
{
	[DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
	static extern uint CGMainDisplayID();

	[System.Runtime.InteropServices.DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern CoreGraphics.CGSize CGDisplayScreenSize(uint display);

	[DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
	static extern int CGDisplayPixelsHigh(uint display);

	[DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
	static extern int CGDisplayPixelsWide(uint display);

	/// <summary>
	/// Returns the display width and height in pixel units.
	/// </summary>
	public static CGSize Resolution
	{
		get
		{
			var width = CGDisplayPixelsWide(CGMainDisplayID());
			var height = CGDisplayPixelsHigh(CGMainDisplayID());
			CGSize screenRes = new CGSize(width, height);

			return screenRes;
		}
	}

	/// <summary>
	/// Returns the width and height of a display in millimeters.
	/// </summary>
	public static CGSize Size => CGDisplayScreenSize(CGMainDisplayID());

	public static int Dpi => 218;

	/// <summary>
	/// https://support.apple.com/en-gb/HT202471#:~:text=Native%20resolution%3A%202304%20x%201440%20at%20226%20pixels%20per%20inch.
	/// </summary>
	private Dictionary<string, int> dpiMap = new Dictionary<string, int>()
	{
		// Macbook Pros 
		{ "Mbp16_2021", 254 },
		{ "Mbp14_2021", 254},
		{ "Mbp16_2019", 226 },
		{ "Mbp15_2015", 220 },
		{ "Mbp13_2015", 227 },

		//Macbook Airs
		{ "Mba16_2018", 227 },

		//Macbook
		{ "Mb16_2015", 226 },

		//iMac
		{ "iMac24_2021", 218 },
		{ "iMac27_2014", 218 },
		{ "iMac21_2015", 218 },
	};
}
