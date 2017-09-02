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

namespace Transonic.MIDI
{
    public class Sequence
    {
        public const int DEFAULTDIVISION = 120;

        public List<Track> tracks;
        public int lastTrack;
        public int division;
        public int duration;
        public Track tempoMap;

        public Sequence(int _division)
        {
            division = _division;
            duration = 0;
            tracks = new List<Track>(257);
            for (int i = 0; i <= 256; i++)            
            {
                Track track = new Track(i);
                tracks.Add(track);
            }
            
            lastTrack = 0;
        }

        public void setTrack(Track track, int trackNumber)
        {
            tracks[trackNumber] = track;
            if (trackNumber > lastTrack) lastTrack = trackNumber;
            if (trackNumber == 0) tempoMap = track;
        }

        public void finalizeLoad()
        {
            for (int i = 0; i <= lastTrack; i++) 
            {
                tracks[i].finalizeLoad();
                if (duration < tracks[i].duration) duration = tracks[i].duration;
            }
            Console.WriteLine("seq length = {0}", duration);
            calcTempoMap();
        }

        public void calcTempoMap()
        {
            int time = 0;               //time in MICROseconds
            int tempo = 0;              //microseconds per quarter note
            int prevtick = 0;           //tick of prev tempo event

            for (int i = 0; i < tempoMap.events.Count; i++)
            {
                Event evt = tempoMap.events[i];
                if (evt.msg is TempoMessage)
                {
                    TempoMessage tempoMsg = (TempoMessage)evt.msg;
                    int msgtick = (int)evt.time;                            //the tick this tempo message occurs at
                    int delta = (msgtick - prevtick);                       //amount of ticks at _prev_ tempo
                    time += (int)((((float)delta) / division) * tempo);     //calc time in microsec of this tempo event
                    tempoMsg.timing = new Timing(msgtick, time, 0);         //timing maps time -> ticks

                    prevtick = msgtick;
                    tempo = tempoMsg.tempo;
                }
            }
        }

        public void dump()
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                Console.WriteLine("contents of track[{0}]", i);
                tracks[i].dump();
            }
        }

        public void allNotesOff()
        {
            for (int trackNum = 1; trackNum <= lastTrack; trackNum++)
            {
                tracks[trackNum].allNotesOff();
            }
        }
    }

//-----------------------------------------------------------------------------

    public class Timing
    {
        public int tick;
        public int microsec;
        public int beat;

        public Timing(int _tick, int _microsec, int _beat)
        {
            tick = _tick;
            microsec = _microsec;
            beat = _beat;
        }
    }
}
