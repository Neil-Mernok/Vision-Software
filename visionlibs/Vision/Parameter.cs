using System;
using System.Text;

namespace Vision.Parameter
{
    public class Parameter
    {
        public bool param_valid = false, param_changed = false;
        public static int string_max = 20;

        ///< \enum Type
        public enum Type
        {
            TypeNull = 0, /*!< Null type */
            TypeBoolean = 3, /*!< Boolean */
            TypeInt = 4, /*!< Integer */
            TypeShort = 2, /*!< Short */
            TypeByte = 1, /*!< Byte */
            TypeString = 5, /*!< String */
        };

        public int address;
        //	int def_val = 0, min = 0, max;
        public Type type;
        public string name;

        public Parameter(int adr, Type p, UInt32 value, string Name)
        {
            address = adr;
            name = Name;
            type = p;
            _value = value;
        }

        public Parameter(int adr, string val, string Name)
        {
            address = adr;
            name = Name;
            type = Type.TypeString;
            _value_str = val;
        }

        UInt32 _value;
        public UInt32 Value
        {
            get
            {
                if (type == Type.TypeByte)
                    return _value & 0xff;
                else if (type == Type.TypeShort)
                    return _value & 0xffff;
                else
                    return _value;
            }
            set
            {
                param_valid = true;            // tells us the param has been read/modified. (contains a valid value) 
                param_changed = true;
                if (type == Type.TypeByte)
                    _value = value & 0xff;
                else if (type == Type.TypeShort)
                    _value = value & 0xffff;
                else
                    _value = value;
            }
        }

        string _value_str;
        public string Value_str
        {
            get { return _value_str; }
            set
            {
                param_valid = true;            // tells us the param has been read/modified. (contains a valid value) 
                param_changed = true;
                _value_str = value;
            }
        }

        int len()
        {
            if (type == Type.TypeString)
                return _value_str.Length;
            else
                return 4;
        }

        public Byte[] GetSaveMessage()
        {
            Byte[] data = new Byte[Math.Max(7, len() + 3)];

            data[0] = (Byte)'c';
            BitConverter.GetBytes((UInt16)address).CopyTo(data, 1);     // index of the parameter


            if (type == Type.TypeString)
                Encoding.ASCII.GetBytes(_value_str).CopyTo(data, 3);
            else
                BitConverter.GetBytes(_value).CopyTo(data, 3);
            return data;
        }

        /// <summary>
        /// Added in V10. Allows setting to be saved, but response is the full setting value. 
        /// </summary>
        /// <returns></returns>
        public Byte[] GetSetGetMessage()
        {
            Byte[] data = GetSaveMessage();
            data[0] = (Byte)'C';            // different command char, but the rest is same.
            return data;
        }

        public Byte[] GetReadMessage()
        {
            Byte[] data = new Byte[3];

            data[0] = (Byte)'s';
            BitConverter.GetBytes((UInt16)address).CopyTo(data, 1);     // index of the parameter

            return data;
        }

        /**
         * only used for status type params...
         */
        public Byte[] GetStatusMessage()
        {
            Byte[] data = new Byte[3];

            data[0] = (Byte)'F';
            BitConverter.GetBytes((UInt16)address).CopyTo(data, 1);     // index of the parameter

            return data;
        }

        public Byte[] GetData()
        {
            Byte[] data = new Byte[len()];

            if (type == Type.TypeString)
                return Encoding.ASCII.GetBytes(_value_str + "\0");
            else
                return BitConverter.GetBytes(_value);
        }
    }
}
