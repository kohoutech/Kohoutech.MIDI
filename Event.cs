/* ----------------------------------------------------------------------------
Transonic MIDI Library
Copyright (C) 1995-2018  George E Greaney

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

namespace Transonic.MIDI
{
    //base event class
    public class Event : IComparable<Event>
    {
        public uint time;

        public Event(uint _time)
        {
            time = _time;       //time in ticks
        }

        //public void dump()
        //{
        //    Console.WriteLine("time = {0}, msg = {1}", time, msg);
        //}

        public int CompareTo(Event other)
        {
            return this.time.CompareTo(other.time);
        }
    }

//-----------------------------------------------------------------------------
//  MESSAGE EVENTS
//-----------------------------------------------------------------------------

    public class MessageEvent : Event
    {
        public Message msg;

        public MessageEvent(uint time, Message _msg)
            : base(time)
        {
            msg = _msg;         //midi message
        }

    }

//-----------------------------------------------------------------------------
//  META EVENTS
//-----------------------------------------------------------------------------

    //J Glatt's Midi file page describing defined meta events: http://midi.teragonaudio.com/tech/midifile.htm

    //meta event base class
    public class MetaEvent : Event
    {
        public int datalen;

        public MetaEvent(MidiInStream stream)
            : base()
        {
            datalen = (int)stream.getVariableLengthVal();
        }
    }

    public class SequenceNumberMessage : MetaEvent    //0xff 0x00
    {
        int b1, b2;

        public SequenceNumberMessage(MidiInStream stream)
            : base(stream)
        {
            b1 = 0;
            b2 = 0;
            if (datalen > 0)
            {
                b1 = stream.getOne();
                b2 = stream.getOne();
            }
        }
    }

    public class TextMessage : MetaEvent      //0xff 0x01
    {
        String text;

        public TextMessage(MidiInStream stream)
            : base(stream)
        {
            text = stream.getString(datalen);
        }
    }

    public class CopyrightMessage : MetaEvent     //0xff 0x02
    {
        String copyright;

        public CopyrightMessage(MidiInStream stream)
            : base(stream)
        {
            copyright = stream.getString(datalen);
        }
    }

    public class TrackNameMessage : MetaEvent     //0xff 0x03
    {
        public String trackName;

        public TrackNameMessage(MidiInStream stream)
            : base(stream)
        {
            trackName = stream.getString(datalen);
        }
    }

    public class InstrumentMessage : MetaEvent    //0xff 0x04
    {
        public String instrumentName;

        public InstrumentMessage(MidiInStream stream)
            : base(stream)
        {
            instrumentName = stream.getString(datalen);
        }
    }

    public class LyricMessage : MetaEvent     //0xff 0x05
    {
        public String lyric;

        public LyricMessage(MidiInStream stream)
            : base(stream)
        {
            lyric = stream.getString(datalen);
        }
    }

    public class MarkerMessage : MetaEvent        //0xff 0x06
    {
        public String marker;

        public MarkerMessage(MidiInStream stream)
            : base(stream)
        {
            marker = stream.getString(datalen);
        }
    }

    public class CuePointMessage : MetaEvent      //0xff 0x07
    {
        public String cuePoint;

        public CuePointMessage(MidiInStream stream)
            : base(stream)
        {
            cuePoint = stream.getString(datalen);
        }
    }

    public class PatchNameMessage : MetaEvent        //0xff 0x08
    {
        public String patchName;

        public PatchNameMessage(MidiInStream stream)
            : base(stream)
        {
            patchName = stream.getString(datalen);
        }
    }

    public class DeviceNameMessage : MetaEvent        //0xff 0x09
    {
        public String deviceName;

        public DeviceNameMessage(MidiInStream stream)
            : base(stream)
        {
            deviceName = stream.getString(datalen);
        }
    }

    //obsolete
    public class MidiChannelMessage : MetaEvent       //0xff 0x20
    {
        int cc;

        public MidiChannelMessage(MidiInStream stream)
            : base(stream)
        {
            cc = stream.getOne();
        }
    }

    //obsolete
    public class MidiPortMessage : MetaEvent          //0xff 0x21
    {
        int pp;

        public MidiPortMessage(MidiInStream stream)
            : base(stream)
        {
            pp = stream.getOne();
        }
    }

    public class EndofTrackMessage : MetaEvent        //0xff 0x2f
    {

        public EndofTrackMessage(MidiInStream stream)
            : base(stream)
        {
            //length should be 0
        }

        public override string ToString()
        {
            return "End of Track";
        }
    }

    public class TempoMessage : MetaEvent             //0xff 0x51
    {
        public int tempo;
        public Tempo timing;

        public TempoMessage(MidiInStream stream)
            : base(stream)
        {
            int b1 = stream.getOne();
            int b2 = stream.getOne();
            int b3 = stream.getOne();
            tempo = ((b1 * 0x100 + b2) * 0x100) + b3;
            timing = null;
        }

        public override string ToString()
        {
            return "Tempo = " + tempo + " at time = " + timing.microsec;
        }
    }

    public class SMPTEOffsetMessage : MetaEvent       //0xff 0x54
    {
        int hour, min, sec, frame, frame100;

        public SMPTEOffsetMessage(MidiInStream stream)
            : base(stream)
        {
            hour = stream.getOne();
            min = stream.getOne();
            sec = stream.getOne();
            frame = stream.getOne();
            frame100 = stream.getOne();
        }
    }

    public class TimeSignatureMessage : MetaEvent         //0xff 0x58
    {
        int numerator;
        int denominator;
        int clicks;
        int clocksPerQuarter;

        public TimeSignatureMessage(MidiInStream stream)
            : base(stream)
        {
            numerator = stream.getOne();
            int b1 = stream.getOne();
            denominator = (int)Math.Pow(2.0, b1);
            clicks = stream.getOne();
            clocksPerQuarter = stream.getOne();
        }

        public override string ToString()
        {
            return "Time Signature = " + numerator + "/" + denominator + " clicks = " + clicks + " clocks/quarter = " + clocksPerQuarter;
        }
    }

    public class KeySignatureMessage : MetaEvent          //0xff 0x59
    {
        int sf;
        int mi;

        public KeySignatureMessage(MidiInStream stream)
            : base(stream)
        {
            sf = stream.getOne();
            mi = stream.getOne();
        }
    }

    public class UnknownMetaMessage : MetaEvent
    {
        int msgtype;

        public UnknownMetaMessage(MidiInStream stream, int _msgtype)
            : base(stream)
        {
            msgtype = _msgtype;
            stream.skipBytes(datalen);
        }
    }


}
