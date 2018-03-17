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
using System.IO;

using Transonic.MIDI.System;

//J Glatt's Midi page: http://midi.teragonaudio.com/tech/midifile.htm
//Somascape's MIDI Files Specification: http://www.somascape.org/midi/tech/mfile.html
//Standard MIDI-File Format Spec. 1.1 http://www.music.mcgill.ca/~ich/classes/mumt306/midiformat.pdf

namespace Transonic.MIDI
{
    public class MidiFile
    {
        static int curTrackNum;
        static int runningStatus;
        static bool sysexCont;
        static SysExMessage prevSysEx;


//- loading -------------------------------------------------------------------

        //read in a midi seqence from a standard midi file
        public static Sequence readMidiFile(String filename)
        {
            MidiInStream stream = new MidiInStream(filename);

            //read midi file header
            String sig = stream.getString(4);
            uint hdrsize = stream.getFour();
            int fileFormat = stream.getTwo();
            int trackCount = stream.getTwo();
            int division = stream.getTwo();

            if (!sig.Equals("MThd") || hdrsize != 6)
            {
                throw new MidiFileException(filename + " is not a valid MIDI file ", 0);
            }

            Sequence seq = new Sequence(division);
            loadTrackZeroData(stream, seq);

            //read midi track data
            for (int trackNum = 1; trackNum < trackCount; trackNum++)
            {
                curTrackNum = trackNum;
                loadTrackData(stream, seq, trackNum);                
            }

            finalizeTracks(seq);
            return seq;
        }
        
        //build the tempo, meter and marker maps from tempo message from track 0
        private static void loadTrackZeroData(MidiInStream stream, Sequence seq)
        {
            //read track header
            String trackSig = stream.getString(4);
            uint trackDataLength = stream.getFour();

            if (!trackSig.Equals("MTrk"))
            {
                throw new MidiFileException(stream.filename + " has an invalid track 0 at ", stream.getDataPos() - 8);
            }

            int currentTime = 0;        //event time in ticks
            runningStatus = 0;
            sysexCont = false;
            prevSysEx = null;
            Event evt;

            Meter prevMeter = null;

            int startpos = stream.getDataPos();
            while ((stream.getDataPos() - startpos) < trackDataLength)
            {
                currentTime += (int)stream.getVariableLengthVal();      //add delta time to current num of ticks
                evt = loadEventData(stream);

                if (evt is TempoEvent)
                {
                    Tempo tempo = new Tempo(currentTime, ((TempoEvent)evt).tempo);
                    seq.tempoMap.addTempo(tempo);
                }
                else if (evt is SMPTEOffsetEvent)       //not handling smpte timing yet
                {
                }
                else if (evt is TimeSignatureEvent)
                {
                    int keysig = (prevMeter != null) ? prevMeter.keysig : 0;
                    Meter meter = new Meter(currentTime, ((TimeSignatureEvent)evt).numer, ((TimeSignatureEvent)evt).denom, keysig);
                    seq.meterMap.addMeter(meter);
                    prevMeter = meter;
                }
                else if (evt is KeySignatureEvent)
                {
                    int numer = 4;
                    int denom = 4;
                    if (prevMeter != null)
                    {
                        numer = prevMeter.numer;
                        denom = prevMeter.denom;
                    }
                    Meter meter = new Meter(currentTime, numer, denom, ((KeySignatureEvent)evt).keySig);
                    seq.meterMap.addMeter(meter);
                    prevMeter = meter;
                }
                else if (evt is MarkerEvent)
                {
                }
                else if (evt is CuePointEvent)
                {
                }
            } 
        }        

        //read data from a single track chunk
        private static void loadTrackData(MidiInStream stream, Sequence seq, int trackNum)
        {
            //read track header
            String trackSig = stream.getString(4);
            uint trackDataLength = stream.getFour();

            if (!trackSig.Equals("MTrk"))
            {
                throw new MidiFileException(stream.filename + " has an invalid track at ", stream.getDataPos() - 8);
            }

            Track track = seq.addTrack();                           //get new track from sequence
            track.setName("Track " + trackNum.ToString());

            int currentTime = 0;                //event time in ticks
            runningStatus = 0;
            sysexCont = false;
            prevSysEx = null;
            Event evt;

            int startpos = stream.getDataPos();
            while ((stream.getDataPos() - startpos) < trackDataLength)
            {
                currentTime += (int)stream.getVariableLengthVal();      //add delta time to current num of ticks
                evt = loadEventData(stream);
                if (evt != null)
                {
                    track.addEvent(evt, currentTime);
                }
            }        
        }

        //read data for a single event in a track's data, convert the event's delta time to an absolute track time
        private static Event loadEventData(MidiInStream stream)
        {
            Event evt = null;

            int status = stream.getOne();
            if (status < 0x80)                              //running status 
            {
                stream.pushBack(1);
                status = runningStatus;
            }
            if (status >= 0x80 && status < 0xff)             //message event
            {
                Message msg = loadMessageData(stream, status);
                runningStatus = status;
                evt = new MessageEvent(msg);
            }
            else if (status == 0xff)                        //meta event
            {
                evt = loadMetaEventData(stream);            //this may return null for unrecognized events
            }
            else
            {
                throw new MidiFileException(stream.filename + " has an invalid event at", stream.getDataPos() - 8);
            }
            return evt;
        }

        //read data for a midi message (80 - ff), handle sysex continuation and escape sequences
        private static Message loadMessageData(MidiInStream stream, int status)
        {
            Message msg = null;

            if (status < 0xF0)          //midi channel message
            {
                int msgtype = status / 16;
                int channel = status % 16;

                int b1 = stream.getOne();
                int b2 = 0;
                if ((msgtype != 0xC) && (msgtype != 0xD))
                {
                    b2 = stream.getOne();
                }

                switch (msgtype)        
                {
                    case 0x8:
                        msg = new NoteOffMessage(channel, b1, b2);
                        break;
                    case 0x9:
                        msg = new NoteOnMessage(channel, b1, b2);
                        break;
                    case 0xa:
                        msg = new AftertouchMessage(channel, b1, b2);
                        break;
                    case 0xb:
                        msg = new ControllerMessage(channel, b1, b2);
                        break;
                    case 0xc:
                        msg = new PatchChangeMessage(channel, b1);
                        break;
                    case 0xd:
                        msg = new ChannelPressureMessage(channel, b1);
                        break;
                    case 0xe:
                        int wheelamt = ((b1 % 128) * 128) + (b2 % 128);
                        msg = new PitchWheelMessage(channel, wheelamt);
                        break;
                    default:
                        break;
                }
                //convert noteon msg w/ vel = 0 to noteoff msg
                if (msg is NoteOnMessage)
                {
                    NoteOnMessage noteOn = (NoteOnMessage)msg;
                    if (noteOn.velocity == 0)
                    {
                        NoteOffMessage noteOff = new NoteOffMessage(noteOn.channel, noteOn.noteNumber, 0);
                        msg = noteOff;
                    }
                }
            }
            else if (status == 0xF0)            //sysex message
            {
                int len = stream.getOne();
                List<byte> sysExData = stream.getRange(len);
                sysexCont = (sysExData[sysExData.Count - 1] != 0xf7);       //is the last byte of sysex data a F7?
                msg = new SysExMessage(sysExData);
                prevSysEx = (SysExMessage)msg;
                runningStatus = 0;                      //sysex msg cancel running status
            }
            else if (status == 0xF7)            
            {
                if (sysexCont)                  //sysex continuation - append this data to prev sysex message
                {
                    int len = stream.getOne();
                    List<byte> contData = stream.getRange(len);
                    sysexCont = (contData[contData.Count - 1] != 0xf7);       //is the last byte of sysex data a F7?
                    prevSysEx.sysExData.AddRange(contData);
                }
                else
                {                                   //escape sequence
                    int len = stream.getOne();
                    List<byte> escData = stream.getRange(len);
                    msg = new EscapeMessage(escData);                
                }
                runningStatus = 0;
            }
            else
            {                                   //system common msgs shouldn't occur here, but if they do, we need to skip them
                int b1 = 0;
                int b2 = 0;
                int datalen = SystemMessage.SysMsgLen[status - 0xF0] - 1;
                if (datalen > 0)
                {
                    b1 = stream.getOne();
                }
                if (datalen > 1)
                {
                    b2 = stream.getOne();
                    b1 = ((b1 % 128) * 128) + (b2 % 128);
                }
                msg = new SystemMessage(status, b1);      
                runningStatus = 0;
            }

            return msg;
        }

        //read data for known meta events & skip any we don't recognize
        private static MetaEvent loadMetaEventData(MidiInStream stream)
        {
            MetaEvent meta = null;
            int metatype = stream.getOne();
            int metalen = (int)stream.getVariableLengthVal();

            switch (metatype)
            {
                case 0x00:
                    if (metalen == 0)
                    {
                        meta = new SequenceNumberEvent(curTrackNum);
                    }
                    if (metalen >= 2 )
                    {
                        int val = stream.getTwo();
                        metalen -= 2;
                        meta = new SequenceNumberEvent(val);
                    }
                    break;

                //text events
                case 0x01:
                    String txt = stream.getString(metalen);
                    metalen = 0;
                    meta = new TextEvent(txt);
                    break;
                case 0x02:
                    String copyright = stream.getString(metalen);
                    metalen = 0;
                    meta = new CopyrightEvent(copyright);
                    break;
                case 0x03:
                    String trackname = stream.getString(metalen);
                    metalen = 0;
                    meta = new TrackNameEvent(trackname);
                    break;
                case 0x04:
                    String instrument = stream.getString(metalen);
                    metalen = 0;
                    meta = new InstrumentEvent(instrument);
                    break;
                case 0x05:
                    String lyric = stream.getString(metalen);
                    metalen = 0;
                    meta = new LyricEvent(lyric);
                    break;
                case 0x06:
                    String marker = stream.getString(metalen);
                    metalen = 0;
                    meta = new MarkerEvent(marker);
                    break;
                case 0x07:
                    String cue = stream.getString(metalen);
                    metalen = 0;
                    meta = new CuePointEvent(cue);
                    break;
                case 0x08:
                    String patchname = stream.getString(metalen);
                    metalen = 0;
                    meta = new PatchNameEvent(patchname);
                    break;
                case 0x09:
                    String devname = stream.getString(metalen);
                    metalen = 0;
                    meta = new DeviceNameEvent(devname);
                    break;

                //obsolete events
                case 0x20:
                    int chanNum = stream.getOne();
                    metalen -= 1;
                    meta = new MidiChannelEvent(chanNum);
                    break;
                case 0x21:
                    int portNum = stream.getOne();
                    metalen -= 1;
                    meta = new MidiPortEvent(portNum);
                    break;

                //end of track event
                case 0x2f:
                    meta = new EndofTrackEvent();
                    break;

                //timing events
                case 0x51:
                    int t1 = stream.getTwo();
                    int t2 = stream.getOne();
                    int tempo = (t1 * 256) + t2;
                    metalen -= 3;
                    meta = new TempoEvent(tempo);
                    break;
                case 0x54:
                    int hr = stream.getOne();
                    int rr = (hr / 32) % 4;
                    int hh = hr % 32;
                    int mn = stream.getOne();
                    int se = stream.getOne();
                    int fr = stream.getOne();
                    int ff = stream.getOne();
                    metalen -= 5;                    
                    meta = new SMPTEOffsetEvent(rr, hh, mn, se, fr, ff);
                    break;
                case 0x58:
                    int nn = stream.getOne();
                    int b1 = stream.getOne();
                    int dd = (int)Math.Pow(2.0, b1);
                    int cc = stream.getOne();
                    int bb = stream.getOne();
                    metalen -= 4;  
                    meta = new TimeSignatureEvent(nn, dd, cc, bb);
                    break;
                case 0x59:
                    int sf = stream.getOne();
                    int mi = stream.getOne();
                    metalen -= 2;
                    meta = new KeySignatureEvent(sf, mi);
                    break;

                //other people's events
                case 0x7f:
                    List<byte> propdata = stream.getRange(metalen);
                    metalen = 0;
                    meta = new ProprietaryEvent(propdata);
                    break;

                //skip any other events
                default:
                    break;
            }
            stream.skipBytes(metalen);      //skip unknown events & any extra bytes at the end of known events
            runningStatus = 0;              //meta events cancel running status

            return meta;
        }

        public static void finalizeTracks(Sequence seq)
        {
            for (int i = 1; i < seq.tracks.Count; i++)
            {
                finalizeTrack(seq.tracks[i]);
            }
        }

        public static void finalizeTrack(Track track)
        {

            bool haveName = false;
            bool haveOutChannel = false;
            bool havePatchNum = false;
            bool haveVolume = false;

            for (int i = 0; i < track.events.Count; i++)
            {
                if (!haveName && (track.events[i] is TrackNameEvent))
                {
                    track.setName(((TrackNameEvent)track.events[i]).trackName);
                    haveName = true;
                }

        //        if (!haveOutChannel && events[i].msg is NoteOnMessage)
        //        {
        //            NoteOnMessage noteMsg = (NoteOnMessage)events[i].msg;
        //            outputChannel = noteMsg.channel;
        //            haveOutChannel = true;
        //        }

        //        if (!havePatchNum && events[i].msg is PatchChangeMessage)
        //        {
        //            PatchChangeMessage patchMsg = (PatchChangeMessage)events[i].msg;
        //            patchNum = patchMsg.patchNumber;
        //            havePatchNum = true;
        //        }

        //        if (!haveVolume && events[i].msg is ControllerMessage)
        //        {
        //            ControllerMessage ctrlMsg = (ControllerMessage)events[i].msg;
        //            if (ctrlMsg.ctrlNumber == 7)
        //            {
        //                volume = ctrlMsg.ctrlValue;
        //                haveVolume = true;
        //            }
        //        }

        //        if (haveName && haveOutChannel && havePatchNum && haveVolume) break;
            }
        }

//- saving -------------------------------------------------------------------

        public static void writeMidiFile(Sequence seq, String filename)
        {
            MidiOutStream stream = new MidiOutStream(filename);

            //midi file header
            stream.putString("MThd");
            stream.putFour(6);                      //header size
            stream.putTwo(1);                       //type 1 midi file
            stream.putTwo(seq.tracks.Count);        //track count
            stream.putTwo(seq.division);            //division

            for (int trackNum = 0; trackNum < seq.tracks.Count; trackNum++)
            {                
                seq.tracks[trackNum].saveTrack(stream);
            }
        }
    }

//- input stream --------------------------------------------------------------

    //midi files store data in big endian format!
    public class MidiInStream
    {
        public String filename;
        byte[] midiData;
        int dataSize;
        int dataPos;

        //read midi data from file
        public MidiInStream(String _filename)
        {
            filename = _filename;
            try
            {
                midiData = File.ReadAllBytes(filename);
            }
            catch (FileNotFoundException e)
            {
                throw new MidiFileException("couldn't open " + filename, 0);
            }
            catch (Exception e)
            {
                throw new MidiFileException("couldn't read MIDI data from " + filename, 0);
            }
            dataSize = midiData.Length;
            dataPos = 0;
        }

        //read midi data from incoming midi bytes
        public MidiInStream(byte[] data)
        {
            midiData = data;
            dataSize = midiData.Length;
            dataPos = 0;
        }

        public int getDataPos()
        {
            return dataPos;
        }

        public void checkStream(int size)
        {
            if (dataPos + size > dataSize)
            {
                throw new MidiFileException("tried to read past end of file " + filename, dataPos);
            }
        }

        public int getOne()
        {
            checkStream(1);
            byte a = midiData[dataPos++];
            int result = (int)(a);
            return result;
        }

        public int getTwo()
        {
            checkStream(2);
            byte a = midiData[dataPos++];
            byte b = midiData[dataPos++];
            int result = (int)(a * 256 + b);
            return result;
        }

        //returns unsigned 4 byte val
        public uint getFour()
        {
            checkStream(4);
            byte a = midiData[dataPos++];
            byte b = midiData[dataPos++];
            byte c = midiData[dataPos++];
            byte d = midiData[dataPos++];
            uint result = (uint)(a * 256 + b);
            result = (result * 256 + c);
            result = (result * 256 + d);
            return result;
        }

        public uint getVariableLengthVal()
        {
            uint result = 0;                        //largest var len quant allowed = 0xffffffff
            uint b = (uint)getOne();
            while (b >= 0x80)
            {
                uint d = b % 128;
                result *= 128;
                result += d;
                b = (uint)getOne();
            }
            result *= 128;
            result += b;
            return result;
        }

        public List<byte> getRange(int length)
        {
            checkStream(length);
            List<byte> data = new List<byte>(length);
            for (int i = 0; i < length; i++)
            {
                byte b1 = midiData[dataPos++];
                data.Add(b1);
            }
            return data;
        }

        public String getString(int length)
        {
            checkStream(length);
            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                char a = (char)midiData[dataPos++];
                result.Append(a);
            }
            return result.ToString();
        }

        public void skipBytes(int skip)
        {
            checkStream(skip);
            dataPos += skip;
        }

        public void pushBack(int backup)
        {
            dataPos -= backup;
        }
    }

//- output stream ---------------------------------------------------------------

    //midi files store data in big endian format!
    public class MidiOutStream
    {
        public String filename;
        List<byte> midiData;

        public MidiOutStream(String _filename)
        {
            filename = _filename;
            midiData = new List<byte>();
        }

        public void putOne(int val)
        {
            byte a = (byte)(val % 256);
            midiData.Add(a);
        }

        public void putTwo(int val)
        {
            byte b = (byte)(val % 256);
            val /= 256;
            byte a = (byte)(val % 256);
            midiData.Add(a);
            midiData.Add(b);
        }

        public void putFour(int val)
        {
            byte d = (byte)(val % 256);
            val /= 256;
            byte c = (byte)(val % 256);
            val /= 256;
            byte b = (byte)(val % 256);
            val /= 256;
            byte a = (byte)(val % 256);
            midiData.Add(a);
            midiData.Add(b);
            midiData.Add(c);
            midiData.Add(d);            
        }

        public List<byte> getVarLenQuantity(uint delta)
        {
            List<byte> result = new List<byte>();
            for (int i = 0; i < 4; i++)
            {
                if (delta >= 0x80)
                {
                    result.Add((byte)(delta % 0x80));
                    delta /= 0x80;
                }
                else
                {
                    result.Add((byte)delta);
                    break;
                }
            }
            result.Reverse();
            for (int i = 0; i < result.Count - 1; i++)
                result[i] += 0x80;
            return result;
        }

        public void putString(String s)
        {
            byte[] data = Encoding.ASCII.GetBytes(s);
            midiData.AddRange(data);
        }

        public void putData(byte[] data)
        {
            midiData.AddRange(data);
        }

        public void writeOut()
        {
            File.WriteAllBytes(filename, midiData.ToArray());
        }
    }

//- file exception ------------------------------------------------------------

    public class MidiFileException : Exception
    {
        public MidiFileException(String errorMsg, int pos) : base(errorMsg + " at pos [" + pos.ToString() + "]")
        {
        }
    }
}

//Console.WriteLine("there's no sun in the shadow of the wizard");
