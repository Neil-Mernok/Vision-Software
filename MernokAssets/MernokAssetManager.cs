using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


////Example to use assets
////Create an asset file
//MernokAssetFile mernokAssetFile = new MernokAssetFile();
//mernokAssetFile.createdBy = "Schalk JVR";
//            mernokAssetFile.dateCreated = DateTime.Now;
//            mernokAssetFile.version = 1;

//            //Create a list of assets
//            List<MernokAsset> mernokAssets = new List<MernokAsset>();
//            MernokAsset a = new MernokAsset();
//            a.Type = 1;
//            a.Group = 101;
//            a.Description = "Asset a";
//            a.IsLicensable = true;

//            MernokAsset b = new MernokAsset();
//            b.Type = 2;
//            b.Group = 102;
//            b.Description = "Asset b";
//            b.IsLicensable = false;

//            MernokAsset c = new MernokAsset();
//            c.Type = 3;
//            c.Group = 103;
//            c.Description = "Asset c";
//            c.IsLicensable = true;

//            mernokAssets.Add(a);
//            mernokAssets.Add(b);
//            mernokAssets.Add(c);

//            mernokAssetFile.mernokAssetList = mernokAssets;

//            MernokAssetManager.CreateMernokAssetFile(mernokAssetFile);

namespace MernokAssets
{
    public static class MernokAssetManager
    {
        //Change this to accept a path and name for the file
        public static string CreateMernokAssetFile(MernokAssetFile f)
        {
            string result = "File created succesfully";            
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MernokAssetFile));
                using (TextWriter writer = new StreamWriter(@"C:\Commander\Infrastructure\MernokAssetMasterList.xml"))
                {
                    serializer.Serialize(writer, f);
                }
            }
            catch (Exception e)
            {
                result = e.ToString();
            }

            return result;
        }

        public static string MernokAssetContent { get; set; }
        //todo: Change this to accept a path for the file
        public static MernokAssetFile ReadMernokAssetFile(string filename)
        {
            //todo: add exception handling
            //Try Read the XML file
            XmlSerializer deserializer = new XmlSerializer(typeof(MernokAssetFile));
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            TextReader reader = new StreamReader(filename);//(Environment.CurrentDirectory + @"\C2xxParameters.xml");
            MernokAssetContent = reader.ReadToEnd();
            reader = new StringReader((string)MernokAssetContent.Clone());
            object obj = deserializer.Deserialize(reader);
            MernokAssetFile f = (MernokAssetFile)obj;
            reader.Close();
            return f;
        }


    }
}
