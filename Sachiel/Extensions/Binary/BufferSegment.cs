namespace Sachiel.Extensions.Binary
{
    public unsafe struct BufferSegment
    {
        public readonly byte* Data;
        public readonly int Length;

        public byte* EndOfBuffer => Data + Length;

        public BufferSegment(byte* data, int length = 0)
        {
            Data = data;
            Length = length;
        }
    }
}
