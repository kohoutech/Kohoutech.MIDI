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
        public MetaEvent(uint time)
            : base(time)
        {            
        }
    }

    public class SequenceNumberEvent : MetaEvent    //0xff 0x00
    {
        int val;

        public SequenceNumberEvent(uint time, int _val)
            : base(time)
        {
            val = _val;
        }
    }

    //text events
    public class TextEvent : MetaEvent      //0xff 0x01
    {
        String text;

        public TextEvent(uint time, String _text)
            : base(time)
        {
            text = _text;
        }
    }

    public class CopyrightEvent : MetaEvent     //0xff 0x02
    {
        String copyright;

        public CopyrightEvent(uint time, String _copy)
            : base(time)
        {
            copyright = _copy;
        }
    }

    public class TrackNameEvent : MetaEvent     //0xff 0x03
    {
        public String trackName;

        public TrackNameEvent(uint time, String name)
            : base(time)
        {
            trackName = name;
        }
    }

    public class InstrumentEvent : MetaEvent    //0xff 0x04
    {
        public String instrumentName;

        public InstrumentEvent(uint time, String name)
            : base(time)
        {
            instrumentName = name;
        }
    }

    public class LyricEvent : MetaEvent     //0xff 0x05
    {
        public String lyric;

        public LyricEvent(uint time, String _lyric)
            : base(time)
        {
            lyric = _lyric;
        }
    }

    public class MarkerEvent : MetaEvent        //0xff 0x06
    {
        public String marker;

        public MarkerEvent(uint time, String _marker)
            : base(time)
        {
            marker = _marker;
        }
    }

    public class CuePointEvent : MetaEvent      //0xff 0x07
    {
        public String cuePoint;

        public CuePointEvent(uint time, String cue)
            : base(time)
        {
            cuePoint = cue;
        }
    }

    public class PatchNameEvent : MetaEvent        //0xff 0x08
    {
        public String patchName;

        public PatchNameEvent(uint time, String name)
            : base(time)
        {
            patchName = name;
        }
    }

    public class DeviceNameEvent : MetaEvent        //0xff 0x09
    {
        public String deviceName;

        public DeviceNameEvent(uint time, String name)
            : base(time)
        {
            deviceName = name;
        }
    }

    //obsolete
    public class MidiChannelEvent : MetaEvent       //0xff 0x20
    {
        int channelNum;

        public MidiChannelEvent(uint time, int cc)
            : base(time)
        {
            channelNum = cc;
        }
    }

    //obsolete
    public class MidiPortEvent : MetaEvent          //0xff 0x21
    {
        int portNum;

        public MidiPortEvent(uint time, int pp)
            : base(time)
        {
            portNum = pp;
        }
    }

    //end of track
    public class EndofTrackEvent : MetaEvent        //0xff 0x2f
    {

        public EndofTrackEvent(uint time)
            : base(time)
        {
            //length should be 0
        }

        public override string ToString()
        {
            return "End of Track";
        }
    }

    //timing events
    public class TempoEvent : MetaEvent             //0xff 0x51
    {
        public int tempo;        

        public TempoEvent(uint time, int _tempo)
            : base(time)
        {
            tempo = _tempo;            
        }

        public override string ToString()
        {
            return "Tempo = " + tempo;
        }
    }

    public class SMPTEOffsetEvent : MetaEvent       //0xff 0x54
    {
        int frameRate, hour, min, sec, frame, frame100;

        public SMPTEOffsetEvent(uint time, int rr, int hh, int mn, int se, int fr, int ff)
            : base(time)
        {
            frameRate = rr;
            hour = hh;
            min = mn;
            sec = se;
            frame = fr;
            frame100 = ff;
        }
    }

    public class TimeSignatureEvent : MetaEvent         //0xff 0x58
    {
        int numer;
        int denom;
        int clicks;
        int clocksPerQuarter;

        public TimeSignatureEvent(uint time, int nn, int dd, int cc, int bb)
            : base(time)
        {
            numer = nn;
            denom = dd;
            clicks = cc;
            clocksPerQuarter = bb;
        }

        public override string ToString()
        {
            return "Time Signature = " + numer + "/" + denom + " clicks = " + clicks + " clocks/quarter = " + clocksPerQuarter;
        }
    }

    public class KeySignatureEvent : MetaEvent          //0xff 0x59
    {
        int keySig;
        bool minor;

        public KeySignatureEvent(uint time, int sf, int mi)
            : base(time)
        {
            keySig = sf;
            minor = (mi == 1);
        }
    }

    public class ProprietaryEvent : MetaEvent          //0xff 0x7f
    {
        List<byte> data;

        public ProprietaryEvent(uint time, List<byte> _data)
            : base(time)
        {
            data = _data;            
        }
    }


}
