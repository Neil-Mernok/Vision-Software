using MernokAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vision_Libs.Vision;

namespace Vision_config
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        public static MernokAssetFile mernokAssetFile = new MernokAssetFile();
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mernokAssetFile = MernokAssetManager.ReadMernokAssetFile(@"C:\MernokAssets\MernokAssetList.xml");
            //mernokAssetFile.mernokAssetList.Insert(80, mernokAssetFile.mernokAssetList[(int)TagType.Person - 1]);

            foreach (MernokAsset item in mernokAssetFile.mernokAssetList)
            {
                TagTypesL.MernokAssetType.Add(item);
                TagTypesL.MenokAssetTypeName.Add(item.TypeName);
            }

            Application.Run(new Vision_Tag_Config_Utility());
        }
    }
}
