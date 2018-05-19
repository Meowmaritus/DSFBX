using MeowDSIO.DataTypes.PARAM;
using MeowDSIO.DataTypes.PARAMDEF.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.PARAMDEF
{
    public class ParamDefEntry
    {
        public string DisplayName { get; set; }

        private ParamTypeDef __guiValueType;
        public ParamTypeDef GuiValueType
        {
            get => __guiValueType;
            set
            {
                switch (value)
                {
                    case ParamTypeDef.f32:
                    case ParamTypeDef.s8:
                    case ParamTypeDef.s16:
                    case ParamTypeDef.s32:
                    case ParamTypeDef.u8:
                    case ParamTypeDef.u16:
                    case ParamTypeDef.u32:
                    case ParamTypeDef.dummy8:
                        __guiValueType = value;
                        break;
                    default:
                        throw new Exception($"Tried to set the {nameof(ParamDefEntry)}.{nameof(GuiValueType)} property " +
                            $"to a value type unsupported by the game ({value}); " +
                            $"Only the {nameof(ParamDefEntry)}.{nameof(InternalValueType)} property can be set to types other than " +
                            $"{nameof(ParamTypeDef.f32)}, " +
                            $"{nameof(ParamTypeDef.u8)}, " +
                            $"{nameof(ParamTypeDef.u16)}, " +
                            $"{nameof(ParamTypeDef.u32)}, " +
                            $"{nameof(ParamTypeDef.s8)}, " +
                            $"{nameof(ParamTypeDef.s16)}, or " +
                            $"{nameof(ParamTypeDef.s32)}.");
                }
            }
        }

        public string GuiValueStringFormat { get; set; }
        public float DefaultValue { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float Increment { get; set; }
        public int GuiValueDisplayMode { get; set; }
        public int GuiValueByteCount { get; set; }
        public string Description { get; set; }
        public ParamTypeDef InternalValueType { get; set; }
        public string Name { get; set; }
        public int ID { get; set; }

        public int ValueBitCount { get; set; }

        public dynamic ReadValueFromParamEntryRawData(ParamRow param, ref int offset, ref int bitField, ref int bitVal)
        {
            try
            {
                dynamic val = double.NaN;

                //bool
                if (ValueBitCount == 1)
                {
                    bool boolResult = ((param.RawData[offset] & (1 << bitField)) > 0);

                    for (int i = 0; i < ValueBitCount; i++)
                    {
                        bitField++;
                        if (bitField == 8)
                        {
                            bitField = 0;
                            offset++;
                        }
                    }

                    return boolResult;
                }
                else if (ValueBitCount < 8)
                {
                    int u8calc = 0;

                    for (int p = 0; p < ValueBitCount; p++)
                    {
                        if ((param.RawData[offset] & (1 << bitField)) > 0)
                        {
                            u8calc += (1 << p);
                        }

                        bitField++;
                        if (bitField == 8)
                        {
                            bitField = 0;
                            offset++;
                        }
                    }

                    if (u8calc < 0)
                        Console.WriteLine();

                    return u8calc;
                }
                else if (GuiValueType == ParamTypeDef.dummy8 || InternalValueType == ParamTypeDef.dummy8)
                {
                    val = (byte)0;

                    if (bitField > 0)
                    {
                        bitField = 0;
                        offset++;
                    }

                    offset += (ValueBitCount / 8);

                    return val;
                }
                else
                {
                    switch (GuiValueType)
                    {
                        //case ParamTypeDef.dummy8:
                        case ParamTypeDef.f32: val = BitConverter.ToSingle(param.RawData, offset); break;
                        case ParamTypeDef.s8: val = (sbyte)param.RawData[offset]; break;
                        case ParamTypeDef.s16: val = BitConverter.ToInt16(param.RawData, offset); break;
                        case ParamTypeDef.s32: val = BitConverter.ToInt32(param.RawData, offset); break;
                        case ParamTypeDef.u8:

                            byte v = param.RawData[offset];
                            offset++;

                            if (InternalValueType == ParamTypeDef.ON_OFF)
                                return (bool)(v != 0);
                            else
                                return v;

                        //if (ValueBitCount == 8)
                        //{

                        //}
                        //else
                        //{

                        //}
                        case ParamTypeDef.u16: val = BitConverter.ToUInt16(param.RawData, offset); break;
                        case ParamTypeDef.u32: val = BitConverter.ToUInt32(param.RawData, offset); break;
                    }
                }

                offset += (ValueBitCount / 8);

                if (InternalValueType == ParamTypeDef.ON_OFF)
                {
                    return (bool)(val != 0);
                }

                return val;
            }
            catch (Exception e)
            {
                throw new Exception($"Critical error while loading: {e.Message}");
            }
        }

        private void NextBit(ParamRow param, ref int offset, ref int bitField, ref int bitVal)
        {
            bitField++;
            if (bitField == 8)
            {
                param.RawData[offset] = (byte)bitVal;
                offset++;
                bitVal = 0;
                bitField = 0;
            }
        }

        public void WriteValueToParamEntryRawData(ParamRow param, dynamic value, ref int offset, ref int bitField, ref int bitVal)
        {
            try
            {
                double num = double.NaN;

                //bool
                if (ValueBitCount == 1)
                {
                    if (value != null)
                    {
                        if (value.GetType() == typeof(bool))
                        {
                            if ((bool)value)
                                bitVal += (1 << bitField);
                        }
                        else
                        {
                            throw new Exception("INT value found for a boolean Param entry.");

                            if (value != 0)
                                bitVal += (1 << bitField);
                        }
                    }
                    else
                    {
                        throw new Exception("Null value found for a boolean Param entry.");
                    }

                    NextBit(param, ref offset, ref bitField, ref bitVal);

                    return;
                }
                else if (ValueBitCount < 8)
                {
                    for (int i = 0; i < ValueBitCount; i++)
                    {
                        if (((byte)value & (1 << i)) > 0)
                        {
                            bitVal |= (1 << bitField);
                        }

                        NextBit(param, ref offset, ref bitField, ref bitVal);
                    }

                    return;
                }
                else if (GuiValueType == ParamTypeDef.dummy8 || InternalValueType == ParamTypeDef.dummy8)
                {
                    if (bitField > 0)
                    {
                        bitField = 0;
                        param.RawData[offset] = (byte)bitVal;
                        bitVal = 0;
                        offset++;
                    }

                    offset += (ValueBitCount / 8);

                    return;
                }
                else
                {
                    
                    //num = Clamp(Convert.ToDouble(value), Min, Max);
                    //Do not clamp values on actual write, only when the value is *changed*
                    num = Convert.ToDouble(value);

                    switch (GuiValueType)
                    {
                        //case ParamTypeDef.dummy8:
                        case ParamTypeDef.f32:
                            Array.Copy(BitConverter.GetBytes((float)num), 0, param.RawData, offset, sizeof(float)); break;
                        case ParamTypeDef.u8:

                            if (ValueBitCount == 8)
                            {
                                param.RawData[offset] = (byte)(int)num;
                            }
                            else
                            {
                                throw new Exception();
                            }
                            

                            break;
                        case ParamTypeDef.u16: Array.Copy(BitConverter.GetBytes((ushort)num), 0, param.RawData, offset, sizeof(ushort)); break;
                        case ParamTypeDef.u32: Array.Copy(BitConverter.GetBytes((uint)num), 0, param.RawData, offset, sizeof(uint)); break;
                        case ParamTypeDef.s8: param.RawData[offset] = unchecked((byte)((sbyte)((int)num))); break;
                        case ParamTypeDef.s16: Array.Copy(BitConverter.GetBytes((short)num), 0, param.RawData, offset, sizeof(short)); break;
                        case ParamTypeDef.s32: Array.Copy(BitConverter.GetBytes((int)num), 0, param.RawData, offset, sizeof(int)); break;
                        default:
                            throw new Exception($"[{nameof(WriteValueToParamEntryRawData)}() Error] " +
                                $"Invalid GuiValueType: [ParamTypeDef.{GuiValueType}]");
                    }

                    offset += (ValueBitCount / 8);
                }

                
            }
            catch (Exception e)
            {
                throw new Exception($"Unexpected exception while writing value [{value}] to [byte[] {nameof(ParamRow)}.{nameof(ParamRow.RawData)}]: {e.Message}");
            }
        }
    }
}
