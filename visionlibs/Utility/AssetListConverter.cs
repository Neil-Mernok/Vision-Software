using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision_Libs.Vision;

namespace Vision_Libs.Utility
{
    class AssetListConverter : StringConverter
    {
        public override Boolean GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override Boolean GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<String> list = new List<String>();
            list = TagTypesL.MenokAssetTypeName;
            return new StandardValuesCollection(list);
        }
    }
}
