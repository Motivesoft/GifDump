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
                    var gctSize = (int) ( packed & 0x03 );
                    var ctsf = ( packed & (int)Math.Pow(2,3) ) != 0;
                    var colorRes = (int) ( (packed>>4) & 0x03 );
                    var gctf = ( packed & (int) Math.Pow( 2, 7 ) ) != 0;
                    Console.WriteLine( $" {gctSize}, {ctsf}, {colorRes}, {gctf}" );

                    var background = file.ReadByte();
                    Console.WriteLine( $" Background colour index: {background}" );

                    var aspect = file.ReadByte();
                    Console.WriteLine( $" Aspect ratio: {(aspect+15)/64}" );

                    var gctCount = Math.Pow( 2, gctSize );
                    var gctSizeVal = 3 * gctCount;

                    for ( int loop = 0; loop < gctCount; loop++ )
                    {
                        Console.WriteLine( $" #{file.ReadByte():x2}{file.ReadByte():x2}{file.ReadByte():x2}" );
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
