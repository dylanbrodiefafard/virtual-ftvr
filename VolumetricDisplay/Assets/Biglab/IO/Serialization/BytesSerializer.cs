using System;
using System.Runtime.InteropServices;

namespace Biglab.IO.Serialization
{
    public static class StructSerializer
        // TODO: CC: Documentation
    {
        // TODO: Should this be replaced with the GC pin method like the array? Is it faster?
        public static byte[] SerializeBytes<T>(this T structure) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var data = new byte[size];

            // Allocate memory for structure
            var ptr = Marshal.AllocHGlobal(size);

            // Convert structure into memory
            Marshal.StructureToPtr(structure, ptr, true);
            Marshal.Copy(ptr, data, 0, size);

            // Deallocate memmory
            Marshal.FreeHGlobal(ptr);

            return data;
        }

        // SerializeBytes takes an array of structures and converts it to a byte array
        public static byte[] SerializeBytes<T>(this T[] structures) where T : struct
        {
            if (structures == null || structures.Length == 0)
            {
                // Die because nothing to do
                return Array.Empty<byte>();
            }

            var structSize = Marshal.SizeOf(typeof(T));
            // resultant byte array size = struct size * number of structures
            var byteArrayLength = structSize * structures.Length;
            var arr = new byte[byteArrayLength];

            // Allocate memory and store structures at the address
            var pin = GCHandle.Alloc(structures, GCHandleType.Pinned);
            try
            {
                // copy our memory containing the byte data for structures into arr
                var ptr = pin.AddrOfPinnedObject();
                Marshal.Copy(ptr, arr, 0, byteArrayLength);
            }
            finally
            {
                pin.Free();
            }

            return arr;
        }

        // TODO: Replace with the GC pin method above?
        public static T DeserializeBytes<T>(this byte[] data) where T : struct
        {
            var size = Marshal.SizeOf<T>();

            // Allocate memory for structure
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, ptr, size); // Copy data into memory

            // Convert memory into structure
            var obj = Marshal.PtrToStructure<T>(ptr);

            // Deallocate memmory
            Marshal.FreeHGlobal(ptr);

            return obj;
        }

        /// <summary>
        /// Using <see cref="Marshal"/>, converts the byte array into an array of structs.
        /// </summary>
        /// <typeparam name="TStruct"> Some blittable struct type </typeparam>
        /// <param name="bytes"> The block of memory </param>
        /// <returns> The bytes converted into an array of structs </returns>
        public static TStruct[] DeserializeBytesAsArray<TStruct>(this byte[] bytes) where TStruct : struct
        {
            return DeserializeBytesAsArray<TStruct>(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Using <see cref="Marshal"/>, converts a subset of the byte array into an array of structs.
        /// </summary>
        /// <typeparam name="TStruct"> Some blittable struct type </typeparam>
        /// <param name="bytes"> The block of memory </param>
        /// <param name="offset"> Offset of the block to convert </param>
        /// <param name="length"> Length of the block bytes to convert </param>
        /// <returns> The byte block converted into an array of structs </returns>
        public static TStruct[] DeserializeBytesAsArray<TStruct>(this byte[] bytes, int offset, int length)
            where TStruct : struct
        {
            // Allocate array of structure
            var array = new TStruct[bytes.Length / Marshal.SizeOf<TStruct>()];

            // Pin data
            var pin = GCHandle.Alloc(array, GCHandleType.Pinned);
            var ptr = pin.AddrOfPinnedObject();

            // Copy bytes into array memory
            Marshal.Copy(bytes, offset, ptr, length);

            // Release pin
            pin.Free();

            return array;
        }
    }
}