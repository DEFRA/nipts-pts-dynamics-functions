using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.Helper
{
    public static class JsonHelper
    {
        public static bool IsValidJson(string jsonString)
        {
            try
            {
                JToken.Parse(jsonString);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }
    }    
}
