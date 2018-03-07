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
    public class TempoMap
    {
        public List<Tempo> tempos;
        public int count;

        public TempoMap()
        {
            Tempo tempo = new Tempo(0, 500000, 0);
            tempos.Add(tempo);
            count = 1;
        }
    }

//-----------------------------------------------------------------------------

    //maps a tempo message or a time signature message to a elapsed time, so if move the cur pos
    //in a sequence, we can calculate what time that is; needs to be recalculated any time tempo or time sig change
    public class Tempo
    {
        public int tick;
        public int time;
        public int beat;

        public Tempo(int _tick, int _time, int _beat)
        {
            tick = _tick;
            time = _time;
            beat = _beat;
        }
    }
}
