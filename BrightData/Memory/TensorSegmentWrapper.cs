﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BrightData.Memory
{
    class TensorSegmentWrapper<T> : ITensorSegment<T>
        where T : struct
    {
        readonly ITensorSegment<T> _segment;
        readonly uint _offset, _stride;
        bool _wasDisposed = false;

        public TensorSegmentWrapper(ITensorSegment<T> segment, uint offset, uint stride, uint length)
        {
            _segment = segment;
            _segment.AddRef();
            _offset = offset;
            _stride = stride;
            Size = length;
            segment.Context.MemoryLayer.Add(this);
        }

        public void Dispose()
        {
            if (!_wasDisposed) {
                _wasDisposed = true;
                _segment.Release();
            }
        }

        public uint Size { get; }
        public int AddRef() => _segment.AddRef();
        public int Release() => _segment.Release();
        public long AllocationIndex => _segment.AllocationIndex;
        public bool IsValid => !_wasDisposed && _segment.IsValid;
        public T[] ToArray() => Values.ToArray();
        public IBrightDataContext Context => _segment.Context;

        public IEnumerable<T> Values
        {
            get
            {
                for (uint i = 0; i < Size; i++)
                    yield return this[i];
            }
        }

        public ITensorBlock<T> GetBlock(ITensorPool pool)
        {
            var ret = pool.Get<T>(Size);
            using(var segment = ret.GetSegment())
                segment.Initialize(i => this[i]);
            return ret;
        }

        public T this[uint index] {
            get => _segment[_offset + index * _stride];
            set => _segment[_offset + index * _stride] = value;
        }
        public T this[long index] {
            get => _segment[_offset + index * _stride];
            set => _segment[_offset + index * _stride] = value;
        }

        public void InitializeFrom(Stream stream)
        {
            uint index = 0;
            var buffer = new byte[Size * Unsafe.SizeOf<T>()];
            stream.Read(buffer, 0, buffer.Length);
            var ptr = MemoryMarshal.Cast<byte, T>(buffer);
            for (uint i = 0; i < Size; i++)
                this[index++] = ptr[(int)i];
        }

        public void Initialize(Func<uint, T> initializer)
        {
            for (uint i = 0; i < Size; i++)
                this[i] = initializer(i);
        }

        public void Initialize(T initializer)
        {
            for (uint i = 0; i < Size; i++)
                this[i] = initializer;
        }

        public unsafe void WriteTo(Stream stream)
        {
            var buffer = new byte[Size * Unsafe.SizeOf<T>()];
            fixed(byte* ptr = &buffer[0]) {
                var p = ptr;
                for (uint i = 0; i < Size; i++) {
                    Unsafe.Write(p, this[i]);
                    p += Unsafe.SizeOf<T>();
                }
            }
            stream.Write(buffer);
        }
    }
}