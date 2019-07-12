﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public enum FlverVertexStructMemberValueType : int
    {
        INVALID = -1,

        //UNK0x00 = 0x00,
        //UNK0x01 = 0x01,
        Vector3 = 0x02,
        //UNK0x03 = 0x03,
        //UNK0x04 = 0x04,
        //UNK0x05 = 0x05,
        //UNK0x06 = 0x06,
        //UNK0x07 = 0x07,
        //UNK0x08 = 0x08,
        //UNK0x09 = 0x09,
        //UNK0x0A = 0x0A,
        //UNK0x0B = 0x0B,
        //UNK0x0C = 0x0C,
        //UNK0x0D = 0x0D,
        //UNK0x0E = 0x0E,
        //UNK0x0F = 0x0F,
        PackedVector4B = 0x10,
        BoneIndicesStruct = 0x11,
        //UNK0x12 = 0x12,
        PackedVector4 = 0x13,
        //UNK0x14 = 0x14,
        UV = 0x15,
        UVPair = 0x16,
        //UNK0x17 = 0x17,
        //UNK0x18 = 0x18,
        //UNK0x19 = 0x19,
        BoneWeightsStruct = 0x1A,
        BoneIndicesStructB = 0x2F,
        Vector3B = 0xF0,
    }
}
