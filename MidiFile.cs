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
using System.IO;

using Transonic.MIDI.System;

//J Glatt's Midi page: http://midi.teragonaudio.com/tech/midifile.htm

namespace Transonic.MIDI
{
    public class MidiFile
    {
        MidiSystem midiSystem;
        public String filename;

        byte[] midiData;
        int dataPos;
        uint currentTime;       //event time in ticks
        int runningStatus;

        public MidiFile(MidiSystem _system, String _filename)
        {
            midiSystem = _system;
            filename = _filename;
        }

//- loading -------------------------------------------------------------------

        public Sequence readMidiFile()
        {
            midiData = File.ReadAllBytes(filename);
            dataPos = 0;

            //read midi file header
            String sig = getString(4);
            uint hdrsize = getFour();
            uint fileFormat = getTwo();
            int trackCount = (int)getTwo();
            int division = (int)getTwo();

            Console.WriteLine("sig = {0}, file format = {1}, track count = {2}, div = {3}", sig, fileFormat, trackCount, division);

            Sequence seq = new Sequence(division);

            //read midi track data
            for (int i = 0; i < trackCount; i++)
            {
                Console.WriteLine("loading track {0}", i);
                Track track = loadTrackData(i);
                track.setOutputDevice(midiSystem.outputDevices[0]);
                //track.setOutputChannel(i+1);
                seq.setTrack(track, i);
            }

            seq.finalizeLoad();

            return seq;
        }

        public Track loadTrackData(int num)
        {
            String trackSig = getString(4);
            uint trackDataLength = getFour();

            Track track = new Track(num);
            int startpos = dataPos;
            currentTime = 0;
            runningStatus = 0;
            while ((dataPos - startpos) < trackDataLength)
            {
                Event msg = loadMessageData();
                track.addEvent(msg);
            }
            return track;
        }

        public uint getVariableLengthVal()
        {
            uint result = 0;         //largest var len quant allowed = 0xffffffff
            uint b = getOne();
            while (b >= 0x80)
            {
                uint d = b % 128;
                result *= 128;
                result += d;
                b = getOne();
            }
            result *= 128;
            result += b;
            return result;
        }

        Event loadMessageData()         
        {
            Message msg = null;
            currentTime += getVariableLengthVal();      //add delta time to current num of ticks
            int status = (int)getOne();
            if (status < 0x80)              //running status 
            {
                pushBack(1);
                status = runningStatus;
            }
            msg = Message.getMessage(midiData, dataPos);                
            runningStatus = status;
            if (msg == null) 
                Console.WriteLine("got a null msg at {0} with a status of {1}", dataPos, status.ToString("X2"));
            Event evt = new Event(currentTime, msg);
            return evt;
        }

//- saving -------------------------------------------------------------------

        public void writeMidiFile(Sequence seq)
        {
            //midi file header
            List<byte> outbytes = new List<byte>();
            outbytes.AddRange(Encoding.ASCII.GetBytes("MThd"));
            outbytes.AddRange(putFour(6));
            outbytes.AddRange(putTwo(1));
            outbytes.AddRange(putTwo(seq.lastTrack + 1));
            outbytes.AddRange(putTwo(seq.division));

            for(int trackNum = 0; trackNum <= seq.lastTrack; trackNum++) {                
                List<byte> trackbytes = seq.tracks[trackNum].saveTrack(this);
                outbytes.AddRange(trackbytes);
            }

            File.WriteAllBytes(filename, outbytes.ToArray());
        }

//- utility methods -----------------------------------------------------------

        //midi files store data in big endian format!

        public uint getOne()
        {
            byte a = midiData[dataPos++];
            uint result = (uint)(a);
            return result;
        }

        public uint getTwo()
        {
            byte a = midiData[dataPos++];
            byte b = midiData[dataPos++];
            uint result = (uint)(a * 256 + b);
            return result;
        }

        public uint getFour()
        {
            byte a = midiData[dataPos++];
            byte b = midiData[dataPos++];
            byte c = midiData[dataPos++];
            byte d = midiData[dataPos++];
            uint result = (uint)(a * 256 + b);
            result = (result * 256 + c);
            result = (result * 256 + d);
            return result;
        }

        public String getString(int width)
        {
            String result = "";
            for (int i = 0; i < width; i++)
            {
                byte a = midiData[dataPos++];
                if ((a >= 0x20) && (a <= 0x7E))
                {
                    result += (char)a;
                }
            }
            return result;
        }

        public void skipBytes(int skip)
        {
            dataPos += skip;
        }

        public void pushBack(int backup)
        {
            dataPos -= backup;
        }

        public byte[] putTwo(int val)
        {
            byte[] result = new byte[2];
            result[1] = (byte)(val % 256);
            val /= 256;
            result[0] = (byte)(val % 256);
            return result;
        }

        public byte[] putFour(int val)
        {
            byte[] result = new byte[4];
            result[3] = (byte)(val % 256);
            val /= 256;
            result[2] = (byte)(val % 256);
            val /= 256;
            result[1] = (byte)(val % 256);
            val /= 256;
            result[0] = (byte)(val % 256);
            return result;
        }

    }
}

