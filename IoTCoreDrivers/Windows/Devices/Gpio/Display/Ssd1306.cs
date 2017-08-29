//*************************************************************************************************
// DEBUT DU FICHIER
//*************************************************************************************************

//*************************************************************************************************
// Nom           : Ssd1306.cs
// Auteur        : Nicolas Dagnas
// Description   : Déclaration de l'objet Ssd1306
// Environnement : Visual Studio 2015
// Créé le       : 25/08/2017
// Modifié le    : 28/08/2017
//-------------------------------------------------------------------------------------------------
// Inspiré de    : https://github.com/stefangordon/IoTCore-SSD1306-Driver
// Inspiré de    : https://github.com/adafruit/Adafruit-GFX-Library
// Aidé de       : http://dotmatrixtool.com
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
using Windows.Devices.Media;
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
	// Classe Ssd1306
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

		//*****************************************************************************************
		/// <summary>
		/// Swap 2 valeurs.
		/// </summary>
		/// <param name="A">Valeur A.</param>
		/// <param name="B">Valeur B.</param>
		//-----------------------------------------------------------------------------------------
		static void SwapIntegers ( ref int A, ref int B ) { int C = A; A = B; B = C; }
		//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures d'Initialisation
		//-----------------------------------------------------------------------------------------

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

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Modifie l'état d'un Pixel.
		/// </summary>
		/// <param name="X">Position en X.</param>
		/// <param name="Y">Position en Y.</param>
		/// <param name="Value"><b>True</b> pour allumé, sinon <b>False</b>.</param>
		//-----------------------------------------------------------------------------------------
		public static void Pixel ( int X, int Y, bool Value )
			{
			//-------------------------------------------------------------------------------------
			if ( X < 0 || X >= SCREEN_WIDTH_PX  ) return;
			if ( Y < 0 || Y >= SCREEN_HEIGHT_PX ) return;

			if ( DisplayBuffer[X, Y] != Value ) SegmentsUpdated = true;

			DisplayBuffer[X, Y] = Value;

			// System.Drawing.Graphics
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures type 'Ligne'
		//-----------------------------------------------------------------------------------------

		//*****************************************************************************************
		/// <summary>
		/// Dessine une ligne reliant les deux points spécifiés par les paires de coordonnées.
		/// </summary>
		/// <param name="X0">Coordonnée x du premier point.</param>
		/// <param name="Y0">Coordonnée y du premier point.</param>
		/// <param name="X1">Coordonnée x du deuxième point.</param>
		/// <param name="Y1">Coordonnée y du deuxième point.</param>
		//-----------------------------------------------------------------------------------------
		private static void WriteLine ( int X0, int Y0, int X1, int Y1 )
			{
			//-------------------------------------------------------------------------------------
			bool Steep = ( Math.Abs ( Y1 - Y0 ) > Math.Abs ( X1 - X0 ) );

			if ( Steep   ) { SwapIntegers ( ref X0, ref Y0 ); SwapIntegers ( ref X1, ref Y1 ); }
			if ( X0 > X1 ) { SwapIntegers ( ref X0, ref X1 ); SwapIntegers ( ref Y0, ref Y1 ); }
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			int Dx = X1 - X0;
			int Dy = Math.Abs( Y1 - Y0 );

			int Err = Dx / 2;
			int YStep;

			if ( Y0 < Y1 ) { YStep = 1; } else { YStep = -1; }

			for ( ; X0 <= X1 ; X0 ++ )
				{
				if ( Steep ) { Pixel ( Y0, X0, true ); } else { Pixel ( X0, Y0, true ); }

				Err -= Dy;

				if ( Err < 0 ) { Y0 += YStep; Err += Dx; }
				}
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Dessine une ligne reliant les deux points spécifiés par les paires de coordonnées.
		/// </summary>
		/// <param name="X0">Coordonnée x du premier point.</param>
		/// <param name="Y0">Coordonnée y du premier point.</param>
		/// <param name="X1">Coordonnée x du deuxième point.</param>
		/// <param name="Y1">Coordonnée y du deuxième point.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawLine ( int X0, int Y0, int X1, int Y1 )
			{
			//-------------------------------------------------------------------------------------
			if ( X0 == X1 )
				{
				if ( Y0 > Y1 ) SwapIntegers ( ref Y0, ref Y1 );

				DrawVLine ( X0, Y0, Y1 - Y0 + 1 );
				}
			//-------------------------------------------------------------------------------------
			else if ( Y0 == Y1 )
				{
				if ( X0 > X1 ) SwapIntegers ( ref X0, ref X1 );

				DrawHLine ( X0, Y0, X1 - X0 + 1 );
				}
			//-------------------------------------------------------------------------------------
			else { WriteLine ( X0, Y0, X1, Y1 ); }
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Dessine une ligne horizontale reliant les deux points spécifiés par les paires de 
		/// coordonnées.
		/// </summary>
		/// <param name="X">Coordonnée x du point de départ.</param>
		/// <param name="Y">Coordonnée y du point de départ.</param>
		/// <param name="Width">Largeur de la ligne.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawHLine ( int X, int Y, int Width )
			{
			//-------------------------------------------------------------------------------------
			WriteLine ( X, Y, X + Width - 1, Y );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Dessine une ligne verticale reliant les deux points spécifiés par les paires de 
		/// coordonnées.
		/// </summary>
		/// <param name="X">Coordonnée x du point de départ.</param>
		/// <param name="Y">Coordonnée y du point de départ.</param>
		/// <param name="Height">Hauteur de la ligne.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawVLine ( int X, int Y, int Height )
			{
			//-------------------------------------------------------------------------------------
			WriteLine ( X, Y, X, Y + Height - 1 );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures type 'Rectangle'
		//-----------------------------------------------------------------------------------------

		//*****************************************************************************************
		/// <summary>
		/// Dessine les bordures arrondies
		/// </summary>
		/// <param name="X">...</param>
		/// <param name="Y">...</param>
		/// <param name="Rayon">...</param>
		/// <param name="CornerName">...</param>
		//-----------------------------------------------------------------------------------------
		private static void DrawCircleHelper ( int X, int Y, int Rayon, int CornerName )
			{
			//-------------------------------------------------------------------------------------
			int f     = 1 - Rayon;
			int ddF_x = 1;
			int ddF_y = -2 * Rayon;
			int x     = 0;
			int y     = Rayon;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			while ( x < y )
				{
				//---------------------------------------------------------------------------------
				if ( f >= 0 ) { y--; ddF_y += 2; f += ddF_y; }

				x++;
				ddF_x += 2;
				f     += ddF_x;
				//---------------------------------------------------------------------------------

				//---------------------------------------------------------------------------------
				if ( ( CornerName & 0x4 ) != 0 )
					{ Pixel ( X + x, Y + y, true ); Pixel ( X + y, Y + x, true ); }
				//---------------------------------------------------------------------------------
				if ( ( CornerName & 0x2 ) != 0 )
					{ Pixel ( X + x, Y - y, true ); Pixel ( X + y, Y - x, true ); }
				//---------------------------------------------------------------------------------
				if ( ( CornerName & 0x8 ) != 0 )
					{ Pixel ( X - y, Y + x, true ); Pixel ( X - x, Y + y, true ); }
				//---------------------------------------------------------------------------------
				if ( ( CornerName & 0x1 ) != 0 )
					{ Pixel ( X - y, Y - x, true ); Pixel ( X - x, Y - y, true ); }
				//---------------------------------------------------------------------------------
				}
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Dessine un rectangle spécifié par une paire de coordonnées, une largeur et une hauteur.
		/// </summary>
		/// <param name="X">
		/// Coordonnée x de l'angle supérieur gauche du rectangle à dessiner.
		/// </param>
		/// <param name="Y">
		/// Coordonnée y de l'angle supérieur gauche du rectangle à dessiner.
		/// </param>
		/// <param name="Width">Largeur du rectangle à dessiner.</param>
		/// <param name="Height">Hauteur du rectangle à dessiner.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawRectangle ( int X, int Y, int Width, int Height )
			{
			//-------------------------------------------------------------------------------------
			DrawHLine ( X            , Y             , Width  );
			DrawHLine ( X            , Y + Height - 1, Width  );
			DrawVLine ( X            , Y             , Height );
			DrawVLine ( X + Width - 1, Y             , Height );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Remplit l'intérieur d'un rectangle spécifié par une paire de coordonnées, une 
		/// largeur et une hauteur.
		/// </summary>
		/// <param name="X">
		/// Coordonnée x de l'angle supérieur gauche du rectangle à remplir.
		/// </param>
		/// <param name="Y">
		/// Coordonnée y de l'angle supérieur gauche du rectangle à remplir.
		/// </param>
		/// <param name="Width">Largeur du rectangle à remplir.</param>
		/// <param name="Height">Hauteur du rectangle à remplir.</param>
		//-----------------------------------------------------------------------------------------
		public static void FillRectangle ( int X, int Y, int Width, int Height )
			{
			//-------------------------------------------------------------------------------------
			for ( int Index = X ; Index < X + Width ; Index ++ ) DrawVLine ( Index, Y, Height );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Dessine un rectangle spécifié par une paire de coordonnées, une largeur et une 
		/// hauteur et aux bords arrondis.
		/// </summary>
		/// <param name="X">
		/// Coordonnée x de l'angle supérieur gauche du rectangle à dessiner.
		/// </param>
		/// <param name="Y">
		/// Coordonnée y de l'angle supérieur gauche du rectangle à dessiner.
		/// </param>
		/// <param name="Width">Largeur du rectangle à dessiner.</param>
		/// <param name="Height">Hauteur du rectangle à dessiner.</param>
		/// <param name="Rayon">Rayon de l'arrondit</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawRoundRectangle ( int X, int Y, int Width, int Height, int Rayon )
			{
			//-------------------------------------------------------------------------------------
			DrawHLine ( X + Rayon    , Y             , Width  - 2 * Rayon ); // Top
			DrawHLine ( X + Rayon    , Y + Height - 1, Width  - 2 * Rayon ); // Bottom
			DrawVLine ( X            , Y + Rayon     , Height - 2 * Rayon ); // Left
			DrawVLine ( X + Width - 1, Y + Rayon     , Height - 2 * Rayon ); // Right
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			DrawCircleHelper ( X + Rayon            , Y + Rayon             , Rayon, 1 );
			DrawCircleHelper ( X + Width - Rayon - 1, Y + Rayon             , Rayon, 2 );
			DrawCircleHelper ( X + Width - Rayon - 1, Y + Height - Rayon - 1, Rayon, 4 );
			DrawCircleHelper ( X + Rayon            , Y + Height - Rayon - 1, Rayon, 8 );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Remplit l'intérieur d'un rectangle spécifié par une paire de coordonnées, une 
		/// largeur et une hauteur et aux bords arrondis.
		/// </summary>
		/// <param name="X">
		/// Coordonnée x de l'angle supérieur gauche du rectangle à remplir.
		/// </param>
		/// <param name="Y">
		/// Coordonnée y de l'angle supérieur gauche du rectangle à remplir.
		/// </param>
		/// <param name="Width">Largeur du rectangle à remplir.</param>
		/// <param name="Height">Hauteur du rectangle à remplir.</param>
		/// <param name="Rayon">Rayon de l'arrondit</param>
		//-----------------------------------------------------------------------------------------
		public static void FillRoundRectangle ( int X, int Y, int Width, int Height, int Rayon )
			{
			//-------------------------------------------------------------------------------------
			FillRectangle ( X + Rayon, Y, Width - 2 * Rayon, Height );
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			// draw four corners
			//-------------------------------------------------------------------------------------
			FillCircleHelper (X + Width - Rayon - 1, Y + Rayon, Rayon, 1, Height - 2 * Rayon - 1);
			FillCircleHelper (X + Rayon            , Y + Rayon, Rayon, 2, Height - 2 * Rayon - 1);
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Remplis l'écran.
		/// </summary>
		//-----------------------------------------------------------------------------------------
		public static void FillScreen ()
			{
			//-------------------------------------------------------------------------------------
			FillRectangle ( 0, 0, (int)SCREEN_WIDTH_PX, (int)SCREEN_HEIGHT_PX );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures type 'Cercle'
		//-----------------------------------------------------------------------------------------

		//*****************************************************************************************
		/// <summary>
		/// // Used to do circles and roundrects.
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <param name="Rayon"></param>
		/// <param name="Corner"></param>
		/// <param name="Delta"></param>
		//-----------------------------------------------------------------------------------------
		private static void FillCircleHelper ( int X, int Y, int Rayon, int Corner, int Delta )
			{
			//-------------------------------------------------------------------------------------
			int f     = 1 - Rayon;
			int ddF_x = 1;
			int ddF_y = -2 * Rayon;
			int x     = 0;
			int y     = Rayon;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			while ( x < y )
				{
				if ( f >= 0 ) { y--; ddF_y += 2; f += ddF_y; }

				x++;
				ddF_x += 2;
				f     += ddF_x;

				if ( ( Corner & 0x1 ) != 0 )
					{
					DrawVLine ( X + x, Y - y, 2 * y + 1 + Delta );
					DrawVLine ( X + y, Y - x, 2 * x + 1 + Delta );
					}

				if ( ( Corner & 0x2 ) != 0 )
					{
					DrawVLine ( X - x, Y - y, 2 * y + 1 + Delta );
					DrawVLine ( X - y, Y - x, 2 * x + 1 + Delta );
					}
				}
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Dessine un cercle définie par un rectangle englobant spécifié par une paire de 
		/// coordonnées, une hauteur et une largeur.
		/// </summary>
		/// <param name="X">
		/// Coordonnée x de l'angle supérieur gauche du rectangle englobant qui définit le cercle.
		/// </param>
		/// <param name="Y">
		/// Coordonnée y de l'angle supérieur gauche du rectangle englobant qui définit le cercle.
		/// </param>
		/// <param name="Rayon">Rayon du cercle.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawCircle ( int X, int Y, int Rayon )
			{
			//-------------------------------------------------------------------------------------
			int f     = 1 - Rayon;
			int ddF_x = 1;
			int ddF_y = -2 * Rayon;
			int x     = 0;
			int y     = Rayon;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			Pixel ( X        , Y + Rayon, true );
			Pixel ( X        , Y - Rayon, true );
			Pixel ( X + Rayon, Y        , true );
			Pixel ( X - Rayon, Y        , true );
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			while ( x < y )
				{
				if ( f >= 0 ) { y--; ddF_y += 2; f += ddF_y; }

				x++;
				ddF_x += 2;
				f += ddF_x;

				Pixel ( X + x, Y + y, true );
				Pixel ( X - x, Y + y, true );
				Pixel ( X + x, Y - y, true );
				Pixel ( X - x, Y - y, true );
				Pixel ( X + y, Y + x, true );
				Pixel ( X - y, Y + x, true );
				Pixel ( X + y, Y - x, true );
				Pixel ( X - y, Y - x, true );
				}
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Remplit l'intérieur d'un cercle définie par un rectangle englobant spécifié par une 
		/// paire de coordonnées, une largeur et une hauteur.
		/// </summary>
		/// <param name="X">
		/// Coordonnée x de l'angle supérieur gauche du rectangle englobant qui définit le cercle.
		/// </param>
		/// <param name="Y">
		/// Coordonnée y de l'angle supérieur gauche du rectangle englobant qui définit le cercle.
		/// </param>
		/// <param name="Rayon">Rayon du cercle.</param>
		//-----------------------------------------------------------------------------------------
		public static void FillCircle ( int X, int Y, int Rayon )
			{
			//-------------------------------------------------------------------------------------
			DrawVLine ( X, Y - Rayon, 2 * Rayon + 1 );

			FillCircleHelper ( X, Y, Rayon, 3, 0 );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures type 'Triangle'
		//-----------------------------------------------------------------------------------------
		
		//*****************************************************************************************
		/// <summary>
		/// Dessine un triangle spécifié par une série de 3 coordonnées.
		/// </summary>
		/// <param name="X0">Coordonnée x du premier point.</param>
		/// <param name="Y0">Coordonnée x du premier point.</param>
		/// <param name="X1">Coordonnée x du second point.</param>
		/// <param name="Y1">Coordonnée x du second point.</param>
		/// <param name="X2">Coordonnée x du troisième point.</param>
		/// <param name="Y2">Coordonnée x du troisième point.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawTriangle ( int X0, int Y0,int X1, int Y1, int X2, int Y2 )
			{
			//-------------------------------------------------------------------------------------
			DrawLine ( X0, Y0, X1, Y1 );
			DrawLine ( X1, Y1, X2, Y2 );
			DrawLine ( X2, Y2, X0, Y0 );
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Remplit un triangle spécifié par une série de 3 coordonnées.
		/// </summary>
		/// <param name="X0">Coordonnée x du premier point.</param>
		/// <param name="Y0">Coordonnée x du premier point.</param>
		/// <param name="X1">Coordonnée x du second point.</param>
		/// <param name="Y1">Coordonnée x du second point.</param>
		/// <param name="X2">Coordonnée x du troisième point.</param>
		/// <param name="Y2">Coordonnée x du troisième point.</param>
		//-----------------------------------------------------------------------------------------
		public static void FillTriangle ( int X0, int Y0,int X1, int Y1, int X2, int Y2 )
			{
			//-------------------------------------------------------------------------------------
			int a, b, y, last;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			// Sort coordinates by Y order (y2 >= y1 >= y0)
			//-------------------------------------------------------------------------------------
			if ( Y0 > Y1 ) { SwapIntegers ( ref Y0, ref Y1) ; SwapIntegers ( ref X0, ref X1 ); }
			if ( Y1 > Y2 ) { SwapIntegers ( ref Y2, ref Y1 ); SwapIntegers ( ref X2, ref X1 ); }
			if ( Y0 > Y1 ) { SwapIntegers ( ref Y0, ref Y1 ); SwapIntegers ( ref X0, ref X1 ); }
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			// Handle awkward all-on-same-line case as its own thing
			//-------------------------------------------------------------------------------------
			if( Y0 == Y2 )
				{ 
				a = b = X0;

				if      ( X1 < a ) a = X1;
				else if ( X1 > b ) b = X1;

				if      ( X2 < a ) a = X2;
				else if ( X2 > b ) b = X2;

				DrawHLine ( a, Y0, b - a + 1 ); return;
				}
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			int dx01 = X1 - X0,
				dy01 = Y1 - Y0,
				dx02 = X2 - X0,
				dy02 = Y2 - Y0,
				dx12 = X2 - X1,
				dy12 = Y2 - Y1;
			int sa = 0, sb = 0;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			// For upper part of triangle, find scanline crossings for segments
			// 0-1 and 0-2.  If y1=y2 (flat-bottomed triangle), the scanline y1
			// is included here (and second loop will be skipped, avoiding a /0
			// error there), otherwise scanline y1 is skipped here and handled
			// in the second loop...which also avoids a /0 error here if y0=y1
			// (flat-topped triangle).
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			if ( Y1 == Y2 ) last = Y1;     // Include y1 scanline
			else            last = Y1 - 1; // Skip it
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			for(y=Y0; y<=last; y++)
				{
				a   = X0 + sa / dy01;
				b   = X0 + sb / dy02;

				sa += dx01;
				sb += dx02;

				if ( a > b ) SwapIntegers ( ref a, ref b );

				DrawHLine ( a, y, b - a + 1 );
				}
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			// For lower part of triangle, find scanline crossings for segments
			// 0-2 and 1-2.  This loop is skipped if y1=y2.
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			sa = dx12 * ( y - Y1 );
			sb = dx02 * ( y - Y0 );

			for ( ; y <= Y2; y ++ )
				{
				a   = X1 + sa / dy12;
				b   = X0 + sb / dy02;
				sa += dx12;
				sb += dx02;

				if ( a > b ) SwapIntegers ( ref a, ref b );

				DrawHLine ( a, y, b - a + 1 );
				}
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures type 'String'
		//-----------------------------------------------------------------------------------------

		//*****************************************************************************************
		/// <summary>
		/// Dessine la chaîne de texte précisée à l'emplacement indiqué avec l'objet 
		/// <b>DisplayFont</b> spécifié.
		/// </summary>
		/// <param name="Text">Chaîne à dessiner.</param>
		/// <param name="Font">
		/// <b>DisplayFont</b> qui définit le format du texte de la chaîne.
		/// </param>
		/// <param name="X">Coordonnée x de l'angle supérieur gauche du texte dessiné.</param>
		/// <param name="Y">Coordonnée y de l'angle supérieur gauche du texte dessiné.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawString ( string Text, DisplayFont Font, int X, int Y )
			{
			//-------------------------------------------------------------------------------------
			if ( string.IsNullOrEmpty ( Text ) || Font == null ) return;

			int VerticalAdvance   = 0;
			int HorizontalAdvance = 0;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			foreach ( char Character in Text )
				{
				if ( Character == '\n' )
					{ VerticalAdvance += Font.LineSpacing; HorizontalAdvance = 0; continue; }

				HorizontalAdvance += DrawChar ( Character, Font, ( X + HorizontalAdvance ), 
					                                             ( Y + VerticalAdvance   ) );
				}
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Dessine le caractère précisé à l'emplacement indiqué avec l'objet <b>DisplayFont</b> 
		/// spécifié.
		/// </summary>
		/// <param name="Character">Caractère à dessiner.</param>
		/// <param name="Font">
		/// <b>DisplayFont</b> qui définit le format du texte de la chaîne.
		/// </param>
		/// <param name="X">Coordonnée x de l'angle supérieur gauche du texte dessiné.</param>
		/// <param name="Y">Coordonnée y de l'angle supérieur gauche du texte dessiné.</param>
		/// <returns>
		/// Retourne le nombre de pixel à décaler pour le dessin du caractère suivant.
		/// </returns>
		//-----------------------------------------------------------------------------------------
		public static int DrawChar ( char Character, DisplayFont Font, int X, int Y )
			{
			//-------------------------------------------------------------------------------------
			if ( Font == null ) return 0;

			DisplayFontGlyph Glyph = Font.GetGlyph ( Character );

			if ( Glyph.IsEmpty ) return 0;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			int BitmapOffset      = Glyph.Offset;
			int Width             = Glyph.Width;
			int Height            = Glyph.Height;
			int HorizontalOffset  = Glyph.HorizontalOffset;
			int VerticalOffset    = Glyph.VerticalOffset;
			int HorizontalAdvance = Glyph.Advance;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			if ( Font.Type == DisplayFontType.Matriciel )
				{
				for ( int Index = 0 ; Index < Width ; Index ++ )
					{
					uint line = Font.Bitmaps[BitmapOffset + Index];

					for ( int j = 0 ; j < Height ; j ++, line >>= 1 )
						{
						if ( ( line & 1 ) != 0 ) Pixel ( ( X + Index ), Y + j, true  );
						else                     Pixel ( ( X + Index ), Y + j, false );
						}
					}
				}
			//-------------------------------------------------------------------------------------
			else
				{
				int Bottom = Font.LineSpacing;

				VerticalOffset = (int)Bottom + VerticalOffset;

				int xx, yy;
				uint bits = 0, bit = 0;

				for ( yy = 0 ; yy < Height ; yy ++ )
					{
					for ( xx = 0 ; xx < Width ; xx ++ )
						{
						if ( ( (byte)( bit ++ ) & 7 ) == 0 )
							bits = Font.Bitmaps[BitmapOffset++];

						if ( ( bits & 0x80 ) != 0 )
							Pixel ( ( X + HorizontalOffset + xx ), 
								    ( Y + VerticalOffset   + yy ), true  );
						else
							Pixel ( ( X + HorizontalOffset + xx ), 
								    ( Y + VerticalOffset   + yy ), false );

						bits <<= 1;
						}
					}
				}
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			return HorizontalAdvance;
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures type 'Glyph'
		//-----------------------------------------------------------------------------------------

		//*****************************************************************************************
		/// <summary>
		/// Dessine le glyph précisé à l'emplacement indiqué.
		/// </summary>
		/// <param name="Glyph">Glyph à dessiner.</param>
		/// <param name="X">Coordonnée x de l'angle supérieur gauche du texte dessiné.</param>
		/// <param name="Y">Coordonnée y de l'angle supérieur gauche du texte dessiné.</param>
		//-----------------------------------------------------------------------------------------
		public static void DrawGlyph ( DisplayGlyph Glyph, int X, int Y )
			{
			//-------------------------------------------------------------------------------------
			if ( Glyph.IsEmpty ) return;
			//-------------------------------------------------------------------------------------

			//-------------------------------------------------------------------------------------
			for ( int Index = 0 ; Index < Glyph.Width && Index < Glyph.Bitmap.Length ; Index ++ )
				{
				uint line = Glyph.Bitmap[Index];

				for ( int j = 0 ; j < Glyph.Height ; j ++, line >>= 1 )
					{
					if ( ( line & 1 ) != 0 ) Pixel ( ( X + Index ), Y + j, true  );
					else                     Pixel ( ( X + Index ), Y + j, false );
					}
				}
			//-------------------------------------------------------------------------------------
			}
		//*****************************************************************************************

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		#region // Section des Procédures de Mise à Jour
		//-----------------------------------------------------------------------------------------

		//*****************************************************************************************
		/// <summary>
		/// Writes the Display Buffer out to the physical screen for display.
		/// </summary>
		//-----------------------------------------------------------------------------------------
		public static void Update ()
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

		//-----------------------------------------------------------------------------------------
		#endregion
		//*****************************************************************************************

		//*****************************************************************************************
		/// <summary>
		/// Indique si la puce de gestion de l'heure est présente.
		/// </summary>
		//-----------------------------------------------------------------------------------------
		public static bool DevicePresent { get { return ( I2cCardDevice != null ); } }
		//*****************************************************************************************
		
		//*****************************************************************************************
		public static void Clear () { Array.Clear ( DisplayBuffer, 0, DisplayBuffer.Length ); }
		//*****************************************************************************************
		}
	//---------------------------------------------------------------------------------------------
	#endregion
	//*********************************************************************************************
	
	} // Fin du namespace "Windows.Devices.Gpio"
//*************************************************************************************************

//*************************************************************************************************
// FIN DU FICHIER
//*************************************************************************************************
