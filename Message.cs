/* ----------------------------------------------------------------------------
Transonic MIDI Library
Copyright (C) 1995-2017  George E Greaney

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//J Glatt's Midi page: http://midi.teragonaudio.com/tech/midispec.htm

namespace Transonic.MIDI
{
    public class Message
    {
        public enum MESSAGECLASS { 
            CHANNEL, 
            SYSTEM, 
            META 
        }; 

        public int status;
        public MESSAGECLASS msgClass;
        public int varLenCount;

//- static methods ------------------------------------------------------------

        public static Message getMessage(byte[] data, int ofs) 
        {
            Message msg = null;
            int status = data[ofs];
            if (status >= 0x80 && status < 0xf0)
            {
                msg = getChannelMessage(data, ofs);
            }
            else if (status >= 0xf0 && status < 0xff)
            {
                msg = getSystemMessage(data, ofs);
            }
            else if (status == 0xff)
            {
                msg = getMetaMessage(data, ofs);
            }

            return msg;
        }

        static Message getChannelMessage(byte[] data, int ofs)
        {

            Message msg = null;
            int status = data[ofs];
            int msgtype = status / 16;
            int channel = status % 16;
            switch (msgtype)
            {
                case 0x8 :
                    msg = new NoteOffMessage(data, ofs, channel);
                    break;
                case 0x9:
                    msg = new NoteOnMessage(data, ofs, channel);
                    break;
                case 0xa:
                    msg = new AftertouchMessage(data, ofs, channel);
                    break;
                case 0xb:
                    msg = new ControllerMessage(data, ofs, channel);
                    break;
                case 0xc:
                    msg = new PatchChangeMessage(data, ofs, channel);
                    break;
                case 0xd:
                    msg = new ChannelPressureMessage(data, ofs, channel);
                    break;
                case 0xe:
                    msg = new PitchWheelMessage(data, ofs, channel);
                    break;
                default :
                    break;
            }
            msg.msgClass = MESSAGECLASS.CHANNEL;
            return msg;
        }

        static Message getSystemMessage(byte[] data, int ofs)
        {
            Message msg = null;
            int status = data[ofs];
            if (status == 0xF0)
            {
                msg = new SysExMessage(data, ofs); 
            }
            else
            {
                msg = new SystemMessage(data, ofs, status);
            }
            msg.msgClass = MESSAGECLASS.SYSTEM;
            return msg;
        }

        static Message getMetaMessage(byte[] data, int ofs)
        {
            Message msg = null;
            int msgtype = (int)data[ofs + 1];
            switch (msgtype) 
            {
                case 0x00:
                    msg = new SequenceNumberMessage(data, ofs);
                    break;
                case 0x01:
                    msg = new TextMessage(data, ofs);
                    break;
                case 0x02:
                    msg = new CopyrightMessage(data, ofs);
                    break;
                case 0x03:
                    msg = new TrackNameMessage(data, ofs);
                    break;
                case 0x04:
                    msg = new InstrumentMessage(data, ofs);
                    break;
                case 0x05:
                    msg = new LyricMessage(data, ofs);
                    break;
                case 0x06:
                    msg = new MarkerMessage(data, ofs);
                    break;
                case 0x07:
                    msg = new CuePointMessage(data, ofs);
                    break;
                case 0x08:
                    msg = new PatchNameMessage(data, ofs);
                    break;
                case 0x09:
                    msg = new DeviceNameMessage(data, ofs);
                    break;
                case 0x20:
                    msg = new MidiChannelMessage(data, ofs);
                    break;
                case 0x21:
                    msg = new MidiPortMessage(data, ofs);
                    break;
                case 0x2f:
                    msg = new EndofTrackMessage(data, ofs);
                    break;
                case 0x51:
                    msg = new TempoMessage(data, ofs);
                    break;
                case 0x54:
                    msg = new SMPTEOffsetMessage(data, ofs);
                    break;
                case 0x58:
                    msg = new TimeSignatureMessage(data, ofs);
                    break;
                case 0x59:
                    msg = new KeySignatureMessage(data, ofs);
                    break;
                default:
                    msg = new UnknownMetaMessage(data, ofs, msgtype);
                    break;
            }
            msg.msgClass = MESSAGECLASS.META;
            return msg;
        }


//- base class ----------------------------------------------------------------

        public Message(int _status)
        {
            status = _status;
            varLenCount = 0;
        }

        public Message copy()
        {
            return (Message)this.MemberwiseClone();
        }

        virtual public byte[] getDataBytes() 
        {
            return null;
        }

        protected uint getVariableLengthVal(byte[] data, int ofs)
        {
            uint result = 0;         //largest var len quant allowed = 0xffffffff
            int start = ofs;
            uint b = data[ofs++];
            while (b >= 0x80)
            {
                uint d = b % 128;
                result *= 128;
                result += d;
                b = data[ofs++];
            }
            result *= 128;
            result += b;
            varLenCount = ofs - start;
            return result;
        }


        protected String getMessageText(byte[] data, int ofs, uint len)
        {
            StringBuilder str = new StringBuilder((int)len);
            for (int i = 0; i < len; i++)
            {
                byte ch = (byte)data[ofs++];
                str.Append(Convert.ToChar(ch));
            }
            return str.ToString();
        }
    }

//- subclasses ----------------------------------------------------------------

//-----------------------------------------------------------------------------
//  CHANNEL MESSAGES
//-----------------------------------------------------------------------------

    public class ChannelMessage : Message
    {
        public int channel;

        public ChannelMessage(int status, int _channel) : base(status)
        {
            channel = _channel;
        }
    }

    public class NoteOffMessage : ChannelMessage   //0x80
    {
        public int noteNumber;
        public int velocity;

        public NoteOffMessage(byte[] data, int ofs, int _channel) : base(0x80, _channel)
        {
            noteNumber = (int)data[ofs + 1];
            velocity = (int)data[ofs + 2];
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[3];
            bytes[0] = (byte)(0x80 + channel);
            bytes[1] = (byte)noteNumber;
            bytes[2] = (byte)velocity;
            return bytes;
        }

        public override string ToString()
        {
            return "Note Off (" + channel + ") note = " + noteNumber;
        }
    }

    public class NoteOnMessage : ChannelMessage     //0x90
    {
        public int noteNumber;
        public int velocity;

        public NoteOnMessage(byte[] data, int ofs, int _channel) : base(0x90, _channel)
        {
            noteNumber = (int)data[ofs + 1];
            velocity = (int)data[ofs + 2];
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[3];
            bytes[0] = (byte)(0x90 + channel);
            bytes[1] = (byte)noteNumber;
            bytes[2] = (byte)velocity;
            return bytes;
        }

        public override string ToString()
        {
            return "Note On (" + channel + ") note = " + noteNumber + ", velocity = " + velocity;
        }
    }

    public class AftertouchMessage : ChannelMessage     //0xA0
    {
        public int noteNumber;
        public int pressure;

        public AftertouchMessage(byte[] data, int ofs, int _channel)
            : base(0xa0, _channel)
        {
            noteNumber = (int)data[ofs + 1];
            pressure = (int)data[ofs + 2];
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[3];
            bytes[0] = (byte)(0xa0 + channel);
            bytes[1] = (byte)noteNumber;
            bytes[2] = (byte)pressure;
            return bytes;
        }
    }

    public class ControllerMessage : ChannelMessage     //0xB0
    {
        public int controllerNumber;
        public int controllerValue;

        public ControllerMessage(byte[] data, int ofs, int _channel)
            : base(0xb0, _channel)
        {
            controllerNumber = (int)data[ofs + 1];
            controllerValue = (int)data[ofs + 2];
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[3];
            bytes[0] = (byte)(0xb0 + channel);
            bytes[1] = (byte)controllerNumber;
            bytes[2] = (byte)controllerValue;
            return bytes;
        }

        public override string ToString()
        {
            return "Controller (" + channel + ") number = " + controllerNumber + ", value = " + controllerValue;
        }
    }

    public class PatchChangeMessage : ChannelMessage       //0xC0
    {
        public int patchNumber;

        public PatchChangeMessage(int _channel, byte b1)
            : base(0xc0, _channel)
        {
            patchNumber = (int)b1;
        }

        public PatchChangeMessage(byte[] data, int ofs, int _channel)
            : base(0xc0, _channel)
        {
            patchNumber = (int)data[ofs + 1];
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(0xc0 + channel);
            bytes[1] = (byte)patchNumber;
            return bytes;
        }

        public override string ToString()
        {
            return "Patch Change (" + channel + ") number = " + patchNumber;
        }
    }

    public class ChannelPressureMessage : ChannelMessage       //0xD0
    {
        public int pressure;

        public ChannelPressureMessage(byte[] data, int ofs, int _channel)
            : base(0xd0, _channel)
        {
            pressure = (int)data[ofs + 1];
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(0xd0 + channel);
            bytes[1] = (byte)pressure;
            return bytes;
        }
    }

    public class PitchWheelMessage : ChannelMessage     //0xE0
    {
        public int wheel;

        public PitchWheelMessage(byte[] data, int ofs, int _channel)
            : base(0xe0, _channel)
        {
            int b1 = (int)data[ofs + 1];
            int b2 = (int)data[ofs + 2];
            wheel = b1 * 128 + b2;
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[3];
            bytes[0] = (byte)(0xe0 + channel);
            bytes[1] = (byte)(wheel / 128);
            bytes[2] = (byte)(wheel % 128);
            return bytes;
        }
    }

//-----------------------------------------------------------------------------
//  SYSTEM MESSAGES
//-----------------------------------------------------------------------------

    public class SysExMessage : Message
    {
        List<int> sysExData;

        public SysExMessage(byte[] data, int ofs)
            : base(0xF0)
        {
            sysExData = new List<int>();
            ofs++;
            int b1 = (int)data[ofs++];
            while (b1 != 0xf7)
            {
                sysExData.Add(b1);
                b1 = (int)data[ofs++];
            }            
        }
    }

    public enum SYSTEMMESSAGE { 
        QUARTERFRAME = 0Xf1, 
        SONGPOSITION, 
        SONGSELECT, 
        UNKNOWN1,
        UNKNOWN2,
        TUNEREQUEST,
        SYSEXEND,
        MIDICLOCK,
        MIDITICK, 
        MIDISTART, 
        MIDICONTINUE, 
        MIDISTOP,
        UNKNOWN3,
        ACTIVESENSE = 0xfe
    }; 

    public class SystemMessage : Message
    {
        SYSTEMMESSAGE msgtype;
        int value;

        public SystemMessage(byte[] data, int ofs, int status)
            : base(status)
        {
            msgtype = (SYSTEMMESSAGE)status;
            value = 0;
            switch (msgtype)
            {
                case SYSTEMMESSAGE.QUARTERFRAME :
                case SYSTEMMESSAGE.SONGSELECT :
                    value = (int)data[ofs + 1];
                    break;
                case SYSTEMMESSAGE.SONGPOSITION:
                    int b1 = (int)data[ofs + 1];
                    int b2 = (int)data[ofs + 2];
                    value = b1 * 128 + b2;
                    break;
                default :
                    break;
            }        
        }
    }

//-----------------------------------------------------------------------------
//  META MESSAGES
//-----------------------------------------------------------------------------

    public class SequenceNumberMessage : Message    //0xff 0x00
    {
        int b1, b2;

        public SequenceNumberMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            b1 = 0;
            b2 = 0;
            if (length > 0)
            {
                b1 = (int)data[ofs++];
                b2 = (int)data[ofs++];
            }
        }
    }

    public class TextMessage : Message      //0xff 0x01
    {
        String text;

        public TextMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            text = getMessageText(data, ofs, length);
        }
    }

    public class CopyrightMessage : Message     //0xff 0x02
    {
        String copyright;

        public CopyrightMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            copyright = getMessageText(data, ofs, length);
        }
    }

    public class TrackNameMessage : Message     //0xff 0x03
    {
        public String trackName;

        public TrackNameMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            trackName = getMessageText(data, ofs, length);
        }
    }

    public class InstrumentMessage : Message    //0xff 0x04
    {
        public String instrumentName;

        public InstrumentMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            instrumentName = getMessageText(data, ofs, length);
        }
    }

    public class LyricMessage : Message     //0xff 0x05
    {
        public String lyric;

        public LyricMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            lyric = getMessageText(data, ofs, length);
        }
    }

    public class MarkerMessage : Message        //0xff 0x06
    {
        public String marker;

        public MarkerMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            marker = getMessageText(data, ofs, length);
        }
    }

    public class CuePointMessage : Message      //0xff 0x07
    {
        public String cuePoint;

        public CuePointMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            cuePoint = getMessageText(data, ofs, length);
        }
    }

    public class PatchNameMessage : Message        //0xff 0x08
    {
        public String patchName;

        public PatchNameMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            patchName = getMessageText(data, ofs, length);
        }
    }

    public class DeviceNameMessage : Message        //0xff 0x09
    {
        public String deviceName;

        public DeviceNameMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            deviceName = getMessageText(data, ofs, length);
        }
    }

    //obsolete
    public class MidiChannelMessage : Message       //0xff 0x20
    {
        int cc;

        public MidiChannelMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            cc = (int)data[ofs++];
        }
    }

    //obsolete
    public class MidiPortMessage : Message          //0xff 0x21
    {
        int pp;

        public MidiPortMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            pp = (int)data[ofs++];
        }
    }

    public class EndofTrackMessage : Message        //0xff 0x2f
    {

        public EndofTrackMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            //length should be 0
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[3];
            bytes[0] = 0xff;
            bytes[1] = 0x2f;
            bytes[2] = 0x00;
            return bytes;
        }

        public override string ToString()
        {
            return "End of Track";
        }
    }

    public class TempoMessage : Message             //0xff 0x51
    {
        public int tempo;
        public Timing timing;

        public TempoMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            int b1 = (int)data[ofs++];
            int b2 = (int)data[ofs++];
            int b3 = (int)data[ofs++];
            tempo = ((b1 * 16 + b2) * 256) + b3;
            timing = null;
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[6];
            bytes[0] = 0xff;
            bytes[1] = 0x51;
            bytes[2] = 0x03;
            int _tempo = tempo;
            bytes[5] = (byte)(_tempo % 0x100);
            _tempo = _tempo / 0x100;
            bytes[4] = (byte)(_tempo % 0x100);
            _tempo = _tempo / 0x100;
            bytes[3] = (byte)(_tempo % 0x100);
            return bytes;
        }

        public override string ToString()
        {
            return "Tempo = " + tempo + " at time = " + timing.microsec;
        }
    }

    public class SMPTEOffsetMessage : Message       //0xff 0x54
    {
        int hour, min, sec, frame, frame100;

        public SMPTEOffsetMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            hour = (int)data[ofs++];
            min = (int)data[ofs++];
            sec = (int)data[ofs++];
            frame = (int)data[ofs++];
            frame100 = (int)data[ofs++];
        }
    }

    public class TimeSignatureMessage : Message         //0xff 0x58
    {
        int numerator;
        int denominator;
        int clicks;
        int clocksPerQuarter;

        public TimeSignatureMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            numerator = (int)data[ofs++];
            int b1 = (int)data[ofs++];
            denominator = (int)Math.Pow(2.0, b1);
            clicks = (int)data[ofs++];
            clocksPerQuarter = (int)data[ofs++];
        }

        override public byte[] getDataBytes()
        {
            byte[] bytes = new byte[7];
            bytes[0] = 0xff;
            bytes[1] = 0x58;
            bytes[2] = 0x04;
            bytes[3] = (byte)numerator;
            bytes[4] = (byte)(Math.Log(denominator, 2.0));
            bytes[5] = (byte)clicks;
            bytes[6] = (byte)clocksPerQuarter;
            return bytes;
        }

        public override string ToString()
        {
            return "Time Signature = " + numerator + "/" + denominator + " clicks = " + clicks + " clocks/quarter = " + clocksPerQuarter;
        }

    }

    public class KeySignatureMessage : Message          //0xff 0x59
    {
        int sf;
        int mi;

        public KeySignatureMessage(byte[] data, int ofs)
            : base(0xFF)
        {
            sf = (int)data[ofs++];
            mi = (int)data[ofs++];
        }
    }

    public class UnknownMetaMessage : Message
    {
        int msgtype;

        public UnknownMetaMessage(byte[] data, int ofs, int _msgtype)
            : base(0xFF)
        {
            msgtype = _msgtype;
            uint length = getVariableLengthVal(data, ofs);
            ofs += varLenCount;
            Console.WriteLine("got unknown meta message type = {0}", msgtype.ToString("X2"));            
        }
    }

}

//Console.WriteLine("there's no sun in the shadow of the wizard");
