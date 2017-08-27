//*************************************************************************************************
// DEBUT DU FICHIER
//*************************************************************************************************

//*************************************************************************************************
// Nom           : Ssd1306.cs
// Auteur        : Nicolas Dagnas
// Description   : Déclaration de l'objet Ssd1306
// Environnement : Visual Studio 2015
// Créé le       : 25/08/2017
// Modifié le    : 27/08/2017
//-------------------------------------------------------------------------------------------------
// Inspiré de    : https://github.com/stefangordon/IoTCore-SSD1306-Driver
//*************************************************************************************************

//-------------------------------------------------------------------------------------------------
#region Using directives
//-------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Threading.Tasks;
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
using Windows.Devices.I2c;
using Windows.Devices.Enumeration;
//-------------------------------------------------------------------------------------------------
#endregion
//-------------------------------------------------------------------------------------------------

//*************************************************************************************************
// Début du bloc "Windows.Devices.Gpio"
//*************************************************************************************************
namespace Windows.Devices.Gpio
    {

    //   ####   ####  ####     #   ###    ###    ####
    //  #      #      #   #   ##  #   #  #   #  #
    //   ###    ###   #   #  # #     #   #   #   ###
    //      #      #  #   #    #  #   #  #   #  #   #
    //  ####   ####   ####     #   ###    ###    ###

	//*********************************************************************************************
	// Classe Ssd1308
	//*********************************************************************************************
	#region // Déclaration et Implémentation de l'Objet
	//---------------------------------------------------------------------------------------------
	/// <summary>
	/// Gère un éceanr led Kuman 168*64
	/// </summary>
	//---------------------------------------------------------------------------------------------
    public static class Ssd1308
        {
	    //-----------------------------------------------------------------------------------------
        // Section des Constantes
	    //-----------------------------------------------------------------------------------------
        // Largeur de l'écran
        private const UInt32 SCREEN_WIDTH_PX    = 128;
	    //-----------------------------------------------------------------------------------------
        // Hauteur de l'écran
        private const UInt32 SCREEN_HEIGHT_PX   = 64;
	    //-----------------------------------------------------------------------------------------
	    // Nombre de pages
	    private const UInt32 SCREEN_PAGES_COUNT = 8;
	    //-----------------------------------------------------------------------------------------
	    // Hauteur d'une page
	    private const UInt32 SCREEN_HEIGHT_PAGE = SCREEN_HEIGHT_PX / SCREEN_PAGES_COUNT;
	    //-----------------------------------------------------------------------------------------

	    //-----------------------------------------------------------------------------------------
	    // Section des Buffers
	    //-----------------------------------------------------------------------------------------
        // Buffer interne en 2 dimensions
        static bool[,] DisplayBuffer           = new bool[SCREEN_WIDTH_PX, SCREEN_HEIGHT_PX];
	    //-----------------------------------------------------------------------------------------
        // A temporary buffer used to prepare graphics data for sending over i2c          */
        static byte[ ] SerializedDisplayBuffer = new byte[SCREEN_WIDTH_PX * SCREEN_HEIGHT_PAGE];
	    //-----------------------------------------------------------------------------------------

	    //-----------------------------------------------------------------------------------------
	    // Section des Commandes
	    //-----------------------------------------------------------------------------------------
        // Turns the display off
        private static readonly byte[] CMD_DISPLAY_OFF   = { 0xAE };
	    //-----------------------------------------------------------------------------------------
        // Turns the display on
        private static readonly byte[] CMD_DISPLAY_ON    = { 0xAF };
	    //-----------------------------------------------------------------------------------------
        // Turn on internal charge pump to supply power to display
        private static readonly byte[] CMD_CHARGEPUMP_ON = { 0x8D, 0x14 };
	    //-----------------------------------------------------------------------------------------
        // Horizontal memory mode
        private static readonly byte[] CMD_MEMADDRMODE   = { 0x20, 0x00 };
	    //-----------------------------------------------------------------------------------------
        // Remaps the segments, which has the effect of mirroring the display horizontally
        private static readonly byte[] CMD_SEGREMAP      = { 0xA1 };
	    //-----------------------------------------------------------------------------------------
        // Set the COM scan direction to inverse, which flips the screen vertically
        private static readonly byte[] CMD_COMSCANDIR    = { 0xC8 };
	    //-----------------------------------------------------------------------------------------
        // Reset the column address pointer
        private static readonly byte[] CMD_RESETCOLADDR  = { 0x21, 0x00, 0x7F };
	    //-----------------------------------------------------------------------------------------
        // Reset the page address pointer
        private static readonly byte[] CMD_RESETPAGEADDR = { 0x22, 0x00, 0x07 };
	    //-----------------------------------------------------------------------------------------

	    //-----------------------------------------------------------------------------------------
        // Section des Attributs
	    //-----------------------------------------------------------------------------------------
		private static bool      DevicePresenceException = false;
		private static bool      Initializing            = false;
		private static bool      SegmentsUpdated         = false;
		private static I2cDevice I2cCardDevice           = null;
		private static DateTime  LastCheck               = DateTime.MinValue;
	    //-----------------------------------------------------------------------------------------

    	//*****************************************************************************************
		#region // Section des Procédures Privées
		//-----------------------------------------------------------------------------------------

    	//*****************************************************************************************
		/// <summary>
		/// Send graphics data to the screen.
		/// </summary>
		/// <param name="Data"></param>
		//-----------------------------------------------------------------------------------------
		private static void DisplaySendData ( byte[] Data )
			{
			//-------------------------------------------------------------------------------------
			if ( I2cCardDevice != null )
				{
				byte[] CommandBuffer = new byte[Data.Length + 1];

				Data.CopyTo ( CommandBuffer, 1);

				CommandBuffer[0] = 0x40;

				I2cCardDevice.Write ( CommandBuffer );
				}
			//-------------------------------------------------------------------------------------
			}
    	//*****************************************************************************************

    	//*****************************************************************************************
		/// <summary>
		/// Send commands to the screen.
		/// </summary>
		/// <param name="Command"></param>
		//-----------------------------------------------------------------------------------------
		private static void DisplaySendCommand ( byte[] Command )
			{
			//-------------------------------------------------------------------------------------
			if ( I2cCardDevice != null )
				{
				byte[] CommandBuffer = new byte[Command.Length + 1];

				Command.CopyTo ( CommandBuffer, 1 );

				CommandBuffer[0] = 0x00;

				I2cCardDevice.Write ( CommandBuffer );
				}
			//-------------------------------------------------------------------------------------
			}
    	//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
    	//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Initialise le bus de communication avec la carte de gestion de l'heure.
		/// </summary>
		//-----------------------------------------------------------------------------------------
		public static async Task<bool> Initialize ()
			{
            //-------------------------------------------------------------------------------------
			if ( ! Initializing && I2cCardDevice == null && ! DevicePresenceException )
				{
	            //---------------------------------------------------------------------------------
				Initializing = true;
	            //---------------------------------------------------------------------------------

	            //---------------------------------------------------------------------------------
				try
					{
		            //-----------------------------------------------------------------------------
					var QuerySyntaxString = I2cDevice.GetDeviceSelector ( "I2C1" );

					var DeviceIds = await DeviceInformation.FindAllAsync ( QuerySyntaxString );

					I2cConnectionSettings ConnSettings = new I2cConnectionSettings ( 0x3C );

                    ConnSettings.BusSpeed    = I2cBusSpeed.FastMode;
                    ConnSettings.SharingMode = I2cSharingMode.Shared;

					I2cCardDevice = await I2cDevice.FromIdAsync ( DeviceIds[0].Id, ConnSettings );
		            //-----------------------------------------------------------------------------

		            //-----------------------------------------------------------------------------
					// Turn on the internal charge pump to provide power to the screen
					DisplaySendCommand ( CMD_CHARGEPUMP_ON );

					// Set the addressing mode to "horizontal"
					DisplaySendCommand ( CMD_MEMADDRMODE );

					// Flip the display horizontally, so it's easier to read on the breadboard
					DisplaySendCommand ( CMD_SEGREMAP );

					// Flip the display vertically, so it's easier to read on the breadboard
					DisplaySendCommand ( CMD_COMSCANDIR );

					// Turn the display on
					DisplaySendCommand ( CMD_DISPLAY_ON );

					// Reset the column address pointer back to 0
					DisplaySendCommand ( CMD_RESETCOLADDR );

					// Reset the page address pointer back to 0
					DisplaySendCommand ( CMD_RESETPAGEADDR );
		            //-----------------------------------------------------------------------------
					}
	            //---------------------------------------------------------------------------------
				catch ( FileNotFoundException )
					{ I2cCardDevice = null; DevicePresenceException = true; }
	            //---------------------------------------------------------------------------------
				catch {}
	            //---------------------------------------------------------------------------------
				finally { Initializing = false; }
	            //---------------------------------------------------------------------------------
				}
            //-------------------------------------------------------------------------------------

            //-------------------------------------------------------------------------------------
			return true;
            //-------------------------------------------------------------------------------------
			}
    	//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Initialise le bus de communication avec la carte de gestion de l'heure.
		/// </summary>
		//-----------------------------------------------------------------------------------------
		public static async void InitializeAsync ()
			{
            //-------------------------------------------------------------------------------------
			if ( ! Initializing && I2cCardDevice == null && ! DevicePresenceException )
				{
	            //---------------------------------------------------------------------------------
				Initializing = true;
	            //---------------------------------------------------------------------------------

	            //---------------------------------------------------------------------------------
				try
					{
		            //-----------------------------------------------------------------------------
					var QuerySyntaxString = I2cDevice.GetDeviceSelector ( "I2C1" );

					var DeviceIds = await DeviceInformation.FindAllAsync ( QuerySyntaxString );

					I2cConnectionSettings ConnSettings = new I2cConnectionSettings ( 0x3C );

                    ConnSettings.BusSpeed    = I2cBusSpeed.FastMode;
                    ConnSettings.SharingMode = I2cSharingMode.Shared;

					I2cCardDevice = await I2cDevice.FromIdAsync ( DeviceIds[0].Id, ConnSettings );
		            //-----------------------------------------------------------------------------

		            //-----------------------------------------------------------------------------
					// Turn on the internal charge pump to provide power to the screen
					DisplaySendCommand ( CMD_CHARGEPUMP_ON );

					// Set the addressing mode to "horizontal"
					DisplaySendCommand ( CMD_MEMADDRMODE );

					// Flip the display horizontally, so it's easier to read on the breadboard
					DisplaySendCommand ( CMD_SEGREMAP );

					// Flip the display vertically, so it's easier to read on the breadboard
					DisplaySendCommand ( CMD_COMSCANDIR );

					// Turn the display on
					DisplaySendCommand ( CMD_DISPLAY_ON );

					// Reset the column address pointer back to 0
					DisplaySendCommand ( CMD_RESETCOLADDR );

					// Reset the page address pointer back to 0
					DisplaySendCommand ( CMD_RESETPAGEADDR );
		            //-----------------------------------------------------------------------------
					}
	            //---------------------------------------------------------------------------------
				catch ( FileNotFoundException )
					{ I2cCardDevice = null; DevicePresenceException = true; }
	            //---------------------------------------------------------------------------------
				catch {}
	            //---------------------------------------------------------------------------------
				finally { Initializing = false; }
	            //---------------------------------------------------------------------------------
				}
            //-------------------------------------------------------------------------------------
			}
    	//*****************************************************************************************

    	//*****************************************************************************************
		/// <summary>
		/// Indique si la puce de gestion de l'heure est présente.
		/// </summary>
	    //-----------------------------------------------------------------------------------------
		public static bool DevicePresent { get { return ( I2cCardDevice != null ); } }
    	//*****************************************************************************************
    	
    	//*****************************************************************************************
    	private static void TogglePixel ( uint X, uint Y, bool Value )
    		{
    		if ( X >= SCREEN_WIDTH_PX || Y >= SCREEN_HEIGHT_PX ) return;
    		
    		if ( DisplayBuffer[X, Y] != Value )
				SegmentsUpdated = true;
    		
    		DisplayBuffer[X, Y] = Value;
    		}
    	//*****************************************************************************************
    	
    	//*****************************************************************************************
    	public static void DrawHLine ( uint X, uint Y, uint Width )
    		{
    		for ( uint _X = X ; _X < SCREEN_WIDTH_PX && _X < X + Width ; _X ++ )
    			TogglePixel ( _X, Y, true );
    		}
    	//*****************************************************************************************

    	//*****************************************************************************************
    	public static void DrawVLine ( uint X, uint Y, uint Height )
    		{
    		for ( uint _Y = Y ; _Y < SCREEN_HEIGHT_PX && _Y < Y + Height ; _Y ++ )
    			TogglePixel ( X, _Y, true );
    		}
    	//*****************************************************************************************

    	//*****************************************************************************************
    	public static void DrawRectangle ( uint X, uint Y, uint Width, uint Height )
    		{
    		for ( uint _X = X ; _X < SCREEN_WIDTH_PX && _X < X + Width ; _X ++ )
	    		for ( uint _Y = Y ; _Y < SCREEN_HEIGHT_PX && _Y < Y + Height ; _Y ++ )
					TogglePixel ( _X, _Y, true );
    		}
    	//*****************************************************************************************

    	//*****************************************************************************************
		public static void ClearDisplay ()
			{ Array.Clear ( DisplayBuffer, 0, DisplayBuffer.Length ); }
    	//*****************************************************************************************

    	//*****************************************************************************************
		public static void DrawChar ( char Character, DisplayFont Font, uint X, uint Y )
			{
			FontCharacterDescriptor Cd = ( Font != null ) ? Font.GetChar ( Character ) : null;
			
			if ( Cd == null ) return;

			for ( int SegX = 0 ; SegX < Cd.Width ; SegX ++ )
				{
				Int64 Segment = Cd.Segments[SegX];

				for ( int SegY = 0 ; SegY < Cd.Height ; SegY ++ )
					{
					uint PixelX = (uint)( X + SegX );
					uint PixelY = (uint)( Y + SegY );
					
					if ( ( Segment & ( 0x00000001 << SegY ) ) != 0 )
						{
						TogglePixel ( PixelX, PixelY, true );
						}
					else
						{
						if ( PixelX < X + Cd.Width && PixelY < Y + Cd.Height )
							TogglePixel ( PixelX, PixelY, false );
						}
					}
				}
			}
    	//*****************************************************************************************

    	//*****************************************************************************************
		/// <summary>
		/// Writes the Display Buffer out to the physical screen for display.
		/// </summary>
		//-----------------------------------------------------------------------------------------
		public static void DisplayUpdate ()
			{
			//-------------------------------------------------------------------------------------
			if ( ! SegmentsUpdated ) return;
			
			int Index = 0;
			
			for ( int PageIndex = 0 ; PageIndex < SCREEN_PAGES_COUNT ; PageIndex ++ )
				{
				for ( int PixelX = 0 ; PixelX < SCREEN_WIDTH_PX ; PixelX ++ )
					{
					byte Pixel = 0x00;
					
					for ( uint PixelY = SCREEN_HEIGHT_PAGE ; PixelY > 0 ; PixelY -- )
						{
						Pixel = (byte)( (int)Pixel << 1 );

						if ( DisplayBuffer[PixelX, ( PageIndex * 8 ) + ( PixelY - 1 )] )
							Pixel |= 0x01;
						}

					SerializedDisplayBuffer[Index ++] = Pixel;
					}
				}

			// Write the data out to the screen

			DisplaySendData ( SerializedDisplayBuffer );
			
			SegmentsUpdated = false;
			//-------------------------------------------------------------------------------------
			}
    	//*****************************************************************************************

        }
	//---------------------------------------------------------------------------------------------
	#endregion
	//*********************************************************************************************

	//*********************************************************************************************
	#region // Polices, en cours ...
	//---------------------------------------------------------------------------------------------
	public class FontCharacterDescriptor
		{
		public char Character;
		public uint Width;
		public uint Height;
		public Int64[] Segments;

		public FontCharacterDescriptor ( char Character, uint Width, uint Height, Int64[] Segments )
			{
			this.Character = Character;
			this.Width     = Width;
			this.Height    = Height;
			this.Segments  = Segments;
			}
		}

	public class DisplayFont
		{
		protected FontCharacterDescriptor[] FontTable { get; set; }

		internal FontCharacterDescriptor GetChar ( char Value )
			{
			if ( FontTable != null )
				{
				foreach ( FontCharacterDescriptor Descriptor in FontTable )
					{
					if ( Descriptor.Character == Value ) return Descriptor;
					}
				}
			
			return null;
			}
		}

	public class DisplayFontSpecial : DisplayFont
		{
		static DisplayFontSpecial () { Font = new DisplayFontSpecial (); }

		private DisplayFontSpecial ()
			{
			base.FontTable = new FontCharacterDescriptor []
				{
				new FontCharacterDescriptor ( '°', 4,  4, new Int64[] { 0x06, 0x09, 0x09, 0x06 } ), //0x06, 0x09, 0x09, 0x06, 0x00, 0x00, 0x00, 0x00 } ),
				new FontCharacterDescriptor ( '%', 7, 12, new Int64[] { 0x0006, 0x0089, 0x00c9, 0x0666, 0x0930, 0x0910, 0x0600, 0x0000 } ), //0x06, 0x00, 0x89, 0x00, 0xc9, 0x00, 0x66, 0x06, 0x30, 0x09, 0x10, 0x09, 0x00, 0x06, 0x00, 0x00 } ),
				new FontCharacterDescriptor ( '/', 3,  6, new Int64[] { 0x30, 0x0c, 0x03, 0x00 } ), //0x30, 0x0c, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00 } ),

				};
			}

		public static DisplayFont Font { get; private set; }
		}

	public class DisplayFontSize10 : DisplayFont
		{
		static DisplayFontSize10 () { Font = new DisplayFontSize10 (); }

		private DisplayFontSize10 ()
			{
			base.FontTable = new FontCharacterDescriptor []
				{
				new FontCharacterDescriptor ( '_', 8, 10, new Int64[] { 0x0200, 0x0200, 0x0200, 0x0200, 0x0200, 0x0200, 0x0200, 0x0200 } ), //0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02 } ),
				new FontCharacterDescriptor ( '0', 8, 10, new Int64[] { 0x00fc, 0x01fe, 0x0387, 0x0303, 0x0303, 0x03c7, 0x01fe, 0x00fc } ), //0xfc, 0x00, 0xfe, 0x01, 0x87, 0x03, 0x03, 0x03, 0x03, 0x03, 0x87, 0x03, 0xfe, 0x01, 0xfc, 0x00 } ),
				new FontCharacterDescriptor ( '1', 8, 10, new Int64[] { 0x0300, 0x0306, 0x0306, 0x03ff, 0x03ff, 0x0300, 0x0300, 0x0300 } ), //0x00, 0x03, 0x06, 0x03, 0x06, 0x03, 0xff, 0x03, 0xff, 0x03, 0x00, 0x03, 0x00, 0x03, 0x00, 0x03 } ),
				new FontCharacterDescriptor ( '2', 8, 10, new Int64[] { 0x0304, 0x0306, 0x0387, 0x03c3, 0x0363, 0x033f, 0x031e, 0x030c } ), //0x04, 0x03, 0x86, 0x03, 0xc7, 0x03, 0xe3, 0x03, 0x73, 0x03, 0x3f, 0x03, 0x1e, 0x03, 0x0c, 0x03 } ),
				new FontCharacterDescriptor ( '3', 8, 10, new Int64[] { 0x0186, 0x0186, 0x0303, 0x0333, 0x0333, 0x0333, 0x01fe, 0x01ce } ), //0x86, 0x01, 0x86, 0x01, 0x03, 0x03, 0x33, 0x03, 0x33, 0x03, 0x33, 0x03, 0xff, 0x03, 0xce, 0x01 } ),
				new FontCharacterDescriptor ( '4', 8, 10, new Int64[] { 0x0070, 0x0078, 0x006c, 0x0066, 0x03ff, 0x03ff, 0x0060, 0x0060 } ), //0x70, 0x00, 0x78, 0x00, 0x6c, 0x00, 0x66, 0x00, 0xff, 0x03, 0xff, 0x03, 0x60, 0x00, 0x60, 0x00 } ),
				new FontCharacterDescriptor ( '5', 8, 10, new Int64[] { 0x018e, 0x039f, 0x031b, 0x031b, 0x031b, 0x033b, 0x03f3, 0x01e0 } ), //0x8e, 0x01, 0x9f, 0x03, 0x1b, 0x03, 0x1b, 0x03, 0x1b, 0x03, 0x3b, 0x03, 0xf3, 0x03, 0xe0, 0x01 } ),
				new FontCharacterDescriptor ( '6', 8, 10, new Int64[] { 0x00fc, 0x01fe, 0x0337, 0x0333, 0x0333, 0x0333, 0x01e3, 0x00c0 } ), //0xfc, 0x00, 0xfe, 0x01, 0x37, 0x03, 0x33, 0x03, 0x33, 0x03, 0x33, 0x03, 0xe3, 0x01, 0xc0, 0x00 } ),
				new FontCharacterDescriptor ( '7', 8, 10, new Int64[] { 0x0000, 0x0003, 0x0003, 0x0383, 0x03f3, 0x007f, 0x000f, 0x0003 } ), //0x00, 0x00, 0x03, 0x00, 0x03, 0x00, 0x83, 0x03, 0xf3, 0x03, 0x7f, 0x00, 0x0f, 0x00, 0x03, 0x00 } ),
				new FontCharacterDescriptor ( '8', 8, 10, new Int64[] { 0x00cc, 0x01ce, 0x03ff, 0x0333, 0x0333, 0x03ff, 0x01ce, 0x00cc } ), //0xcc, 0x00, 0xce, 0x01, 0xff, 0x03, 0x33, 0x03, 0x33, 0x03, 0xff, 0x03, 0xce, 0x01, 0xcc, 0x00 } ),
				new FontCharacterDescriptor ( '9', 8, 10, new Int64[] { 0x000c, 0x031e, 0x0333, 0x0333, 0x0333, 0x03b3, 0x01fe, 0x00fc } ), //0x0c, 0x00, 0x1e, 0x03, 0x33, 0x03, 0x33, 0x03, 0x33, 0x03, 0x33, 0x03, 0xbe, 0x01, 0xfc, 0x00 } ),
				};
			}

		public static DisplayFont Font { get; private set; }
		}

	public class DisplayFontSize11 : DisplayFont
		{
		static DisplayFontSize11 () { Font = new DisplayFontSize11 (); }

		private DisplayFontSize11 ()
			{
			base.FontTable = new FontCharacterDescriptor []
				{
				new FontCharacterDescriptor ( '_', 8, 11, new Int64[] { 0x0400, 0x0400, 0x0400, 0x0400, 0x0400, 0x0400, 0x0400, 0x0400 } ), //0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02 } ),
				new FontCharacterDescriptor ( '0', 8, 11, new Int64[] { 0x01fc, 0x03fe, 0x0707, 0x0603, 0x0603, 0x0707, 0x03fe, 0x01fc } ), //0xfc, 0x01, 0xfe, 0x03, 0x07, 0x07, 0x03, 0x06, 0x03, 0x06, 0x07, 0x07, 0xfe, 0x03, 0xfc, 0x01 } ),
				new FontCharacterDescriptor ( '1', 8, 11, new Int64[] { 0x0606, 0x0606, 0x0606, 0x07ff, 0x07ff, 0x0600, 0x0600, 0x0600 } ), //0x00, 0x06, 0x06, 0x06, 0x06, 0x06, 0xff, 0x07, 0xff, 0x07, 0x00, 0x06, 0x00, 0x06, 0x00, 0x06 } ),
				new FontCharacterDescriptor ( '2', 8, 11, new Int64[] { 0x060c, 0x070e, 0x0783, 0x06c3, 0x0663, 0x063e, 0x061c, 0x0600 } ), //0x0c, 0x06, 0x0e, 0x06, 0x07, 0x07, 0x83, 0x07, 0xc3, 0x06, 0x67, 0x06, 0x3e, 0x06, 0x1c, 0x06 } ),
				new FontCharacterDescriptor ( '3', 8, 11, new Int64[] { 0x0306, 0x0707, 0x0603, 0x0633, 0x0633, 0x0673, 0x03ff, 0x01ce } ), //0x0c, 0x06, 0x0e, 0x06, 0x07, 0x07, 0x83, 0x07, 0xc3, 0x06, 0x67, 0x06, 0x3e, 0x06, 0x1c, 0x06 } ),
				new FontCharacterDescriptor ( '4', 8, 11, new Int64[] { 0x00c0, 0x00f0, 0x00f8, 0x06de, 0x06c7, 0x07ff, 0x07ff, 0x06c0 } ), //0xc0, 0x00, 0xf0, 0x00, 0xf8, 0x00, 0xde, 0x06, 0xc7, 0x06, 0xff, 0x07, 0xff, 0x07, 0xc0, 0x06 } ),
				new FontCharacterDescriptor ( '5', 8, 11, new Int64[] { 0x031e, 0x073f, 0x0633, 0x0633, 0x0633, 0x0633, 0x03e3, 0x01c3 } ), //0x1e, 0x03, 0x3f, 0x07, 0x33, 0x06, 0x33, 0x06, 0x33, 0x06, 0x33, 0x06, 0xe3, 0x03, 0xc3, 0x01 } ),
				new FontCharacterDescriptor ( '6', 8, 11, new Int64[] { 0x01fc, 0x03fe, 0x0767, 0x0633, 0x0633, 0x0773, 0x03e3, 0x01c3 } ), //0xfc, 0x01, 0xfe, 0x03, 0x67, 0x07, 0x33, 0x06, 0x33, 0x06, 0x73, 0x07, 0xe3, 0x03, 0xc3, 0x01 } ),
				new FontCharacterDescriptor ( '7', 8, 11, new Int64[] { 0x0003, 0x0003, 0x0603, 0x0783, 0x01e3, 0x007b, 0x001f, 0x0007 } ), //0x03, 0x00, 0x03, 0x00, 0x03, 0x06, 0x83, 0x07, 0xe3, 0x01, 0x7b, 0x00, 0x1f, 0x00, 0x07, 0x00 } ),
				new FontCharacterDescriptor ( '8', 8, 11, new Int64[] { 0x018c, 0x03de, 0x0673, 0x0623, 0x0623, 0x0673, 0x03de, 0x018c } ), //0x8c, 0x01, 0xde, 0x03, 0x77, 0x07, 0x23, 0x06, 0x23, 0x06, 0x77, 0x07, 0xde, 0x03, 0x8c, 0x01 } ),
				new FontCharacterDescriptor ( '9', 8, 11, new Int64[] { 0x061c, 0x063e, 0x0677, 0x0663, 0x0663, 0x0737, 0x03fe, 0x01fc } ), //0x1c, 0x06, 0x3e, 0x06, 0x77, 0x06, 0x63, 0x06, 0x63, 0x06, 0x37, 0x07, 0xfe, 0x03, 0xfc, 0x01 } ),
				new FontCharacterDescriptor ( 'A', 8, 11, new Int64[] { 0x07fc, 0x07fe, 0x00c3, 0x00c3, 0x00c3, 0x00c3, 0x07fe, 0x07fc } ), //0xfc, 0x07, 0xfe, 0x07, 0xc7, 0x00, 0xc3, 0x00, 0xc3, 0x00, 0xc7, 0x00, 0xfe, 0x07, 0xfc, 0x07 } ),
				new FontCharacterDescriptor ( 'M', 8, 11, new Int64[] { 0x07ff, 0x07ff, 0x0006, 0x000c, 0x000c, 0x0006, 0x07ff, 0x07ff } ), //0xff, 0x07, 0xff, 0x07, 0x06, 0x00, 0x0c, 0x00, 0x0c, 0x00, 0x06, 0x00, 0xff, 0x07, 0xff, 0x07 } ),
				new FontCharacterDescriptor ( 'N', 8, 11, new Int64[] { 0x07ff, 0x07ff, 0x0006, 0x000c, 0x0018, 0x0030, 0x07ff, 0x07ff } ), //0xff, 0x07, 0xff, 0x07, 0x06, 0x00, 0x0c, 0x00, 0x18, 0x00, 0x30, 0x00, 0xff, 0x07, 0xff, 0x07 } ),
				new FontCharacterDescriptor ( 'U', 8, 11, new Int64[] { 0x01ff, 0x03ff, 0x0700, 0x0600, 0x0600, 0x0700, 0x03ff, 0x01ff } ), //0xff, 0x01, 0xff, 0x03, 0x00, 0x07, 0x00, 0x06, 0x00, 0x06, 0x00, 0x07, 0xff, 0x03, 0xff, 0x01 } ),
				};
			}

		public static DisplayFont Font { get; private set; }
		}
	//---------------------------------------------------------------------------------------------
	#endregion
	//*********************************************************************************************

	} // Fin du namespace "Windows.Devices.Gpio"
//*************************************************************************************************

//*************************************************************************************************
// FIN DU FICHIER
//*************************************************************************************************
