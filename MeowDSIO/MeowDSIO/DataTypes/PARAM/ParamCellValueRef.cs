using MeowDSIO.DataTypes.PARAMDEF;
using MeowDSIO.DataTypes.PARAMDEF.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.PARAM
{
    public class ParamCellValueRef : INotifyPropertyChanged
    {
        public ParamDefEntry Def { get; private set; } = null;

        private dynamic _value = null;
        private bool _valueOutOfBounds = false;

        public bool ValueOutOfBounds
        {
            get => _valueOutOfBounds;
            set
            {
                _valueOutOfBounds = value;
                NotifyPropertyChanged(nameof(_valueOutOfBounds));
            }
        }

        public object Value
        {
            get => _value;
            set
            {
                if (Def.InternalValueType == ParamTypeDef.dummy8 || Def.GuiValueType == ParamTypeDef.dummy8)
                {
                    _value = 0;
                    ValueOutOfBounds = false;
                    return;
                }

                if (Def.ValueBitCount == 1)
                {
                    if (value?.GetType() != typeof(bool))
                        Console.WriteLine("Breakpoint hit");

                    _value = (bool)value;
                    ValueOutOfBounds = false;
                }
                else
                {
                    switch (Def.InternalValueType)
                    {
                        default:
                            _value = Convert.ToDouble(value);
                            break;
                        case ParamTypeDef.dummy8: return;
                        case ParamTypeDef.f32:
                            _value = (float)CheckBounds(Convert.ToDouble(value), Def.Min, Def.Max); break;
                        case ParamTypeDef.s8:
                            _value = (sbyte)CheckBounds(Convert.ToDouble(value), Def.Min, Def.Max); break;
                        case ParamTypeDef.s16:
                            _value = (short)CheckBounds(Convert.ToDouble(value), Def.Min, Def.Max); break;
                        case ParamTypeDef.s32:
                            _value = (int)CheckBounds(Convert.ToDouble(value), Def.Min, Def.Max); break;
                        case ParamTypeDef.u8:
                            if (Def.ValueBitCount == 1)
                            {
                                if (value?.GetType() != typeof(bool))
                                    Console.WriteLine("Breakpoint hit");

                                _value = (bool)value;
                            }
                            else
                            {
                                _value = (byte)CheckBounds(Convert.ToDouble(value), Def.Min, Def.Max);
                            }
                            break;
                        case ParamTypeDef.u16:
                            _value = (ushort)CheckBounds(Convert.ToDouble(value), Def.Min, Def.Max); break;
                        case ParamTypeDef.u32:
                            _value = (uint)CheckBounds(Convert.ToDouble(value), Def.Min, Def.Max); break;
                        case ParamTypeDef.ATK_PARAM_BOOL:
                        case ParamTypeDef.EQUIP_BOOL:
                        case ParamTypeDef.ITEMLOT_CUMULATE_RESET:
                        case ParamTypeDef.ITEMLOT_ENABLE_LUCK:
                        case ParamTypeDef.MAGIC_BOOL:
                        case ParamTypeDef.NPC_BOOL:
                        case ParamTypeDef.ON_OFF:
                        case ParamTypeDef.SP_EFFECT_BOOL:
                            if (value?.GetType() != typeof(bool))
                                Console.WriteLine("Breakpoint hit");
                            _value = (bool)value;
                            break;
                    }
                }

                NotifyPropertyChanged(nameof(Value));
            }
        }

        private double CheckBounds(double input, double min, double max)
        {
            ValueOutOfBounds = (input < min || input > max);
            return input;
        }

        public ParamCellValueRef(ParamDefEntry Def)
        {
            this.Def = Def;
        }

        public override string ToString()
        {
            return Def.Name + " = " + Value.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
