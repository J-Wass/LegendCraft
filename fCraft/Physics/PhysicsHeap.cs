//Copyright (C) 2012 Lao Tszy (lao_tszy@yahoo.co.uk)﻿
  	
//fCraft Copyright (C) 2009, 2010, 2011, 2012 Matvei Stefarov <me@matvei.org>	
//Permission is hereby granted, free of charge, to any person obtaining a copy of this 
//software and associated documentation files (the "Software"), 
//to deal in the Software without restriction, including without limitation the rights to use, 
//copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies 
//or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER	  	
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
//WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    /// <summary> Base class for physic tasks </summary>
    public abstract class PhysicsTask
    {
        /// <summary> the task due time in milliseconds since some start moment </summary>
        public Int64 DueTime;

        /// <summary>
        /// The flag indicating that task must not be performed when due.
        /// This flag is introduced because the heap doesnt allow deletion of elements.
        /// It is possible to implement but far too complicated than just marking elements like that.
        /// </summary>
        public bool Deleted = false;

        protected World _world;
        protected Map _map;

        protected PhysicsTask(World world) //a task must be created under the map syn root
        {
            if (null == world)
                throw new ArgumentNullException("world");
            //if the map is null the task will not be rescheduled
            //if (null == world.Map)
            //    throw new ArgumentException("world has no map");
            lock (world.SyncRoot)
            {
                _world = world;
                _map = world.Map;
            }
        }

		public Int64 GetHeapKey() 
	    {
            return DueTime;
        }

        /// <summary> Performs the action. The returned value is used as the reschedule delay. If 0 - the task is completed and
        /// should not be rescheduled </summary>
        public int Perform()
        {
			lock (_world.SyncRoot)
            {
                if (null == _map || !ReferenceEquals(_map, _world.Map))
                    return 0;
                return PerformInternal();
            }
        }
        
        /// <summary> The real implementation of the action </summary>
        protected abstract int PerformInternal();

        protected void UpdateMap(BlockUpdate upd)
        {
            _map.SetBlock(upd.X, upd.Y, upd.Z, upd.BlockType);
            _map.QueueUpdate(upd);
        }
    }

    /// <summary>
    /// Min binary heap implementation.
    /// Note that the base array size is never decreased, i.e. Clear just moves the internal pointer to the first element.
    /// </summary>
    class MinBinaryHeap
    {
        readonly List<PhysicsTask> _heap = new List<PhysicsTask>();
        int _free = 0;

        /// <summary> Adds an element. </summary>
        public void Add(PhysicsTask t)
        {
            int me = _free++;
            if (_heap.Count > me)
                _heap[me] = t;
            else
                _heap.Add(t);
            
            long myKey = t.GetHeapKey();
            while (me > 0)
            {
                int parent = ParentIdx(me);
                if (_heap[parent].GetHeapKey() < myKey)
                    break;
                Swap(me, parent);
                me = parent;
            }
        }

        /// <summary> Head of this heap. Assumes that size was checked before accessing the head element. </summary>
        public PhysicsTask Head()
        {
            return _heap[0];
        }

        /// <summary> Removes the head. Assumes that size was checked before removing the head element. </summary>
        public void RemoveHead()
        {
            _heap[0] = _heap[--_free];
            _heap[_free] = null; //to enable garbage collection for the deleted item when necessary
            if (_free == 0)
                return;
            
            int me = 0;
            long myKey = _heap[0].GetHeapKey();

            for (; ; )
            {
                int kid1, kid2;
                Kids(me, out kid1, out kid2);
                if (kid1 >= _free) break;
                
                int minKid;
                long minKidKey;
                if (kid2 >= _free)
                {
                    minKid = kid1;
                    minKidKey = _heap[minKid].GetHeapKey();
                }
                else
                {
                    long key1 = _heap[kid1].GetHeapKey();
                    long key2 = _heap[kid2].GetHeapKey();
                    if (key1 < key2)
                    {
                        minKid = kid1;
                        minKidKey = key1;
                    }
                    else
                    {
                        minKid = kid2;
                        minKidKey = key2;
                    }
                }
                if (myKey > minKidKey)
                {
                    Swap(me, minKid);
                    me = minKid;
                }
                else
                    break;
            }
        }

        /// <summary> Heap size. </summary>
        public int Size { get { return _free; } }

        public void Clear()
        {
            for (int i = 0; i < _free; ++i)
                _heap[i] = null; //enables garbage collecting for the deleted elements
            _free = 0;
        }

        static int ParentIdx(int idx)
        {
            return (idx - 1) / 2;
        }

        static void Kids(int idx, out int kid1, out int kid2)
        {
            kid1 = 2 * idx + 1;
            kid2 = kid1 + 1;
        }

        void Swap(int i1, int i2)
        {
            PhysicsTask t = _heap[i1];
            _heap[i1] = _heap[i2];
            _heap[i2] = t;
        }
    }
}
