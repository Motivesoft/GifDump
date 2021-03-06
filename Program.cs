﻿using System;
using System.IO;

namespace GifDump
{
    public class GiFile
    {
        int index;
        byte[] bytes;

        public GiFile( String filename )
        {
            bytes = File.ReadAllBytes( filename );
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
            if ( length == 0 )
            {
                return new byte[0];
            }

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
            bool isGIF;
            Spec spec = Spec.Unknown;

            if ( args.Length < 1 )
            {
                Console.Error.WriteLine("Provide the filename of a GIF");
                Environment.Exit( 1 );
            }

            var file = new GiFile( args[0] );

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

                    Console.WriteLine( $"Logical Screen Descriptor" );
                    var width = file.ReadWord();
                    var height = file.ReadWord();
                    Console.WriteLine( $" Width/Height: {width}, {height}" );

                    var packed = file.ReadByte();
                    var globalColourTableSize = (int) ( packed & 0b00000111 );
                    var colourTableSortFlag = ( packed & ( 0b00001000 ) ) != 0;
                    var colourResolution = (int) ( (packed & 0b01110000) >> 4 );
                    var globalColourTableFlag = ( packed & ( 0b10000000 ) ) != 0;
                    Console.WriteLine( $" Global colour table size: {globalColourTableSize} ({1<<(globalColourTableSize+1)})" );
                    Console.WriteLine( $" Colour table sort flag: {colourTableSortFlag}" );
                    Console.WriteLine( $" Colour resolution: {colourResolution} ({1<<(1+colourResolution)})" );
                    Console.WriteLine( $" Global colour table flag: {globalColourTableFlag}" );

                    var background = file.ReadByte();
                    Console.WriteLine( $" Background colour index: {background} (if meaningful, based on flag: {globalColourTableFlag})" );

                    var aspect = file.ReadByte();
                    Console.WriteLine( $" Aspect ratio: {aspect} ({(aspect+15)/64})" );

                    if ( globalColourTableFlag )
                    {
                        var globalColourTableEntries = 1 << ( globalColourTableSize + 1 );

                        Console.WriteLine( $"Global Colour Table. Entries: {globalColourTableEntries}" );
                        for ( int loop = 0; loop < globalColourTableEntries; loop++ ) 
                        {
                            Console.WriteLine( $" #{file.ReadByte():x2}{file.ReadByte():x2}{file.ReadByte():x2}" );
                        }
                    }

                    while ( true )
                    {
                        if ( file.PeekByte() == 0x3B )
                        {
                            // Step out - reached the end
                            Console.WriteLine( $"End" );
                            break;
                        }
                        while ( file.PeekByte() == 0x21 )
                        {
                            var introducer = file.ReadByte(); // 0x21
                            var extensionType = file.ReadByte();

                            switch ( extensionType )
                            {
                                case 0x01: // Plain Text
                                {
                                    Console.WriteLine( $"{extensionType:2x}: Plain Text Extension" );
                                    var blockSize = file.ReadByte();
                                    var textGridLeft = file.ReadWord();
                                    var textGridTop = file.ReadWord();
                                    var textGridWidth = file.ReadWord();
                                    var textGridHeight = file.ReadWord();
                                    var cellWidth = file.ReadByte();
                                    var cellHeight = file.ReadByte();
                                    var fgColourIndex = file.ReadByte();
                                    var bgColourIndex = file.ReadByte();

                                    Console.WriteLine( $"  blockSize: {blockSize}" );
                                    Console.WriteLine( $"  textGrid: {textGridLeft},{textGridTop}-{textGridWidth},{textGridHeight}" );
                                    Console.WriteLine( $"  cellSze: {cellWidth}x{cellHeight}" );
                                    Console.WriteLine( $"  foreground/background colour index: {fgColourIndex}, {bgColourIndex}" );

                                    while ( file.PeekByte() > 0 )
                                    {
                                        var size = file.ReadByte();
                                        var plainText = file.ReadBytes( size );

                                        Console.WriteLine( $"  Plain Text of size: {size} ({ByteArrayToString( plainText )})" );
                                    }
                                    break;
                                }

                                case 0xF9: // Graphic Control
                                {
                                    Console.WriteLine( $"{extensionType:x2}: Graphic Control Extension" );
                                    var blockSize = file.ReadByte();
                                    var gpacked = file.ReadByte();
                                    var delayTime = file.ReadWord();
                                    var transparentColorIndex = file.ReadByte();

                                    var binRepresentation = "00000000" + Convert.ToString( gpacked, 2 );
                                    Console.WriteLine( $"  blockSize: {blockSize}" );
                                    Console.WriteLine( $"  packed: {binRepresentation.Substring(binRepresentation.Length-8)}" );
                                    Console.WriteLine( $"  delayTime: {delayTime}" );
                                    Console.WriteLine( $"  transparentColourIndex: {transparentColorIndex}" );
                                    break;
                                }

                                case 0xFE: // Comment
                                {
                                    Console.WriteLine( $"{extensionType:x2}: Comment Extension" );
                                    var blockSize = file.ReadByte();

                                    Console.WriteLine( $"  blockSize: {blockSize}" );
                                    while ( file.PeekByte() > 0 )
                                    {
                                        var size = file.ReadByte();
                                        var comment = file.ReadBytes( size );

                                        Console.WriteLine( $"  Comment Data of size: {size} ({ByteArrayToString(comment)})" );
                                    }
                                    break;
                                }

                                case 0xFF: // Application Extension
                                {
                                    Console.WriteLine( $"{extensionType:x2}: Application Extension" );
                                    var blockSize = file.ReadByte();
                                    var appIdentifier = file.ReadBytes( blockSize - 3 );
                                    var authCode = file.ReadBytes( 3 );

                                    var appDataLen = file.ReadByte();
                                    var appData = file.ReadBytes( appDataLen );

                                    Console.WriteLine( $"  blockSize: {blockSize}" );
                                    Console.WriteLine( $"  appIdentifier: {ByteArrayToString(appIdentifier)}" );
                                    Console.WriteLine( $"  authCode: {ByteArrayToString( authCode )}" );

                                    var text = "";
                                    foreach ( var b in appData )
                                    {
                                        if ( text.Length > 0 )
                                        {
                                            text += " ";
                                        }
                                        text += $"{b:x2}";
                                    }
                                    Console.WriteLine( $"  appDataLen: {appDataLen} ({text})" );
                                    break;
                                }

                                default:
                                {
                                    Console.WriteLine( $"{extensionType:x2}: Unknown Extension" );
                                    var blockSize = file.ReadByte();
                                    // Dunno what to do here
                                    file.ReadBytes( blockSize );
                                    break;
                                }
                            }
                            var blockEnd = file.ReadByte();
                        }

                        var iseparator = file.ReadByte();
                        if ( iseparator == 0x2C )
                        {
                            // Local Image Descriptor
                            Console.WriteLine( $"Local Image Descriptor" );

                            var ileft = file.ReadWord();
                            var itop = file.ReadWord();
                            var iwidth = file.ReadWord();
                            var iheight = file.ReadWord();
                            var ipacked = file.ReadByte();
                            Console.WriteLine( $" {ileft},{itop}-{iwidth},{iheight} {ipacked:x2}" );

                            // TODO some difference between GIF87 and GIF89 here? Not significant though

                            var ilctf = ( ipacked & 0b10000000 ) != 0;
                            var iInterfaceFlag = ( ipacked & 0b01000000 ) != 0;
                            var iSortFlag = ( ipacked & 0b00100000 ) != 0;
                            var iReserved = ( ipacked & 0b00011000 ) >> 3; // Don't really need this
                            var iLCTSize = ( ipacked & 0b00000111 );
                            Console.Write( $" {ilctf},{iInterfaceFlag},{iSortFlag},{iReserved},{iLCTSize}" );

                            if ( ilctf )
                            {
                                Console.WriteLine( $" ({1 << ( iLCTSize + 1 )})" );

                                if ( ilctf )
                                {
                                    Console.WriteLine( $"Local Color Table" );
                                    for ( int i = 0; i < 1 << ( iLCTSize + 1 ); i++ )
                                    {
                                        Console.WriteLine( $" #{file.ReadByte():x2}{file.ReadByte():x2}{file.ReadByte():x2}" );
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine();
                            }

                            Console.WriteLine( $"Table Based Image Data" );

                            var blockNum = file.ReadByte();
                            Console.WriteLine( $"Sub-block count: {blockNum}" );

                            while( file.PeekByte() > 0 )
                            {
                                var blockLen = file.ReadByte();
                                var block = file.ReadBytes( blockLen );

                                Console.WriteLine( $"Sub-block len: {blockLen}" );
                            }

                            var zero = file.PeekByte();
                        }
                    }
                }
                else
                {
                    Console.WriteLine( $"Not a GIF" );
                }
            }
        }

        static string ByteArrayToString( byte[] array )
        {
            var text = "";
            foreach ( var b in array )
            {
                text += (char) b;
            }
            return text;
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
