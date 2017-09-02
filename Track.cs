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

using Transonic.MIDI.System;

namespace Transonic.MIDI
{
    public class Track
    {
        public int number;
        public String name;
        public List<Event> events;
        public int duration;

        //track i/o
        public InputDevice inDev;
        public int inputChannel;
        public OutputDevice outDev;
        public int outputChannel;

        public bool muted;
        public bool recording;

        public int keyOfs;
        public int VelOfs;
        public int TimeOfs;

        public int bankNum;
        public int patchNum;
        public int volume;
        public int pan;

        public Track(int _num)
        {
            number = _num;
            name = "Track " + number.ToString();
            events = new List<Event>();

            muted = false;
            recording = false;

            inDev = null;
            inputChannel = 1;
            outDev = null;
            outputChannel = 1;
            patchNum = 0;
            volume = 127;
        }

//- track settings -----------------------------------------------------------------

        public void setName(String _name)
        {
        }

        public void setMuted(bool on)
        {
            muted = on;
            if (muted)
            {
                allNotesOff();
            }
        }

        public void setSoloing(bool on)
        {
            if (on)
            {
                muted = false;
            }
        }

        public void setRecording(bool on)
        {
            recording = on;
        }

        public void setPatch(int patch)
        {
            patchNum = patch;
        }

        public void setVolume(int vol)
        {
            volume = vol;
        }


//- track input -----------------------------------------------------------------

        public void setInputDevice(InputDevice _inDev)
        {
            inDev = _inDev;
            inDev.open();
        }

        public void setInputChannel(int channel) 
        {
            inputChannel = channel;
        }

//- track output -----------------------------------------------------------------

        public void setOutputDevice(OutputDevice _outDev) 
        {
            outDev = _outDev;
            outDev.open();
        }

        public void setOutputChannel(int channel)
        {
            outputChannel = channel;
        }

        public void sendMessage(Message msg)
        {
            if (!muted)
            {
                //outDev.sendMessage(msg, outputChannel);
            }
        }

        public void allNotesOff()
        {
            if (outDev != null)
            {
                outDev.allNotesOff();
            }
        }

//- track loading -------------------------------------------------------------

        public void addEvent(Event evt)
        {
            events.Add(evt);
        }

        public void finalizeLoad() 
        {
            duration = (int)events[events.Count - 1].time;
            loadTrackSettings();
        }

        //scan track for name meta event, use the first one we find (should be only one)
        public void loadTrackSettings() 
        {
            bool haveName = false;
            bool haveOutChannel = false;
            bool havePatchNum = false;
            bool haveVolume = false;
            for (int i = 0; i < events.Count; i++)
            {
                if (!haveName && events[i].msg is TrackNameMessage)
                {
                    TrackNameMessage nameMsg = (TrackNameMessage)events[i].msg;
                    name = nameMsg.trackName;
                    haveName = true;
                }

                if (!haveOutChannel && events[i].msg is NoteOnMessage)
                {
                    NoteOnMessage noteMsg = (NoteOnMessage)events[i].msg;
                    outputChannel = noteMsg.channel;
                    haveOutChannel = true;
                }

                if (!havePatchNum && events[i].msg is PatchChangeMessage)
                {
                    PatchChangeMessage patchMsg = (PatchChangeMessage)events[i].msg;
                    patchNum = patchMsg.patchNumber;
                    havePatchNum = true;
                }

                if (!haveVolume && events[i].msg is ControllerMessage)
                {
                    ControllerMessage ctrlMsg = (ControllerMessage)events[i].msg;
                    if (ctrlMsg.controllerNumber == 7)
                    {
                        volume = ctrlMsg.controllerValue;
                        haveVolume = true;
                    }
                }

                if (haveName && haveOutChannel && havePatchNum && haveVolume) break;
            }
        }

//- track saving -------------------------------------------------------------

        public List<byte> saveTrack(MidiFile midifile)
        {
            List<byte> bytes = new List<byte>();

            uint curtime = 0;
            foreach(Event evt in events) {
                uint delta = evt.time - curtime;
                curtime = evt.time;
                List<byte> vardelta = getVarLenQuantity(delta);
                bytes.AddRange(vardelta);
                byte[] msgbytes = evt.msg.getDataBytes();
                bytes.AddRange(msgbytes);
            }

            //track header
            int size = bytes.Count;
            bytes.InsertRange(0, Encoding.ASCII.GetBytes("MTrk"));
            bytes.InsertRange(4,midifile.putFour(size));

            return bytes;
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
                } else 
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

        public void dump()
        {
            for (int i = 0; i < events.Count; i++)
            {
                events[i].dump();
            }
        }
    }
}
