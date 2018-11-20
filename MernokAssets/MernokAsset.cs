using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MernokAssets
{
    public class MernokAsset
    {
        public byte Type;
        public byte Group;
        public bool IsLicensable;
        public string TypeName;
        public string GroupName;
    }

    public class MernokAssetFile
    {
        public List<MernokAsset> mernokAssetList;
        public UInt16 version;
        public DateTime dateCreated;
        public string createdBy;            //Name of file creator
    }    
}
