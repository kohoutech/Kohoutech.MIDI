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

using Transonic.MIDI;

namespace Transonic.MIDI.System
{
    public class Transport
    {
        IMidiView window;           //for updating the UI

        Sequence seq;
        Track tempoMap;
        int tempo;
        float playbackSpeed;
        int division;
        int trackCount;

        MidiTimer timer;
        long startTime;
        long startOffset;
        long tick;          //number of 0.1 microsecs per tick
        long tickTime;      //cumulative tick time
        int tempoPos;
        int[] trackPos;     //pos of the next event in each track

        public int tickCount;      //cur tick number
        public TempoMessage curTempo;

        public Transport(IMidiView _window)
        {
            window = _window;

            timer = new MidiTimer();
            timer.Timer += new EventHandler(OnPulse);

            division = 120;
            playbackSpeed = 1.0f;
            setTempo(120);      //default tempo & division
        }

        public void setSequence(Sequence _seq)
        {
            seq = _seq;
            division = seq.division;                                    //ticks / quarter note
            tempoMap = seq.tracks[0];
            int i = 0;
            while (i < tempoMap.events.Count && !(tempoMap.events[i].msg is TempoMessage)) i++;
            if (i < tempoMap.events.Count)
            {
                setTempo(((TempoMessage)tempoMap.events[i].msg).tempo);
            }

            trackCount = seq.lastTrack;
            trackPos = new int[trackCount];

            rewindSequence();
        }

        public void setTempo(int _tempo)
        {
            tempo = _tempo;                                                 //microsec / quarter note
            tick = (long)((tempo / (division * playbackSpeed)) * 10.0f);    //len of each tick in 0.1 microsecs (or 100 nanosecs)
        }

//- operation methods ---------------------------------------------------------

        public void init()
        {            
        }

        public void shutdown()
        {
            stopSequence();            
        }

        public void rewindSequence()
        {
            tickCount = 0;
            tickTime = tick;                    //time of first tick (not 0 - that would be no ticks)

            tempoPos = 0;
            curTempo = null;
            for (int i = 0; i < trackPos.Length; i++)
                trackPos[i] = 0;
            startOffset = 0;
        }

        public void playSequence()
        {
            startTime = DateTime.Now.Ticks - startOffset;

            timer.Start(1);               //timer interval = 1 msec
        }

        public void stopSequence()
        {
            long now = DateTime.Now.Ticks;
            startOffset = (now - startTime);
            timer.stop();
            seq.allNotesOff();
        }

        public void sequenceDone()
        {
            timer.stop();
            seq.allNotesOff();
            window.sequenceDone();
        }

        public void halfSpeedSequence(bool on)
        {
            playbackSpeed = on ? 0.5f : 1.0f;
            setTempo(curTempo != null ? curTempo.tempo : 120);
        }

        public void setSequencePos(int ticknum)
        {
            tickCount = ticknum;
            setTempo(120);                          //default tempo & division
            tempoPos = 0;
            curTempo = null;

            //find the most recent tempo event - if no tempo events, go with the default
            List<Event> events = tempoMap.events;
            if (events.Count != 0)
            {
                while ((tempoPos < events.Count) && (tickCount > events[tempoPos].time))
                {
                    tempoPos++;
                }
                while ((tempoPos >= 0) && !(events[tempoPos].msg is TempoMessage))      //we've passed it, backup to prev tempo event
                {
                    tempoPos--;
                }
                if (tempoPos < events.Count)
                {
                    curTempo = (TempoMessage)events[tempoPos].msg;
                    setTempo(curTempo.tempo);
                    int tickOfs = ticknum - curTempo.timing.tick;                          //num of ticks from prev tempo msg to now
                    tickTime = (curTempo.timing.microsec * 10L) + (tickOfs * tick);       //prev tempo's time (in usec) + time of ticks to now
                    //tickTime = tickCount * tick;            
                }
            }
            else
            {
                tickTime = tickCount * tick;
            }

            startOffset = tickTime;
            startTime = DateTime.Now.Ticks - startOffset;

            //set cur pos in each track
            for (int trackNum = 1; trackNum < trackCount; trackNum++)
            {
                Track track = seq.tracks[trackNum];
                events = seq.tracks[trackNum].events;
                trackPos[trackNum] = 0;
                PatchChangeMessage patchmsg = null;
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].msg is PatchChangeMessage)
                        patchmsg = (PatchChangeMessage)events[i].msg;
                    if (events[i].time > ticknum)
                        break;
                    trackPos[trackNum]++;
                }
                if (patchmsg != null)
                {                    
                    track.sendMessage(patchmsg);
                }
            }
        }

        public int getMilliSecTime()
        {
            return (int)(tickTime / 10000L);            //ret tick time in msec
        }

//- timer method --------------------------------------------------------------

        public void OnPulse(object sender, EventArgs e)
        {
            long now = DateTime.Now.Ticks;          //one tick = 0.1 microsec
            long runningTime = (now - startTime);

            while (runningTime > tickTime)          //we've passed one or more ticks
            {
                tickCount++;                        //update tick number
                tickTime = tickTime + tick;         //and get time of next tick

                //handle tempo msgs
                List<Event> events = tempoMap.events;
                while ((tempoPos < events.Count) && (tickCount >= events[tempoPos].time))
                {
                    Message msg = events[tempoPos].msg;
                    if (msg is TempoMessage)
                    {
                        curTempo = (TempoMessage)msg;
                        setTempo(curTempo.tempo);
                    }
                    tempoPos++;                    
                }

                bool alldone = true;
                for (int trackNum = 1; trackNum < trackCount; trackNum++)
                {
                    Track track = seq.tracks[trackNum];
                    events = track.events;

                    bool done = (trackPos[trackNum] >= events.Count);
                    while (!done && tickCount >= events[trackPos[trackNum]].time)
                    {
                        Message msg = events[trackPos[trackNum]].msg;
                        track.sendMessage(msg);
                        window.handleMessage(trackNum, msg);
                        trackPos[trackNum]++;
                        done = (trackPos[trackNum] >= events.Count);
                    }

                    alldone = alldone && done;
                }
                if (alldone)
                {
                    sequenceDone();
                }
            }
        }
    }
}

//Console.WriteLine("there's no sun in the shadow of the wizard");
