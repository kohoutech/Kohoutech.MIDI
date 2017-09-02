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
using System.Runtime.InteropServices;

// p/invoke calls and structs used with WINMM.DLL library taken from http://www.pinvoke.net

namespace Transonic.MIDI.System
{
    public class InputDevice
    {
        //midi input callback delegate
        public delegate void MidiInProc(int handle, int msg, int instance, int param1, int param2);

        //p/invoke imports
        [DllImport("winmm.dll", SetLastError = true)]
        static extern MMRESULT midiInOpen(out IntPtr lphMidiIn, int uDeviceID, MidiInProc dwCallback,
            IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiInStart(IntPtr lphMidiIn);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiInStop(IntPtr lphMidiIn);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern MMRESULT midiInClose(IntPtr lphMidiIn);

//-----------------------------------------------------------------------------

        //local vars
        public int devID;
        public String devName;
        public IntPtr devHandle;

        private MidiInProc midiInProc;
        private bool opened;
        private bool started;
        
        const int CALLBACK_FUNCTION = 0x30000;

        List<SystemUnit> unitList;

        //cons
        public InputDevice(int _id, string _name)
        {
            devID = _id;
            devName = _name;
            opened = false;
            started = false;
            unitList = new List<SystemUnit>();            
        }

        public void connectUnit(SystemUnit unit)
        {
            unitList.Add(unit);
        }

// midi funcs -----------------------------------------------------------------

        public void open()
        {
            if (!opened)
            {
                midiInProc = HandleMessage;
                MMRESULT result = midiInOpen(out devHandle, devID, midiInProc, IntPtr.Zero, CALLBACK_FUNCTION);
                opened = true;
                //Console.WriteLine("opened device " + devName + " result = " + result);
            }
        }

        public void start()
        {
            if (!started)
            {
                MMRESULT result = midiInStart(devHandle);
                started = true;
                //Console.WriteLine("started device " + devName + " result = " + result);
            }
        }

        public void stop() 
        {
            if (started)
            {
                MMRESULT result = midiInStop(devHandle);
                started = false;
                //Console.WriteLine("stopped device " + devName + " result = " + result);
            }
        }

        public void close()
        {
            if (opened)
            {
                MMRESULT result = midiInClose(devHandle);
                opened = false;
                //Console.WriteLine("closed device " + devName + " result = " + result);
            }
        }

// midi input handler -----------------------------------------------------------------

        const int MIM_OPEN = 0x3C1;
        const int MIM_CLOSE = 0x3C2;
        const int MIM_DATA = 0x3C3;
        const int MIM_LONGDATA = 0x3C4;
        const int MIM_ERROR = 0x3C5;
        const int MIM_LONGERROR = 0x3C6;
        const int MIM_MOREDATA = 0x3CC;

        private void HandleMessage(int handle, int msg, int instance, int param1, int param2)
        {
            if (msg == MIM_OPEN)    
            {
            }
            else if (msg == MIM_CLOSE)
            {
            }
            else if (msg == MIM_DATA)
            {
                //short midi message, 3 midi bytes packed into 32-bit int, highest byte unused
                byte[] msgbytes = BitConverter.GetBytes(param1);
                foreach (SystemUnit unit in unitList)
                {
                    unit.receiveMessage(msgbytes);
                }
                //Console.WriteLine("name = " + devName + ", " + instance + ", " +
                //    msgbytes[0].ToString("X2") + "." + msgbytes[1].ToString("X2") + "." + msgbytes[2].ToString("X2") + ", " + 
                //    param2.ToString("X8"));
            }
            else if (msg == MIM_LONGDATA)
            {
                //we don't handle receiving sys ex msgs yet
            }
            else if (msg == MIM_MOREDATA)
            {
            }
            else if (msg == MIM_ERROR)
            {             
            }
            else if (msg == MIM_LONGERROR)
            {                
            }
        }
    }
}
