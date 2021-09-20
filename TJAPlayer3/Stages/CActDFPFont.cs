using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
	internal class CActDFPFont : CActivity
	{
		// コンストラクタ

		public CActDFPFont()
		{
			ST文字領域[] st文字領域Array = new ST文字領域[ 0x5d+2 ];
			ST文字領域 st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域 = st文字領域94;
			st文字領域.ch = ' ';
			st文字領域.rc = new Rectangle( 10, 3, 13, 0x1b );
			st文字領域Array[ 0 ] = st文字領域;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域2 = st文字領域94;
			st文字領域2.ch = '!';
			st文字領域2.rc = new Rectangle( 0x19, 3, 14, 0x1b );
			st文字領域Array[ 1 ] = st文字領域2;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域3 = st文字領域94;
			st文字領域3.ch = '"';
			st文字領域3.rc = new Rectangle( 0x2c, 3, 0x11, 0x1b );
			st文字領域Array[ 2 ] = st文字領域3;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域4 = st文字領域94;
			st文字領域4.ch = '#';
			st文字領域4.rc = new Rectangle( 0x40, 3, 0x18, 0x1b );
			st文字領域Array[ 3 ] = st文字領域4;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域5 = st文字領域94;
			st文字領域5.ch = '$';
			st文字領域5.rc = new Rectangle( 90, 3, 0x15, 0x1b );
			st文字領域Array[ 4 ] = st文字領域5;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域6 = st文字領域94;
			st文字領域6.ch = '%';
			st文字領域6.rc = new Rectangle( 0x71, 3, 0x1b, 0x1b );
			st文字領域Array[ 5 ] = st文字領域6;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域7 = st文字領域94;
			st文字領域7.ch = '&';
			st文字領域7.rc = new Rectangle( 0x8e, 3, 0x18, 0x1b );
			st文字領域Array[ 6 ] = st文字領域7;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域8 = st文字領域94;
			st文字領域8.ch = '\'';
			st文字領域8.rc = new Rectangle( 0xab, 3, 11, 0x1b );
			st文字領域Array[ 7 ] = st文字領域8;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域9 = st文字領域94;
			st文字領域9.ch = '(';
			st文字領域9.rc = new Rectangle( 0xc0, 3, 0x10, 0x1b );
			st文字領域Array[ 8 ] = st文字領域9;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域10 = st文字領域94;
			st文字領域10.ch = ')';
			st文字領域10.rc = new Rectangle( 0xd0, 3, 0x10, 0x1b );
			st文字領域Array[ 9 ] = st文字領域10;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域11 = st文字領域94;
			st文字領域11.ch = '*';
			st文字領域11.rc = new Rectangle( 0xe2, 3, 0x15, 0x1b );
			st文字領域Array[ 10 ] = st文字領域11;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域12 = st文字領域94;
			st文字領域12.ch = '+';
			st文字領域12.rc = new Rectangle( 2, 0x1f, 0x18, 0x1b );
			st文字領域Array[ 11 ] = st文字領域12;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域13 = st文字領域94;
			st文字領域13.ch = ',';
			st文字領域13.rc = new Rectangle( 0x1b, 0x1f, 11, 0x1b );
			st文字領域Array[ 12 ] = st文字領域13;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域14 = st文字領域94;
			st文字領域14.ch = '-';
			st文字領域14.rc = new Rectangle( 0x29, 0x1f, 13, 0x1b );
			st文字領域Array[ 13 ] = st文字領域14;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域15 = st文字領域94;
			st文字領域15.ch = '.';
			st文字領域15.rc = new Rectangle( 0x37, 0x1f, 11, 0x1b );
			st文字領域Array[ 14 ] = st文字領域15;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域16 = st文字領域94;
			st文字領域16.ch = '/';
			st文字領域16.rc = new Rectangle( 0x44, 0x1f, 0x15, 0x1b );
			st文字領域Array[ 15 ] = st文字領域16;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域17 = st文字領域94;
			st文字領域17.ch = '0';
			st文字領域17.rc = new Rectangle( 0x5b, 0x1f, 20, 0x1b );
			st文字領域Array[ 0x10 ] = st文字領域17;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域18 = st文字領域94;
			st文字領域18.ch = '1';
			st文字領域18.rc = new Rectangle( 0x75, 0x1f, 14, 0x1b );
			st文字領域Array[ 0x11 ] = st文字領域18;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域19 = st文字領域94;
			st文字領域19.ch = '2';
			st文字領域19.rc = new Rectangle( 0x86, 0x1f, 0x15, 0x1b );
			st文字領域Array[ 0x12 ] = st文字領域19;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域20 = st文字領域94;
			st文字領域20.ch = '3';
			st文字領域20.rc = new Rectangle( 0x9d, 0x1f, 20, 0x1b );
			st文字領域Array[ 0x13 ] = st文字領域20;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域21 = st文字領域94;
			st文字領域21.ch = '4';
			st文字領域21.rc = new Rectangle( 0xb3, 0x1f, 20, 0x1b );
			st文字領域Array[ 20 ] = st文字領域21;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域22 = st文字領域94;
			st文字領域22.ch = '5';
			st文字領域22.rc = new Rectangle( 0xca, 0x1f, 0x13, 0x1b );
			st文字領域Array[ 0x15 ] = st文字領域22;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域23 = st文字領域94;
			st文字領域23.ch = '6';
			st文字領域23.rc = new Rectangle( 0xe0, 0x1f, 20, 0x1b );
			st文字領域Array[ 0x16 ] = st文字領域23;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域24 = st文字領域94;
			st文字領域24.ch = '7';
			st文字領域24.rc = new Rectangle( 4, 0x3b, 0x13, 0x1b );
			st文字領域Array[ 0x17 ] = st文字領域24;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域25 = st文字領域94;
			st文字領域25.ch = '8';
			st文字領域25.rc = new Rectangle( 0x18, 0x3b, 20, 0x1b );
			st文字領域Array[ 0x18 ] = st文字領域25;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域26 = st文字領域94;
			st文字領域26.ch = '9';
			st文字領域26.rc = new Rectangle( 0x2f, 0x3b, 0x13, 0x1b );
			st文字領域Array[ 0x19 ] = st文字領域26;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域27 = st文字領域94;
			st文字領域27.ch = ':';
			st文字領域27.rc = new Rectangle( 0x44, 0x3b, 12, 0x1b );
			st文字領域Array[ 0x1a ] = st文字領域27;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域28 = st文字領域94;
			st文字領域28.ch = ';';
			st文字領域28.rc = new Rectangle( 0x51, 0x3b, 13, 0x1b );
			st文字領域Array[ 0x1b ] = st文字領域28;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域29 = st文字領域94;
			st文字領域29.ch = '<';
			st文字領域29.rc = new Rectangle( 0x60, 0x3b, 20, 0x1b );
			st文字領域Array[ 0x1c ] = st文字領域29;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域30 = st文字領域94;
			st文字領域30.ch = '=';
			st文字領域30.rc = new Rectangle( 0x74, 0x3b, 0x11, 0x1b );
			st文字領域Array[ 0x1d ] = st文字領域30;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域31 = st文字領域94;
			st文字領域31.ch = '>';
			st文字領域31.rc = new Rectangle( 0x85, 0x3b, 20, 0x1b );
			st文字領域Array[ 30 ] = st文字領域31;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域32 = st文字領域94;
			st文字領域32.ch = '?';
			st文字領域32.rc = new Rectangle( 0x9c, 0x3b, 20, 0x1b );
			st文字領域Array[ 0x1f ] = st文字領域32;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域33 = st文字領域94;
			st文字領域33.ch = 'A';
			st文字領域33.rc = new Rectangle( 0xb1, 0x3b, 0x17, 0x1b );
			st文字領域Array[ 0x20 ] = st文字領域33;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域34 = st文字領域94;
			st文字領域34.ch = 'B';
			st文字領域34.rc = new Rectangle( 0xcb, 0x3b, 0x15, 0x1b );
			st文字領域Array[ 0x21 ] = st文字領域34;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域35 = st文字領域94;
			st文字領域35.ch = 'C';
			st文字領域35.rc = new Rectangle( 0xe3, 0x3b, 0x16, 0x1b );
			st文字領域Array[ 0x22 ] = st文字領域35;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域36 = st文字領域94;
			st文字領域36.ch = 'D';
			st文字領域36.rc = new Rectangle( 2, 0x57, 0x16, 0x1b );
			st文字領域Array[ 0x23 ] = st文字領域36;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域37 = st文字領域94;
			st文字領域37.ch = 'E';
			st文字領域37.rc = new Rectangle( 0x1a, 0x57, 0x16, 0x1b );
			st文字領域Array[ 0x24 ] = st文字領域37;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域38 = st文字領域94;
			st文字領域38.ch = 'F';
			st文字領域38.rc = new Rectangle( 0x30, 0x57, 0x16, 0x1b );
			st文字領域Array[ 0x25 ] = st文字領域38;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域39 = st文字領域94;
			st文字領域39.ch = 'G';
			st文字領域39.rc = new Rectangle( 0x48, 0x57, 0x16, 0x1b );
			st文字領域Array[ 0x26 ] = st文字領域39;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域40 = st文字領域94;
			st文字領域40.ch = 'H';
			st文字領域40.rc = new Rectangle( 0x61, 0x57, 0x18, 0x1b );
			st文字領域Array[ 0x27 ] = st文字領域40;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域41 = st文字領域94;
			st文字領域41.ch = 'I';
			st文字領域41.rc = new Rectangle( 0x7a, 0x57, 13, 0x1b );
			st文字領域Array[ 40 ] = st文字領域41;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域42 = st文字領域94;
			st文字領域42.ch = 'J';
			st文字領域42.rc = new Rectangle( 0x88, 0x57, 20, 0x1b );
			st文字領域Array[ 0x29 ] = st文字領域42;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域43 = st文字領域94;
			st文字領域43.ch = 'K';
			st文字領域43.rc = new Rectangle( 0x9d, 0x57, 0x18, 0x1b );
			st文字領域Array[ 0x2a ] = st文字領域43;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域44 = st文字領域94;
			st文字領域44.ch = 'L';
			st文字領域44.rc = new Rectangle( 0xb7, 0x57, 20, 0x1b );
			st文字領域Array[ 0x2b ] = st文字領域44;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域45 = st文字領域94;
			st文字領域45.ch = 'M';
			st文字領域45.rc = new Rectangle( 0xce, 0x57, 0x1a, 0x1b );
			st文字領域Array[ 0x2c ] = st文字領域45;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域46 = st文字領域94;
			st文字領域46.ch = 'N';
			st文字領域46.rc = new Rectangle( 0xe9, 0x57, 0x17, 0x1b );
			st文字領域Array[ 0x2d ] = st文字領域46;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域47 = st文字領域94;
			st文字領域47.ch = 'O';
			st文字領域47.rc = new Rectangle( 2, 0x73, 0x18, 0x1b );
			st文字領域Array[ 0x2e ] = st文字領域47;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域48 = st文字領域94;
			st文字領域48.ch = 'P';
			st文字領域48.rc = new Rectangle( 0x1c, 0x73, 0x15, 0x1b );
			st文字領域Array[ 0x2f ] = st文字領域48;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域49 = st文字領域94;
			st文字領域49.ch = 'Q';
			st文字領域49.rc = new Rectangle( 0x33, 0x73, 0x17, 0x1b );
			st文字領域Array[ 0x30 ] = st文字領域49;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域50 = st文字領域94;
			st文字領域50.ch = 'R';
			st文字領域50.rc = new Rectangle( 0x4c, 0x73, 0x16, 0x1b );
			st文字領域Array[ 0x31 ] = st文字領域50;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域51 = st文字領域94;
			st文字領域51.ch = 'S';
			st文字領域51.rc = new Rectangle( 100, 0x73, 0x15, 0x1b );
			st文字領域Array[ 50 ] = st文字領域51;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域52 = st文字領域94;
			st文字領域52.ch = 'T';
			st文字領域52.rc = new Rectangle( 0x7c, 0x73, 0x16, 0x1b );
			st文字領域Array[ 0x33 ] = st文字領域52;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域53 = st文字領域94;
			st文字領域53.ch = 'U';
			st文字領域53.rc = new Rectangle( 0x93, 0x73, 0x16, 0x1b );
			st文字領域Array[ 0x34 ] = st文字領域53;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域54 = st文字領域94;
			st文字領域54.ch = 'V';
			st文字領域54.rc = new Rectangle( 0xad, 0x73, 0x16, 0x1b );
			st文字領域Array[ 0x35 ] = st文字領域54;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域55 = st文字領域94;
			st文字領域55.ch = 'W';
			st文字領域55.rc = new Rectangle( 0xc5, 0x73, 0x1a, 0x1b );
			st文字領域Array[ 0x36 ] = st文字領域55;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域56 = st文字領域94;
			st文字領域56.ch = 'X';
			st文字領域56.rc = new Rectangle( 0xe0, 0x73, 0x1a, 0x1b );
			st文字領域Array[ 0x37 ] = st文字領域56;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域57 = st文字領域94;
			st文字領域57.ch = 'Y';
			st文字領域57.rc = new Rectangle( 4, 0x8f, 0x17, 0x1b );
			st文字領域Array[ 0x38 ] = st文字領域57;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域58 = st文字領域94;
			st文字領域58.ch = 'Z';
			st文字領域58.rc = new Rectangle( 0x1b, 0x8f, 0x16, 0x1b );
			st文字領域Array[ 0x39 ] = st文字領域58;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域59 = st文字領域94;
			st文字領域59.ch = '[';
			st文字領域59.rc = new Rectangle( 0x31, 0x8f, 0x11, 0x1b );
			st文字領域Array[ 0x3a ] = st文字領域59;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域60 = st文字領域94;
			st文字領域60.ch = '\\';
			st文字領域60.rc = new Rectangle( 0x42, 0x8f, 0x19, 0x1b );
			st文字領域Array[ 0x3b ] = st文字領域60;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域61 = st文字領域94;
			st文字領域61.ch = ']';
			st文字領域61.rc = new Rectangle( 0x5c, 0x8f, 0x11, 0x1b );
			st文字領域Array[ 60 ] = st文字領域61;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域62 = st文字領域94;
			st文字領域62.ch = '^';
			st文字領域62.rc = new Rectangle( 0x71, 0x8f, 0x10, 0x1b );
			st文字領域Array[ 0x3d ] = st文字領域62;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域63 = st文字領域94;
			st文字領域63.ch = '_';
			st文字領域63.rc = new Rectangle( 0x81, 0x8f, 0x13, 0x1b );
			st文字領域Array[ 0x3e ] = st文字領域63;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域64 = st文字領域94;
			st文字領域64.ch = 'a';
			st文字領域64.rc = new Rectangle( 150, 0x8f, 0x13, 0x1b );
			st文字領域Array[ 0x3f ] = st文字領域64;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域65 = st文字領域94;
			st文字領域65.ch = 'b';
			st文字領域65.rc = new Rectangle( 0xac, 0x8f, 20, 0x1b );
			st文字領域Array[ 0x40 ] = st文字領域65;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域66 = st文字領域94;
			st文字領域66.ch = 'c';
			st文字領域66.rc = new Rectangle( 0xc3, 0x8f, 0x12, 0x1b );
			st文字領域Array[ 0x41 ] = st文字領域66;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域67 = st文字領域94;
			st文字領域67.ch = 'd';
			st文字領域67.rc = new Rectangle( 0xd8, 0x8f, 0x15, 0x1b );
			st文字領域Array[ 0x42 ] = st文字領域67;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域68 = st文字領域94;
			st文字領域68.ch = 'e';
			st文字領域68.rc = new Rectangle( 2, 0xab, 0x13, 0x1b );
			st文字領域Array[ 0x43 ] = st文字領域68;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域69 = st文字領域94;
			st文字領域69.ch = 'f';
			st文字領域69.rc = new Rectangle( 0x17, 0xab, 0x11, 0x1b );
			st文字領域Array[ 0x44 ] = st文字領域69;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域70 = st文字領域94;
			st文字領域70.ch = 'g';
			st文字領域70.rc = new Rectangle( 40, 0xab, 0x15, 0x1b );
			st文字領域Array[ 0x45 ] = st文字領域70;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域71 = st文字領域94;
			st文字領域71.ch = 'h';
			st文字領域71.rc = new Rectangle( 0x3f, 0xab, 20, 0x1b );
			st文字領域Array[ 70 ] = st文字領域71;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域72 = st文字領域94;
			st文字領域72.ch = 'i';
			st文字領域72.rc = new Rectangle( 0x55, 0xab, 13, 0x1b );
			st文字領域Array[ 0x47 ] = st文字領域72;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域73 = st文字領域94;
			st文字領域73.ch = 'j';
			st文字領域73.rc = new Rectangle( 0x62, 0xab, 0x10, 0x1b );
			st文字領域Array[ 0x48 ] = st文字領域73;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域74 = st文字領域94;
			st文字領域74.ch = 'k';
			st文字領域74.rc = new Rectangle( 0x74, 0xab, 20, 0x1b );
			st文字領域Array[ 0x49 ] = st文字領域74;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域75 = st文字領域94;
			st文字領域75.ch = 'l';
			st文字領域75.rc = new Rectangle( 0x8a, 0xab, 13, 0x1b );
			st文字領域Array[ 0x4a ] = st文字領域75;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域76 = st文字領域94;
			st文字領域76.ch = 'm';
			st文字領域76.rc = new Rectangle( 0x98, 0xab, 0x1a, 0x1b );
			st文字領域Array[ 0x4b ] = st文字領域76;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域77 = st文字領域94;
			st文字領域77.ch = 'n';
			st文字領域77.rc = new Rectangle( 0xb5, 0xab, 20, 0x1b );
			st文字領域Array[ 0x4c ] = st文字領域77;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域78 = st文字領域94;
			st文字領域78.ch = 'o';
			st文字領域78.rc = new Rectangle( 0xcc, 0xab, 0x13, 0x1b );
			st文字領域Array[ 0x4d ] = st文字領域78;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域79 = st文字領域94;
			st文字領域79.ch = 'p';
			st文字領域79.rc = new Rectangle( 0xe1, 0xab, 0x15, 0x1b );
			st文字領域Array[ 0x4e ] = st文字領域79;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域80 = st文字領域94;
			st文字領域80.ch = 'q';
			st文字領域80.rc = new Rectangle( 2, 0xc7, 20, 0x1b );
			st文字領域Array[ 0x4f ] = st文字領域80;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域81 = st文字領域94;
			st文字領域81.ch = 'r';
			st文字領域81.rc = new Rectangle( 0x18, 0xc7, 0x12, 0x1b );
			st文字領域Array[ 80 ] = st文字領域81;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域82 = st文字領域94;
			st文字領域82.ch = 's';
			st文字領域82.rc = new Rectangle( 0x2a, 0xc7, 0x13, 0x1b );
			st文字領域Array[ 0x51 ] = st文字領域82;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域83 = st文字領域94;
			st文字領域83.ch = 't';
			st文字領域83.rc = new Rectangle( 0x3f, 0xc7, 0x10, 0x1b );
			st文字領域Array[ 0x52 ] = st文字領域83;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域84 = st文字領域94;
			st文字領域84.ch = 'u';
			st文字領域84.rc = new Rectangle( 80, 0xc7, 20, 0x1b );
			st文字領域Array[ 0x53 ] = st文字領域84;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域85 = st文字領域94;
			st文字領域85.ch = 'v';
			st文字領域85.rc = new Rectangle( 0x68, 0xc7, 20, 0x1b );
			st文字領域Array[ 0x54 ] = st文字領域85;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域86 = st文字領域94;
			st文字領域86.ch = 'w';
			st文字領域86.rc = new Rectangle( 0x7f, 0xc7, 0x1a, 0x1b );
			st文字領域Array[ 0x55 ] = st文字領域86;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域87 = st文字領域94;
			st文字領域87.ch = 'x';
			st文字領域87.rc = new Rectangle( 0x9a, 0xc7, 0x16, 0x1b );
			st文字領域Array[ 0x56 ] = st文字領域87;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域88 = st文字領域94;
			st文字領域88.ch = 'y';
			st文字領域88.rc = new Rectangle( 0xb1, 0xc7, 0x16, 0x1b );
			st文字領域Array[ 0x57 ] = st文字領域88;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域89 = st文字領域94;
			st文字領域89.ch = 'z';
			st文字領域89.rc = new Rectangle( 200, 0xc7, 0x13, 0x1b );
			st文字領域Array[ 0x58 ] = st文字領域89;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域90 = st文字領域94;
			st文字領域90.ch = '{';
			st文字領域90.rc = new Rectangle( 220, 0xc7, 15, 0x1b );
			st文字領域Array[ 0x59 ] = st文字領域90;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域91 = st文字領域94;
			st文字領域91.ch = '|';
			st文字領域91.rc = new Rectangle( 0xeb, 0xc7, 13, 0x1b );
			st文字領域Array[ 90 ] = st文字領域91;
			st文字領域94 = new ST文字領域();
			ST文字領域 st文字領域92 = st文字領域94;
			st文字領域92.ch = '}';
			st文字領域92.rc = new Rectangle( 1, 0xe3, 15, 0x1b );
			st文字領域Array[ 0x5b ] = st文字領域92;
			ST文字領域 st文字領域93 = new ST文字領域();
			st文字領域93.ch = '~';
			st文字領域93.rc = new Rectangle( 0x12, 0xe3, 0x12, 0x1b );
			st文字領域Array[ 0x5c ] = st文字領域93;

			st文字領域Array[ 0x5d ] = new ST文字領域();						// #24954 2011.4.23 yyagi
			st文字領域Array[ 0x5d ].ch = '@';
			st文字領域Array[ 0x5d ].rc = new Rectangle( 38, 227, 28, 28 );
			st文字領域Array[ 0x5e ] = new ST文字領域();
			st文字領域Array[ 0x5e ].ch = '`';
			st文字領域Array[ 0x5e ].rc = new Rectangle( 69, 226, 14, 29 );

	
			this.st文字領域 = st文字領域Array;
		}


		// メソッド

		public int n文字列長dot( string str )
		{
			return this.n文字列長dot( str, 1f );
		}
		public int n文字列長dot( string str, float fScale )
		{
			if( string.IsNullOrEmpty( str ) )
			{
				return 0;
			}
			int num = 0;
			foreach( char ch in str )
			{
				foreach( ST文字領域 st文字領域 in this.st文字領域 )
				{
					if( st文字領域.ch == ch )
					{
						num += (int) ( ( st文字領域.rc.Width - 5 ) * fScale );
						break;
					}
				}
			}
			return num;
		}
		public void t文字列描画( int x, int y, string str )
		{
			this.t文字列描画( x, y, str, false, 1f );
		}
		public void t文字列描画( int x, int y, string str, bool b強調 )
		{
			this.t文字列描画( x, y, str, b強調, 1f );
		}
		public void t文字列描画( int x, int y, string str, bool b強調, float fScale )
		{
			if( !base.b活性化してない && !string.IsNullOrEmpty( str ) )
			{
				CTexture texture = b強調 ? TJAPlayer3.Tx.Config_Font_Bold : TJAPlayer3.Tx.Config_Font;
				if( texture != null )
				{
					texture.vc拡大縮小倍率 = new Vector3( fScale, fScale, 1f );
					foreach( char ch in str )
					{
						foreach( ST文字領域 st文字領域 in this.st文字領域 )
						{
							if( st文字領域.ch == ch )
							{
								texture.t2D描画( TJAPlayer3.app.Device, x, y, st文字領域.rc );
								x += (int) ( ( st文字領域.rc.Width - 5 ) * fScale );
								break;
							}
						}
					}
				}
			}
		}


		// CActivity 実装

		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				//this.tx通常文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Screen font dfp.png" ), false );
				//this.tx強調文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Screen font dfp em.png" ), false );
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			//if( !base.b活性化してない )
			//{
			//	if( this.tx強調文字 != null )
			//	{
			//		this.tx強調文字.Dispose();
			//		this.tx強調文字 = null;
			//	}
			//	if( this.tx通常文字 != null )
			//	{
			//		this.tx通常文字.Dispose();
			//		this.tx通常文字 = null;
			//	}
				base.OnManagedリソースの解放();
			//}
		}
		

		// その他

		#region [ private ]
		//-----------------
		[StructLayout( LayoutKind.Sequential )]
		private struct ST文字領域
		{
			public char ch;
			public Rectangle rc;
		}

		private readonly ST文字領域[] st文字領域;
		//private CTexture tx強調文字;
		//private CTexture tx通常文字;
		//-----------------
		#endregion
	}
}
