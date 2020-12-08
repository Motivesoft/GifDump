using System;
using System.IO;

namespace GifDump
{
    public class GiFile
    {
        int index;
        byte[] bytes;

        public GiFile( String filename )
        {
            bytes = File.ReadAllBytes( @"C:\Users\ian\Pictures\Saved Pictures\mmgood.gif" );
        }

        public void Rewind( int distance = 1 )
        {
            index -= distance;
        }

        public byte PeekByte()
        {
            byte[] value = ReadBytes( 1 );
            Rewind( 1 );
            return value[ 0 ];
        }

        public byte ReadByte()
        {
            byte[] value = ReadBytes( 1 );

            return value[ 0 ];
        }

        public int ReadWord()
        {
            byte[] value = ReadBytes( 2 );

            return value[0] + (value[1]<<8);
        }

        public byte[] ReadBytes( int length )
        {
            byte[] value = new byte[ length ];
            for ( int loop = 0; loop < length; loop++ )
            {
                value[ loop ] = bytes[ index++ ];
            }

            return value;
        }
    }

    class Program
    {
        enum Spec
        {
            GIF87a,GIF89a,Unknown
        }

        static void Main( string[] args )
        {
            Console.WriteLine( "Hello World!" );

            bool isGIF;
            Spec spec = Spec.Unknown;

            var file = new GiFile( @"C:\Users\ian\Pictures\Saved Pictures\mmgood.gif" );

            {
                var header = file.ReadBytes( 3 );

                isGIF = Compare( header, "GIF" );

                if ( isGIF )
                {
                    var version = file.ReadBytes( 3 );
                    
                    if ( Compare( version, "87a" ) )
                    {
                        spec = Spec.GIF87a;
                    }
                    else if ( Compare( version, "89a" ) )
                    {
                        spec = Spec.GIF89a;
                    }

                    Console.WriteLine( $"{spec}" );

                    var width = file.ReadWord();
                    var height = file.ReadWord();
                    Console.WriteLine( $"W/H: {width}, {height}" );

                    var packed = file.ReadByte();
                    var gctSize = (int) ( packed & 0b00000111 );
                    var ctsf = ( packed & ( 0b00001000 ) ) != 0;
                    var colorRes = (int) ( (packed & 0b01110000) >> 4 );
                    var gctf = ( packed & ( 0b10000000 ) ) != 0;
                    Console.WriteLine( $" {gctSize}, {ctsf}, {colorRes}, {gctf}" );

                    var background = file.ReadByte();
                    Console.WriteLine( $" Background colour index: {background}" );

                    var aspect = file.ReadByte();
                    Console.WriteLine( $" Aspect ratio: {(aspect+15)/64}" );

                    if ( gctf ) // Is this if() logic correct?
                    {
                        var gctCount = 1 << ( gctSize + 1 );
                        var gctSizeVal = 3 * gctCount;

                        Console.WriteLine( $"Colour count: {gctCount}" );
                        for ( int loop = 0; loop < gctCount; loop++ ) 
                        {
                            Console.WriteLine( $" #{file.ReadByte():x2}{file.ReadByte():x2}{file.ReadByte():x2}" );
                        }
                    }


                    while ( file.PeekByte() == 0x21 )
                    {
                        file.ReadByte();
                        var extensionType = file.ReadByte();

                        switch ( extensionType )
                        {
                            case 0xF9: // Graphic Control
                            {
                                Console.WriteLine( $"Graphic Control" );
                                var blockSize = file.ReadByte();
                                var gpacked = file.ReadByte();
                                var delayTime = file.ReadWord();
                                var transparentColorIndex = file.ReadByte();
                                break;
                            }

                            case 0xFF: // Application Extension
                            {
                                Console.WriteLine( $"Application Extension" );
                                var blockSize = file.ReadByte();
                                var appIdentifier = file.ReadBytes( blockSize - 3 );
                                var authCode = file.ReadBytes( 3 );

                                var appDataLen = file.ReadByte();
                                var appData = file.ReadBytes( appDataLen );
                                break;
                            }
                        }
                        var blockEnd = file.ReadByte();
                    }

                    // Local Image Descriptor
                    {
                        Console.WriteLine( $"Local Image Descriptor" );
                        var iseparator = file.ReadByte();

                        var ileft = file.ReadWord();
                        var itop = file.ReadWord();
                        var iwidth = file.ReadWord();
                        var iheight = file.ReadWord();
                        var ipacked = file.ReadWord();
                        Console.WriteLine( $" {ileft},{itop}-{iwidth},{iheight}" );
                        var ilctf = ( ipacked & 0b10000000 ) != 0;
                        var iInterfaceFlag = ( ipacked & 0b01000000 ) != 0;
                        var iSortFlag = ( ipacked & 0b00100000 ) != 0;
                        var iReserved = ( ipacked & 0b00011000 ) >> 3; // Don't really need this
                        var iLCTSize = ( ipacked & 0b00000111 );
                        Console.Write( $" {ilctf}, {iInterfaceFlag},{iSortFlag},{iReserved},{iLCTSize}" );
                        if ( ilctf )
                        {
                            Console.Write( $" ({1 << ( iLCTSize + 1 )})" );
                        }
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine( $"Not a GIF" );
                }
            }
        }

        static bool Compare( byte[] buffer, String value )
        {
            byte[] bArray = new byte[ value.Length ];
            for ( int loop = 0; loop < value.Length; loop++ )
            {
                bArray[ loop ] = (byte) value[ loop ];
            }
            return Compare( buffer, bArray );
        }

        static bool Compare( byte[] buffer, byte[] value )
        {
            if ( buffer.Length < value.Length )
            {
                return false;
            }

            for ( int loop = 0; loop < value.Length; loop++ )
            {
                if ( buffer[ loop ] != value[ loop ] )
                {
                    return false;
                }
            }

            return true;
        }
    }
}
