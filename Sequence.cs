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

using Transonic.MIDI.System;

namespace Transonic.MIDI
{
    public class Sequence
    {
        public const int DEFAULTDIVISION = 96;         //ticks / quarter note

        public int division;                //ppq - ticks (pulses) / quarter note
        public int length;                  //total length in ticks

        public List<Track> tracks;
        public TempoMap tempoMap;
        public MeterMap meterMap;
        public MarkerMap markerMap;


        public Sequence() : this(DEFAULTDIVISION) { }

        public Sequence(int _division)
        {
            division = _division;
            length = 0;

            tracks = new List<Track>();
            tempoMap = new TempoMap();
            meterMap = new MeterMap();
            markerMap = new MarkerMap();
        }

        public void addTrack(Track track)
        {
            tracks.Add(track);
            if (track.length > length)
            {
                length = track.length;
            }
        }

        public void deleteTrack(Track track)
        {
            tracks.Remove(track);            
        }

        //public void finalizeLoad()
        //{
        //    calcTempoMap();
        //    for (int i = 1; i < tracks.Count; i++) 
        //    {
        //        tracks[i].finalizeLoad();
        //        if (length < tracks[i].duration) length = tracks[i].duration;
        //    }
        //}

        //build the tempo map from tempo message ONLY from track 0; tempo messages in other tracks will be IGNORED
        //public void calcTempoMap()
        //{
            //int time = 0;               //time in MICROseconds
            //int tempo = 0;              //microseconds per quarter note
            //int prevtick = 0;           //tick of prev tempo event

            //Track tempoTrack = tracks[0];
            //for (int i = 0; i < tempoTrack.events.Count; i++)
            //{
            //    Event evt = tempoTrack.events[i];
            //    if (evt.msg is TempoEvent)
            //    {
            //        TempoEvent tempoMsg = (TempoEvent)evt.msg;
            //        int msgtick = (int)evt.time;                                //the tick this tempo message occurs at
            //        int delta = (msgtick - prevtick);                           //amount of ticks at _prev_ tempo
            //        time += (int)((((float)delta) / division) * tempo);         //calc time in microsec of this tempo event
            //        tempoMsg.timing = new Tempo(msgtick, time, 0);
            //        tempoMap.Add(evt);

            //        prevtick = msgtick;
            //        tempo = tempoMsg.tempo;
            //    }
            //}
        //}

        //public void dump()
        //{
        //    for (int i = 0; i < tracks.Count; i++)
        //    {
        //        Console.WriteLine("contents of track[{0}]", i);
        //        tracks[i].dump();
        //    }
        //}

        public void allNotesOff()
        {
            for (int trackNum = 1; trackNum < tracks.Count; trackNum++)
            {
                tracks[trackNum].allNotesOff();
            }
        }
    }

}
