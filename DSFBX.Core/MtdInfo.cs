using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX
{
    public class MtdInfo
    {
        public List<MtdChannelInfo> Channels { get; set; } = new List<MtdChannelInfo>();
        public MtdInfo(string mtdName)
        {
            string insideBrackets = Util.GetBracketContents(mtdName);

            if (insideBrackets.Contains("]["))
            {
                var nameSplit = insideBrackets.Split(']', '['); //<stuff>]<empty string>[<stuff>

                if (!string.IsNullOrWhiteSpace(nameSplit[1]))
                {
                    throw new Exception("Meowmaritus.ShittyProgrammerException: " +
                        "string.Split(cher[]) works differently than how Meowmaritus expected.");
                }

                var matLetterGroupA = nameSplit[0];
                var matLetterGroupB = nameSplit[2];

                foreach (var letter in matLetterGroupA)
                {
                    switch (letter)
                    {
                        case 'D':
                            Channels.Add(new MtdChannelInfo("D", "g_Diffuse"));
                            break;
                        case 'S':
                            Channels.Add(new MtdChannelInfo("S", "g_Specular"));
                            break;
                        case 'B':
                            Channels.Add(new MtdChannelInfo("B", "g_Bumpmap"));
                            break;
                        //g_DetailBumpmap ...?

                    }
                }
            }
            else
            {

            }
        }
    }
}
